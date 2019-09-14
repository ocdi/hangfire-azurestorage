using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage.Entities
{
    public class HashEntity : TableEntity
    {
        public string Value { get; set; }
    }
}