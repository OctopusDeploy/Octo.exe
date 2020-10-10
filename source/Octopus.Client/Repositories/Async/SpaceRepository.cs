using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ISpaceRepository :
        ICreate<SpaceResource>,
        IModify<SpaceResource>,
        IDelete<SpaceResource>,
        IFindByName<SpaceResource>,
        IGet<SpaceResource>
    {
        Task SetLogo(SpaceResource space, string fileName, Stream contents, CancellationToken token = default);
    }

    class SpaceRepository : BasicRepository<SpaceResource>, ISpaceRepository
    {
        public SpaceRepository(IOctopusAsyncRepository repository) : base(repository, "Spaces")
        {
        }

        public Task SetLogo(SpaceResource space, string fileName, Stream contents, CancellationToken token = default)
        {
            return Client.Post(space.Link("Logo"), new FileUpload { Contents = contents, FileName = fileName }, false, token);
        }
    }
}
