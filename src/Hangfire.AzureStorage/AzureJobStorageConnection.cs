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

      

        public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            // this is a big thing, it handles counters and job updates
            throw new NotImplementedException();
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            // defer to azure queues
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key)
        {
            throw new NotImplementedException();
        }

        public HashSet<string> GetAllItemsFromSet([NotNull] string key)
        {
            // get all the keys for a partition
            _storage.Sets.ExecuteQuery(TableQuery)
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