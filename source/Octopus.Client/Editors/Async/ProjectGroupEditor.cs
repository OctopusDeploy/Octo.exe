using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class ProjectGroupEditor : IResourceEditor<ProjectGroupResource, ProjectGroupEditor>
    {
        private readonly IProjectGroupRepository repository;

        public ProjectGroupEditor(IProjectGroupRepository repository)
        {
            this.repository = repository;
        }

        public ProjectGroupResource Instance { get; private set; }

        public async Task<ProjectGroupEditor> CreateOrModify(string name, CancellationToken token)
        {
            var existing = await repository.FindByName(name).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new ProjectGroupResource
                {
                    Name = name,
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;

                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }
        public async Task<ProjectGroupEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new ProjectGroupResource
                {
                    Name = name,
                    Description = description
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                existing.Description = description;

                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public ProjectGroupEditor Customize(Action<ProjectGroupResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<ProjectGroupEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}