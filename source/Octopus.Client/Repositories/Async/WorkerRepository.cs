using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;

namespace Octopus.Client.Repositories.Async
{
    public interface IWorkerRepository : IFindByName<WorkerResource>, IGet<WorkerResource>, ICreate<WorkerResource>, IModify<WorkerResource>, IDelete<WorkerResource>
    {
        Task<WorkerResource> Discover(string host, int port = 10933, DiscoverableEndpointType? discoverableEndpointType = null, CancellationToken token = default);
        Task<MachineConnectionStatus> GetConnectionStatus(WorkerResource machine, CancellationToken token = default);
        Task<List<WorkerResource>> FindByThumbprint(string thumbprint, CancellationToken token = default);

        Task<WorkerEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            WorkerPoolResource[] pools,
            CancellationToken token = default);

        Task<ResourceCollection<WorkerResource>> List(int skip = 0,
            int? take = null,
            string ids = null,
            string name = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string workerpoolIds = null, 
            CancellationToken token = default);
    }

    class WorkerRepository : BasicRepository<WorkerResource>, IWorkerRepository
    {
        public WorkerRepository(IOctopusAsyncRepository repository) : base(repository, "Workers")
        {
        }

        public async Task<WorkerResource> Discover(string host, int port = 10933, DiscoverableEndpointType? type = null, CancellationToken token = default)
        {
            return await Client.Get<WorkerResource>(await Repository.Link("DiscoverWorker").ConfigureAwait(false), new { host, port, type }, token).ConfigureAwait(false);
        }

        public Task<MachineConnectionStatus> GetConnectionStatus(WorkerResource worker, CancellationToken token = default)
        {
            if (worker == null) throw new ArgumentNullException("worker");
            return Client.Get<MachineConnectionStatus>(worker.Link("Connection"), token: token);
        }

        public async Task<List<WorkerResource>> FindByThumbprint(string thumbprint, CancellationToken token = default)
        {
            if (thumbprint == null) throw new ArgumentNullException("thumbprint");
            return await Client.Get<List<WorkerResource>>(await Repository.Link("Workers").ConfigureAwait(false), new { id = IdValueConstant.IdAll, thumbprint }, token).ConfigureAwait(false);
        }

        public Task<WorkerEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            WorkerPoolResource[] workerpools,
            CancellationToken token = default)
        {
            return new WorkerEditor(this).CreateOrModify(name, endpoint, workerpools, token);
        }

        public async Task<ResourceCollection<WorkerResource>> List(int skip = 0,
            int? take = null,
            string ids = null,
            string name = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string workerpoolIds = null,
            CancellationToken token = default)
        {
            return await Client.List<WorkerResource>(await Repository.Link("Workers").ConfigureAwait(false), new
            {
                skip,
                take,
                ids,
                name,
                partialName,
                isDisabled,
                healthStatuses,
                commStyles,
                workerpoolIds
            }, token).ConfigureAwait(false);
        }
    }
}
