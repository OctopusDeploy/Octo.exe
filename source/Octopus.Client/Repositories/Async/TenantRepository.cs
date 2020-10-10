using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ITenantRepository : ICreate<TenantResource>, IModify<TenantResource>, IGet<TenantResource>, IDelete<TenantResource>, IFindByName<TenantResource>, IGetAll<TenantResource>
    {
        Task<MultiTenancyStatusResource> Status(CancellationToken token = default);
        Task SetLogo(TenantResource tenant, string fileName, Stream contents, CancellationToken token = default);
        Task<TenantVariableResource> GetVariables(TenantResource tenant, CancellationToken token = default);
        Task<TenantVariableResource> ModifyVariables(TenantResource tenant, TenantVariableResource variables, CancellationToken token = default);
        Task<List<TenantsMissingVariablesResource>> GetMissingVariables(string tenantId = null, string projectId = null, string environmentId = null, CancellationToken token = default);
        Task<List<TenantResource>> FindAll(string name, string[] tags = null, int pageSize = Int32.MaxValue, CancellationToken token = default);
        Task<TenantEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<TenantEditor> CreateOrModify(string name, string description, CancellationToken token = default);
        Task<TenantEditor> CreateOrModify(string name, string description, string cloneId, CancellationToken token = default);
    }

    class TenantRepository : BasicRepository<TenantResource>, ITenantRepository
    {
        public TenantRepository(IOctopusAsyncRepository repository)
            : base(repository, "Tenants")
        {
        }

        public Task<TenantVariableResource> GetVariables(TenantResource tenant, CancellationToken token = default)
        {
            return Client.Get<TenantVariableResource>(tenant.Link("Variables"), token: token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tags"></param>
        /// <param name="pageSize">Number of items per page, setting to less than the total items still retreives all items, but uses multiple requests reducing memory load on the server</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        public async Task<List<TenantResource>> FindAll(string name, string[] tags, int pageSize = int.MaxValue, CancellationToken token = default)
        {
            return await Client.Get<List<TenantResource>>(await Repository.Link("Tenants").ConfigureAwait(false), new { id = IdValueConstant.IdAll, name, tags, take = pageSize }, token).ConfigureAwait(false);
        }

        public Task<TenantVariableResource> ModifyVariables(TenantResource tenant, TenantVariableResource variables, CancellationToken token = default)
        {
            return Client.Post<TenantVariableResource, TenantVariableResource>(tenant.Link("Variables"), variables, token: token);
        }

        public async Task<List<TenantsMissingVariablesResource>> GetMissingVariables(string tenantId = null, string projectId = null, string environmentId = null, CancellationToken token = default)
        {
            return await Client.Get<List<TenantsMissingVariablesResource>>(await Repository.Link("TenantsMissingVariables").ConfigureAwait(false), new
            {
                tenantId = tenantId,
                projectId = projectId,
                environmentId = environmentId
            }, token).ConfigureAwait(false);
        }

        public async Task<MultiTenancyStatusResource> Status(CancellationToken token = default)
        {
            return await Client.Get<MultiTenancyStatusResource>(await Repository.Link("TenantsStatus").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public Task SetLogo(TenantResource tenant, string fileName, Stream contents, CancellationToken token = default)
        {
            return Client.Post(tenant.Link("Logo"), new FileUpload { Contents = contents, FileName = fileName }, false, token);
        }

        public Task<TenantEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new TenantEditor(this).CreateOrModify(name, token);
        }

        public Task<TenantEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new TenantEditor(this).CreateOrModify(name, description, token: token);
        }
        
        public Task<TenantEditor> CreateOrModify(string name, string description, string cloneId, CancellationToken token = default)
        {
            return new TenantEditor(this).CreateOrModify(name, description, cloneId, token);
        }
    }
}
