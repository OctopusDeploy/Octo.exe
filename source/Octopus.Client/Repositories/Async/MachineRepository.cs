using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;

namespace Octopus.Client.Repositories.Async
{
    public interface IMachineRepository : IFindByName<MachineResource>, IGet<MachineResource>, ICreate<MachineResource>, IModify<MachineResource>, IDelete<MachineResource>
    {
        Task<MachineResource> Discover(string host, int port = 10933, DiscoverableEndpointType? discoverableEndpointType = null, CancellationToken token = default);
        Task<MachineResource> Discover(DiscoverMachineOptions options, CancellationToken token = default);
        Task<MachineConnectionStatus> GetConnectionStatus(MachineResource machine, CancellationToken token = default);
        Task<List<MachineResource>> FindByThumbprint(string thumbprint, CancellationToken token = default);
        Task<IReadOnlyList<TaskResource>> GetTasks(MachineResource machine, CancellationToken token = default);
        Task<IReadOnlyList<TaskResource>> GetTasks(MachineResource machine, object pathParameters, CancellationToken token = default);

        Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles,
            TenantResource[] tenants,
            TagResource[] tenantTags,
            TenantedDeploymentMode? tenantedDeploymentParticipation, 
            CancellationToken token = default);

        Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles, 
            CancellationToken token = default);

        Task<ResourceCollection<MachineResource>> List(int skip = 0,
            int? take = null,
            string ids = null,
            string name = null,
            string partialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null,
            string environmentIds = null, 
            CancellationToken token = default);
    }

    class MachineRepository : BasicRepository<MachineResource>, IMachineRepository
    {
        public MachineRepository(IOctopusAsyncRepository repository) : base(repository, "Machines")
        {
        }

        public Task<MachineResource> Discover(string host, int port = 10933, DiscoverableEndpointType? type = null, CancellationToken token = default)
            => Discover(new DiscoverMachineOptions(host)
            {
                Port = port,
                Type = type
            }, token);

        public async Task<MachineResource> Discover(DiscoverMachineOptions options, CancellationToken token = default)
            => await Client.Get<MachineResource>(await Repository.Link("DiscoverMachine").ConfigureAwait(false), new
            {
                host = options.Host,
                port = options.Port,
                type = options.Type,
                proxyId = options.Proxy?.Id
            }, token).ConfigureAwait(false);

        public Task<MachineConnectionStatus> GetConnectionStatus(MachineResource machine, CancellationToken token = default)
        {
            if (machine == null) throw new ArgumentNullException("machine");
            return Client.Get<MachineConnectionStatus>(machine.Link("Connection"), token: token);
        }

        public async Task<List<MachineResource>> FindByThumbprint(string thumbprint, CancellationToken token = default)
        {
            if (thumbprint == null) throw new ArgumentNullException("thumbprint");
            return await Client.Get<List<MachineResource>>(await Repository.Link("machines").ConfigureAwait(false), new { id = IdValueConstant.IdAll, thumbprint }, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all tasks involving the specified machine
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        public Task<IReadOnlyList<TaskResource>> GetTasks(MachineResource machine, CancellationToken token = default) => GetTasks(machine, new {  skip = 0 }, token);

        /// <summary>
        /// Gets all tasks associated with this machine
        /// 
        /// The `take` pathParmeter is only respected in Octopus 4.0.6 and later
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="pathParameters"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        public async Task<IReadOnlyList<TaskResource>> GetTasks(MachineResource machine, object pathParameters, CancellationToken token = default)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));

            return await Client.ListAll<TaskResource>(machine.Link("TasksTemplate"), pathParameters, token).ConfigureAwait(false);
        }

        public Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles,
            TenantResource[] tenants,
            TagResource[] tenantTags,
            TenantedDeploymentMode? tenantedDeploymentParticipation,
            CancellationToken token = default)
        {
            return new MachineEditor(this).CreateOrModify(name, endpoint, environments, roles, tenants, tenantTags, tenantedDeploymentParticipation, token);
        }

        public Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles,
            CancellationToken token = default)
        {
            return new MachineEditor(this).CreateOrModify(name, endpoint, environments, roles, token);
        }

        public async Task<ResourceCollection<MachineResource>> List(int skip = 0,
            int? take = null,
            string ids = null,
            string name = null,
            string partialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null,
            string environmentIds = null,
            CancellationToken token = default)
        {
            return await Client.List<MachineResource>(await Repository.Link("Machines").ConfigureAwait(false), new
            {
                skip,
                take,
                ids,
                name,
                partialName,
                roles,
                isDisabled,
                healthStatuses,
                commStyles,
                tenantIds,
                tenantTags,
                environmentIds,
            }, token).ConfigureAwait(false);
        }
    }
}
