using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IVariableSetRepository : IGet<VariableSetResource>, IModify<VariableSetResource>
    {
        Task<string[]> GetVariableNames(string projects, string[] environments);
    }

    class VariableSetRepository : BasicRepository<VariableSetResource>, IVariableSetRepository
    {
        public VariableSetRepository(IOctopusAsyncRepository repository)
            : base(repository, "Variables")
        {
        }

        public async Task<string[]> GetVariableNames(string project, string[] environments)
        {
            return await Client.Get<string[]>(await Repository.Link("VariableNames").ConfigureAwait(false), new { project, projectEnvironmentsFilter = environments ?? new string[0] }).ConfigureAwait(false);
        }

        public override Task<List<VariableSetResource>> Get(params string[] ids)
        {
            throw new NotSupportedException("VariableSet does not support this operation");
        }
    }
}
