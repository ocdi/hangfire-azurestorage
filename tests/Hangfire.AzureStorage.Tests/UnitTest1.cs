using System;
using Xunit;

namespace Hangfire.AzureStorage.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            var result = connection.GetAllItemsFromSet("test");

            Assert.Empty(result);
            
        }

        [Fact]
        public void Test2()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            connection.AnnounceServer("TEST", new Server.ServerContext { });
        }

        [Fact]
        public void Test3()
        {
            var storage = new AzureJobStorage(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" });
            var connection = storage.GetConnection();
            var write = connection.CreateWriteTransaction();

            write.AddToSet("set1", "value");
            write.Commit();
        }
    }
}
