using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class TagSetEditor : IResourceEditor<TagSetResource, TagSetEditor>
    {
        private readonly ITagSetRepository repository;

        public TagSetEditor(ITagSetRepository repository)
        {
            this.repository = repository;
        }

        public TagSetResource Instance { get; private set; }

        public async Task<TagSetEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new TagSetResource
                {
                    Name = name,
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Name = name;
                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public async Task<TagSetEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                Instance = await repository.Create(new TagSetResource
                {
                    Name = name,
                }, token: token).ConfigureAwait(false);
            }
            else
            {
                existing.Description = description;
                Instance = await repository.Modify(existing, token).ConfigureAwait(false);
            }

            return this;
        }

        public TagSetEditor ClearTags()
        {
            Instance.Tags.Clear();
            return this;
        }

        public TagSetEditor AddOrUpdateTag(
            string name,
            string description = null,
            string color = TagResource.StandardColor.DarkGrey)
        {
            Instance.AddOrUpdateTag(name, description, color);
            return this;
        }

        public TagSetEditor RemoveTag(string name)
        {
            Instance.RemoveTag(name);
            return this;
        }

        public TagSetEditor Customize(Action<TagSetResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<TagSetEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }
    }
}