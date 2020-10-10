using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ISchedulerRepository
    {
        Task Start(CancellationToken token = default);
        Task Start(string taskName, CancellationToken token = default);
        Task Stop(CancellationToken token = default);
        Task Stop(string taskName, CancellationToken token = default);
        Task Trigger(string taskName, CancellationToken token = default);
        Task<ScheduledTaskDetailsResource> GetLogs(string taskName, CancellationToken token = default);
        Task<Stream> GetRawLogs(string taskName, CancellationToken token = default);
        Task<SchedulerStatusResource> Status(CancellationToken token = default);
    }

    class SchedulerRepository : ISchedulerRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public SchedulerRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public Task Start(CancellationToken token = default)
        {
            return repository.Client.GetContent("~/api/scheduler/start", token: token);
        }

        public Task Start(string taskName, CancellationToken token = default)
        {
            return repository.Client.GetContent($"~/api/scheduler/start?task={taskName}", token: token);
        }

        public Task Trigger(string taskName, CancellationToken token = default)
        {
            return repository.Client.GetContent($"~/api/scheduler/trigger?task={taskName}", token: token);
        }

        public Task Stop(CancellationToken token = default)
        {
            return repository.Client.GetContent("~/api/scheduler/stop", token: token);
        }

        public Task Stop(string taskName, CancellationToken token = default)
        {
            return repository.Client.GetContent($"~/api/scheduler/stop?task={taskName}", token: token);
        }

        public Task<ScheduledTaskDetailsResource> GetLogs(string taskName, CancellationToken token = default)
        {
            return repository.Client.Get<ScheduledTaskDetailsResource>($"~/api/scheduler/{taskName}/logs", token: token);
        }

        public Task<Stream> GetRawLogs(string taskName, CancellationToken token = default)
        {
            return repository.Client.GetContent($"~/api/scheduler/{taskName}/logs/raw", token: token);
        }

        public Task<SchedulerStatusResource> Status(CancellationToken token = default)
        {
            return repository.Client.Get<SchedulerStatusResource>("~/api/scheduler", token: token);
        }
    }
}
