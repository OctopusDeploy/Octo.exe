using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IRunbookSnapshotRepository : IGet<RunbookSnapshotResource>, ICreate<RunbookSnapshotResource>, IPaginate<RunbookSnapshotResource>, IModify<RunbookSnapshotResource>, IDelete<RunbookSnapshotResource>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runbookSnapshot"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<RunbookRunResource>> GetRunbookRuns(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null, CancellationToken token = default);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runbookSnapshot"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null, CancellationToken token = default);
        Task<RunbookRunTemplateResource> GetTemplate(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default);
        Task<RunbookRunPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default);
        Task<RunbookSnapshotResource> SnapshotVariables(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default);
        Task<RunbookSnapshotResource> Create(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default);
    }

    class RunbookSnapshotRepository : BasicRepository<RunbookSnapshotResource>, IRunbookSnapshotRepository
    {
        public RunbookSnapshotRepository(IOctopusAsyncRepository repository)
            : base(repository, "RunbookSnapshots")
        {
        }

        public Task<ResourceCollection<RunbookRunResource>> GetRunbookRuns(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return Client.List<RunbookRunResource>(runbookSnapshot.Link("RunbookRuns"), new { skip, take }, token);
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(RunbookSnapshotResource runbookSnapshot, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return Client.List<ArtifactResource>(runbookSnapshot.Link("Artifacts"), new { skip, take }, token);
        }

        public Task<RunbookRunTemplateResource> GetTemplate(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default)
        {
            return Client.Get<RunbookRunTemplateResource>(runbookSnapshot.Link("RunbookRunTemplate"), token: token);
        }

        public Task<RunbookRunPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default)
        {
            return Client.Get<RunbookRunPreviewResource>(promotionTarget.Link("RunbookRunPreview"), token: token);
        }

        public async Task<RunbookSnapshotResource> SnapshotVariables(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default)
        {
            await Client.Post(runbookSnapshot.Link("SnapshotVariables"), token: token).ConfigureAwait(false);
            return await Get(runbookSnapshot.Id, token).ConfigureAwait(false);
        }

        public async Task<RunbookSnapshotResource> Create(RunbookSnapshotResource runbookSnapshot, CancellationToken token = default)
        {
            return await Client.Create(await Repository.Link(CollectionLinkName).ConfigureAwait(false), runbookSnapshot, token: token).ConfigureAwait(false);
        }
    }
}
