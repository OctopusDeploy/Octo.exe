using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IFeaturesConfigurationRepository
    {
        Task<FeaturesConfigurationResource> GetFeaturesConfiguration(CancellationToken token = default);
        Task<FeaturesConfigurationResource> ModifyFeaturesConfiguration(FeaturesConfigurationResource resource, CancellationToken token = default);
    }

    class FeaturesConfigurationRepository : IFeaturesConfigurationRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public FeaturesConfigurationRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<FeaturesConfigurationResource> GetFeaturesConfiguration(CancellationToken token = default)
        {
            return await repository.Client.Get<FeaturesConfigurationResource>(await repository.Link("FeaturesConfiguration").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public async Task<FeaturesConfigurationResource> ModifyFeaturesConfiguration(FeaturesConfigurationResource resource, CancellationToken token = default)
        {
            return await repository.Client.Update(await repository.Link("FeaturesConfiguration").ConfigureAwait(false), resource, token: token).ConfigureAwait(false);
        }
    }
}
