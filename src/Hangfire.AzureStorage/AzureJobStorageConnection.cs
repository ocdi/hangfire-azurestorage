using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.AzureStorage
{
    public class AzureJobStorageConnection : IStorageConnection
    {
        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void AnnounceServer(string serverId, ServerContext context)
        {
            throw new NotImplementedException();
        }

        public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            throw new NotImplementedException();
        }

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetAllEntriesFromHash([NotNull] string key)
        {
            throw new NotImplementedException();
        }

        public HashSet<string> GetAllItemsFromSet([NotNull] string key)
        {
            throw new NotImplementedException();
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