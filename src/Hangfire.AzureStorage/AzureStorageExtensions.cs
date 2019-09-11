using Hangfire;

namespace Hangfire.AzureStorage
{
    public class AzureStorageOptions
    {
        public string ConnectionString {get;set;}
        /// <summary>
        /// This sets a prefix for the various storage account queues and tables
        /// </summary>
        /// <value></value>
        public string Prefix {get;set;}
        
    }
    public static class AzureStorageExtensions {

        // todo make overloads to simplify the common case

        public static IGlobalConfiguration<AzureJobStorage> UseAzureStorage(this IGlobalConfiguration configuration, 
                AzureStorageOptions options) {

            var storage = new AzureJobStorage(options);
            
            return configuration.UseStorage(storage);
        }
    }
}