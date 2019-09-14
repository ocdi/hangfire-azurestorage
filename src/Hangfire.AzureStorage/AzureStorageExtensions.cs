using System;
using Hangfire;

namespace Hangfire.AzureStorage
{
    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; }
        /// <summary>
        /// This sets a prefix for the various storage account queues and tables
        /// </summary>
        /// <value></value>
        public string Prefix { get; set; }

        /// <summary>
        /// How often to sleep between polling when there are no jobs, in seconds
        /// </summary>
        public int QueuePollInterval { get; set; } = 30;
        public TimeSpan? VisibilityTimeout { get; set; } = TimeSpan.FromMinutes(60);
    }
    public static class AzureStorageExtensions
    {

        // todo make overloads to simplify the common case

        public static IGlobalConfiguration<AzureJobStorage> UseAzureStorage(this IGlobalConfiguration configuration,
                AzureStorageOptions options)
        {

            var storage = new AzureJobStorage(options);

            return configuration.UseStorage(storage);
        }
    }
}