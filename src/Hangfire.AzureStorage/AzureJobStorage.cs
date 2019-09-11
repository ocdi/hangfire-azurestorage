using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.AzureStorage
{
    public class AzureJobStorage : JobStorage
    {
        private AzureStorageOptions _options;

        public AzureJobStorage(AzureStorageOptions options)
        {
            _options = options;
        }

        public override IStorageConnection GetConnection()
        {
            return new AzureJobStorageConnection();
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new AzureMonitoringApi();
        }
    }



}