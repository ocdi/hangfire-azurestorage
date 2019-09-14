using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage.Entities
{

    /// <summary>
    /// PartitionKey is the set name, RowKey is the value in question
    /// </summary>
    public class SetEntity : TableEntity
    {
        public double? Score { get; set; }
    }
}