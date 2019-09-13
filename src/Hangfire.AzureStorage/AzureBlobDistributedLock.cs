using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Hangfire.AzureStorage
{
    public class AzureBlobDistributedLock 
    {
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _ref;

        public AzureBlobDistributedLock(CloudBlobClient client)
        {
            _client = client;
            _ref = _client.GetContainerReference("locks");
        }


          /// <summary>
        /// Tries to adquire a lease
        /// </summary>
        /// <param name="id"></param>
        /// <param name="throttleTime"></param>
        /// <param name="leaseId"></param>
        /// <returns></returns>
        public async Task<bool> TryAdquireLeaseAsync(string id, TimeSpan throttleTime, string leaseId)
        {
            // TODO: make this smarter, call only if the container does not exists
            await _ref.CreateIfNotExistsAsync();


            // create blob if not exists
            var blob = _ref.GetAppendBlobReference(id);
            try
            {
                await blob.CreateOrReplaceAsync(
                    AccessCondition.GenerateIfNotExistsCondition(),
                    null,
                    null);
            }
            catch (StorageException ex) when (ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict || ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                // ignore exception caused by conflict as it means the file already exists
            }


            try
            {
                // try to acquire lease
                await blob.AcquireLeaseAsync(
                    throttleTime,
                    leaseId,
                    new AccessCondition { LeaseId = null },
                    null,
                    null
                    );
                // we leased the file!
                return true;
            }
            catch (StorageException ex) when (ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict || ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                // if a conflict is returned it means that the blob has already been leased
                // in that case the operation failed (someone else has the lease)
                return false;
            }
        }

    }
}