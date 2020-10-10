using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IServerStatusRepository
    {
        Task<ServerStatusResource> GetServerStatus(CancellationToken token = default);
        Task<SystemInfoResource> GetSystemInfo(ServerStatusResource status, CancellationToken token = default);
        Task<ServerStatusHealthResource> GetServerHealth(CancellationToken token = default);
    }

    class ServerStatusRepository : BasicRepository<ServerStatusResource>, IServerStatusRepository
    {
        public ServerStatusRepository(IOctopusAsyncRepository repository)
            : base(repository, "") // Not a collection
        {
        }

        public async Task<ServerStatusResource> GetServerStatus(CancellationToken token = default)
        {
            return await Client.Get<ServerStatusResource>(await Repository.Link("ServerStatus").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public Task<SystemInfoResource> GetSystemInfo(ServerStatusResource status, CancellationToken token = default)
        {
            if (status == null) throw new ArgumentNullException("status");
            return Client.Get<SystemInfoResource>(status.Link("SystemInfo"), token: token);
        }

        public async Task<ServerStatusHealthResource> GetServerHealth(CancellationToken token = default)
        {
            return await Client.Get<ServerStatusHealthResource>(await Repository.Link("ServerHealthStatus").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }
    }
}
