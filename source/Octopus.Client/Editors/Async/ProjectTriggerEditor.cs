using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Model.Triggers;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class ProjectTriggerEditor : IResourceEditor<ProjectTriggerResource, ProjectTriggerEditor>
    {
        private readonly IProjectTriggerRepository repository;

        public ProjectTriggerEditor(IProjectTriggerRepository repository)
        {
            this.repository = repository;
        }

        public ProjectTriggerResource Instance { get; private set; }

        public async Task<ProjectTriggerEditor> CreateOrModify(ProjectResource project, string name, TriggerFilterResource filter, TriggerActionResource action, CancellationToken token = default)
        {
            var existing = await repository.FindByName(project, name, token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new ProjectTriggerResource
                {
                    Name = name,
                    ProjectId = project.Id,
                    Filter = filter,
                    Action = action
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                existing.Filter = filter;
                existing.Action = action;
                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public ProjectTriggerEditor Customize(Action<ProjectTriggerResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<ProjectTriggerEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}