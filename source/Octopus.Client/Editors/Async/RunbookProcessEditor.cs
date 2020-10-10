using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class RunbookProcessEditor : IResourceEditor<RunbookProcessResource, RunbookProcessEditor>
    {
        private readonly IRunbookProcessRepository repository;

        public RunbookProcessEditor(IRunbookProcessRepository repository)
        {
            this.repository = repository;
        }

        public RunbookProcessResource Instance { get; private set; }

        public async Task<RunbookProcessEditor> Load(string id, CancellationToken token = default)
        {
            Instance = await repository.Get(id, token).ConfigureAwait(false);
            return this;
        }

        public DeploymentStepResource FindStep(string name)
        {
            return Instance.FindStep(name);
        }

        public DeploymentStepResource AddOrUpdateStep(string name)
        {
            return Instance.AddOrUpdateStep(name);
        }

        public RunbookProcessEditor RemoveStep(string name)
        {
            Instance.RemoveStep(name);
            return this;
        }

        public RunbookProcessEditor ClearSteps()
        {
            Instance.ClearSteps();
            return this;
        }

        public RunbookProcessEditor Customize(Action<RunbookProcessResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<RunbookProcessEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}