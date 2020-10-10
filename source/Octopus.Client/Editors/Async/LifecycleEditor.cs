using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class LifecycleEditor : IResourceEditor<LifecycleResource, LifecycleEditor>
    {
        private readonly ILifecyclesRepository repository;

        public LifecycleEditor(ILifecyclesRepository repository)
        {
            this.repository = repository;
        }

        public LifecycleResource Instance { get; private set; }

        public async Task<LifecycleEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new LifecycleResource
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

        public async Task<LifecycleEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new LifecycleResource
                {
                    Name = name,
                    Description = description
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                existing.Description = description;

                Instance = await repository.Modify(existing, token: token).ConfigureAwait(false);
            }

            return this;
        }

        public PhaseResource AddOrUpdatePhase(string name)
        {
            return Instance.AddOrUpdatePhase(name);
        }

        public LifecycleEditor AsSimplePromotionLifecycle(IEnumerable<EnvironmentResource> environments)
        {
            Clear();

            foreach (var environment in environments)
            {
                AddOrUpdatePhase(environment.Name).WithOptionalDeploymentTargets(environment);
            }

            return this;
        }

        public LifecycleEditor Clear()
        {
            Instance.Clear();
            return this;
        }

        public LifecycleEditor Customize(Action<LifecycleResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<LifecycleEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}