using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IReleaseRepository : IGet<ReleaseResource>, ICreate<ReleaseResource>, IPaginate<ReleaseResource>, IModify<ReleaseResource>, IDelete<ReleaseResource>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null, CancellationToken token = default);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null, CancellationToken token = default);
        Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release, CancellationToken token = default);
        Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default);
        Task<ReleaseResource> SnapshotVariables(ReleaseResource release, CancellationToken token = default);    
        Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false, CancellationToken token = default);
        Task<LifecycleProgressionResource> GetProgression(ReleaseResource release, CancellationToken token = default);
    }

    class ReleaseRepository : BasicRepository<ReleaseResource>, IReleaseRepository
    {
        public ReleaseRepository(IOctopusAsyncRepository repository)
            : base(repository, "Releases")
        {
        }

        public Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return Client.List<DeploymentResource>(release.Link("Deployments"), new { skip, take }, token);
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null, CancellationToken token = default)
        {
            return Client.List<ArtifactResource>(release.Link("Artifacts"), new { skip, take }, token);
        }

        public Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release, CancellationToken token = default)
        {
            return Client.Get<DeploymentTemplateResource>(release.Link("DeploymentTemplate"), token: token);
        }

        public Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default)
        {
            return Client.Get<DeploymentPreviewResource>(promotionTarget.Link("Preview"), token: token);
        }

        public async Task<ReleaseResource> SnapshotVariables(ReleaseResource release, CancellationToken token = default)
        {
            await Client.Post(release.Link("SnapshotVariables"), token: token).ConfigureAwait(false);
            return await Get(release.Id, token).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false, CancellationToken token = default)
        {
            return await Client.Create(await Repository.Link(CollectionLinkName).ConfigureAwait(false), release, new { ignoreChannelRules }, token).ConfigureAwait(false);
        }
        
        public Task<LifecycleProgressionResource> GetProgression(ReleaseResource release, CancellationToken token = default)
        {
            return Client.Get<LifecycleProgressionResource>(release.Links["Progression"], token: token);
        }
    }
}
