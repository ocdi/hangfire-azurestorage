using System;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage
{
    internal class AzureWriteOnlyTransaction : IWriteOnlyTransaction
    {
        private AzureJobStorageConnection _storage;

        private SortedDictionary<string, (string, double)> _sets = new SortedDictionary<string, (string, double)>();

        public AzureWriteOnlyTransaction(AzureJobStorageConnection azureJobStorageConnection)
        {
            _storage = azureJobStorageConnection;
        }

        public void AddJobState([NotNull] string jobId, [NotNull] IState state)
        {
            throw new NotImplementedException();
        }

        public void AddToQueue([NotNull] string queue, [NotNull] string jobId)
        {
            throw new NotImplementedException();
        }

        public void AddToSet([NotNull] string key, [NotNull] string value) => AddToSet(key, value, 0.0);

        public void AddToSet([NotNull] string key, [NotNull] string value, double score)
        {
            _sets[key] = (value, score);
        }

        public void Commit()
        {
            // _storage.Storage.Sets.ExecuteBatch(new TableBatchOperation { })
            
            foreach (var set in _sets) {

            }
        }

        public void DecrementCounter([NotNull] string key)
        {
            throw new NotImplementedException();
        }

        public void DecrementCounter([NotNull] string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public void ExpireJob([NotNull] string jobId, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public void IncrementCounter([NotNull] string key)
        {
            throw new NotImplementedException();
        }

        public void IncrementCounter([NotNull] string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public void InsertToList([NotNull] string key, [NotNull] string value)
        {
            throw new NotImplementedException();
        }

        public void PersistJob([NotNull] string jobId)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromList([NotNull] string key, [NotNull] string value)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromSet([NotNull] string key, [NotNull] string value)
        {
            throw new NotImplementedException();
        }

        public void RemoveHash([NotNull] string key)
        {
            throw new NotImplementedException();
        }

        public void SetJobState([NotNull] string jobId, [NotNull] IState state)
        {
            throw new NotImplementedException();
        }

        public void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
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