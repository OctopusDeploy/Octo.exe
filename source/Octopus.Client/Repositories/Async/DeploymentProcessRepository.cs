using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IDeploymentProcessRepository : IGet<DeploymentProcessResource>, IModify<DeploymentProcessResource>
    {
        IDeploymentProcessBetaRepository Beta();
        Task<ReleaseTemplateResource> GetTemplate(DeploymentProcessResource deploymentProcess, ChannelResource channel, CancellationToken token = default);
    }

    class DeploymentProcessRepository : BasicRepository<DeploymentProcessResource>, IDeploymentProcessRepository
    {
        private readonly IDeploymentProcessBetaRepository beta;

        public DeploymentProcessRepository(IOctopusAsyncRepository repository)
            : base(repository, "DeploymentProcesses")
        {
            beta = new DeploymentProcessBetaRepository(repository);
        }

        public IDeploymentProcessBetaRepository Beta()
        {
            return beta;
        }

        public Task<ReleaseTemplateResource> GetTemplate(DeploymentProcessResource deploymentProcess, ChannelResource channel, CancellationToken token = default)
        {
            return Client.Get<ReleaseTemplateResource>(deploymentProcess.Link("Template"), new { channel = channel?.Id }, token);
        }
    }

    public interface IDeploymentProcessBetaRepository
    {
        Task<DeploymentProcessResource> Get(ProjectResource projectResource, string gitref = null, CancellationToken token = default);

        Task<DeploymentProcessResource> Modify(ProjectResource projectResource, DeploymentProcessResource resource,
            string commitMessage = null, CancellationToken token = default);
    }

    class DeploymentProcessBetaRepository : IDeploymentProcessBetaRepository
    {
        private readonly IOctopusAsyncRepository repository;
        private readonly IOctopusAsyncClient client;

        public DeploymentProcessBetaRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
            this.client = repository.Client;
        }

        public async Task<DeploymentProcessResource> Get(ProjectResource projectResource, string gitref = null, CancellationToken token = default)
        {
            if (!string.IsNullOrWhiteSpace(gitref))
            {
                var branchResource = await repository.Projects.Beta().GetVersionControlledBranch(projectResource, gitref);

                return await client.Get<DeploymentProcessResource>(branchResource.Link("DeploymentProcess"), token: token);
            }

            return await client.Get<DeploymentProcessResource>(projectResource.Link("DeploymentProcess"), token: token);
        }

        public async Task<DeploymentProcessResource> Modify(ProjectResource projectResource,
            DeploymentProcessResource resource, string commitMessage = null, CancellationToken token = default)
        {
            if (!projectResource.IsVersionControlled)
            {
                return await client.Update(projectResource.Link("DeploymentProcess"), resource, token: token);
            }

            var commitResource = new CommitResource<DeploymentProcessResource>
            {
                Resource = resource,
                CommitMessage = commitMessage
            };
            await client.Update(resource.Link("Self"), commitResource, token: token);
            return await client.Get<DeploymentProcessResource>(resource.Link("Self"), token: token);
        }
    }
}
