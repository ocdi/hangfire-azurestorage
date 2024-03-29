﻿using System;
using System.Collections.Generic;

namespace Hangfire.AzureStorage
{
    public class HangfireJobStateModel
    {
        public string Name { get;  set; }
        public string Reason { get;  set; }
        public Dictionary<string, string> Data { get;  set; }
        public DateTime CreatedAt { get;  set; }
    }

}