using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IVariableSetRepository : IGet<VariableSetResource>, IModify<VariableSetResource>, IGetAll<VariableSetResource>
    {
        Task<string[]> GetVariableNames(string projects, string[] environments, CancellationToken token = default);
        Task<VariableSetResource> GetVariablePreview(string project, string channel, string tenant, string runbook, string action, string environment, string machine, string role, CancellationToken token = default);
    }

    class VariableSetRepository : BasicRepository<VariableSetResource>, IVariableSetRepository
    {
        public VariableSetRepository(IOctopusAsyncRepository repository)
            : base(repository, "Variables")
        {
        }

        public async Task<string[]> GetVariableNames(string project, string[] environments, CancellationToken token = default)
        {
            return await Client.Get<string[]>(await Repository.Link("VariableNames").ConfigureAwait(false), new { project, projectEnvironmentsFilter = environments ?? new string[0] }, token).ConfigureAwait(false);
        }

        public async Task<VariableSetResource> GetVariablePreview(string project, string channel, string tenant, string runbook, string action, string environment, string machine, string role, CancellationToken token = default)
        {
            return await Client.Get<VariableSetResource>(await Repository.Link("VariablePreview").ConfigureAwait(false), new { project, channel, tenant, runbook, action, environment, machine, role }, token).ConfigureAwait(false);
        }

        public override Task<List<VariableSetResource>> Get(CancellationToken token = default, params string[] ids)
        {
            throw new NotSupportedException("VariableSet does not support this operation");
        }
    }
}
