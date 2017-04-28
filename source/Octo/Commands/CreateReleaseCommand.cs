﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Octopus.Cli.Diagnostics;
using Octopus.Cli.Infrastructure;
using Octopus.Cli.Repositories;
using Octopus.Cli.Util;
using Octopus.Client;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Serilog.Events;

namespace Octopus.Cli.Commands
{
    [Command("create-release", Description = "Creates (and, optionally, deploys) a release.")]
    public class CreateReleaseCommand : DeploymentCommandBase
    {
        private readonly IReleasePlanBuilder releasePlanBuilder;

        public CreateReleaseCommand(IOctopusAsyncRepositoryFactory repositoryFactory, ILogger log, IOctopusFileSystem fileSystem, IPackageVersionResolver versionResolver, IReleasePlanBuilder releasePlanBuilder, IOctopusClientFactory clientFactory)
            : base(repositoryFactory, log, fileSystem, clientFactory)
        {
            this.releasePlanBuilder = releasePlanBuilder;
            DeploymentStatusCheckSleepCycle = TimeSpan.FromSeconds(10);
            DeploymentTimeout = TimeSpan.FromMinutes(10);

            var options = Options.For("Release creation");
            options.Add("project=", "Name of the project", v => ProjectName = v);
            options.Add("defaultpackageversion=|packageversion=", "Default version number of all packages to use for this release. Override per-package using --package.", versionResolver.Default);
            options.Add("version=|releaseNumber=", "[Optional] Release number to use for the new release.", v => VersionNumber = v);
            options.Add("channel=", "[Optional] Channel to use for the new release. Omit this argument to automatically select the best channel.", v => ChannelName = v);
            options.Add("package=", "[Optional] Version number to use for a package in the release. Format: --package={StepName}:{Version}", v => versionResolver.Add(v));
            options.Add("packagesFolder=", "[Optional] A folder containing NuGet packages from which we should get versions.", v => {v.CheckForIllegalPathCharacters("packagesFolder"); versionResolver.AddFolder(v);});
            options.Add("releasenotes=", "[Optional] Release Notes for the new release. Styling with Markdown is supported.", v => ReleaseNotes = v);
            options.Add("releasenotesfile=", "[Optional] Path to a file that contains Release Notes for the new release. Supports Markdown files.", ReadReleaseNotesFromFile);
            options.Add("ignoreexisting", "[Optional, Flag] Don't create this release if there is already one with the same version number.", v => IgnoreIfAlreadyExists = true);
            options.Add("ignorechannelrules", "[Optional, Flag] Create the release ignoring any version rules specified by the channel.", v => IgnoreChannelRules = true);
            options.Add("packageprerelease=", "[Optional] Pre-release for latest version of all packages to use for this release.", v => VersionPreReleaseTag = v);
            options.Add("whatif", "[Optional, Flag] Perform a dry run but don't actually create/deploy release.", v => WhatIf = true);

            options = Options.For("Deployment");
            options.Add("deployto=", "[Optional] Environment to automatically deploy to, e.g., Production", v => DeployToEnvironmentNames.Add(v));
        }

        public string ChannelName { get; set; }
        public string VersionNumber { get; set; }
        public string ReleaseNotes { get; set; }
        public bool IgnoreIfAlreadyExists { get; set; }
        public bool IgnoreChannelRules { get; set; }
        public string VersionPreReleaseTag { get; set; }
        public bool WhatIf { get; set; }

        protected override void ValidateParameters()
        {
            if (!string.IsNullOrWhiteSpace(ChannelName) && !Repository.SupportsChannels())
                throw new CommandException("Your Octopus server does not support channels, which was introduced in Octopus 3.2. Please upgrade your Octopus server, or remove the --channel argument.");

            base.ValidateParameters();
        }

