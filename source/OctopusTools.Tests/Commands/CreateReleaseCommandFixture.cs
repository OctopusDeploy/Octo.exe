﻿using NSubstitute;
using NUnit.Framework;
using OctopusTools.Commands;
using OctopusTools.Infrastructure;
using OctopusTools.Util;

namespace OctopusTools.Tests.Commands
{
    public class CreateReleaseCommandFixture : ApiCommandFixtureBase
    {
        CreateReleaseCommand createReleaseCommand;
        IPackageVersionResolver versionResolver;
        IChannelResolver channelResolver;

        [SetUp]
        public void SetUp()
        {            
            versionResolver = Substitute.For<IPackageVersionResolver>();
            channelResolver = Substitute.For<IChannelResolver>();
        }

        [Test]
        public void ShouldLoadOptionsFromFile()
        {
            createReleaseCommand = new CreateReleaseCommand(RepositoryFactory, Log, new OctopusPhysicalFileSystem(Log), versionResolver, channelResolver);

            Assert.Throws<CouldNotFindException>(delegate {
                createReleaseCommand.Execute("--configfile=Commands/Resources/CreateRelease.config.txt");
            });
            
            Assert.AreEqual("Test Project", createReleaseCommand.ProjectName);
            Assert.AreEqual("1.0.0", createReleaseCommand.VersionNumber);
            Assert.AreEqual("Test config file.", createReleaseCommand.ReleaseNotes);
        }
    }
}