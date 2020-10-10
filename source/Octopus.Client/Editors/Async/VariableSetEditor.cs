using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class VariableSetEditor : IResourceEditor<VariableSetResource, VariableSetEditor>
    {
        private readonly IVariableSetRepository repository;

        public VariableSetEditor(IVariableSetRepository repository)
        {
            this.repository = repository;
        }

        public VariableSetResource Instance { get; private set; }

        public async Task<VariableSetEditor> Load(string id, CancellationToken token = default)
        {
            Instance = await repository.Get(id, token).ConfigureAwait(false);
            return this;
        }

        public VariableSetEditor AddOrUpdateVariableValue(string name, string value, ScopeSpecification scope, bool isSensitive)
        {
            Instance.AddOrUpdateVariableValue(name, value, scope, isSensitive);
            return this;
        }

        public VariableSetEditor AddOrUpdateVariableValue(string name, string value, ScopeSpecification scope, bool isSensitive, string description)
        {
            Instance.AddOrUpdateVariableValue(name, value, scope, isSensitive, description);
            return this;
        }

        public VariableSetEditor AddOrUpdateVariableValue(string name, string value, ScopeSpecification scope)
        {
            Instance.AddOrUpdateVariableValue(name, value, scope);
            return this;
        }

        public VariableSetEditor AddOrUpdateVariableValue(string name, string value)
        {
            Instance.AddOrUpdateVariableValue(name, value);
            return this;
        }

        public VariableSetEditor AddOrUpdateVariableValue(string name, string value, string description)
        {
            Instance.AddOrUpdateVariableValue(name, value, description);
            return this;
        }

        public VariableSetEditor Customize(Action<VariableSetResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<VariableSetEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}