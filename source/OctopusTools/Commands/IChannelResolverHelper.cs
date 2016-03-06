﻿using Octopus.Client;
using Octopus.Client.Model;

namespace OctopusTools.Commands
{
    public interface IChannelResolverHelper
    {
        int GetApplicableStepCount(IOctopusRepository repository, DeploymentProcessResource deploymentProcess, ChannelResource channel, IPackageVersionResolver versionResolver);
        bool TestChannelRuleAgainstOctopusApi(IOctopusRepository repository, ChannelResource channel, ChannelVersionRuleResource rule, string packageVersion);
    }
}