﻿using System;
using System.Collections.Generic;
using System.Linq;
using OctopusTools.Client;
using OctopusTools.Infrastructure;
using OctopusTools.Model;
using log4net;

namespace OctopusTools.Commands
{
    public class DeployReleaseCommand : ApiCommand
    {
        public DeployReleaseCommand(IOctopusClientFactory clientFactory, ILog log) : base(clientFactory, log)
        {
        }

        public string ProjectName { get; set; }
        public string DeployToEnvironmentName { get; set; }
        public string VersionNumber { get; set; }
        public bool Force { get; set; }

        public override OptionSet Options
        {
            get
            {
                var options = base.Options;
                options.Add("project=", "Name of the project", v => ProjectName = v);
                options.Add("deployto=", "Environment to deploy to, e.g., Production", v => DeployToEnvironmentName = v);
                options.Add("version=", "Version number of the release to deploy.", v => VersionNumber = v);
                options.Add("force", "Whether to force redeployment of already installed packages (flag, default false).", v => Force = true);
                return options;
            }
        }

        public override void Execute()
        {
            if (string.IsNullOrWhiteSpace(ProjectName)) throw new CommandException("Please specify a project name using the parameter: --project=XYZ");
            if (string.IsNullOrWhiteSpace(DeployToEnvironmentName)) throw new CommandException("Please specify an environment using the parameter: --deployto=XYZ");
            if (string.IsNullOrWhiteSpace(VersionNumber)) throw new CommandException("Please specify a release version using the parameter: --version=1.0.0.0");
            
            var server = Client.Handshake().Execute();

            var project = FindProject(server);

            var environment = FindEnvironment(server);

            var release = FindRelease(server, project);

            DeployRelease(release, environment);
        }

        void DeployRelease(Release release, DeploymentEnvironment environment)
        {
            var deployment = new Deployment();
            deployment.EnvironmentId = environment.Id;
            deployment.ReleaseId = release.Id;
            deployment.ForceRedeployment = Force;

            var result = Client.Create(release.Link("Deployments"), deployment).Execute();

            Log.InfoFormat("Successfully scheduled release '{0}' for deployment to environment '{1}'" + result.Name, release.Version, environment.Name);
        }

        Release FindRelease(OctopusInstance server, Project project)
        {
            Log.DebugFormat("Searching for release '{0}'", VersionNumber);

            var releases = Client.List<Release>(project.Link("Releases")).Execute();

            var release = releases.FirstOrDefault(x => string.Equals(x.Version, VersionNumber, StringComparison.InvariantCultureIgnoreCase));
            if (release == null)
            {
                throw new ArgumentException(string.Format("A release named '{0}' could not be found.", VersionNumber));
            }

            Log.InfoFormat("Found release: {0} [{1}]", release.Version, release.Id);

            return release;
        }

        Project FindProject(OctopusInstance server)
        {
            Log.DebugFormat("Searching for project '{0}'", ProjectName);

            var projects = Client.List<Project>(server.Link("Projects")).Execute();

            var project = projects.FirstOrDefault(x => string.Equals(x.Name, ProjectName, StringComparison.InvariantCultureIgnoreCase));
            if (project == null)
            {
                throw new ArgumentException(string.Format("A project named '{0}' could not be found.", ProjectName));
            }

            Log.InfoFormat("Found project: {0} [{1}]", project.Name, project.Id);

            return project;
        }

        DeploymentEnvironment FindEnvironment(OctopusInstance server)
        {
            if (string.IsNullOrWhiteSpace(DeployToEnvironmentName))
                return null;

            Log.DebugFormat("Searching for environment '{0}'", DeployToEnvironmentName);

            var environments = Client.List<DeploymentEnvironment>(server.Link("Environments")).Execute();

            var environment = environments.FirstOrDefault(x => string.Equals(x.Name, DeployToEnvironmentName, StringComparison.InvariantCultureIgnoreCase));
            if (environment == null)
            {
                throw new ArgumentException(string.Format("An environment named '{0}' could not be found.", DeployToEnvironmentName));
            }

            Log.InfoFormat("Found environment: {0} [{1}]", environment.Name, environment.Id);

            return environment;
        }
    }
}
