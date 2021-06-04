using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IFeedRepository : ICreate<FeedResource>, IModify<FeedResource>, IDelete<FeedResource>, IGet<FeedResource>, IFindByName<FeedResource>
    {
        Task<List<PackageResource>> GetVersions(FeedResource feed, string[] packageIds);
    }

    class FeedRepository : BasicRepository<FeedResource>, IFeedRepository
    {
        public FeedRepository(IOctopusAsyncRepository repository) : base(repository, "Feeds")
        {
        }

        public Task<List<PackageResource>> GetVersions(FeedResource feed, string[] packageIds)
        {
            return Client.Get<List<PackageResource>>(feed.Link("VersionsTemplate"), new { packageIds = packageIds });
        }
    }
}
