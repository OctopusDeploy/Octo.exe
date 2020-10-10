using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model.Accounts;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class AzureServicePrincipalAccountEditor : AccountEditor<AzureServicePrincipalAccountResource, AzureServicePrincipalAccountEditor>
    {
        public AzureServicePrincipalAccountEditor(IAccountRepository repository) : base(repository)
        {
        }

        public Task<List<AzureServicePrincipalAccountResource.ResourceGroup>> ResourceGroups(CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureServicePrincipalAccountResource.ResourceGroup>>(Instance.Link("ResourceGroups"), token: token);
        }

        public Task<List<AzureServicePrincipalAccountResource.WebSite>> WebSites(CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureServicePrincipalAccountResource.WebSite>>(Instance.Link("WebSites"), token: token);
        }

        public Task<List<AzureServicePrincipalAccountResource.WebSlot>> WebSlots(AzureServicePrincipalAccountResource.WebSite site, CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureServicePrincipalAccountResource.WebSlot>>(Instance.Link("WebSlots"),
                new {id = Instance.Id, resourceGroupName = site.ResourceGroup, webSiteName = site.WebSpace}, token);
        }

        public Task<List<AzureStorageAccount>> StorageAccounts(CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureStorageAccount>>(Instance.Link("StorageAccounts"), token: token);
        }
    }
}