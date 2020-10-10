using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IWorkerPoolRepository : IFindByName<WorkerPoolResource>, IGet<WorkerPoolResource>, ICreate<WorkerPoolResource>, IModify<WorkerPoolResource>, IDelete<WorkerPoolResource>, IGetAll<WorkerPoolResource>
    {
        Task<List<WorkerResource>> GetMachines(WorkerPoolResource workerPool,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null, 
            CancellationToken token = default);
        Task<WorkerPoolsSummaryResource> Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            bool? hideEmptyPools = false, 
            CancellationToken token = default);
        Task Sort(string[] workerPoolIdsInOrder, CancellationToken token = default);
        Task<WorkerPoolEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<WorkerPoolEditor> CreateOrModify(string name, string description, CancellationToken token = default);
    }

    class WorkerPoolRepository : BasicRepository<WorkerPoolResource>, IWorkerPoolRepository
    {
        public WorkerPoolRepository(IOctopusAsyncRepository repository)
            : base(repository, "WorkerPools")
        {
        }

        public async Task<List<WorkerResource>> GetMachines(WorkerPoolResource workerPool,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            CancellationToken token = default)
        {
            var resources = new List<WorkerResource>();

            await Client.Paginate<WorkerResource>(workerPool.Link("Workers"), new {
                skip,
                take,
                partialName,
                isDisabled,
                healthStatuses,
                commStyles,
            }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public async Task<WorkerPoolsSummaryResource> Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            bool? hideEmptyPools = false,
            CancellationToken token = default)
        {
            return await Client.Get<WorkerPoolsSummaryResource>(await Repository.Link("WorkerPoolsSummary").ConfigureAwait(false), new
            {
                ids,
                partialName,
                machinePartialName,
                isDisabled,
                healthStatuses,
                commStyles,
                hideEmptyPools,
            }, token).ConfigureAwait(false);
        }

        public async Task Sort(string[] workerPoolIdsInOrder, CancellationToken token = default)
        {
            await Client.Put(await Repository.Link("WorkerPoolSortOrder").ConfigureAwait(false), workerPoolIdsInOrder, token: token).ConfigureAwait(false);
        }

        public Task<WorkerPoolEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new WorkerPoolEditor(this).CreateOrModify(name, token);
        }

        public Task<WorkerPoolEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new WorkerPoolEditor(this).CreateOrModify(name, description, token);
        }
    }
}
