using System;
using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage.Entities
{
    public class CounterEntity : TableEntity
    {
        public DateTime? ExpireAt { get; set; }
        public int Value { get; set; }
    }
}