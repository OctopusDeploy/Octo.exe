﻿using System.Threading.Tasks;
using Octopus.Client.Model.PackageMetadata;

namespace Octopus.Client.Repositories.Async
{
    public class PackageMetadataRepository : IPackageMetadataRepository
    {
        private readonly IOctopusAsyncRepository repository;

        public PackageMetadataRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<OctopusPackageMetadataGetResource> Get(string id)
        {
            var rootDocument = await repository.Client.Repository.LoadRootDocument();
            return await repository.Client.Get<OctopusPackageMetadataGetResource>(rootDocument.Links["PackageMetadata"], new { id });
        }

        public async Task<OctopusPackageMetadataGetResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata)
        {
            var resource = new OctopusPackageMetadataPostResource
            {
                PackageId = packageId,
                Version = version,
                OctopusPackageMetadata = octopusMetadata
            };

            var rootDocument = await repository.Client.Repository.LoadRootDocument();
            return await repository.Client.Post<OctopusPackageMetadataPostResource, OctopusPackageMetadataGetResource>(rootDocument.Links["PackageMetadata"], resource);
        }
    }

    public interface IPackageMetadataRepository
    {
        Task<OctopusPackageMetadataGetResource> Get(string id);
        Task<OctopusPackageMetadataGetResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata);
    }
}