        protected override async Task Execute()
        {
            var serverSupportsChannels = ServerSupportsChannels();
            Log.Debug(serverSupportsChannels ? "This Octopus Server supports channels" : "This Octopus Server does not support channels");

            Log.Debug("Finding project: {Project:l}", ProjectName);
            var project = await Repository.Projects.FindByName(ProjectName).ConfigureAwait(false);
            if (project == null)
                throw new CouldNotFindException("a project named", ProjectName);

            var plan = await BuildReleasePlan(project).ConfigureAwait(false);

            string versionNumber;
            if (!string.IsNullOrWhiteSpace(VersionNumber))
            {
                versionNumber = VersionNumber;
                Log.Debug("Using version number provided on command-line: {Version:l}", versionNumber);
            }
            else if (!string.IsNullOrWhiteSpace(plan.ReleaseTemplate.NextVersionIncrement))
            {
                versionNumber = plan.ReleaseTemplate.NextVersionIncrement;
                Log.Debug("Using version number from release template: {Version:l}", versionNumber);
            }
            else if (!string.IsNullOrWhiteSpace(plan.ReleaseTemplate.VersioningPackageStepName))
            {
                versionNumber = plan.GetActionVersionNumber(plan.ReleaseTemplate.VersioningPackageStepName);
                Log.Debug("Using version number from package step: {Version:l}", versionNumber);
            }
            else
            {
                throw new CommandException("A version number was not specified and could not be automatically selected.");
            }

            Log.Write(
                plan.IsViableReleasePlan() ? LogEventLevel.Information : LogEventLevel.Warning,
                "Release plan for {Project:l} {Version:l}" + Environment.NewLine + "{Plan:l}",
                ProjectName, versionNumber, plan.FormatAsTable()
            );

            if (plan.HasUnresolvedSteps())
            {
                throw new CommandException("Package versions could not be resolved for one or more of the package steps in this release. See the errors above for details. Either ensure the latest version of the package can be automatically resolved, or set the version to use specifically by using the --package argument.");
            }

            if (plan.HasStepsViolatingChannelVersionRules())
            {
                if (IgnoreChannelRules)
                {
                    Log.Warning("At least one step violates the package version rules for the Channel '{Channel:l}'. Forcing the release to be created ignoring these rules...", plan.Channel.Name);
                }
                else
                {
                    throw new CommandException($"At least one step violates the package version rules for the Channel '{plan.Channel.Name}'. Either correct the package versions for this release, let Octopus select the best channel by omitting the --channel argument, select a different channel using --channel=MyChannel argument, or ignore these version rules altogether by using the --ignoreChannelRules argument.");
                }
            }

            if (IgnoreIfAlreadyExists)
            {
                Log.Debug("Checking for existing release for {Project:l} {Version:l} because you specified --ignoreexisting...", ProjectName, versionNumber);
                try
                {
                    var found = await Repository.Projects.GetReleaseByVersion(project, versionNumber).ConfigureAwait(false);
                    if (found != null)
                    {
                        Log.Information("A release of {Project:l} with the number {Version:l} already exists, and you specified --ignoreexisting, so we won't even attempt to create the release.", ProjectName, versionNumber);
                        return;
                    }
                }
                catch (OctopusResourceNotFoundException)
                {
                    // Expected
                    Log.Debug("No release exists - the coast is clear!");
                }
            }

            if (WhatIf)
            {
                // We were just doing a dry run - bail out here
                if (DeployToEnvironmentNames.Any())
                    Log.Information("[WhatIf] This release would have been created using the release plan and deployed to {Environments:l}", DeployToEnvironmentNames.CommaSeperate());
                else
                    Log.Information("[WhatIf] This release would have been created using the release plan");
            }
            else
            {
                // Actually create the release!
                Log.Debug("Creating release...");
                var release = await Repository.Releases.Create(new ReleaseResource(versionNumber, project.Id, plan.Channel?.Id)
                {
                    ReleaseNotes = ReleaseNotes,
                    SelectedPackages = plan.GetSelections()
                }, ignoreChannelRules: IgnoreChannelRules)
                .ConfigureAwait(false);

                Log.Information("Release {Version:l} created successfully!", release.Version);
                Log.ServiceMessage("setParameter", new { name = "octo.releaseNumber", value = release.Version });
                Log.TfsServiceMessage(ServerBaseUrl, project, release);

                await DeployRelease(project, release).ConfigureAwait(false);
            }
        }

