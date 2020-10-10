using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IOctopusServerNodeRepository : IModify<OctopusServerNodeResource>, IDelete<OctopusServerNodeResource>, IGet<OctopusServerNodeResource>, IFindByName<OctopusServerNodeResource>
    {
        Task<OctopusServerNodeDetailsResource> Details(OctopusServerNodeResource node, CancellationToken token = default);
        Task<OctopusServerClusterSummaryResource> Summary(CancellationToken token = default);
    }

    class OctopusServerNodeRepository : BasicRepository<OctopusServerNodeResource>, IOctopusServerNodeRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public OctopusServerNodeRepository(IOctopusAsyncRepository repository)
            : base(repository, "OctopusServerNodes")
        {
            this.repository = repository;
        }

        public async Task<OctopusServerNodeDetailsResource> Details(OctopusServerNodeResource node, CancellationToken token = default)
        {
            return await repository.Client.Get<OctopusServerNodeDetailsResource>(node.Link("Details"), token: token);
        }

        public async Task<OctopusServerClusterSummaryResource> Summary(CancellationToken token = default)
        {
            return await repository.Client.Get<OctopusServerClusterSummaryResource>(await repository.Link("OctopusServerClusterSummary").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }
    }
}
