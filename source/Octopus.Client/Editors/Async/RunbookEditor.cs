using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class RunbookEditor : IResourceEditor<RunbookResource, RunbookEditor>
    {
        private readonly IRunbookRepository repository;
        private readonly Lazy<Task<RunbookProcessEditor>> runbookProcess;

        public RunbookEditor(IRunbookRepository repository,
            IRunbookProcessRepository runbookProcessRepository)
        {
            this.repository = repository;
            runbookProcess = new Lazy<Task<RunbookProcessEditor>>(() => new RunbookProcessEditor(runbookProcessRepository).Load(Instance.RunbookProcessId));
        }

        public RunbookResource Instance { get; private set; }

        public Task<RunbookProcessEditor> RunbookProcess => runbookProcess.Value;

        public async Task<RunbookEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(project, name, token).ConfigureAwait(false);

            if (existing == null)
            {
                Instance = await repository.Create(new RunbookResource
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

        public async Task<RunbookEditor> Load(string id, CancellationToken token = default)
        {
            Instance = await repository.Get(id, token).ConfigureAwait(false);
            return this;
        }

        public RunbookEditor Customize(Action<RunbookResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<RunbookEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            if (runbookProcess.IsValueCreated)
            {
                var steps = await runbookProcess.Value.ConfigureAwait(false);
                await steps.Save(token).ConfigureAwait(false);
            }
            return this;
        }
    }
}