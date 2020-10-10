using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Model.Triggers;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class ProjectTriggersEditor
    {
        private readonly IProjectTriggerRepository repository;
        private readonly ProjectResource owner;
        private readonly List<ProjectTriggerEditor> trackedProjectTriggerBuilders = new List<ProjectTriggerEditor>(); 

        public ProjectTriggersEditor(IProjectTriggerRepository repository, ProjectResource owner)
        {
            this.repository = repository;
            this.owner = owner;
        }

        public async Task<ProjectTriggerEditor> CreateOrModify(string name, TriggerFilterResource filter, TriggerActionResource action, CancellationToken token = default)
        {
            var projectTriggerBuilder = await new ProjectTriggerEditor(repository).CreateOrModify(owner, name, filter, action, token).ConfigureAwait(false);
            trackedProjectTriggerBuilders.Add(projectTriggerBuilder);
            return projectTriggerBuilder;
        }

        public async Task<ProjectTriggersEditor> Delete(string name, CancellationToken token = default)
        {
            var trigger = await repository.FindByName(owner, name, token).ConfigureAwait(false);
            if (trigger != null)
                await repository.Delete(trigger, token).ConfigureAwait(false);
            return this;
        }

        public async Task<ProjectTriggersEditor> SaveAll(CancellationToken token = default)
        {
            await Task.WhenAll(trackedProjectTriggerBuilders.Select(x => x.Save(token))).ConfigureAwait(false);
            return this;
        }
    }
}