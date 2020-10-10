using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IRunbookRunRepository : IGet<RunbookRunResource>, ICreate<RunbookRunResource>, IPaginate<RunbookRunResource>, IDelete<RunbookRunResource>
    {
        Task<TaskResource> GetTask(RunbookRunResource resource, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="runbooks"></param>
        /// <param name="environments"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<RunbookRunResource>> FindBy(string[] projects, string[] runbooks, string[] environments, int skip = 0, int? take = null, CancellationToken token = default);
        Task Paginate(string[] projects, string[] runbooks, string[] environments, Func<ResourceCollection<RunbookRunResource>, bool> getNextPage, CancellationToken token = default);
        Task Paginate(string[] projects, string[] runbooks, string[] environments, string[] tenants, Func<ResourceCollection<RunbookRunResource>, bool> getNextPage, CancellationToken token = default);
    }

    class RunbookRunRepository : BasicRepository<RunbookRunResource>, IRunbookRunRepository
    {
        public RunbookRunRepository(IOctopusAsyncRepository repository)
            : base(repository, "RunbookRuns")
        {
        }

        public Task<TaskResource> GetTask(RunbookRunResource resource, CancellationToken token = default)
        {
            return Client.Get<TaskResource>(resource.Link("Task"), token: token);
        }

        public async Task<ResourceCollection<RunbookRunResource>> FindBy(string[] projects, string[] runbooks, string[] environments, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return await Client.List<RunbookRunResource>(await Repository.Link("RunbookRuns").ConfigureAwait(false), new { skip, take, projects = projects ?? new string[0], runbooks = runbooks ?? new string[0], environments = environments ?? new string[0] }, token).ConfigureAwait(false);
        }

        public Task Paginate(string[] projects, string[] runbooks, string[] environments, Func<ResourceCollection<RunbookRunResource>, bool> getNextPage, CancellationToken token = default)
        {
            return Paginate(projects, runbooks, environments, new string[0], getNextPage, token);
        }

        public async Task Paginate(string[] projects, string[] runbooks, string[] environments, string[] tenants, Func<ResourceCollection<RunbookRunResource>, bool> getNextPage, CancellationToken token = default)
        {
            await Client.Paginate(await Repository.Link("RunbookRuns").ConfigureAwait(false), new { projects = projects ?? new string[0], runbooks = runbooks ?? new string[0], environments = environments ?? new string[0], tenants = tenants ?? new string[0] }, getNextPage, token).ConfigureAwait(false);
        }
    }
}
