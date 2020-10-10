using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IDeploymentRepository : IGet<DeploymentResource>, ICreate<DeploymentResource>, IPaginate<DeploymentResource>, IDelete<DeploymentResource>
    {
        Task<TaskResource> GetTask(DeploymentResource resource, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="environments"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<DeploymentResource>> FindBy(string[] projects, string[] environments, int skip = 0, int? take = null, CancellationToken token = default);

        [Obsolete("This method is not a find all, it still requires paging. So it has been renamed to `FindBy`")]
        Task<ResourceCollection<DeploymentResource>> FindAll(string[] projects, string[] environments, int skip = 0, int? take = null, CancellationToken token = default);
        Task Paginate(string[] projects, string[] environments, Func<ResourceCollection<DeploymentResource>, bool> getNextPage, CancellationToken token = default);
        Task Paginate(string[] projects, string[] environments, string[] tenants, Func<ResourceCollection<DeploymentResource>, bool> getNextPage, CancellationToken token = default);
    }

    class DeploymentRepository : BasicRepository<DeploymentResource>, IDeploymentRepository
    {
        public DeploymentRepository(IOctopusAsyncRepository repository)
            : base(repository, "Deployments")
        {
        }

        public Task<TaskResource> GetTask(DeploymentResource resource, CancellationToken token = default)
        {
            return Client.Get<TaskResource>(resource.Link("Task"), token: token);
        }

        public async Task<ResourceCollection<DeploymentResource>> FindBy(string[] projects, string[] environments, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return await Client.List<DeploymentResource>(await Repository.Link("Deployments").ConfigureAwait(false), new { skip, take, projects = projects ?? new string[0], environments = environments ?? new string[0] }, token).ConfigureAwait(false);
        }

        [Obsolete("This method is not a find all, it still requires paging. So it has been renamed to `FindBy`")]
        public Task<ResourceCollection<DeploymentResource>> FindAll(string[] projects, string[] environments, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return FindBy(projects, environments, skip, take, token);
        }

        public Task Paginate(string[] projects, string[] environments, Func<ResourceCollection<DeploymentResource>, bool> getNextPage, CancellationToken token = default)
        {
            return Paginate(projects, environments, new string[0], getNextPage, token);
        }

        public async Task Paginate(string[] projects, string[] environments, string[] tenants, Func<ResourceCollection<DeploymentResource>, bool> getNextPage, CancellationToken token = default)
        {
            await Client.Paginate(await Repository.Link("Deployments").ConfigureAwait(false), new { projects = projects ?? new string[0], environments = environments ?? new string[0], tenants = tenants ?? new string[0] }, getNextPage, token).ConfigureAwait(false);
        }
    }
}
