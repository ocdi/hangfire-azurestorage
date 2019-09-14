using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage
{
    public interface IAzureJobStorageInternal
    {
        CloudTable Servers { get; }
        CloudTable Sets { get; }
        CloudTable Lists { get; }
        CloudTable Hashs { get; }
        AzureStorageOptions Options { get; }
    }

    public class AzureJobStorage : JobStorage, IAzureJobStorageInternal
    {
        private AzureStorageOptions _options;
        private readonly CloudStorageAccount _cosmosAccount;
        private readonly Microsoft.Azure.Storage.CloudStorageAccount _account;
        private readonly CloudQueueClient _queueClient;
        private readonly CloudBlobClient _blobClient;
        private readonly CloudTableClient _tableClient;

        AzureStorageOptions IAzureJobStorageInternal.Options => _options;

        const string SERVER_TABLE = "Servers";
        const string SETS_TABLE = "Sets";
        const string LISTS_TABLE = "Lists";
        const string HASHS_TABLE = "Hashs";

        public AzureJobStorage(AzureStorageOptions options)
        {
            _options = options;

            _cosmosAccount = CloudStorageAccount.Parse(options.ConnectionString);
            _account = Microsoft.Azure.Storage.CloudStorageAccount.Parse(options.ConnectionString);

            _tableClient = _cosmosAccount.CreateCloudTableClient();
            _blobClient = _account.CreateCloudBlobClient();
            _queueClient = _account.CreateCloudQueueClient();

            // ensure the required tables / containers exists
            GetTable(SERVER_TABLE).CreateIfNotExists();
            GetTable(SETS_TABLE).CreateIfNotExists();
            GetTable(LISTS_TABLE).CreateIfNotExists();
            GetTable(HASHS_TABLE).CreateIfNotExists();
        }


        CloudTable IAzureJobStorageInternal.Servers => GetTable(SERVER_TABLE);
        CloudTable IAzureJobStorageInternal.Sets => GetTable(SETS_TABLE);
        CloudTable IAzureJobStorageInternal.Lists => GetTable(LISTS_TABLE);
        CloudTable IAzureJobStorageInternal.Hashs => GetTable(HASHS_TABLE);


        public override IStorageConnection GetConnection()
        {
            return new AzureJobStorageConnection(this);
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new AzureMonitoringApi();
        }


        private CloudTable GetTable(string name)
        {
            var table = _tableClient.GetTableReference($"{_options.Prefix}{name}");
            return table;
        }
    }



}