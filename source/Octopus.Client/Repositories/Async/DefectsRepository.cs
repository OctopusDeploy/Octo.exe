using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IDefectsRepository
    {
        Task<ResourceCollection<DefectResource>> GetDefects(ReleaseResource release, CancellationToken token = default);
        Task RaiseDefect(ReleaseResource release, string description, CancellationToken token = default);
        Task ResolveDefect(ReleaseResource release, CancellationToken token = default);
    }

    class DefectsRepository : BasicRepository<DefectResource>, IDefectsRepository
    {
        public DefectsRepository(IOctopusAsyncRepository repository)
            : base(repository, "Defects")
        {
        }

        public Task<ResourceCollection<DefectResource>> GetDefects(ReleaseResource release, CancellationToken token = default)
        {
            return Client.List<DefectResource>(release.Link("Defects"), token: token);
        }

        public Task RaiseDefect(ReleaseResource release, string description, CancellationToken token = default)
        {
            return Client.Post(release.Link("ReportDefect"), new DefectResource(description), token: token);
        }

        public Task ResolveDefect(ReleaseResource release, CancellationToken token = default)
        {
            return Client.Post(release.Link("ResolveDefect"), token: token);
        }
    }
}
