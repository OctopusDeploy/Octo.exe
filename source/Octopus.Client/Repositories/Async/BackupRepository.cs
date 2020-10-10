using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IBackupRepository
    {
        Task<BackupConfigurationResource> GetConfiguration(CancellationToken token = default);
        Task<BackupConfigurationResource> ModifyConfiguration(BackupConfigurationResource backupConfiguration, CancellationToken token = default);
    }

    class BackupRepository : IBackupRepository
    {
        private readonly IOctopusAsyncRepository repository;
        readonly IOctopusAsyncClient client;

        public BackupRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
            this.client = repository.Client;
        }

        public async Task<BackupConfigurationResource> GetConfiguration(CancellationToken token = default)
        {
            return await client.Get<BackupConfigurationResource>(await repository.Link("BackupConfiguration").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public Task<BackupConfigurationResource> ModifyConfiguration(BackupConfigurationResource backupConfiguration, CancellationToken token = default)
        {
            return client.Update(backupConfiguration.Link("Self"), backupConfiguration, token: token);
        }
    }
}
