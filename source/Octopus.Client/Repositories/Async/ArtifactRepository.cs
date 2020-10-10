using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Extensibility;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IArtifactRepository :
        IPaginate<ArtifactResource>,
        IGet<ArtifactResource>,
        ICreate<ArtifactResource>,
        IModify<ArtifactResource>,
        IDelete<ArtifactResource>
    {
        Task<Stream> GetContent(ArtifactResource artifact, CancellationToken token = default);
        Task PutContent(ArtifactResource artifact, Stream contentStream, CancellationToken token = default);
        Task<ResourceCollection<ArtifactResource>> FindRegarding(IResource resource, CancellationToken token = default);
    }

    class ArtifactRepository : BasicRepository<ArtifactResource>, IArtifactRepository
    {
        public ArtifactRepository(IOctopusAsyncRepository repository)
            : base(repository, "Artifacts")
        {
        }

        public Task<Stream> GetContent(ArtifactResource artifact, CancellationToken token = default)
        {
            return Client.GetContent(artifact.Link("Content"), token: token);
        }

        public Task PutContent(ArtifactResource artifact, Stream contentStream, CancellationToken token = default)
        {
            return Client.PutContent(artifact.Link("Content"), contentStream, token);
        }

        public async Task<ResourceCollection<ArtifactResource>> FindRegarding(IResource resource, CancellationToken token = default)
        {
            return await Client.List<ArtifactResource>(await Repository.Link("Artifacts").ConfigureAwait(false), new { regarding = resource.Id }, token).ConfigureAwait(false);
        }
    }
}
