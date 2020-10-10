using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using Octopus.Client.Model.Triggers;

namespace Octopus.Client.Repositories.Async
{
    public interface IProjectTriggerRepository : ICreate<ProjectTriggerResource>, IModify<ProjectTriggerResource>, IGet<ProjectTriggerResource>, IDelete<ProjectTriggerResource>
    {
        Task<ProjectTriggerResource> FindByName(ProjectResource project, string name, CancellationToken token = default);

        Task<ProjectTriggerEditor> CreateOrModify(ProjectResource project, string name, TriggerFilterResource filter, TriggerActionResource action, CancellationToken token = default);
        Task<ResourceCollection<ProjectTriggerResource>> FindByRunbook(CancellationToken token = default, params string[] runbookIds);
    }

    class ProjectTriggerRepository : BasicRepository<ProjectTriggerResource>, IProjectTriggerRepository
    {
        public ProjectTriggerRepository(IOctopusAsyncRepository repository)
            : base(repository, "ProjectTriggers")
        {
            MinimumCompatibleVersion("2019.11.0");
        }

        public Task<ProjectTriggerResource> FindByName(ProjectResource project, string name, CancellationToken token = default)
        {
            return FindByName(name, path: project.Link("Triggers"), token: token);
        }

        public Task<ProjectTriggerEditor> CreateOrModify(ProjectResource project, string name, TriggerFilterResource filter, TriggerActionResource action, CancellationToken token = default)
        {
            ThrowIfServerVersionIsNotCompatible().ConfigureAwait(false);
            
            return new ProjectTriggerEditor(this).CreateOrModify(project, name, filter, action, token);
        }

        public async Task<ResourceCollection<ProjectTriggerResource>> FindByRunbook(CancellationToken token = default, params string[] runbookIds)
        {
            await ThrowIfServerVersionIsNotCompatible();
            
            return await Client.List<ProjectTriggerResource>(await Repository.Link("Triggers"), new { runbooks = runbookIds }, token);
        }
    }
}
