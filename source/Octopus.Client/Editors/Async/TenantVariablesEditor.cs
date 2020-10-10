using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class TenantVariablesEditor : IResourceEditor<TenantVariableResource, TenantVariablesEditor>
    {
        private readonly ITenantRepository repository;
        private readonly TenantResource tenant;

        public TenantVariablesEditor(ITenantRepository repository, TenantResource tenant)
        {
            this.repository = repository;
            this.tenant = tenant;
        }

        public TenantVariableResource Instance { get; private set; }

        public async Task<TenantVariablesEditor> Load(CancellationToken token = default)
        {
            Instance = await repository.GetVariables(tenant, token).ConfigureAwait(false);
            return this;
        }

        public TenantVariablesEditor Customize(Action<TenantVariableResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<TenantVariablesEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.ModifyVariables(tenant, Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}