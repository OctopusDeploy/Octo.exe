using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IProjectGroupRepository : IFindByName<ProjectGroupResource>, IGet<ProjectGroupResource>, ICreate<ProjectGroupResource>, IModify<ProjectGroupResource>, IDelete<ProjectGroupResource>, IGetAll<ProjectGroupResource>
    {
        Task<List<ProjectResource>> GetProjects(ProjectGroupResource projectGroup, CancellationToken token = default);
        Task<ProjectGroupEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<ProjectGroupEditor> CreateOrModify(string name, string description, CancellationToken token = default);
    }

    class ProjectGroupRepository : BasicRepository<ProjectGroupResource>, IProjectGroupRepository
    {
        public ProjectGroupRepository(IOctopusAsyncRepository repository)
            : base(repository, "ProjectGroups")
        {
        }

        public async Task<List<ProjectResource>> GetProjects(ProjectGroupResource projectGroup, CancellationToken token = default)
        {
            var resources = new List<ProjectResource>();

            await Client.Paginate<ProjectResource>(projectGroup.Link("Projects"), new { }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public Task<ProjectGroupEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new ProjectGroupEditor(this).CreateOrModify(name, token);
        }

        public Task<ProjectGroupEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new ProjectGroupEditor(this).CreateOrModify(name, description, token);
        }
    }
}
