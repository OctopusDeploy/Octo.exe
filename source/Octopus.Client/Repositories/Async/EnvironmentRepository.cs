using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IEnvironmentRepository : IFindByName<EnvironmentResource>, IGet<EnvironmentResource>, ICreate<EnvironmentResource>, IModify<EnvironmentResource>, IDelete<EnvironmentResource>, IGetAll<EnvironmentResource>
    {
        Task<List<MachineResource>> GetMachines(EnvironmentResource environment,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null, 
            CancellationToken token = default);
        Task<EnvironmentsSummaryResource> Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null,
            bool? hideEmptyEnvironments = false, 
            CancellationToken token = default);
        Task Sort(string[] environmentIdsInOrder, CancellationToken token = default);
        Task<EnvironmentEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<EnvironmentEditor> CreateOrModify(string name, string description, CancellationToken token = default);
        Task<EnvironmentEditor> CreateOrModify(string name, string description, bool allowDynamicInfrastructure, CancellationToken token = default);
    }

    class EnvironmentRepository : BasicRepository<EnvironmentResource>, IEnvironmentRepository
    {
        public EnvironmentRepository(IOctopusAsyncRepository repository)
            : base(repository, "Environments")
        {
        }

        public async Task<List<MachineResource>> GetMachines(EnvironmentResource environment,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null,
            CancellationToken token = default)
        {
            var resources = new List<MachineResource>();

            await Client.Paginate<MachineResource>(environment.Link("Machines"), new {
                skip,
                take,
                partialName,
                roles,
                isDisabled,
                healthStatuses,
                commStyles,
                tenantIds,
                tenantTags
            }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public async Task<EnvironmentsSummaryResource> Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            string roles = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            string tenantIds = null,
            string tenantTags = null,
            bool? hideEmptyEnvironments = false,
            CancellationToken token = default)
        {
            return await Client.Get<EnvironmentsSummaryResource>(await Repository.Link("EnvironmentsSummary").ConfigureAwait(false), new
            {
                ids,
                partialName,
                machinePartialName,
                roles,
                isDisabled,
                healthStatuses,
                commStyles,
                tenantIds,
                tenantTags,
                hideEmptyEnvironments,
            }, token).ConfigureAwait(false);
        }

        public async Task Sort(string[] environmentIdsInOrder, CancellationToken token = default)
        {
            await Client.Put(await Repository.Link("EnvironmentSortOrder").ConfigureAwait(false), environmentIdsInOrder, token: token).ConfigureAwait(false);
        }

        public Task<EnvironmentEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new EnvironmentEditor(this).CreateOrModify(name, token);
        }

        public Task<EnvironmentEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new EnvironmentEditor(this).CreateOrModify(name, description, token: token);
        }

        public Task<EnvironmentEditor> CreateOrModify(string name, string description, bool allowDynamicInfrastructure, CancellationToken token = default)
        {
            return new EnvironmentEditor(this).CreateOrModify(name, description, allowDynamicInfrastructure, token);
        }
    }
}
