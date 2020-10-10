using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ILicensesRepository
    {
        Task<LicenseResource> GetCurrent(CancellationToken token = default);
        Task<LicenseResource> UpdateCurrent(LicenseResource resource, CancellationToken token = default);
        Task<LicenseStatusResource> GetStatus(CancellationToken token = default);
    }
    
    class LicensesRepository : BasicRepository<LicenseResource>, ILicensesRepository
    {
        public LicensesRepository(IOctopusAsyncRepository repository)
            : base(repository, "CurrentLicense")
        {
        }

        public async Task<LicenseResource> GetCurrent(CancellationToken token = default)
            => await Client.Get<LicenseResource>(await Repository.Link(CollectionLinkName), token: token);

        public async Task<LicenseResource> UpdateCurrent(LicenseResource resource, CancellationToken token = default)
            => await Client.Update(await Repository.Link(CollectionLinkName), resource, token: token);

        public async Task<LicenseStatusResource> GetStatus(CancellationToken token = default)
            => await Client.Get<LicenseStatusResource>(await Repository.Link("CurrentLicenseStatus"), token: token);

    }
}