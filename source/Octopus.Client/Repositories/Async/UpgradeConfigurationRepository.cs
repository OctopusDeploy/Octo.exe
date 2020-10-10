using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IUpgradeConfigurationRepository : IGet<UpgradeConfigurationResource>, IModify<UpgradeConfigurationResource>
    {
        Task<UpgradeConfigurationResource> Get(CancellationToken token = default);
    }
    class UpgradeConfigurationRepository : BasicRepository<UpgradeConfigurationResource>, IUpgradeConfigurationRepository
    {
        public UpgradeConfigurationRepository(IOctopusAsyncRepository repository) : base(repository, "UpgradeConfiguration")
        {
        }

        public async Task<UpgradeConfigurationResource> Get(CancellationToken token = default)
        {
            var link = await ResolveLink();
            var upgradeConfiguration = await Client.Get<UpgradeConfigurationResource>(link, token: token);
            return upgradeConfiguration;
        }
    }
}