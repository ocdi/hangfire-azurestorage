using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.AzureStorage.Entities;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Hangfire.AzureStorage
{
    public class AzureJobStorageConnection : IStorageConnection
    {
        private bool _disposedValue = false; // To detect redundant calls
        private readonly IAzureJobStorageInternal _storage;


        public AzureJobStorageConnection(IAzureJobStorageInternal storage)
        {
            _storage = storage;
        }
        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            // this is based on https://medium.com/@fbeltrao/distributed-locking-in-azure-functions-bc4517c0306c
            throw new NotImplementedException();
        }

        public IAzureJobStorageInternal Storage => _storage;


        /// <summary>
        /// Creates or updates the existance of a server in table storage
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="context"></param>
        public void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var data = new ServerEntity
            {
                PartitionKey = "All",
                RowKey = serverId,
                WorkerCount = context.WorkerCount,
                Queues = JsonConvert.SerializeObject(context.Queues),
                StartedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow
            };

            _storage.Servers.Execute(TableOperation.InsertOrMerge(data));
        }

        class HangfireJobModel
        {
            public string InvocationData { get; set; }
            public string Arguments { get; set; }
            public IDictionary<string,string> Parameters { get; set; }
            public DateTime CreatedAt { get; internal set; }
        }

        string JobReference(string jobId) => $"/{jobId}/job.json";

        public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            var invocationData = InvocationData.SerializeJob(job);
            var payload = invocationData.SerializePayload(excludeArguments: true);

            var jobId = Guid.NewGuid().ToString();

            var model = new HangfireJobModel
            {
                InvocationData = payload,
                Arguments = invocationData.Arguments,
                Parameters = parameters,
                CreatedAt = createdAt
            };

            var blob = JsonConvert.SerializeObject(model);

            var reference = _storage.JobsContainer.GetBlockBlobReference(JobReference(jobId));
            reference.UploadText(blob);

            _storage.Jobs.Execute(TableOperation.InsertOrMerge(new ))

            return jobId;
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            // this is a big thing, it handles counters and job updates
            return new AzureWriteOnlyTransaction(this);
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            // defer to azure queues
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key)
        {
            // key field value
            var query = new TableQuery<HashEntity>().Where(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, key
            ));

            var result = new Dictionary<string,string>();

            foreach (var (RowKey, Value) in Query(_storage.Hashs, query, a => (a.RowKey, a.Value)))
                result.Add(RowKey, Value);

            return result;
        }

        public HashSet<string> GetAllItemsFromSet([NotNull] string key)
        {
            var query = new TableQuery<SetEntity>().Where(TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, key));
            return new HashSet<string>(Query(_storage.Sets, query, a => a.RowKey));

        }

        private IEnumerable<T> Query<TEntity, T>(CloudTable table, TableQuery<TEntity> query, Func<TEntity, T> transform)
            where TEntity : ITableEntity, new()
        {
            TableContinuationToken token = null;

            do
            {
                var segment = table.ExecuteQuerySegmented(query, token);
                foreach (var result in segment.Results)
                    yield return transform(result);

                token = segment.ContinuationToken;
            } while (token != null);
        }
        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            throw new NotImplementedException();
        }

        public JobData GetJobData([NotNull] string jobId)
        {
            throw new NotImplementedException();
        }

        public string GetJobParameter(string id, string name)
        {
            throw new NotImplementedException();
        }

        public StateData GetStateData([NotNull] string jobId)
        {
            throw new NotImplementedException();
        }

        public void Heartbeat(string serverId)
        {
            throw new NotImplementedException();
        }

        public void RemoveServer(string serverId)
        {
            throw new NotImplementedException();
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            throw new NotImplementedException();
        }

        public void SetJobParameter(string id, string name, string value)
        {
            throw new NotImplementedException();
        }

        public void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AzureJobStorageConnection()
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