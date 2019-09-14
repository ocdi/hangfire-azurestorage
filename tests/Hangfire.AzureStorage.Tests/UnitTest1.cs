using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.AzureStorage.Tests
{
    public class UnitTest1
    {
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
        }

        
        [Fact]
        public void HashesCanBeSet()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            var write = connection.CreateWriteTransaction();

            var dict = new Dictionary<string, string> { { "field", "value" } };
            write.SetRangeInHash("hashname", dict);

            write.Commit();

            var result = connection.GetAllEntriesFromHash("hashname");

            Assert.Equal(dict, result);

        }
    }
}
