using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.AzureStorage.Entities;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Azure.Cosmos.Table;

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

        public long DeletedListCount()
        {
            throw new NotImplementedException();
        }

        public long EnqueuedCount(string queue)
        {
            var q = _storage.Storage.Queue(queue);
            
            q.FetchAttributes();

            return q.ApproximateMessageCount ?? -0;
        }

        private IEnumerable<string> JobsByStatus(string status, int from, int count) 
            => _storage.Storage.Jobs.CreateQuery<JobEntity>().Where(a => a.State == status).Skip(from).Take(count).Select(a => a.RowKey);

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        {
            throw new NotImplementedException();
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            throw new NotImplementedException();
        }

        public long FailedCount()
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
                return new FailedJobDto {
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
            throw new NotImplementedException();
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage)
        {
            throw new NotImplementedException();
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

    


        public JobDetailsDto JobDetails(string jobId)
        {
            throw new NotImplementedException();
        }

        public long ProcessingCount()
        {
            throw new NotImplementedException();
        }

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
                    FirstJobs = new JobList<EnqueuedJobDto>(Enumerable.Empty<KeyValuePair<string,EnqueuedJobDto>>()),
                    Length = a.ApproximateMessageCount ?? 0
                };
            });
            return queues.ToList();
        }

        public long ScheduledCount()
        {
            throw new NotImplementedException();
        }

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
            throw new NotImplementedException();
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            throw new NotImplementedException();
        }

        public JobList<SucceededJobDto> SucceededJobs(int from, int count)
        {
            throw new NotImplementedException();
        }

        public long SucceededListCount()
        {
            throw new NotImplementedException();
        }
    }


    public static class DictionaryExtensions
    {
        public static TVal TryGetValue<TKey, TVal>(this IDictionary<TKey, TVal> dict, TKey key) => dict.TryGetValue(key, out var val) ? val : default;
    }
}