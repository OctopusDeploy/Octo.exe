using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using Octopus.Client.Model.VersionControl;

namespace Octopus.Client.Repositories.Async
{
    public interface IProjectRepository : IFindByName<ProjectResource>, IGet<ProjectResource>, ICreate<ProjectResource>, IModify<ProjectResource>, IDelete<ProjectResource>, IGetAll<ProjectResource>
    {
        IProjectBetaRepository Beta();
        Task<ResourceCollection<ReleaseResource>> GetReleases(ProjectResource project, int skip = 0, int? take = null, string searchByVersion = null);
        Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ProjectResource project);
        Task<ReleaseResource> GetReleaseByVersion(ProjectResource project, string version);
        Task<ResourceCollection<ChannelResource>> GetChannels(ProjectResource project);
        Task<IReadOnlyList<ChannelResource>> GetAllChannels(ProjectResource project);
        Task<ProgressionResource> GetProgression(ProjectResource project);
        Task<ResourceCollection<ProjectTriggerResource>> GetTriggers(ProjectResource project);
        Task<IReadOnlyList<ProjectTriggerResource>> GetAllTriggers(ProjectResource project);
        Task SetLogo(ProjectResource project, string fileName, Stream contents);
        Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle);
        Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, string description, string cloneId = null);
        Task<ResourceCollection<RunbookSnapshotResource>> GetRunbookSnapshots(ProjectResource project, int skip = 0, int? take = null, string searchByName = null);
        Task<IReadOnlyList<RunbookSnapshotResource>> GetAllRunbookSnapshots(ProjectResource project);
        Task<RunbookSnapshotResource> GetRunbookSnapshotByName(ProjectResource project, string name);
        Task<ResourceCollection<RunbookResource>> GetRunbooks(ProjectResource project, int skip = 0, int? take = null, string searchByName = null);
        Task<IReadOnlyList<RunbookResource>> GetAllRunbooks(ProjectResource project);
    }

    class ProjectRepository : BasicRepository<ProjectResource>, IProjectRepository
    {
        private readonly IProjectBetaRepository beta;

        public ProjectRepository(IOctopusAsyncRepository repository)
            : base(repository, "Projects")
        {
            beta = new ProjectBetaRepository(repository);
        }

        public IProjectBetaRepository Beta()
        {
            return beta;
        }

        public Task<ResourceCollection<ReleaseResource>> GetReleases(ProjectResource project, int skip = 0, int? take = null, string searchByVersion = null)
        {
            return Client.List<ReleaseResource>(project.Link("Releases"), new { skip, take, searchByVersion });
        }

        public Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ProjectResource project)
        {
            return Client.ListAll<ReleaseResource>(project.Link("Releases"));
        }

        public Task<ReleaseResource> GetReleaseByVersion(ProjectResource project, string version)
        {
            return Client.Get<ReleaseResource>(project.Link("Releases"), new { version });
        }

        public Task<ResourceCollection<ChannelResource>> GetChannels(ProjectResource project)
        {
            return Client.List<ChannelResource>(project.Link("Channels"));
        }

        public Task<IReadOnlyList<ChannelResource>> GetAllChannels(ProjectResource project)
        {
            return Client.ListAll<ChannelResource>(project.Link("Channels"));
        }

        public Task<ProgressionResource> GetProgression(ProjectResource project)
        {
            return Client.Get<ProgressionResource>(project.Link("Progression"));
        }

        public Task<ResourceCollection<ProjectTriggerResource>> GetTriggers(ProjectResource project)
        {
            return Client.List<ProjectTriggerResource>(project.Link("Triggers"));
        }

        public Task<IReadOnlyList<ProjectTriggerResource>> GetAllTriggers(ProjectResource project)
        {
            return Client.ListAll<ProjectTriggerResource>(project.Link("Triggers"));
        }

        public Task SetLogo(ProjectResource project, string fileName, Stream contents)
        {
            return Client.Post(project.Link("Logo"), new FileUpload { Contents = contents, FileName = fileName }, false);
        }

        public Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle)
        {
            return new ProjectEditor(this, new ChannelRepository(Repository), new DeploymentProcessRepository(Repository), new ProjectTriggerRepository(Repository), new VariableSetRepository(Repository)).CreateOrModify(name, projectGroup, lifecycle);
        }

        public Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, string description, string cloneId = null)
        {
            return new ProjectEditor(this, new ChannelRepository(Repository), new DeploymentProcessRepository(Repository), new ProjectTriggerRepository(Repository), new VariableSetRepository(Repository)).CreateOrModify(name, projectGroup, lifecycle, description, cloneId);
        }

        public Task<ResourceCollection<RunbookSnapshotResource>> GetRunbookSnapshots(ProjectResource project, int skip = 0, int? take = null, string searchByName = null)
        {
            return Client.List<RunbookSnapshotResource>(project.Link("RunbookSnapshots"), new { skip, take, searchByName });
        }

        public Task<IReadOnlyList<RunbookSnapshotResource>> GetAllRunbookSnapshots(ProjectResource project)
        {
            return Client.ListAll<RunbookSnapshotResource>(project.Link("RunbookSnapshots"));
        }

        public Task<RunbookSnapshotResource> GetRunbookSnapshotByName(ProjectResource project, string name)
        {
            return Client.Get<RunbookSnapshotResource>(project.Link("RunbookSnapshots"), new { name });
        }

        public Task<ResourceCollection<RunbookResource>> GetRunbooks(ProjectResource project, int skip = 0, int? take = null, string searchByName = null)
        {
            return Client.List<RunbookResource>(project.Link("Runbooks"), new { skip, take, searchByName });
        }

        public Task<IReadOnlyList<RunbookResource>> GetAllRunbooks(ProjectResource project)
        {
            return Client.ListAll<RunbookResource>(project.Link("Runbooks"));
        }
    }

    public interface IProjectBetaRepository
    {
        Task<ResourceCollection<VersionControlBranchResource>> GetVersionControlledBranches(ProjectResource projectResource);
        Task<VersionControlBranchResource> GetVersionControlledBranch(ProjectResource projectResource, string branch);
        Task<ConvertProjectToVersionControlledResponse> ConvertToVersionControlled(ProjectResource project, VersionControlSettingsResource versionControlSettings, string commitMessage);
        Task<DeploymentProcessResource> GetDeploymentProcess(ProjectResource project);
        Task<DeploymentProcessResource> UpdateDeploymentProcess(ProjectResource projectResource,
            DeploymentProcessResource deploymentProcessResource, string commitMessage);
    }

    class ProjectBetaRepository : IProjectBetaRepository
    {
        private readonly IOctopusAsyncClient client;

        public ProjectBetaRepository(IOctopusAsyncRepository repository)
        {
            client = repository.Client;
        }

        public Task<ResourceCollection<VersionControlBranchResource>> GetVersionControlledBranches(ProjectResource projectResource)
        {
            return client.Get<ResourceCollection<VersionControlBranchResource>>(projectResource.Link("Branches"));
        }

        public Task<VersionControlBranchResource> GetVersionControlledBranch(ProjectResource projectResource, string branch)
        {
            return client.Get<VersionControlBranchResource>(projectResource.Link("Branches"), new { name = branch });
        }

        public async Task<ConvertProjectToVersionControlledResponse> ConvertToVersionControlled(ProjectResource project, VersionControlSettingsResource versionControlSettings,
            string commitMessage)
        {
            var payload = new ConvertProjectToVersionControlledCommand
            {
                VersionControlSettings = versionControlSettings,
                CommitMessage = commitMessage
            };

            var url = project.Link("ConvertToVcs");
            var response = await client.Post<ConvertProjectToVersionControlledCommand,ConvertProjectToVersionControlledResponse>(url, payload);
            return response;
        }

        public Task<DeploymentProcessResource> GetDeploymentProcess(ProjectResource projectResource)
        {
            return client.Get<DeploymentProcessResource>(projectResource.Link("DeploymentProcess"));
        }

        public async Task<DeploymentProcessResource> UpdateDeploymentProcess(ProjectResource projectResource, DeploymentProcessResource deploymentProcessResource,
            string commitMessage)
        {
            var payload = new UpdateVersionControlledDeploymentProcessCommand
            {
                Resource = deploymentProcessResource,
                CommitMessage = commitMessage
            };

            var url = projectResource.Link("DeploymentProcess");
            var response = await client.Post<UpdateVersionControlledDeploymentProcessCommand, DeploymentProcessResource>(url, payload);
            return response;
        }
    }
}