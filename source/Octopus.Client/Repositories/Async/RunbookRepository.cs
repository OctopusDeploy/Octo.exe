using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IRunbookRepository : IFindByName<RunbookResource>, IGet<RunbookResource>, ICreate<RunbookResource>, IModify<RunbookResource>, IDelete<RunbookResource>
    {
        Task<RunbookResource> FindByName(ProjectResource project, string name, CancellationToken token = default);
        Task<RunbookEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default);
        Task<RunbookSnapshotTemplateResource> GetRunbookSnapshotTemplate(RunbookResource runbook, CancellationToken token = default);
        Task<RunbookRunTemplateResource> GetRunbookRunTemplate(RunbookResource runbook, CancellationToken token = default);
        Task<RunbookRunPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default);
        Task<RunbookRunResource> Run(RunbookResource runbook, RunbookRunResource runbookRun, CancellationToken token = default);
        Task<RunbookRunResource[]> Run(RunbookResource runbook, RunbookRunParameters runbookRunParameters, CancellationToken token = default);
    }

    class RunbookRepository : BasicRepository<RunbookResource>, IRunbookRepository
    {
        private readonly SemanticVersion integrationTestVersion;
        private readonly SemanticVersion versionAfterWhichRunbookRunParametersAreAvailable;

        public RunbookRepository(IOctopusAsyncRepository repository)
            : base(repository, "Runbooks")
        {
            integrationTestVersion = SemanticVersion.Parse("0.0.0-local");
            versionAfterWhichRunbookRunParametersAreAvailable = SemanticVersion.Parse("2020.3.1");
        }

        public Task<RunbookResource> FindByName(ProjectResource project, string name, CancellationToken token = default)
        {
            return FindByName(name, path: project.Link("Runbooks"), token: token);
        }

        public Task<RunbookEditor> CreateOrModify(ProjectResource project, string name, string description, CancellationToken token = default)
        {
            return new RunbookEditor(this, new RunbookProcessRepository(Repository)).CreateOrModify(project, name, description, token);
        }

        public Task<RunbookSnapshotTemplateResource> GetRunbookSnapshotTemplate(RunbookResource runbook, CancellationToken token = default)
        {
            return Client.Get<RunbookSnapshotTemplateResource>(runbook.Link("RunbookSnapshotTemplate"), token: token);
        }

        public Task<RunbookRunTemplateResource> GetRunbookRunTemplate(RunbookResource runbook, CancellationToken token = default)
        {
            return Client.Get<RunbookRunTemplateResource>(runbook.Link("RunbookRunTemplate"), token: token);
        }

        public Task<RunbookRunPreviewResource> GetPreview(DeploymentPromotionTarget promotionTarget, CancellationToken token = default)
        {
            return Client.Get<RunbookRunPreviewResource>(promotionTarget.Link("RunbookRunPreview"), token: token);
        }

        private bool ServerSupportsRunbookRunParameters(string version)
        {
            var serverVersion = SemanticVersion.Parse(version);

            // Note: We want to ensure the server version is >= *any* 2020.3.1, including all pre-releases to consider what may be rolled out to Octopus Cloud.
            var preReleaseAgnosticServerVersion =
                new SemanticVersion(serverVersion.Major, serverVersion.Minor, serverVersion.Patch);

            return preReleaseAgnosticServerVersion >= versionAfterWhichRunbookRunParametersAreAvailable ||
                   serverVersion == integrationTestVersion;
        }

        public async Task<RunbookRunResource> Run(RunbookResource runbook, RunbookRunResource runbookRun, CancellationToken token = default)
        {
            var serverSupportsRunbookRunParameters = ServerSupportsRunbookRunParameters((await Repository.LoadRootDocument()).Version);

            return serverSupportsRunbookRunParameters
                ? (await Run(runbook, RunbookRunParameters.MapFrom(runbookRun))).FirstOrDefault()
                : await Client.Post<object, RunbookRunResource>(runbook.Link("CreateRunbookRun"), runbookRun, token: token);
        }

        public async Task<RunbookRunResource[]> Run(RunbookResource runbook, RunbookRunParameters runbookRunParameters, CancellationToken token = default)
        {
            var serverVersion = (await Repository.LoadRootDocument()).Version;
            var serverSupportsRunbookRunParameters = ServerSupportsRunbookRunParameters(serverVersion);

            if (serverSupportsRunbookRunParameters == false)
                throw new UnsupportedApiVersionException($"This Octopus Deploy server is an older version ({serverVersion}) that does not yet support RunbookRunParameters. " +
                                                         $"Please update your Octopus Deploy server to 2020.3.* or newer to access this feature.");

            return await Client.Post<object, RunbookRunResource[]>(runbook.Link("CreateRunbookRun"), runbookRunParameters, token);
        }
    }
}
