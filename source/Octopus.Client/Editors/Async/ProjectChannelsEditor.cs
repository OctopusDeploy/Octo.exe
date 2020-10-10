using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;
using System.Threading;

namespace Octopus.Client.Editors.Async
{
    public class ProjectChannelsEditor
    {
        private readonly IChannelRepository repository;
        private readonly ProjectResource owner;
        private readonly List<ChannelEditor> trackedChannelBuilders = new List<ChannelEditor>();

        public ProjectChannelsEditor(IChannelRepository repository, ProjectResource owner)
        {
            this.repository = repository;
            this.owner = owner;
        }

        public async Task<ChannelEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            var channelBuilder = await new ChannelEditor(repository).CreateOrModify(owner, name, token).ConfigureAwait(false);
            trackedChannelBuilders.Add(channelBuilder);
            return channelBuilder;
        }

        public async Task<ChannelEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var channelBuilder = await new ChannelEditor(repository).CreateOrModify(owner, name, description, token).ConfigureAwait(false);
            trackedChannelBuilders.Add(channelBuilder);
            return channelBuilder;
        }

        public async Task<ProjectChannelsEditor> Delete(string name, CancellationToken token = default)
        {
            var channel = await repository.FindByName(owner, name, token).ConfigureAwait(false);
            if (channel != null)
                await repository.Delete(channel, token).ConfigureAwait(false);
            return this;
        }

        public async Task<ProjectChannelsEditor> SaveAll(CancellationToken token = default)
        {
            await Task.WhenAll(
                trackedChannelBuilders.Select(x => x.Save(token))
            ).ConfigureAwait(false);
            return this;
        }
    }
}