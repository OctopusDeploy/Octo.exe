using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Repositories.Async;

namespace Octopus.Client.Editors.Async
{
    public class CertificateEditor : IResourceEditor<CertificateResource, CertificateEditor>
    {
        private readonly ICertificateRepository repository;

        public CertificateEditor(ICertificateRepository repository)
        {
            this.repository = repository;
        }

        public CertificateResource Instance { get; private set; }

        public async Task<CertificateEditor> Create(string name, string certificateData, CancellationToken token = default)
        {
            var existing = repository.FindByName(name, token: token);
            if (existing != null)
            {
                throw new ArgumentException($"A certificate with the name {name} already exists");
            }
            
            Instance = await repository.Create(new CertificateResource(name, certificateData)).ConfigureAwait(false);
            
            return this;
        }

        public async Task<CertificateEditor> FindByName(string name, CancellationToken token = default)
        {
            var existing = await repository.FindByName(name, token: token).ConfigureAwait(false);
            if (existing == null)
            {
                throw new ArgumentException($"A certificate with the name {name} could not be found");
            }
            else
            {
                Instance = existing;
            }

            return this;
        }

        public CertificateEditor Customize(Action<CertificateResource> customize)
        {
            customize?.Invoke(Instance);
            return this;
        }

        public async Task<CertificateEditor> Save(CancellationToken token = default)
        {
            Instance = await repository.Modify(Instance, token).ConfigureAwait(false);
            return this;
        }

        public async Task<CertificateUsageResource> Usages(CancellationToken token = default)
        {
            return await repository.Client.Get<CertificateUsageResource>(Instance.Link("Usages"), token: token).ConfigureAwait(false);
        }
    }
}