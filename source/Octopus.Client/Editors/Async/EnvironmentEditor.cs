using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class EnvironmentEditor : IResourceEditor<EnvironmentResource, EnvironmentEditor>
    {
        private readonly IEnvironmentRepository repository;

        public EnvironmentEditor(IEnvironmentRepository repository)
        {
            this.repository = repository;
        }

        public EnvironmentResource Instance { get; private set; }

        public async Task<EnvironmentEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new EnvironmentResource
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

        public async Task<EnvironmentEditor> CreateOrModify(string name, string description, bool allowDynamicInfrastructure = false, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new EnvironmentResource
                {
                    Name = name,
                    Description = description,
                    AllowDynamicInfrastructure = allowDynamicInfrastructure
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                existing.Description = description;
                existing.AllowDynamicInfrastructure = allowDynamicInfrastructure;

                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public async Task<EnvironmentEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new EnvironmentResource
                {
                    Name = name,
                    Description = description,
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

        public EnvironmentEditor Customize(Action<EnvironmentResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<EnvironmentEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}