        private async Task<ReleasePlan> BuildReleasePlan(ProjectResource project)
        {
            if (!string.IsNullOrWhiteSpace(ChannelName))
            {
                Log.Information("Building release plan for channel '{Channel:l}'...", ChannelName);
                var channels = await Repository.Projects.GetChannels(project).ConfigureAwait(false);
                var matchingChannel = await channels
                    .FindOne(Repository, c => c.Name.Equals(ChannelName, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);

                if (matchingChannel == null)
                    throw new CouldNotFindException($"a channel in {project.Name} named", ChannelName);

                return await releasePlanBuilder.Build(Repository, project, matchingChannel, VersionPreReleaseTag).ConfigureAwait(false);
            }

            // All Octopus 3.2+ servers should have the Channels hypermedia link, we should use the channel information
            // to select the most appropriate channel, or provide enough information to proceed from here
            if (ServerSupportsChannels())
            {
                Log.Debug("Automatically selecting the best channel for this release...");
                return await AutoSelectBestReleasePlanOrThrow(project).ConfigureAwait(false);
            }
            
            // Compatibility: this has to cater for Octopus before Channels existed
            Log.Information("Building release plan without a channel for Octopus Server without channels support...");
            return await releasePlanBuilder.Build(Repository, project, null, VersionPreReleaseTag).ConfigureAwait(false);
        }

        private bool ServerSupportsChannels()
        {
            return Repository.Client.RootDocument.HasLink("Channels");
        }

        async Task<ReleasePlan> AutoSelectBestReleasePlanOrThrow(ProjectResource project)
        {
            // Build a release plan for each channel to determine which channel is the best match for the provided options
            var channels = await Repository.Projects.GetChannels(project).ConfigureAwait(false);
            var candidateChannels = await channels.GetAllPages(Repository).ConfigureAwait(false);
            var releasePlans = new List<ReleasePlan>();
            foreach (var channel in candidateChannels)
            {
                Log.Information("Building a release plan for Channel '{Channel:l}'...", channel.Name);

                var plan = await releasePlanBuilder.Build(Repository, project, channel, VersionPreReleaseTag).ConfigureAwait(false);
                releasePlans.Add(plan);
            }

            var viablePlans = releasePlans.Where(p => p.IsViableReleasePlan()).ToArray();
            if (viablePlans.Length <= 0)
            {
                throw new CommandException(
                    "There are no viable release plans in any channels using the provided arguments. The following release plans were considered:" +
                    Environment.NewLine +
                    $"{releasePlans.Select(p => p.FormatAsTable()).NewlineSeperate()}");
            }

            if (viablePlans.Length == 1)
            {
                var selectedPlan = viablePlans.Single();
                Log.Information("Selected the release plan for Channel '{Channel:l}' - it is a perfect match", selectedPlan.Channel.Name);
                return selectedPlan;
            }

            if (viablePlans.Length > 1 && viablePlans.Any(p => p.Channel.IsDefault))
            {
                var selectedPlan = viablePlans.First(p => p.Channel.IsDefault);
                Log.Information("Selected the release plan for Channel '{Channel:l}' - there were multiple matching Channels ({AllChannels:l}) so we selected the default channel.", selectedPlan.Channel.Name, viablePlans.Select(p => p.Channel.Name).CommaSeperate());
                return selectedPlan;
            }

            throw new CommandException(
                $"There are {viablePlans.Length} viable release plans using the provided arguments so we cannot auto-select one. The viable release plans are:" +
                Environment.NewLine +
                $"{viablePlans.Select(p => p.FormatAsTable()).NewlineSeperate()}" +
                Environment.NewLine +
                "The unviable release plans are:" +
                Environment.NewLine +
                $"{releasePlans.Except(viablePlans).Select(p => p.FormatAsTable()).NewlineSeperate()}");
        }

        void ReadReleaseNotesFromFile(string value)
        {
            try
            {
                ReleaseNotes = File.ReadAllText(value);
            }
            catch (IOException ex)
            {
                throw new CommandException(ex.Message);
            }
        }
    }
}