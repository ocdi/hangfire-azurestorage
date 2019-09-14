using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.AzureStorage.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Hangfire.AzureStorage
{
    internal class AzureWriteOnlyTransaction : IWriteOnlyTransaction
    {
        private readonly AzureJobStorageConnection _storage;
        private readonly Queue<Action> _actions = new Queue<Action>();

        // we will batch this in commit
        private readonly Dictionary<string, List<(string key, double score)>> _sets = new Dictionary<string, List<(string, double)>>();

        public AzureWriteOnlyTransaction(AzureJobStorageConnection azureJobStorageConnection) 
            => _storage = azureJobStorageConnection;

        public void AddJobState([NotNull] string jobId, [NotNull] IState state)
        {
            AddJobStateWithIdentifier(_storage.StateItemReference(jobId, DateTime.UtcNow, state.Name),
                state);
        }

        private void AddJobStateWithIdentifier(string blobName, IState state)
        {
            var model = new HangfireJobStateModel
            {
                Name = state.Name,
                Reason = state.Reason,
                Data = state.SerializeData(),
                CreatedAt = DateTime.UtcNow,
            };
            var json = JsonConvert.SerializeObject(model);

            _actions.Enqueue(() =>
            {
                // this is just adding to a history table which we store in blob storage
                // we store this in blob storage in a subdirectory of the parent job
                var referernce = _storage.Storage.JobsContainer.GetBlockBlobReference(blobName);

                // upload the data
                referernce.UploadText(json);
            });
        }

        public void SetJobState([NotNull] string jobId, [NotNull] IState state)
        {
            var identifier = _storage.StateItemReference(jobId, DateTime.UtcNow, state.Name);
            // persist the state information
            AddJobStateWithIdentifier(identifier, state);

            // update the job state to match the new state
            _actions.Enqueue(() =>
            {
                _storage.Storage.Jobs.Execute(TableOperation.InsertOrMerge(new JobEntity {
                    PartitionKey = _storage.PartitionKeyForJob(jobId),
                    RowKey = jobId,
                    State = state.Name,
                    StateFile = identifier
                }));
            });
        }


        public void AddToQueue([NotNull] string queue, [NotNull] string jobId)
        {
            _actions.Enqueue(() => _storage.Storage.Queue(queue).AddMessage(new CloudQueueMessage(jobId)));
        }

        public void AddToSet([NotNull] string key, [NotNull] string value) => AddToSet(key, value, 0.0);

        public void AddToSet([NotNull] string key, [NotNull] string value, double score)
        {
            
            if (!_sets.TryGetValue(key, out var list))
            {
                list = new List<(string key, double score)>();
                _sets.Add(key, list);
            }
            list.Add((value, score));
        }

        public void Commit()
        {
            foreach (var a in _actions) a();

            // _storage.Storage.Sets.ExecuteBatch(new TableBatchOperation { })
            foreach (var set in _sets)
            {
                PerformBatchedOperation(_storage.Storage.Sets, set.Value.Select(a
                        => TableOperation.InsertOrMerge(new SetEntity
                        {
                            PartitionKey = set.Key,
                            RowKey = a.key,
                            Score = a.score
                        })));
                
            }
        }

        public void DecrementCounter([NotNull] string key) => StoreCounter(key, -1);

        public void DecrementCounter([NotNull] string key, TimeSpan expireIn) => StoreCounter(key, -1, DateTime.UtcNow + expireIn);
       


        public void IncrementCounter([NotNull] string key) => StoreCounter(key, 1);

        public void IncrementCounter([NotNull] string key, TimeSpan expireIn) => StoreCounter(key, 1, DateTime.UtcNow + expireIn);

        private void StoreCounter(string key, int incr, DateTime? expireAt = null) => _actions.Enqueue(()
                => _storage.Storage.Counters.Execute(TableOperation.Insert(new CounterEntity {
                    PartitionKey = key,
                    RowKey = Guid.NewGuid().ToString(),
                    Value = incr,
                    ExpireAt = expireAt
                })));

        public void InsertToList([NotNull] string key, [NotNull] string value)
        {
            throw new NotImplementedException();
        }

        public void ExpireJob([NotNull] string jobId, TimeSpan expireIn)
        {
            // this sets an expiry date for a job in the timespan specified
            SetJobExpiry(jobId, DateTime.UtcNow.Add(expireIn));
        }


        public void PersistJob([NotNull] string jobId)
        {
            // this removes expiry for a job
            SetJobExpiry(jobId, DateTime.MaxValue);
        }

        private void SetJobExpiry(string jobId, DateTime expiry) => _actions.Enqueue(() =>
        {
            _storage.Storage.Jobs.Execute(TableOperation.InsertOrMerge(new JobEntity
            {
                PartitionKey = _storage.PartitionKeyForJob(jobId),
                RowKey = jobId,
                ExpireAt = expiry
            }));
        });

        public void RemoveFromList([NotNull] string key, [NotNull] string value)
        {
            _actions.Enqueue(() => _storage.Storage.Lists.Execute(TableOperation.Delete(new SetEntity { PartitionKey = key, RowKey = value })));
        }

        public void RemoveFromSet([NotNull] string key, [NotNull] string value)
        {
            _actions.Enqueue(() => _storage.Storage.Sets.Execute(TableOperation.Delete(new SetEntity { PartitionKey = key, RowKey = value })));
        }

        public void RemoveHash([NotNull] string key)
        {
            // remove all items in a hash where the key is the partition name
            _actions.Enqueue(() => {
                // first enumerate all the items
                var items = _storage.GetAllEntriesFromHash(key);
                PerformBatchedOperation(_storage.Storage.Hashs,
                    items.Keys.Select(r => TableOperation.Delete(new HashEntity { PartitionKey = key, RowKey = r })));
            });
        }

        void PerformBatchedOperation(CloudTable table, IEnumerable<TableOperation> operations) {
            var i = 0;
            var batch = new TableBatchOperation();

            foreach (var op in operations) {
                i++;
                Console.WriteLine($"op {i}");
                batch.Add(op);

                // limit the batch size
                if (i == 100) {   
                    table.ExecuteBatch(batch);
                    batch = new TableBatchOperation();
                    i = 0;
                }
            }

            // ensure there are no left over
            if (batch.Count > 0) table.ExecuteBatch(batch);
        }

        public void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            _actions.Enqueue(() => PerformBatchedOperation(_storage.Storage.Hashs, keyValuePairs.Select(
                k=> TableOperation.InsertOrMerge(new HashEntity { PartitionKey = key, RowKey = k.Key, Value = k.Value })
            )));
        }

        public void TrimList([NotNull] string key, int keepStartingFrom, int keepEndingAt)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AzureWriteOnlyTransaction()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}