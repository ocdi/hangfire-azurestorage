using System;
using Microsoft.Azure.Cosmos.Table;

namespace Hangfire.AzureStorage.Entities
{
    public class ServerEntity : TableEntity
    {
        public DateTime? Heartbeat { get; set; }
        public int WorkerCount { get; internal set; }

        /// <summary>
        /// This will be stored as a serialized JSON string, but is really an Enumerable of string
        /// </summary>
        /// <value></value>
        public string Queues { get; internal set; }
        public DateTime StartedAt { get; internal set; }
        public DateTime LastHeartbeat { get; internal set; }
    }
}