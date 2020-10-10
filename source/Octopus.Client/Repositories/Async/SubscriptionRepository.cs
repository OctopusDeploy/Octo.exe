using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ISubscriptionRepository : 
        IFindByName<SubscriptionResource>, 
        ICreate<SubscriptionResource>, 
        IModify<SubscriptionResource>, 
        IGet<SubscriptionResource>, 
        IDelete<SubscriptionResource>
    {
        Task<SubscriptionEditor> CreateOrModify(string name, EventNotificationSubscription eventNotificationSubscription, bool isDisabled, CancellationToken token = default);
    }

    class SubscriptionRepository : BasicRepository<SubscriptionResource>, ISubscriptionRepository
    {
        public SubscriptionRepository(IOctopusAsyncRepository repository) : base(repository, "Subscriptions")
        {
        }

        public Task<SubscriptionEditor> CreateOrModify(string name, EventNotificationSubscription eventNotificationSubscription, bool isDisabled, CancellationToken token = default)
        {
            return new SubscriptionEditor(this).CreateOrModify(name, eventNotificationSubscription, isDisabled, token);
        }

    }
}