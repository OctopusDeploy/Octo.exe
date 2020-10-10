using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IRunbookProcessRepository : IGet<RunbookProcessResource>, IModify<RunbookProcessResource>
    {
        Task<RunbookSnapshotTemplateResource> GetTemplate(RunbookProcessResource runbookProcess, CancellationToken token = default);
    }

    class RunbookProcessRepository : BasicRepository<RunbookProcessResource>, IRunbookProcessRepository
    {
        public RunbookProcessRepository(IOctopusAsyncRepository repository)
            : base(repository, "RunbookProcesses")
        {
        }

        public Task<RunbookSnapshotTemplateResource> GetTemplate(RunbookProcessResource runbookProcess, CancellationToken token = default)
        {
            return Client.Get<RunbookSnapshotTemplateResource>(runbookProcess.Link("RunbookSnapshotTemplate"), token: token);
        }
    }
}
