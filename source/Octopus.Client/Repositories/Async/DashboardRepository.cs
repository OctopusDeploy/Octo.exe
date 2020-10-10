using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IDashboardRepository
    {
        Task<DashboardResource> GetDashboard(CancellationToken token = default);

        /// <param name="projects"></param>
        /// <param name="environments"></param>
        /// <param name="dashboardItemsOptions">options for DashboardResource Items property</param>
        /// <param name="token">A cancellation token</param>
        Task<DashboardResource> GetDynamicDashboard(string[] projects, string[] environments, DashboardItemsOptions dashboardItemsOptions = DashboardItemsOptions.IncludeCurrentDeploymentOnly, CancellationToken token = default);
    }

    class DashboardRepository : IDashboardRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public DashboardRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<DashboardResource> GetDashboard(CancellationToken token = default)
        {
            return await repository.Client.Get<DashboardResource>(await repository.Link("Dashboard").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public async Task<DashboardResource> GetDynamicDashboard(string[] projects, string[] environments, DashboardItemsOptions dashboardItemsOptions = DashboardItemsOptions.IncludeCurrentDeploymentOnly, CancellationToken token = default)
        {
            var includePrevious = dashboardItemsOptions == DashboardItemsOptions.IncludeCurrentAndPreviousSuccessfulDeployment;
            return await repository.Client.Get<DashboardResource>(await repository.Link("DashboardDynamic").ConfigureAwait(false), new { projects, environments, includePrevious }, token: token).ConfigureAwait(false);
        }
    }
}
