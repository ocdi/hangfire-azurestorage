using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.AzureStorage.Entities;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Hangfire.AzureStorage
{

    
    public class AzureJobStorageConnection : IStorageConnection
    {
        private bool _disposedValue = false; // To detect redundant calls
        private Timer _timer;

        public AzureJobStorageConnection(IAzureJobStorageInternal storage) => Storage = storage;
        public IAzureJobStorageInternal Storage { get; }


        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            // todo handle errors
            
            var blobName = $"locks/{resource.Replace(":", "-")}.lock";

            // we'll use leases on the job storage to achieve this
            var lockRef = Storage.JobsContainer.GetBlockBlobReference(blobName);
            try
            {
                // ensure the lock file exists
                if (!lockRef.Exists()) lockRef.UploadText(string.Empty);
            }
            catch (Microsoft.Azure.Storage.StorageException ex) when (ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict || ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                // ignore exception caused by conflict as it means the file already exists
            }

            try
            {
                var t = timeout - OneMinute;
                  
                var leaseId = lockRef.AcquireLease(t < TimeSpan.Zero ? timeout : OneMinute, Guid.NewGuid().ToString());

                // we can only hold a lease for a maximum 60 seconds
                return new LeaseLock(leaseId, lockRef, t);
            }
            catch (Microsoft.Azure.Storage.StorageException ex) when(ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict || ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                // we failed to acquire a lock
                throw new DistributedLockTimeoutException(resource);
            }
        }

        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan HalfMinute = TimeSpan.FromSeconds(30);



        class LeaseLock : IDisposable
        {
            private bool _disposed;
            private readonly AccessCondition _lease;
            private readonly CloudBlockBlob _reference;
            private TimeSpan _remaining;
            private readonly Timer _timer;

            public LeaseLock(string leaseId, CloudBlockBlob reference, TimeSpan t)
            {
                _lease = new AccessCondition { LeaseId = leaseId };
                _reference = reference;
                _remaining = t;
                if (t > TimeSpan.Zero)
                {
                    // we will renew every half minute as the lease expires after 60 seconds
                    _timer = new Timer(RenewLease, null, HalfMinute.Milliseconds, HalfMinute.Milliseconds);
                }
            }

            private void RenewLease(object state)
            {
                _remaining -= HalfMinute;

                if (_remaining > TimeSpan.Zero)
                {
                    _reference.RenewLease(_lease);
                }
                else
                {
                    // this will stop the timer and 
                    Dispose();
                }

            }

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;

                _timer?.Dispose();

                _reference.ReleaseLease(_lease);
            }

            // fall back in case we are forgotton to be disposed
            ~LeaseLock()
            {
                Dispose();
            }
        }

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

            Storage.Servers.Execute(TableOperation.InsertOrMerge(data));
        }

       

        public string JobReference(string jobId) => $"{jobId}/job.json";
        public string StateFolderReference(string jobId) => $"{jobId}/state";
        public string StateItemReference(string jobId, DateTime date, string stateName) => $"{jobId}/state/{date:o}{stateName}.json";

        private CloudBlockBlob GetJobRef(string jobId)
            => Storage.JobsContainer.GetBlockBlobReference(JobReference(jobId));

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

            var blobJson = JsonConvert.SerializeObject(model);

            GetJobRef(jobId).UploadText(blobJson);

            Storage.Jobs.Execute(TableOperation.InsertOrMerge(new JobEntity
            {
                PartitionKey = PartitionKeyForJob(jobId),
                RowKey = jobId,
                State = "INITIAL",
                CreatedAt = createdAt,
                ExpireAt = DateTime.UtcNow.Add(expireIn)
            }));

            return jobId;
        }

        public string PartitionKeyForJob(string jobId) => jobId.Substring(0, 5);


        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            // this is a big thing, it handles counters and job updates
            return new AzureWriteOnlyTransaction(this);
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            // the worker will request jobs for all of these queues
            // we should return the first available, rotating through the queues
            var references = queues.Select(Storage.Queue).ToArray();

            CloudQueueMessage message = null;

            var currentQueueIndex = 0;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                message = references[currentQueueIndex].GetMessage(Storage.Options.VisibilityTimeout);

                if (message == null)
                {
                    if (currentQueueIndex == references.Length - 1)
                    {
                        cancellationToken.WaitHandle.WaitOne(Storage.Options.QueuePollInterval * 1000);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    currentQueueIndex = (currentQueueIndex + 1) % queues.Length;
                }

            } while (message == null);


            return new AzureQueueFetchedJob(references[currentQueueIndex], message);
        }

        public Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key)
        {
            // key field value
            var query = new TableQuery<HashEntity>().Where(
                TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, key
            ));

            var result = new Dictionary<string,string>();

            foreach (var (RowKey, Value) in Query(Storage.Hashs, query, a => (a.RowKey, a.Value)))
                result.Add(RowKey, Value);

            return result;
        }

        public HashSet<string> GetAllItemsFromSet([NotNull] string key)
        {
            var query = new TableQuery<SetEntity>().Where(TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, key));
            return new HashSet<string>(Query(Storage.Sets, query, a => a.RowKey));

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
            var query = new TableQuery<SetEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, key))
                .OrderBy(nameof(SetEntity.Score));

            // we can't apply a query to the data, but we can post query filter it (the assumption is the set is a reasonable size)
            return Query(Storage.Sets, query, a => a)
                .Where(a=>a.Score >= fromScore && a.Score <= toScore)
                .FirstOrDefault()
                ?.RowKey;
        }

        public JobData GetJobData([NotNull] string jobId)
        {
            var model = GetJobBlobModel(jobId);

            var result = Storage.Jobs.Execute(TableOperation.Retrieve<JobEntity>(PartitionKeyForJob(jobId), jobId));

            var invocationData = InvocationData.DeserializePayload(model.InvocationData);

            if (!string.IsNullOrEmpty(model.Arguments))
            {
                invocationData.Arguments = model.Arguments;
            }

            Job job = null;
            JobLoadException loadException = null;

            try
            {
                job = invocationData.DeserializeJob();
            }
            catch (JobLoadException ex)
            {
                loadException = ex;
            }

            return new JobData
            {
                Job = job,
                State = (result.Result as JobEntity).State,
                CreatedAt = model.CreatedAt,
                LoadException = loadException
            };
        }

        private HangfireJobModel GetJobBlobModel(string jobId, AccessCondition condition = null)
        {
            // load data from storage
            var jobJson = GetJobRef(jobId).DownloadText(accessCondition: condition);

            var model = JsonConvert.DeserializeObject<HangfireJobModel>(jobJson);
            return model;
        }

        public string GetJobParameter(string id, string name)
        {
            var model = GetJobBlobModel(id);
            if (model.Parameters != null && model.Parameters.TryGetValue(name, out var val)) return val;
            return null;
        }

        
        public void SetJobParameter(string id, string name, string value)
        {
            var jobRef = GetJobRef(id);

            // acquire lease to prevent multiple threads updating this
            var lease = jobRef.AcquireLease(TimeSpan.FromMinutes(1), Guid.NewGuid().ToString());
            var accessCondition = new AccessCondition { LeaseId = lease };

            var model = GetJobBlobModel(id, accessCondition);
            if (model.Parameters == null) model.Parameters = new Dictionary<string, string>();
            model.Parameters[name] = value;

            var json = JsonConvert.SerializeObject(model);
            jobRef.UploadText(json, accessCondition: accessCondition);
            jobRef.ReleaseLease(accessCondition);
        }

        public StateData GetStateData([NotNull] string jobId)
        {
            var (entity, model) = GetStateDataRaw(jobId);

            var data = new StateData
            {
                Name = entity?.State,
                Reason = model?.Reason,
                Data = model?.Data
            };

            return data;
        }

        internal (JobEntity entity, HangfireJobStateModel model) GetStateDataRaw(string jobId)
        {
            // this is the most recent state entry
            var result = LoadTableJob(jobId);

            (JobEntity entity, HangfireJobStateModel model) jobstate = (result.Result as JobEntity, null);

            if (jobstate.entity.StateFile != null)
            {
                var blobRef = Storage.JobsContainer.GetBlockBlobReference(jobstate.entity.StateFile);
                var json = blobRef.DownloadText();
                jobstate.model = JsonConvert.DeserializeObject<HangfireJobStateModel>(json);
            }

            return jobstate;
        }

        private TableResult LoadTableJob(string jobId) => Storage.Jobs.Execute(TableOperation.Retrieve<JobEntity>(PartitionKeyForJob(jobId), jobId));

        public void Heartbeat(string serverId)
        {
            
        }

        public void RemoveServer(string serverId)
        {
            //throw new NotImplementedException();
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            var servers = Storage.Servers.CreateQuery<ServerEntity>().Where(a=>a.LastHeartbeat < DateTime.UtcNow - timeOut).ToArray();

            AzureWriteOnlyTransaction.PerformBatchedOperation(Storage.Servers,
                servers.Select(s => TableOperation.Delete(s)));
            
            return servers.Length;
            
        }


        public void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            
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



    public class AzureQueueFetchedJob : IFetchedJob
    {
        private readonly CloudQueue _queue;
        private readonly CloudQueueMessage _message;

        public AzureQueueFetchedJob(CloudQueue queue, CloudQueueMessage message)
        {
            _queue = queue;
            _message = message;
        }
        public string JobId => _message.AsString;

        void IDisposable.Dispose()
        {
        }

        public void RemoveFromQueue()
        {
            _queue.DeleteMessage(_message);
        }

        public void Requeue()
        {
            _queue.AddMessage(_message);
        }
    }
}

