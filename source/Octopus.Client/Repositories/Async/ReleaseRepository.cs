using System;
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
        /// <returns></returns>
        Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="release"></param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <returns></returns>
        Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null);
        Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release);
        Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget);
        Task<ReleaseResource> SnapshotVariables(ReleaseResource release);
        Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false);
        Task<LifecycleProgressionResource> GetProgression(ReleaseResource release);
        Task<ResourceCollection<ReleaseLogResource>> GetLog(ReleaseResource release, int skip = 0, int? take = null);
    }

    class ReleaseRepository : BasicRepository<ReleaseResource>, IReleaseRepository
    {
        public ReleaseRepository(IOctopusAsyncRepository repository)
            : base(repository, "Releases")
        {
        }

        public Task<ResourceCollection<DeploymentResource>> GetDeployments(ReleaseResource release, int skip = 0, int? take = null)
        {
            return Client.List<DeploymentResource>(release.Link("Deployments"), new { skip, take });
        }

        public Task<ResourceCollection<ArtifactResource>> GetArtifacts(ReleaseResource release, int skip = 0, int? take = null)
        {
            return Client.List<ArtifactResource>(release.Link("Artifacts"), new { skip, take });
        }

        public Task<DeploymentTemplateResource> GetTemplate(ReleaseResource release)
        {
            return Client.Get<DeploymentTemplateResource>(release.Link("DeploymentTemplate"));
        }

        public Task<DeploymentPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget)
        {
            return Client.Get<DeploymentPreviewResource>(promotionTarget.Link("Preview"));
        }

        public async Task<ReleaseResource> SnapshotVariables(ReleaseResource release)
        {
            await Client.Post(release.Link("SnapshotVariables")).ConfigureAwait(false);
            return await Get(release.Id).ConfigureAwait(false);
        }

        public async Task<ReleaseResource> Create(ReleaseResource release, bool ignoreChannelRules = false)
        {
            return await Client.Create(await Repository.Link(CollectionLinkName).ConfigureAwait(false), release, new { ignoreChannelRules }).ConfigureAwait(false);
        }

        public Task<LifecycleProgressionResource> GetProgression(ReleaseResource release)
        {
            return Client.Get<LifecycleProgressionResource>(release.Links["Progression"]);
        }

        public Task<ResourceCollection<ReleaseLogResource>> GetLog(ReleaseResource release, int skip = 0, int? take = null)
        {
            return Client.List<ReleaseLogResource>(release.Link("Log"), new {skip, take});
        }
    }
}