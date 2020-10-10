using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class ChannelEditor : IResourceEditor<ChannelResource, ChannelEditor>
    {
        private readonly IChannelRepository repository;

        public ChannelEditor(IChannelRepository repository)
        {
            this.repository = repository;
        }

        public ChannelResource Instance { get; private set; }

        public async Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, CancellationToken token = default)
        {
            var existing = await repository.FindByName(project, name, token).ConfigureAwait(false);

            if (existing == null)
            {
                Instance = await repository.Create(new ChannelResource
                {
                    ProjectId = project.Id,
                    Name = name
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;

                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public async Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(project, name, token).ConfigureAwait(false);

            if (existing == null)
            {
                Instance = await repository.Create(new ChannelResource
                {
                    ProjectId = project.Id,
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

        public ChannelEditor SetAsDefaultChannel()
        {
            Instance.SetAsDefaultChannel();
            return this;
        }

        public ChannelEditor UsingLifecycle(LifecycleResource lifecycle)
        {
            Instance.UsingLifecycle(lifecycle);
            return this;
        }

        public ChannelEditor ClearRules()
        {
            Instance.ClearRules();
            return this;
        }

        public ChannelEditor AddRule(ChannelVersionRuleResource rule)
        {
            Instance.AddRule(rule);
            return this;
        }

        public ChannelEditor AddCommonRuleForAllActions(string versionRange, string tagRegex, DeploymentProcessResource process)
        {
            Instance.AddCommonRuleForAllActions(versionRange, tagRegex, process);
            return this;
        }

        public ChannelEditor AddRule(string versionRange, string tagRegex, params DeploymentActionResource[] actions)
        {
            Instance.AddRule(versionRange, tagRegex, actions);
            return this;
        }

        public ChannelEditor ClearTenantTags()
        {
            Instance.ClearTenantTags();
            return this;
        }

        public ChannelEditor AddOrUpdateTenantTags(params TagResource[] tags)
        {
            Instance.AddOrUpdateTenantTags(tags);
            return this;
        }

        public ChannelEditor Customize(Action<ChannelResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<ChannelEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}