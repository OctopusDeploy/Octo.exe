using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class WorkerPoolEditor : IResourceEditor<WorkerPoolResource, WorkerPoolEditor>
    {
        private readonly IWorkerPoolRepository repository;

        public WorkerPoolEditor(IWorkerPoolRepository repository)
        {
            this.repository = repository;
        }

        public WorkerPoolResource Instance { get; private set; }

        public async Task<WorkerPoolEditor> CreateOrModify(string name, CancellationToken token)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new WorkerPoolResource
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

        public async Task<WorkerPoolEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new WorkerPoolResource
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

        public WorkerPoolEditor Customize(Action<WorkerPoolResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<WorkerPoolEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}