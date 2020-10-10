using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Extensibility;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IConfigurationRepository
    {
        Task<T> Get<T>(CancellationToken token = default) where T : class, IResource, new();
        Task<T> Modify<T>(T configurationResource, CancellationToken token = default) where T : class, IResource, new();
    }

    class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IOctopusAsyncRepository repository;
        private readonly string collectionLinkName;

        public ConfigurationRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
            this.collectionLinkName = "Configuration";
        }

        public async Task<T> Get<T>(CancellationToken token = default) where T : class, IResource, new()
        {
            var instance = new T();
            var configurationItem = await GetConfigurationItem(instance, token).ConfigureAwait(false);

            return await repository.Client.Get<T>(configurationItem.Link("Values"), token: token).ConfigureAwait(false);
        }

        public async Task<T> Modify<T>(T configurationResource, CancellationToken token = default) where T : class, IResource, new()
        {
            var configurationItem = await GetConfigurationItem(configurationResource, token).ConfigureAwait(false);
            return await repository.Client.Update(configurationItem.Link("Values"), configurationResource, token: token).ConfigureAwait(false);
        }

        private async Task<ConfigurationItemResource> GetConfigurationItem(IResource instance, CancellationToken token)
        {
            return await repository.Client.Get<ConfigurationItemResource>(await repository.Link(collectionLinkName).ConfigureAwait(false), new { instance.Id }, token).ConfigureAwait(false);
        }
    }
}
