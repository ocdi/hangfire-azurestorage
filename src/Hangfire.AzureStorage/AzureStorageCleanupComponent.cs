using System;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.AzureStorage
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class AzureStorageCleanupComponent : IServerComponent
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private AzureJobStorage _storage;
        private readonly TimeSpan _checkInterval;

        public AzureStorageCleanupComponent(AzureJobStorage storage, TimeSpan checkInterval)
        {
            this._storage = storage;
            _checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {


            cancellationToken.Wait(_checkInterval);
        }
    }
}