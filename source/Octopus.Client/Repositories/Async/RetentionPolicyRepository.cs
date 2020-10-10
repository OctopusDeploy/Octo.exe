using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface IRetentionPolicyRepository
    {
        Task<TaskResource> ApplyNow(string spaceId = null, CancellationToken token = default);
    }

    class RetentionPolicyRepository : BasicRepository<RetentionPolicyResource>, IRetentionPolicyRepository
    {
        public RetentionPolicyRepository(IOctopusAsyncRepository repository)
            : base(repository, "RetentionPolicies")
        {
        }

        public Task<TaskResource> ApplyNow(string spaceId = null, CancellationToken token = default)
        {
            var tasks = new TaskRepository(Repository);
            var task = new TaskResource { Name = "Retention", Description = "Request to apply retention policies via the API", SpaceId = spaceId};
            return tasks.Create(task, token: token);
        }
    }
}
