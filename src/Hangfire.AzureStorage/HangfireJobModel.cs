using System;
using System.Collections.Generic;

namespace Hangfire.AzureStorage
{
    public class HangfireJobModel
    {
        public string InvocationData { get; set; }
        public string Arguments { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
        public DateTime CreatedAt { get; internal set; }
    }

}