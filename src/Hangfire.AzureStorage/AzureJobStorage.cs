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
using System.Collections.Concurrent;

namespace Hangfire.AzureStorage
{
    public interface IAzureJobStorageInternal
    {
        CloudTable Servers { get; }
        CloudTable Sets { get; }
        CloudTable Lists { get; }
        CloudTable Hashs { get; }
        AzureStorageOptions Options { get; }


        /// <summary>
        /// Stores the job metadata
        /// </summary>
        CloudTable Jobs { get; }
        /// <summary>
        /// Stores the larger job information in blob storage
        /// </summary>
        CloudBlobContainer JobsContainer { get; }

        CloudQueue Queue(string queue);
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
        const string JOBS_TABLE = "Jobs";

        const string JOB_CONTAINER = "jobs";

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
            GetTable(JOBS_TABLE).CreateIfNotExists();
            GetTable(HASHS_TABLE).CreateIfNotExists();

            GetContainer(JOB_CONTAINER).CreateIfNotExists();
        }


        CloudTable IAzureJobStorageInternal.Servers => GetTable(SERVER_TABLE);
        CloudTable IAzureJobStorageInternal.Sets => GetTable(SETS_TABLE);
        CloudTable IAzureJobStorageInternal.Lists => GetTable(LISTS_TABLE);
        CloudTable IAzureJobStorageInternal.Hashs => GetTable(HASHS_TABLE);
        CloudTable IAzureJobStorageInternal.Jobs => GetTable(JOBS_TABLE);

        CloudBlobContainer IAzureJobStorageInternal.JobsContainer => GetContainer(JOB_CONTAINER);

        
        
        CloudQueue IAzureJobStorageInternal.Queue(string queue)
        {
            var reference = _queueClient.GetQueueReference($"{_options.Prefix?.ToLowerInvariant()}{queue}");
            reference.CreateIfNotExists();
            return reference;
        }

        public override IStorageConnection GetConnection() => new AzureJobStorageConnection(this);

        public override IMonitoringApi GetMonitoringApi() => new AzureMonitoringApi();


        private CloudTable GetTable(string name)
            => _tableClient.GetTableReference($"{_options.Prefix}{name}");

        private CloudBlobContainer GetContainer(string name) 
            => _blobClient.GetContainerReference($"{_options.Prefix?.ToLowerInvariant()}{name}");
    }



}