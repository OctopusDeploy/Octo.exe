using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ICertificateConfigurationRepository : IGet<CertificateConfigurationResource>, IFindByName<CertificateConfigurationResource>
    {
        Task<CertificateConfigurationResource> GetOctopusCertificate(CancellationToken token = default);
        Task<Stream> GetPublicCertificate(CertificateConfigurationResource certificateConfiguration, CancellationToken token = default);
    }

    class CertificateConfigurationRepository : BasicRepository<CertificateConfigurationResource>, ICertificateConfigurationRepository
    {
        public CertificateConfigurationRepository(IOctopusAsyncRepository repository) : base(repository, null, async repo => await repository.HasLink("CertificateConfiguration").ConfigureAwait(false) ? "CertificateConfiguration" : "Certificates")
        {
        }

        public Task<CertificateConfigurationResource> GetOctopusCertificate(CancellationToken token = default)
        {
            return Get("certificate-global", token);
        }

        public Task<Stream> GetPublicCertificate(CertificateConfigurationResource certificateConfiguration, CancellationToken token = default)
        {
            return Client.GetContent(certificateConfiguration.Links["PublicCer"], token: token);
        }
    }
}