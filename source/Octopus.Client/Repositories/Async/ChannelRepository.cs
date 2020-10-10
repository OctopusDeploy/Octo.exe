using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IChannelRepository : ICreate<ChannelResource>, IModify<ChannelResource>, IGet<ChannelResource>, IDelete<ChannelResource>, IPaginate<ChannelResource>
    {
        Task<ChannelResource> FindByName(ProjectResource project, string name, CancellationToken token = default);
        Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, CancellationToken token = default);
        Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default);
        Task<ResourceCollection<ReleaseResource>> GetReleases(ChannelResource channel,
            int skip = 0, int? take = null, string searchByVersion = null, CancellationToken token = default);
        Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ChannelResource channel, CancellationToken token = default);
        Task<ReleaseResource> GetReleaseByVersion(ChannelResource channel, string version, CancellationToken token = default);
    }

    class ChannelRepository : BasicRepository<ChannelResource>, IChannelRepository
    {
        public ChannelRepository(IOctopusAsyncRepository repository)
            : base(repository, "Channels")
        {
        }

        public Task<ChannelResource> FindByName(ProjectResource project, string name, CancellationToken token = default)
        {
            return FindByName(name, path: project.Link("Channels"), token: token);
        }

        public Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, CancellationToken token = default)
        {
            return new ChannelEditor(this).CreateOrModify(project, name, token);
        }

        public Task<ChannelEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default)
        {
            return new ChannelEditor(this).CreateOrModify(project, name, description, token);
        }

        public Task<ResourceCollection<ReleaseResource>> GetReleases(ChannelResource channel,
            int skip = 0, int? take = null, string searchByVersion = null, CancellationToken token = default)
        {
            return Client.List<ReleaseResource>(channel.Link("Releases"), new { skip, take, searchByVersion }, token);
        }

        public Task<IReadOnlyList<ReleaseResource>> GetAllReleases(ChannelResource channel, CancellationToken token = default)
        {
            return Client.ListAll<ReleaseResource>(channel.Link("Releases"), token: token);
        }

        public Task<ReleaseResource> GetReleaseByVersion(ChannelResource channel, string version, CancellationToken token = default)
        {
            return Client.Get<ReleaseResource>(channel.Link("Releases"), new { version }, token);
        }
    }
}
