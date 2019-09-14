using Hangfire.Common;
using Hangfire.States;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.AzureStorage.Tests
{
    public class AzureJobStorageTests
    {
        private readonly AzureJobStorage _storage;

        public AzureJobStorageTests() => _storage = CreateStorage();

        [Fact]
        public void WeCanGetAllItemsFromASet()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            var result = connection.GetAllItemsFromSet("SET_THAT_SHOULD_NEVER_EXIST");

            Assert.Empty(result);
            
        }

        [Fact]
        public void ServersCanBeAnnounced()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            connection.AnnounceServer("TEST", new Server.ServerContext { });
        }

        [Fact]
        public void SetsCanBeSet()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            // clear the sets table for testing
            
            var connection = storage.GetConnection();
            var write = connection.CreateWriteTransaction();
            
            write.AddToSet("set1", "valuefromSet1");
            write.AddToSet("set2", "valueFromSet2", 55);

            write.Commit();

            // validate
            var items = connection.GetAllItemsFromSet("set1");
            Assert.Equal(new[] { "valuefromSet1" }, items);

            var items2 = connection.GetAllItemsFromSet("set2");
            Assert.Equal(new[] { "valueFromSet2" }, items2);

            var lowest = connection.GetFirstByLowestScoreFromSet("set2", 50, 60);
            Assert.Equal("valueFromSet2", lowest);
        }

        
        [Fact]
        public void HashesCanBeSet()
        {
           
            var connection = _storage.GetConnection();
            var write = connection.CreateWriteTransaction();

            var dict = new Dictionary<string, string> { { "field", "value" } };
            write.SetRangeInHash("hashname", dict);

            write.Commit();

            var result = connection.GetAllEntriesFromHash("hashname");

            Assert.Equal(dict, result);

        }

        private static AzureJobStorage CreateStorage() => new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });

        [Fact]
        public void JobsCanBeStored()
        {
            var connection = _storage.GetConnection();
            var t = typeof(TestJob);
            var m = t.GetMethod(nameof(TestJob.Method));
            var job = new Job(m, "arg");
            var jobid = connection.CreateExpiredJob(job, new Dictionary<string, string> { { "p1", "v1" } }, DateTime.UtcNow, TimeSpan.FromMinutes(10));


            var loaded = connection.GetJobData(jobid);
            Assert.NotNull(loaded);

            var trans = connection.CreateWriteTransaction();
            trans.SetJobState(jobid, new EnqueuedState("somequeue"));
            trans.Commit();

            var state = connection.GetStateData(jobid);

            Assert.Equal("Enqueued", state.Name);

        }


       
        
    }


    public class TestJob
    {
        public void Method(string arg)
        {

        }
    }
}
