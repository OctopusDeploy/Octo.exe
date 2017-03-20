using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;

namespace Octopus.Client.Repositories.Async
{
    public interface IMachineRepository : IFindByName<MachineResource>, IGet<MachineResource>, ICreate<MachineResource>, IModify<MachineResource>, IDelete<MachineResource>
    {
        Task<MachineResource> Discover(string host, int port = 10933, DiscoverableEndpointType? discoverableEndpointType = null);
        Task<MachineConnectionStatus> GetConnectionStatus(MachineResource machine);
        Task<List<MachineResource>> FindByThumbprint(string thumbprint);
        Task<List<TaskResource>> GetTasks(MachineResource machine, int skip = 0);

        Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles,
            TenantResource[] tenants,
            TagResource[] tenantTags);

        Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles);
    }

    class MachineRepository : BasicRepository<MachineResource>, IMachineRepository
    {
        public MachineRepository(IOctopusAsyncClient client) : base(client, "Machines")
        {
        }

        public Task<MachineResource> Discover(string host, int port = 10933, DiscoverableEndpointType? type = null)
        {
            return Client.Get<MachineResource>(Client.RootDocument.Link("DiscoverMachine"), new { host, port, type });
        }

        public Task<MachineConnectionStatus> GetConnectionStatus(MachineResource machine)
        {
            if (machine == null) throw new ArgumentNullException("machine");
            return Client.Get<MachineConnectionStatus>(machine.Link("Connection"));
        }

        public Task<List<MachineResource>> FindByThumbprint(string thumbprint)
        {
            if (thumbprint == null) throw new ArgumentNullException("thumbprint");
            return Client.Get<List<MachineResource>>(Client.RootDocument.Link("machines"), new { id = "all", thumbprint });
        }

        public async Task<List<TaskResource>> GetTasks(MachineResource machine, int skip = 0)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));

            var resources = new List<TaskResource>();

            await Client.Paginate<TaskResource>(machine.Link("TasksTemplate"), new { skip }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            }).ConfigureAwait(false);

            return resources;
        }

        public Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles,
            TenantResource[] tenants,
            TagResource[] tenantTags)
        {
            return new MachineEditor(this).CreateOrModify(name, endpoint, environments, roles, tenants, tenantTags);
        }

        public Task<MachineEditor> CreateOrModify(
            string name,
            EndpointResource endpoint,
            EnvironmentResource[] environments,
            string[] roles)
        {
            return new MachineEditor(this).CreateOrModify(name, endpoint, environments, roles);
        }
    }
}
