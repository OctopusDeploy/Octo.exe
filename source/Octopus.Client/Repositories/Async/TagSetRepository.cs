using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ITagSetRepository : ICreate<TagSetResource>, IModify<TagSetResource>, IGet<TagSetResource>, IDelete<TagSetResource>, IFindByName<TagSetResource>, IGetAll<TagSetResource>
    {
        Task Sort(string[] tagSetIdsInOrder, CancellationToken token = default);
        Task<TagSetEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<TagSetEditor> CreateOrModify(string name, string description, CancellationToken token = default);
    }

    class TagSetRepository : BasicRepository<TagSetResource>, ITagSetRepository
    {
        public TagSetRepository(IOctopusAsyncRepository repository) : base(repository, "TagSets")
        {
        }

        public async Task Sort(string[] tagSetIdsInOrder, CancellationToken token = default)
        {
            await Client.Put(await Repository.Link("TagSetSortOrder").ConfigureAwait(false), tagSetIdsInOrder, token: token).ConfigureAwait(false);
        }

        public Task<TagSetEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new TagSetEditor(this).CreateOrModify(name, token);
        }

        public Task<TagSetEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new TagSetEditor(this).CreateOrModify(name, description, token);
        }
    }
}
