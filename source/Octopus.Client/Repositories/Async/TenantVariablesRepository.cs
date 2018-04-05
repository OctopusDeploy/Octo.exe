using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ITenantVariablesRepository : IGetAll<TenantVariableResource>
    {
        Task<List<TenantVariableResource>> GetAll(ProjectResource projectResource);
    }

    class TenantVariablesRepository : BasicRepository<TenantVariableResource>, ITenantVariablesRepository
    {
        public Task<List<TenantVariableResource>> GetAll(ProjectResource projectResource)
        {
            return Client.Get<List<TenantVariableResource>>(Client.RootDocument.Link("TenantVariables"), new
            {
                projectId = projectResource?.Id
            });
        }

        public TenantVariablesRepository(IOctopusAsyncClient client) 
            : base(client, "TenantVariables")
        {
        }
    }
}
