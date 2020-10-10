using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Logging;
using Octopus.Client.Model;
using Octopus.Client.Model.BuildInformation;

namespace Octopus.Client.Repositories.Async
{
    class BuildInformationRepository : IBuildInformationRepository
    {
        private readonly IOctopusAsyncRepository repository;
        private static readonly ILog Logger = LogProvider.For<BuildInformationRepository>();

        public BuildInformationRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<OctopusPackageVersionBuildInformationMappedResource> Get(string id, CancellationToken token = default)
        {
            var link = await repository.Link("BuildInformation").ConfigureAwait(false);
            return await repository.Client.Get<OctopusPackageVersionBuildInformationMappedResource>(link, new { id }, token).ConfigureAwait(false);
        }

        public Task<OctopusPackageVersionBuildInformationMappedResource> Push(string packageId, string version, OctopusBuildInformation octopusMetadata, bool replaceExisting, CancellationToken token = default)
        {
            return Push(packageId, version, octopusMetadata, replaceExisting ? OverwriteMode.OverwriteExisting : OverwriteMode.FailIfExists, token);
        }
        
        public async Task<OctopusPackageVersionBuildInformationMappedResource> Push(string packageId, string version, OctopusBuildInformation octopusMetadata, OverwriteMode overwriteMode, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                throw new ArgumentException("A package Id must be supplied", nameof(packageId));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("A version must be supplied", nameof(version));

            if (!(await repository.HasLink("BuildInformation").ConfigureAwait(false)))
            {
                throw new OperationNotSupportedByOctopusServerException(
                    OctopusBuildInformation.BuildInformationRequiresOctopusVersionMessage,
                    OctopusBuildInformation.BuildInformationRequiresOctopusVersion);
            }
            var link = await repository.Link("BuildInformation").ConfigureAwait(false);

            var resource = new OctopusPackageVersionBuildInformationResource
            {
                PackageId = packageId,
                Version = version,
                OctopusBuildInformation = octopusMetadata
            };

            return await repository.Client.Post<OctopusPackageVersionBuildInformationResource, OctopusPackageVersionBuildInformationMappedResource>(link, resource, new { overwriteMode = overwriteMode }, token).ConfigureAwait(false);
        }
        
        public async Task<ResourceCollection<OctopusPackageVersionBuildInformationMappedResource>> ListBuilds(string packageId, int skip = 0, int take = 30, CancellationToken token = default)
        {
            return await repository.Client.List<OctopusPackageVersionBuildInformationMappedResource>(await repository.Link("BuildInformation").ConfigureAwait(false), new { packageId = packageId, take, skip }, token).ConfigureAwait(false);
        }

        public async Task<ResourceCollection<OctopusPackageVersionBuildInformationMappedResource>> LatestBuilds(int skip = 0, int take = 30, CancellationToken token = default)
        {
            return await repository.Client.List<OctopusPackageVersionBuildInformationMappedResource>(await repository.Link("BuildInformation").ConfigureAwait(false), new { latest = true, take, skip }, token).ConfigureAwait(false);
        }

        public async Task Delete(OctopusPackageVersionBuildInformationMappedResource buildInformation, CancellationToken token = default)
        {
            await repository.Client.Delete(await repository.Link("BuildInformation").ConfigureAwait(false), new { id = buildInformation.Id }, token: token).ConfigureAwait(false);
        }

        public async Task DeleteBuilds(IReadOnlyList<OctopusPackageVersionBuildInformationMappedResource> builds, CancellationToken token = default)
        {
            await repository.Client.Delete(await repository.Link("BuildInformationBulk").ConfigureAwait(false), new { ids = builds.Select(p => p.Id).ToArray() }, token: token).ConfigureAwait(false);
        }
    }

    public interface IBuildInformationRepository
    {
        Task<OctopusPackageVersionBuildInformationMappedResource> Get(string id, CancellationToken token = default);
        Task<OctopusPackageVersionBuildInformationMappedResource> Push(string packageId, string version, OctopusBuildInformation octopusMetadata, OverwriteMode overwriteMode, CancellationToken token = default);
        [Obsolete]
        Task<OctopusPackageVersionBuildInformationMappedResource> Push(string packageId, string version, OctopusBuildInformation octopusMetadata, bool replaceExisting, CancellationToken token = default);

        Task<ResourceCollection<OctopusPackageVersionBuildInformationMappedResource>> ListBuilds(string packageId, int skip = 0, int take = 30, CancellationToken token = default);
        Task<ResourceCollection<OctopusPackageVersionBuildInformationMappedResource>> LatestBuilds(int skip = 0, int take = 30, CancellationToken token = default);
        Task Delete(OctopusPackageVersionBuildInformationMappedResource buildInformation, CancellationToken token = default);
        Task DeleteBuilds(IReadOnlyList<OctopusPackageVersionBuildInformationMappedResource> builds, CancellationToken token = default);
    }
}