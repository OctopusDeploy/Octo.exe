using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IMachinePolicyRepository : IFindByName<MachinePolicyResource>, ICreate<MachinePolicyResource>, IModify<MachinePolicyResource>, IGet<MachinePolicyResource>, IDelete<MachinePolicyResource>
    {
        Task<List<MachineResource>> GetMachines(MachinePolicyResource machinePolicy, CancellationToken token = default);
        Task<MachinePolicyResource> GetTemplate(CancellationToken token = default);

    }

    class MachinePolicyRepository : BasicRepository<MachinePolicyResource>, IMachinePolicyRepository
    {
        public MachinePolicyRepository(IOctopusAsyncRepository repository) : base(repository, "MachinePolicies")
        {
        }

        public async Task<List<MachineResource>> GetMachines(MachinePolicyResource machinePolicy, CancellationToken token = default)
        {
            var resources = new List<MachineResource>();

            await Client.Paginate<MachineResource>(machinePolicy.Link("Machines"), new { }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public async Task<MachinePolicyResource> GetTemplate(CancellationToken token = default)
        {
            var link = await Repository.Link("MachinePolicyTemplate").ConfigureAwait(false);
            return await Client.Get<MachinePolicyResource>(link, token: token).ConfigureAwait(false);
        }
    }
}
