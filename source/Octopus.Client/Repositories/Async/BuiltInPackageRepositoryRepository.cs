using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Features;
using Octopus.Client.Logging;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface IBuiltInPackageRepositoryRepository
    {
        Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, bool replaceExisting = false, CancellationToken token = default);
        Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, bool replaceExisting, bool useDeltaCompression, CancellationToken token = default);
        Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, OverwriteMode overwriteMode, CancellationToken token = default);
        Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, OverwriteMode overwriteMode, bool useDeltaCompression, CancellationToken token = default);

        Task<ResourceCollection<PackageFromBuiltInFeedResource>> ListPackages(string packageId, int skip = 0, int take = 30, CancellationToken token = default);
        Task<ResourceCollection<PackageFromBuiltInFeedResource>> LatestPackages(int skip = 0, int take = 30, CancellationToken token = default);
        Task DeletePackage(PackageResource package, CancellationToken token = default);
        Task DeletePackages(IReadOnlyList<PackageResource> packages, CancellationToken token = default);

        Task<PackageFromBuiltInFeedResource> GetPackage(string packageId, string version, CancellationToken token = default);
    }

    class BuiltInPackageRepositoryRepository : IBuiltInPackageRepositoryRepository
    {
        private readonly IOctopusAsyncRepository repository;
        private static readonly ILog Logger = LogProvider.For<BuiltInPackageRepositoryRepository>();

        public BuiltInPackageRepositoryRepository(IOctopusAsyncRepository repository)
        {
            this.repository = repository;
        }

        public async Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, OverwriteMode overwriteMode, CancellationToken token = default)
        {
            return await PushPackage(fileName, contents, overwriteMode, useDeltaCompression: true, token);
        }

        public async Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, bool replaceExisting = false, CancellationToken token = default)
        {
            return await PushPackage(fileName, contents, replaceExisting ? OverwriteMode.OverwriteExisting : OverwriteMode.FailIfExists, useDeltaCompression: true, token);
        }

        public async Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, bool replaceExisting, bool useDeltaCompression, CancellationToken token = default)
        {
            return await PushPackage(fileName, contents, replaceExisting ? OverwriteMode.OverwriteExisting : OverwriteMode.FailIfExists, useDeltaCompression, token);
        }

        public async Task<PackageFromBuiltInFeedResource> PushPackage(string fileName, Stream contents, OverwriteMode overwriteMode, bool useDeltaCompression, CancellationToken token = default)
        {
            if (useDeltaCompression)
            {
                try
                {
                    var deltaResult = await AttemptDeltaPush(fileName, contents, overwriteMode, token).ConfigureAwait(false);
                    if (deltaResult != null)
                        return deltaResult;
                }
                catch (TimeoutException ex)
                {
                    Logger.Info("Delta push timed out: " + ex.Message);

                    var verificationResult = await VerifyTransfer(fileName, contents, token).ConfigureAwait(false);
                    if (verificationResult != null) return verificationResult;
                }
                catch (Exception ex) when (!(ex is OctopusValidationException))
                {
                    Logger.Info("Something went wrong while performing a delta transfer: " + ex.Message);
                }

                Logger.Info("Falling back to pushing the complete package to the server");
            }
            else
            {
                Logger.Info("Pushing the complete package to the server, as delta compression was explicitly disabled");
            }

            var link = await repository.Link("PackageUpload").ConfigureAwait(false);
            object pathParameters;

            // if the link contains overwriteMode then we're connected to a new server, if not use the old `replace` parameter
            if (link.Contains(OverwriteModeLink.Link))
            {
                pathParameters = new { overwriteMode = overwriteMode };
            }
            else
            {
                pathParameters = new { replace = overwriteMode.ConvertToLegacyReplaceFlag(Logger) };
            }

            contents.Seek(0, SeekOrigin.Begin);

            try
            {
                return await repository.Client.Post<FileUpload, PackageFromBuiltInFeedResource>(
                    link,
                    new FileUpload() {Contents = contents, FileName = fileName},
                    pathParameters).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                var verificationResult = await VerifyTransfer(fileName, contents, token);
                if (verificationResult != null) return verificationResult;

                throw;
            }
        }

        private async Task<PackageFromBuiltInFeedResource> VerifyTransfer(string fileName, Stream contents, CancellationToken token)
        {
            Logger.Info("Trying to find out whether the transfer worked");

            if (!PackageIdentityParser.TryParsePackageIdAndVersion(Path.GetFileNameWithoutExtension(fileName),
                out var packageId, out var version))
            {
                Logger.Info("Can't check whether the transfer actually worked");
                return null;
            }

            var uploadedPackage = await TryFindPackage(packageId, version, token);

            return PackageContentComparer.AreSame(uploadedPackage, contents, Logger) ? uploadedPackage : null;
        }

        private async Task<PackageFromBuiltInFeedResource> TryFindPackage(string packageId, SemanticVersion version, CancellationToken token)
        {
            try
            {
                return await repository.BuiltInPackageRepository.GetPackage(packageId, version.ToString(), token).ConfigureAwait(false);
            }
            catch (OctopusResourceNotFoundException)
            {
                return null;
            }
        }

        private async Task<PackageFromBuiltInFeedResource> AttemptDeltaPush(string fileName, Stream contents, OverwriteMode overwriteMode, CancellationToken token)
        {
            if (! await repository.HasLink("PackageDeltaSignature").ConfigureAwait(false))
            {
                Logger.Info("Server does not support delta compression for package push");
                return null;
            }

            if (!PackageIdentityParser.TryParsePackageIdAndVersion(Path.GetFileNameWithoutExtension(fileName), out var packageId, out var version))
            {
                Logger.Info("Could not determine the package ID and/or version based on the supplied filename");
                return null;
            }

            PackageSignatureResource signatureResult;
            try
            {
                Logger.Info($"Requesting signature for delta compression from the server for upload of a package with id '{packageId}' and version '{version}'");
                signatureResult = await repository.Client.Get<PackageSignatureResource>(await repository.Link("PackageDeltaSignature").ConfigureAwait(false), new {packageId, version}, token).ConfigureAwait(false);
            }
            catch (OctopusResourceNotFoundException)
            {
                Logger.Info("No package with the same ID exists on the server");
                return null;
            }

            using(var deltaTempFile = new TemporaryFile())
            {
                var shouldUpload = DeltaCompression.CreateDelta(contents, signatureResult, deltaTempFile.FileName);
                if (!shouldUpload)
                    return null;

                using (var delta = File.OpenRead(deltaTempFile.FileName))
                {
                    var link = await repository.Link("PackageDeltaUpload").ConfigureAwait(false);
                    object pathParameters;

                    // if the link contains overwriteMode then we're connected to a new server, if not use the old `replace` parameter
                    if (link.Contains(OverwriteModeLink.Link))
                    {
                        pathParameters = new { overwriteMode = overwriteMode, packageId, signatureResult.BaseVersion };
                    }
                    else
                    {
                        pathParameters = new { replace = overwriteMode.ConvertToLegacyReplaceFlag(Logger), packageId, signatureResult.BaseVersion };
                    }

                    var result = await repository.Client.Post<FileUpload, PackageFromBuiltInFeedResource>(
                        link,
                        new FileUpload() {Contents = delta, FileName = Path.GetFileName(fileName)},
                        pathParameters,
                        token).ConfigureAwait(false);

                    Logger.Info("Delta transfer completed");

                    return result;
                }
            }
        }

        public async Task<ResourceCollection<PackageFromBuiltInFeedResource>> ListPackages(string packageId, int skip = 0, int take = 30, CancellationToken token = default)
        {
            return await repository.Client.List<PackageFromBuiltInFeedResource>(await repository.Link("Packages").ConfigureAwait(false), new { nuGetPackageId = packageId, take, skip }, token).ConfigureAwait(false);
        }

        public async Task<PackageFromBuiltInFeedResource> GetPackage(string packageId, string version, CancellationToken token = default)
        {
            return await repository.Client.Get<PackageFromBuiltInFeedResource>(await repository.Link("Packages").ConfigureAwait(false), new { id = $"{packageId}.{version}" }, token).ConfigureAwait(false);
        }

        public async Task<ResourceCollection<PackageFromBuiltInFeedResource>> LatestPackages(int skip = 0, int take = 30, CancellationToken token = default)
        {
            return await repository.Client.List<PackageFromBuiltInFeedResource>(await repository.Link("Packages").ConfigureAwait(false), new { latest = true, take, skip }, token).ConfigureAwait(false);
        }

        public async Task DeletePackage(PackageResource package, CancellationToken token = default)
        {
            await repository.Client.Delete(await repository.Link("Packages").ConfigureAwait(false), new { id = package.Id }, token: token).ConfigureAwait(false);
        }

        public async Task DeletePackages(IReadOnlyList<PackageResource> packages, CancellationToken token = default)
            => await repository.Client.Delete(await repository.Link("PackagesBulk").ConfigureAwait(false), new { ids = packages.Select(p => p.Id).ToArray() }, token: token).ConfigureAwait(false);

    }
}
