using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Logging;
using Octopus.Client.Model;
using Octopus.Client.Model.PackageMetadata;

namespace Octopus.Client.Repositories.Async
{
    class PackageMetadataRepository : IPackageMetadataRepository
    {
        private readonly IOctopusAsyncRepository repository;
        private static readonly ILog Logger = LogProvider.For<PackageMetadataRepository>();

        public PackageMetadataRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<OctopusPackageMetadataMappedResource> Get(string id, CancellationToken token = default)
        {
            var link = await repository.Link("PackageMetadata");
            return await repository.Client.Get<OctopusPackageMetadataMappedResource>(link, new { id });
        }

        public Task<OctopusPackageMetadataMappedResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata, bool replaceExisting, CancellationToken token = default)
        {
            return Push(packageId, version, octopusMetadata, replaceExisting ? OverwriteMode.OverwriteExisting : OverwriteMode.FailIfExists, token);
        }
        
        public async Task<OctopusPackageMetadataMappedResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata, OverwriteMode overwriteMode, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                throw new ArgumentException("A package Id must be supplied", nameof(packageId));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("A version must be supplied", nameof(version));

            var resource = new OctopusPackageMetadataVersionResource
            {
                PackageId = packageId,
                Version = version,
                OctopusPackageMetadata = octopusMetadata
            };

            if (await repository.HasLink("BuildInformation"))
            {
                Logger.Warn("Connected to an Octopus server that supports the BuildInformation API. It is recommended you move to using the BuildInformationRepository as the PackageMetadataRepository is deprecated.");
            }

            if (!(await repository.HasLink("PackageMetadata")))
            {
                throw new OperationNotSupportedByOctopusServerException(
                    OctopusPackageMetadata.PackageMetadataRequiresOctopusVersionMessage,
                    OctopusPackageMetadata.PackageMetadataRequiresOctopusVersion);
            }

            var link = await repository.Link("PackageMetadata");

            // if the link contains overwriteMode then we're connected to a new server, if not use the old `replace` parameter  
            if (link.Contains(OverwriteModeLink.Link))
            {
                return await repository.Client.Post<OctopusPackageMetadataVersionResource, OctopusPackageMetadataMappedResource>(link, resource, new { overwriteMode = overwriteMode }, token);
            }
            else
            {
                return await repository.Client.Post<OctopusPackageMetadataVersionResource, OctopusPackageMetadataMappedResource>(link, resource, new { replace = overwriteMode.ConvertToLegacyReplaceFlag(Logger) }, token);
            }
        }
    }

    public interface IPackageMetadataRepository
    {
        Task<OctopusPackageMetadataMappedResource> Get(string id, CancellationToken token = default);
        Task<OctopusPackageMetadataMappedResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata, OverwriteMode overwriteMode, CancellationToken token = default);
        [Obsolete]
        Task<OctopusPackageMetadataMappedResource> Push(string packageId, string version, OctopusPackageMetadata octopusMetadata, bool replaceExisting, CancellationToken token = default);
    }
}