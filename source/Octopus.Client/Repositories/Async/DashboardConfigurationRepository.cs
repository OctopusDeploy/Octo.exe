using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IDashboardConfigurationRepository
    {
        Task<DashboardConfigurationResource> GetDashboardConfiguration(CancellationToken token = default);
        Task<DashboardConfigurationResource> ModifyDashboardConfiguration(DashboardConfigurationResource resource, CancellationToken token = default);
    }

    class DashboardConfigurationRepository : IDashboardConfigurationRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public DashboardConfigurationRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<DashboardConfigurationResource> GetDashboardConfiguration(CancellationToken token = default)
        {
            return await repository.Client.Get<DashboardConfigurationResource>(await repository.Link("DashboardConfiguration").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public async Task<DashboardConfigurationResource> ModifyDashboardConfiguration(DashboardConfigurationResource resource, CancellationToken token = default)
        {
            return await repository.Client.Update(await repository.Link("DashboardConfiguration").ConfigureAwait(false), resource, token: token).ConfigureAwait(false);
        }
    }
}
