using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Editors.Async
{
    public class SubscriptionEditor : IResourceEditor<SubscriptionResource, SubscriptionEditor>
    {
        private readonly ISubscriptionRepository repository;

        public SubscriptionEditor(
           ISubscriptionRepository repository)
        {
            this.repository = repository;
        }

        public SubscriptionResource Instance { get; private set; }

        public async Task<SubscriptionEditor> CreateOrModify(string name, EventNotificationSubscription eventNotificationSubscription, bool isDisabled, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);

            if (existing == null)
            {
                Instance = await repository.Create(new SubscriptionResource
                    {
                        Name = name,
                        Type = SubscriptionType.Event,
                        IsDisabled = isDisabled,
                        EventNotificationSubscription = eventNotificationSubscription,
                    }, token: token)
                    .ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                existing.IsDisabled = isDisabled;
                existing.EventNotificationSubscription = eventNotificationSubscription;

                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public SubscriptionEditor Customize(Action<SubscriptionResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<SubscriptionEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}
