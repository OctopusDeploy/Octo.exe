using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IPerformanceConfigurationRepository
    {
        Task<PerformanceConfigurationResource> Get(CancellationToken token = default);
        Task<PerformanceConfigurationResource> Modify(PerformanceConfigurationResource resource, CancellationToken token = default);
    }

    class PerformanceConfigurationRepository : IPerformanceConfigurationRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public PerformanceConfigurationRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<PerformanceConfigurationResource> Get(CancellationToken token = default)
        {
            return await repository.Client.Get<PerformanceConfigurationResource>(await repository.Link("PerformanceConfiguration").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public async Task<PerformanceConfigurationResource> Modify(PerformanceConfigurationResource resource, CancellationToken token = default)
        {
            return await repository.Client.Update(await repository.Link("PerformanceConfiguration").ConfigureAwait(false), resource, token: token).ConfigureAwait(false);
        }
    }
}