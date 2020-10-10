using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using System.Threading;

namespace Octopus.Client.Repositories.Async
{
    public interface IProjectRepository : IFindByName<ProjectResource>, IGet<ProjectResource>, ICreate<ProjectResource>, IModify<ProjectResource>, IDelete<ProjectResource>, IGetAll<ProjectResource>
    {
        IProjectBetaRepository Beta();
        Task<ResourceCollection<ReleaseResource>> GetReleases(ProjectResource project, int skip = 0, int? take = null, string searchByVersion = null, CancellationToken token = default);
        Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ProjectResource project, CancellationToken token = default);
        Task<ReleaseResource> GetReleaseByVersion(ProjectResource project, string version, CancellationToken token = default);
        Task<ResourceCollection<ChannelResource>> GetChannels(ProjectResource project, CancellationToken token = default);
        Task<IReadOnlyList<ChannelResource>> GetAllChannels(ProjectResource project, CancellationToken token = default);
        Task<ProgressionResource> GetProgression(ProjectResource project, CancellationToken token = default);
        Task<ResourceCollection<ProjectTriggerResource>> GetTriggers(ProjectResource project, CancellationToken token = default);
        Task<IReadOnlyList<ProjectTriggerResource>> GetAllTriggers(ProjectResource project, CancellationToken token = default);
        Task SetLogo(ProjectResource project, string fileName, Stream contents, CancellationToken token = default);
        Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, CancellationToken token = default);
        Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, string description, string cloneId = null, CancellationToken token = default);
        Task<ResourceCollection<RunbookSnapshotResource>> GetRunbookSnapshots(ProjectResource project, int skip = 0, int? take = null, string searchByName = null, CancellationToken token = default);
        Task<IReadOnlyList<RunbookSnapshotResource>> GetAllRunbookSnapshots(ProjectResource project, CancellationToken token = default);
        Task<RunbookSnapshotResource> GetRunbookSnapshotByName(ProjectResource project, string name, CancellationToken token = default);
        Task<ResourceCollection<RunbookResource>> GetRunbooks(ProjectResource project, int skip = 0, int? take = null, string searchByName = null, CancellationToken token = default);
        Task<IReadOnlyList<RunbookResource>> GetAllRunbooks(ProjectResource project, CancellationToken token = default);
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

        public Task<ResourceCollection<ReleaseResource>> GetReleases(ProjectResource project, int skip = 0, int? take = null, string searchByVersion = null, CancellationToken token = default)
        {
            return Client.List<ReleaseResource>(project.Link("Releases"), new { skip, take, searchByVersion }, token);
        }

        public Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ProjectResource project, CancellationToken token = default)
        {
            return Client.ListAll<ReleaseResource>(project.Link("Releases"), token: token);
        }

        public Task<ReleaseResource> GetReleaseByVersion(ProjectResource project, string version, CancellationToken token = default)
        {
            return Client.Get<ReleaseResource>(project.Link("Releases"), new { version }, token);
        }

        public Task<ResourceCollection<ChannelResource>> GetChannels(ProjectResource project, CancellationToken token = default)
        {
            return Client.List<ChannelResource>(project.Link("Channels"), token: token);
        }

        public Task<IReadOnlyList<ChannelResource>> GetAllChannels(ProjectResource project, CancellationToken token = default)
        {
            return Client.ListAll<ChannelResource>(project.Link("Channels"), token: token);
        }

        public Task<ProgressionResource> GetProgression(ProjectResource project, CancellationToken token = default)
        {
            return Client.Get<ProgressionResource>(project.Link("Progression"), token: token);
        }

        public Task<ResourceCollection<ProjectTriggerResource>> GetTriggers(ProjectResource project, CancellationToken token = default)
        {
            return Client.List<ProjectTriggerResource>(project.Link("Triggers"), token: token);
        }

        public Task<IReadOnlyList<ProjectTriggerResource>> GetAllTriggers(ProjectResource project, CancellationToken token = default)
        {
            return Client.ListAll<ProjectTriggerResource>(project.Link("Triggers"), token: token);
        }

        public Task SetLogo(ProjectResource project, string fileName, Stream contents, CancellationToken token = default)
        {
            return Client.Post(project.Link("Logo"), new FileUpload { Contents = contents, FileName = fileName }, false, token);
        }

        public Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, CancellationToken token = default)
        {
            return new ProjectEditor(this, new ChannelRepository(Repository), new DeploymentProcessRepository(Repository), new ProjectTriggerRepository(Repository), new VariableSetRepository(Repository)).CreateOrModify(name, projectGroup, lifecycle, token);
        }

        public Task<ProjectEditor> CreateOrModify(string name, ProjectGroupResource projectGroup, LifecycleResource lifecycle, string description, string cloneId = null, CancellationToken token = default)
        {
            return new ProjectEditor(this, new ChannelRepository(Repository), new DeploymentProcessRepository(Repository), new ProjectTriggerRepository(Repository), new VariableSetRepository(Repository)).CreateOrModify(name, projectGroup, lifecycle, description, cloneId, token);
        }

        public Task<ResourceCollection<RunbookSnapshotResource>> GetRunbookSnapshots(ProjectResource project, int skip = 0, int? take = null, string searchByName = null, CancellationToken token = default)
        {
            return Client.List<RunbookSnapshotResource>(project.Link("RunbookSnapshots"), new { skip, take, searchByName }, token);
        }

        public Task<IReadOnlyList<RunbookSnapshotResource>> GetAllRunbookSnapshots(ProjectResource project, CancellationToken token = default)
        {
            return Client.ListAll<RunbookSnapshotResource>(project.Link("RunbookSnapshots"), token: token);
        }

        public Task<RunbookSnapshotResource> GetRunbookSnapshotByName(ProjectResource project, string name, CancellationToken token = default)
        {
            return Client.Get<RunbookSnapshotResource>(project.Link("RunbookSnapshots"), new { name }, token);
        }

        public Task<ResourceCollection<RunbookResource>> GetRunbooks(ProjectResource project, int skip = 0, int? take = null, string searchByName = null, CancellationToken token = default)
        {
            return Client.List<RunbookResource>(project.Link("Runbooks"), new { skip, take, searchByName }, token);
        }

        public Task<IReadOnlyList<RunbookResource>> GetAllRunbooks(ProjectResource project, CancellationToken token = default)
        {
            return Client.ListAll<RunbookResource>(project.Link("Runbooks"), token: token);
        }
    }

    public interface IProjectBetaRepository
    {
        Task<VersionControlBranchResource[]> GetVersionControlledBranches(ProjectResource projectResource, CancellationToken token = default);
        Task<VersionControlBranchResource> GetVersionControlledBranch(ProjectResource projectResource, string branch, CancellationToken token = default);
    }

    class ProjectBetaRepository : IProjectBetaRepository
    {
        private readonly IOctopusAsyncClient client;

        public ProjectBetaRepository(IOctopusAsyncRepository repository)
        {
            this.client = repository.Client;
        }

        public Task<VersionControlBranchResource[]> GetVersionControlledBranches(ProjectResource projectResource, CancellationToken token = default)
        {
            return client.Get<VersionControlBranchResource[]>(projectResource.Link("Branches"), token: token);
        }

        public Task<VersionControlBranchResource> GetVersionControlledBranch(ProjectResource projectResource, string branch, CancellationToken token = default)
        {
            return client.Get<VersionControlBranchResource>(projectResource.Link("Branches"), new { name = branch }, token);
        }
    }
}