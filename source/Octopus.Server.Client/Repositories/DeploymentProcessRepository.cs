using Octopus.Client.Model;

namespace Octopus.Client.Repositories
{
    public interface IDeploymentProcessRepository : IGet<DeploymentProcessResource>, IModify<DeploymentProcessResource>
    {
        IDeploymentProcessRepositoryBeta Beta();
        ReleaseTemplateResource GetTemplate(DeploymentProcessResource deploymentProcess, ChannelResource channel);
    }

    class DeploymentProcessRepository : BasicRepository<DeploymentProcessResource>, IDeploymentProcessRepository
    {
        private readonly DeploymentProcessRepositoryBeta beta;

        public DeploymentProcessRepository(IOctopusRepository repository)
            : base(repository, "DeploymentProcesses")
        {
            beta = new DeploymentProcessRepositoryBeta(repository);
        }

        public IDeploymentProcessRepositoryBeta Beta()
        {
            return beta;
        }

        public ReleaseTemplateResource GetTemplate(DeploymentProcessResource deploymentProcess, ChannelResource channel)
        {
            return Client.Get<ReleaseTemplateResource>(deploymentProcess.Link("Template"), new { channel = channel?.Id });
        }
    }

    public interface IDeploymentProcessRepositoryBeta
    {
        DeploymentProcessResource Get(ProjectResource projectResource, string gitRef = null);
        DeploymentProcessResource Modify(ProjectResource projectResource, DeploymentProcessResource resource, string commitMessage = null);
    }

    class DeploymentProcessRepositoryBeta : IDeploymentProcessRepositoryBeta
    {
        private readonly IOctopusRepository repository;
        private readonly IOctopusClient client;

        public DeploymentProcessRepositoryBeta(IOctopusRepository repository)
        {
            this.repository = repository;
            client = repository.Client;
        }

        public DeploymentProcessResource Get(ProjectResource projectResource, string gitRef = null)
        {
            if (!(projectResource.PersistenceSettings is VersionControlSettingsResource settings))
                return repository.DeploymentProcesses.Get(projectResource.DeploymentProcessId);

            gitRef = gitRef ?? settings.DefaultBranch;

            return client.Get<DeploymentProcessResource>(projectResource.Link("DeploymentProcess"), new { gitRef });
        }

        public DeploymentProcessResource Modify(ProjectResource projectResource, DeploymentProcessResource resource, string commitMessage = null)
        {
            if (!projectResource.IsVersionControlled)
            {
                return client.Update(projectResource.Link("DeploymentProcess"), resource);
            }

            var commitResource = new CommitResource<DeploymentProcessResource>
            {
                Resource = resource,
                CommitMessage = commitMessage
            };
            client.Put(resource.Link("Self"), commitResource);
            return client.Get<DeploymentProcessResource>(resource.Link("Self"));
        }
    }
}