using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model.Accounts;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class AzureSubscriptionAccountEditor : AccountEditor<AzureSubscriptionAccountResource, AzureSubscriptionAccountEditor>
    {
        public AzureSubscriptionAccountEditor(IAccountRepository repository) : base(repository)
        {
        }

        public Task<List<AzureStorageAccount>> StorageAccounts(AzureSubscriptionAccountResource account, CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureStorageAccount>>(account.Link("StorageAccounts"), token: token);
        }

        public Task<List<AzureSubscriptionAccountResource.WebSite>> WebSites(AzureSubscriptionAccountResource account, CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureSubscriptionAccountResource.WebSite>>(account.Link("WebSites"), token: token);
        }

        public Task<List<AzureSubscriptionAccountResource.WebSite>> WebSites(CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureSubscriptionAccountResource.WebSite>>(Instance.Link("WebSites"), token: token);
        }

        public Task<List<AzureSubscriptionAccountResource.WebSlot>> WebSlots(AzureSubscriptionAccountResource account,
            AzureSubscriptionAccountResource.WebSite site, CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureSubscriptionAccountResource.WebSlot>>(account.Link("WebSlots"),
                new { resourceGroupName = site.ResourceGroup, webSiteName = site.WebSpace}, token);
        }

        public Task<List<AzureSubscriptionAccountResource.WebSlot>> WebSlots(AzureSubscriptionAccountResource.WebSite site, CancellationToken token = default)
        {
            return Repository.Client.Get<List<AzureSubscriptionAccountResource.WebSlot>>(Instance.Link("WebSlots"),
                new { resourceGroupName = site.ResourceGroup, webSiteName = site.WebSpace }, token: token);
        }
    }
}