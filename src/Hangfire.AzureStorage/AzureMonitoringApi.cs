using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.AzureStorage.Entities;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Hangfire.AzureStorage
{
    public class AzureMonitoringApi : IMonitoringApi
    {

        private readonly AzureJobStorageConnection _storage;

        public AzureMonitoringApi(AzureJobStorageConnection storage)
        {

            _storage = storage;
        }

        public JobList<DeletedJobDto> DeletedJobs(int from, int count)
        {
            throw new NotImplementedException();
        }

        public long DeletedListCount() => JobsByStatus(DeletedState.StateName).Count();

        public long EnqueuedCount(string queue)
        {
            var q = _storage.Storage.Queue(queue);

            q.FetchAttributes();

            return q.ApproximateMessageCount ?? -0;
        }

        private IEnumerable<string> JobsByStatus(string status, int from, int count)
            => JobsByStatus(status).Skip(from).Take(count).Select(a => a.RowKey);

        private IEnumerable<JobEntity> JobsByStatus(string status) => _storage.Storage.Jobs.CreateQuery<JobEntity>().Where(a => a.State == status).AsEnumerable();

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            throw new NotImplementedException();
        }

      

        

        public JobList<FailedJobDto> FailedJobs(int from, int count)
        {
            var jobs = JobsByStatus("Failed", from, count);

            return new JobList<FailedJobDto>(jobs.ToDictionary(a => a, a =>
            {
                // todo this could be optimised, perhaps for getting the state data
                // do we index this data into its own partitioned table?
                var (state, model) = _storage.GetStateDataRaw(a);
                return new FailedJobDto
                {
                    Job = _storage.GetJobData(a).Job,
                    FailedAt = state.CreatedAt,
                    ExceptionDetails = model.Data.TryGetValue(nameof(FailedJobDto.ExceptionDetails)),
                    ExceptionMessage = model.Data.TryGetValue(nameof(FailedJobDto.ExceptionMessage)),
                    ExceptionType = model.Data.TryGetValue(nameof(FailedJobDto.ExceptionType)),
                };
            }));
        }

        public long FetchedCount(string queue)
        {

            return 0;
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
        {
            return new JobList<FetchedJobDto>(Enumerable.Empty<KeyValuePair<string, FetchedJobDto>>());
        }

        public StatisticsDto GetStatistics()
        {
            return new StatisticsDto { };
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return new Dictionary<DateTime, long>();
        }


        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            throw new NotImplementedException();
        }

        public long ProcessingCount() => JobsByStatus(ProcessingState.StateName).Count();

        public JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
        {
            throw new NotImplementedException();
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {

            var prefix = _storage.Storage.Options.Prefix;
            var queues = _storage.Storage.QueueClient.ListQueues(prefix?.ToLower()).Select(a =>
            {
                a.FetchAttributes();
                var name = prefix != null ? a.Name.Substring(prefix.Length) : a.Name;

                return new QueueWithTopEnqueuedJobsDto
                {
                    Name = name,
                    Fetched = 0,
                    FirstJobs = new JobList<EnqueuedJobDto>(Enumerable.Empty<KeyValuePair<string, EnqueuedJobDto>>()),
                    Length = a.ApproximateMessageCount ?? 0
                };
            });
            return queues.ToList();
        }

        public long FailedCount() => JobsByStatus(FailedState.StateName).Count();

        public long ScheduledCount() => JobsByStatus(ScheduledState.StateName).Count();

        public long SucceededListCount() => JobsByStatus(SucceededState.StateName).Count();

        public JobList<ScheduledJobDto> ScheduledJobs(int from, int count)
        {

            var jobs = JobsByStatus(ScheduledState.StateName, from, count);

            return new JobList<ScheduledJobDto>(jobs.ToDictionary(a => a, a =>
            {
                // todo this could be optimised, perhaps for getting the state data
                // do we index this data into its own partitioned table?
                var (state, model) = _storage.GetStateDataRaw(a);

                return new ScheduledJobDto
                {
                    Job = _storage.GetJobData(a).Job,
                    ScheduledAt = state.CreatedAt,
                    InScheduledState = ScheduledState.StateName.Equals(state.State, StringComparison.OrdinalIgnoreCase),
                    EnqueueAt = JobHelper.DeserializeNullableDateTime(model.Data.TryGetValue("EnqueueAt")) ?? DateTime.MinValue
                };
            }));

        }

        public IList<ServerDto> Servers()
        {
            return _storage.Storage.Servers.CreateQuery<ServerEntity>()
                .Select(a => new ServerDto
                {
                    StartedAt = a.StartedAt ?? DateTime.MinValue,
                    Heartbeat = a.LastHeartbeat,
                    Name = a.RowKey,
                    WorkersCount = a.WorkerCount ?? 0,
                    Queues = JsonConvert.DeserializeObject<string[]>(a.Queues)
                }).ToList();
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            return new JobList<SucceededJobDto>(Enumerable.Empty<KeyValuePair<string, SucceededJobDto>>());
        }

    }


    public static class CacheExtensions
    {
        public static void SetObject<T>(this IDistributedCache cache, string key, T obj) => cache.SetString(key, JsonConvert.SerializeObject(obj));
        public static T GetObject<T>(this IDistributedCache cache, string key)
        {
            var str = cache.GetString(key);
            if (str != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(str);
                }
                catch
                {
                    // deserialization exception
                }
            }

            return default;
        }
    }
    public static class DictionaryExtensions
    {
        public static TVal TryGetValue<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key) => dict != null ? dict.TryGetValue(key, out var val) ? val : default : default;
    }
}