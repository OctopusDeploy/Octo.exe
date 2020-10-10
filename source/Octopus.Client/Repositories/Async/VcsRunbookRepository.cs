using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IVcsRunbookRepository
    {
        Task<VcsRunbookResource> Get(string runbookId, CancellationToken token = default);
        Task<VcsRunbookResource> Modify(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default);
        Task<VcsRunbookResource> Create(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default);
        Task Delete(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default);
        Task<ResourceCollection<VcsRunbookResource>> List(string partialName = null, int skip = 0, int? take = null, CancellationToken token = default);
    }

    public class VcsRunbookRepository : IVcsRunbookRepository
    {
        private const string RunbookLinkId = "Runbook";
        private readonly IOctopusAsyncRepository repository;
        private readonly VersionControlBranchResource branch;

        public VcsRunbookRepository(IOctopusAsyncRepository repository, VersionControlBranchResource branch)
        {
            this.repository = repository;
            this.branch = branch;
        }

        public Task<VcsRunbookResource> Get(string runbookId, CancellationToken token = default)
        {
            var uri = branch.Link(RunbookLinkId);
            return repository.Client.Get<VcsRunbookResource>(uri, new { id = runbookId }, token);
        }

        public Task<VcsRunbookResource> Modify(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default)
        {
            var uri = resource.Link("Self");
            var resourceWithCommit = new CommitResource<VcsRunbookResource>
            {
                CommitMessage = commitMessage,
                Resource = resource
            };
            repository.Client.Put(uri, resourceWithCommit, token: token);
            return repository.Client.Get<VcsRunbookResource>(uri, token: token);
        }

        public Task<VcsRunbookResource> Create(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default)
        {
            var uri = branch.Link(RunbookLinkId);
            var resourceWithCommit = new CommitResource<VcsRunbookResource>
            {
                CommitMessage = commitMessage,
                Resource = resource
            };
            return repository.Client.Post<CommitResource<VcsRunbookResource>, VcsRunbookResource>(uri, resourceWithCommit, token: token);
        }

        public Task Delete(VcsRunbookResource resource, string commitMessage = null, CancellationToken token = default)
        {
            var uri = branch.Link(RunbookLinkId);
            var commit = new CommitResource
            {
                CommitMessage = commitMessage
            };
            return repository.Client.Delete(uri, new { id = resource.Id }, commit, token);
        }

        public Task<ResourceCollection<VcsRunbookResource>> List(string partialName = null, int skip = 0, int? take = null, CancellationToken token = default)
        {
            var uri = branch.Link(RunbookLinkId);
            return repository.Client.List<VcsRunbookResource>(uri, new { skip, take, partialName }, token);
        }
    }
}