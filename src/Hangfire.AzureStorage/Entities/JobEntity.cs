using System;
using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage.Entities
{
    public class JobEntity : TableEntity
    {
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public TimeSpan? ExpireIn { get; set; }
        public string StateFile { get; set; }
    }
}