using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Nancy;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octopus.Client.Exceptions;
using Octopus.Client.Extensibility;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Octopus.Client.Operations;

namespace Octopus.Client.Tests.Operations
{
    [TestFixture]
    public class RegisterMachineOperationFixture
    {
        RegisterMachineOperation operation;
        IOctopusClientFactory clientFactory;
        IOctopusAsyncClient client;
        OctopusServerEndpoint serverEndpoint;
        ResourceCollection<EnvironmentResource> environments;
        ResourceCollection<MachineResource> machines;
        ResourceCollection<MachinePolicyResource> machinePolicies;

        [SetUp]
        public void SetUp()
        {
            clientFactory = Substitute.For<IOctopusClientFactory>();
            client = Substitute.For<IOctopusAsyncClient>();
            clientFactory.CreateAsyncClient(Arg.Any<OctopusServerEndpoint>()).Returns(client);
            operation = new RegisterMachineOperation(clientFactory);
            serverEndpoint = new OctopusServerEndpoint("http://octopus", "ABC123");

            environments = new ResourceCollection<EnvironmentResource>(new EnvironmentResource[0], LinkCollection.Self("/foo"));
            machines = new ResourceCollection<MachineResource>(new MachineResource[0], LinkCollection.Self("/foo"));
            machinePolicies = new ResourceCollection<MachinePolicyResource>(new MachinePolicyResource[0], LinkCollection.Self("/foo"));
            var rootDocument = new RootResource
            {
                ApiVersion = "3.0.0",
                Version = "2099.0.0",
                Links = LinkCollection.Self("/api")
                    .Add("Environments", "/api/environments")
                    .Add("Machines", "/api/machines")
                    .Add("MachinePolicies", "/api/machinepolicies")
                    .Add("CurrentUser", "/api/users/me")
                    .Add("SpaceHome", "/api/spaces")
            };
            client.Get<RootResource>(Arg.Any<string>()).Returns(rootDocument);
            client.Repository.LoadRootDocument().Returns(rootDocument);
            client.Get<SpaceResource[]>(Arg.Any<string>())
                .Returns(new[] {new SpaceResource() {Id = "Spaces-1", IsDefault = true}});
            client.Get<UserResource>(Arg.Any<string>()).Returns(new UserResource()
            {
                Links =
                {
                    {"Spaces", ""}
                }
            });
            client.Repository.HasLink(Arg.Any<string>()).Returns(ci => rootDocument.HasLink(ci.Arg<string>()));
            client.Repository.Link(Arg.Any<string>()).Returns(ci => rootDocument.Link(ci.Arg<string>()));

            client.When(x => x.Paginate(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<Func<ResourceCollection<EnvironmentResource>, bool>>()))
                .Do(ci => ci.Arg<Func<ResourceCollection<EnvironmentResource>, bool>>()(environments));
            client.When(x => x.Paginate(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<Func<ResourceCollection<MachineResource>, bool>>()))
                .Do(ci => ci.Arg<Func<ResourceCollection<MachineResource>, bool>>()(machines));
            client.When(x => x.Paginate(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<Func<ResourceCollection<MachinePolicyResource>, bool>>()))
                .Do(ci => ci.Arg<Func<ResourceCollection<MachinePolicyResource>, bool>>()(machinePolicies));

            client.List<MachineResource>(Arg.Any<string>(), Arg.Any<object>()).Returns(machines);
        }

        [Test]
        public void ShouldThrowIfEnvironmentNotFound()
        {
            operation.EnvironmentNames = new[] {"Atlantis"};
            Func<Task> exec = () => operation.ExecuteAsync(serverEndpoint);
            exec.ShouldThrow<ArgumentException>().WithMessage("Could not find the environment named Atlantis on the Octopus Server. Ensure the environment exists and you have permission to access it.");
        }

        [Test]
        public void ShouldThrowIfAnyEnvironmentNotFound()
        {
            environments.Items.Add(new EnvironmentResource { Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines") });

            operation.EnvironmentNames = new[] {"Production", "Atlantis", "Hyperborea"};
            Func<Task> exec = () => operation.ExecuteAsync(serverEndpoint);
            exec.ShouldThrow<ArgumentException>().WithMessage("Could not find the environments named: Atlantis, Hyperborea on the Octopus Server. Ensure the environments exist and you have permission to access them.");
        }

        [Test]
        public async Task ShouldCreateNewMachine()
        {
            environments.Items.Add(new EnvironmentResource {Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines")});
            environments.Items.Add(new EnvironmentResource {Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines")});

            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] {"Production"};

            await operation.ExecuteAsync(serverEndpoint).ConfigureAwait(false);

            await client.Received().Create("/api/machines", Arg.Is<MachineResource>(m =>
                m.Name == "Mymachine"
                && ((ListeningTentacleEndpointResource)m.Endpoint).Uri == "https://mymachine.test.com:10930/"
                && m.EnvironmentIds.First() == "environments-2"), Arg.Any<object>(), Arg.Is(default(CancellationToken)))
                .ConfigureAwait(false);
        }

        [Test]
        public void ShouldNotUpdateExistingMachine()
        {
            environments.Items.Add(new EnvironmentResource {Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines")});
            environments.Items.Add(new EnvironmentResource {Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines")});

            machines.Items.Add(new MachineResource {Id = "machines/84", EnvironmentIds = new ReferenceCollection(new[] {"environments-1"}), Name = "Mymachine", Links = LinkCollection.Self("/machines/whatever/1")});

            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] {"Production"};

            Func<Task> exec = () => operation.ExecuteAsync(serverEndpoint);
            exec.ShouldThrow<ArgumentException>();
        }

        [Test]
        public async Task ShouldUpdateExistingMachineWhenForceIsEnabled()
        {
            environments.Items.Add(new EnvironmentResource {Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines")});
            environments.Items.Add(new EnvironmentResource {Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines")});

            machines.Items.Add(new MachineResource {Id = "machines/84", EnvironmentIds = new ReferenceCollection(new[] {"environments-1"}), Name = "Mymachine", Links = LinkCollection.Self("/machines/whatever/1")});

            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] {"Production"};
            operation.AllowOverwrite = true;

            await operation.ExecuteAsync(serverEndpoint).ConfigureAwait(false);

            await client.Received().Update("/machines/whatever/1", Arg.Is<MachineResource>(m =>
                m.Id == "machines/84"
                && m.Name == "Mymachine"
                && m.EnvironmentIds.First() == "environments-2"), Arg.Any<object>(), Arg.Is(default(CancellationToken))).ConfigureAwait(false);
        }

        [Test]
        public async Task ShouldCreateWhenCantDeserializeMachines()
        {
            client.When(x => x.Paginate(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<Func<ResourceCollection<MachineResource>, bool>>()))
                .Throw(new OctopusDeserializationException(1, "Can not deserialize"));

            environments.Items.Add(new EnvironmentResource { Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines") });
            environments.Items.Add(new EnvironmentResource { Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines") });

            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] { "Production" };

            await operation.ExecuteAsync(serverEndpoint).ConfigureAwait(false);

            await client.Received().Create("/api/machines", Arg.Is<MachineResource>(m =>
                m.Name == "Mymachine"
                && ((ListeningTentacleEndpointResource)m.Endpoint).Uri == "https://mymachine.test.com:10930/"
                && m.EnvironmentIds.First() == "environments-2"), Arg.Any<object>(), Arg.Is(default(CancellationToken))).ConfigureAwait(false);
        }

        [Test]
        public async Task ShouldNotOverwriteMachinePolicyToNull()
        {
            environments.Items.Add(new EnvironmentResource { Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines") });
            environments.Items.Add(new EnvironmentResource { Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines") });

            machines.Items.Add(new MachineResource { Id = "machines/84", MachinePolicyId = "MachinePolicies-1", EnvironmentIds = new ReferenceCollection(new[] { "environments-1" }), Name = "Mymachine", Links = LinkCollection.Self("/machines/whatever/1") });

            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] { "Production" };
            operation.AllowOverwrite = true;

            await operation.ExecuteAsync(serverEndpoint).ConfigureAwait(false);

            await client.Received().Update("/machines/whatever/1", Arg.Is<MachineResource>(m =>
                m.Id == "machines/84"
                && m.Name == "Mymachine"
                && m.EnvironmentIds.First() == "environments-2"
                && m.MachinePolicyId == "MachinePolicies-1"), Arg.Any<object>(), Arg.Is(default(CancellationToken))).ConfigureAwait(false);
        }

        [Test]
        public async Task ShouldOverwriteMachinePolicyWhenPassed()
        {
            environments.Items.Add(new EnvironmentResource { Id = "environments-1", Name = "UAT", Links = LinkCollection.Self("/api/environments/environments-1").Add("Machines", "/api/environments/environments-1/machines") });
            environments.Items.Add(new EnvironmentResource { Id = "environments-2", Name = "Production", Links = LinkCollection.Self("/api/environments/environments-2").Add("Machines", "/api/environments/environments-2/machines") });

            machines.Items.Add(new MachineResource { Id = "machines/84", MachinePolicyId = "MachinePolicies-1", EnvironmentIds = new ReferenceCollection(new[] { "environments-1" }), Name = "Mymachine", Links = LinkCollection.Self("/machines/whatever/1") });
            machinePolicies.Items.Add(new MachinePolicyResource {Id = "MachinePolicies-2", Name = "Machine Policy 2"});
            operation.TentacleThumbprint = "ABCDEF";
            operation.TentaclePort = 10930;
            operation.MachineName = "Mymachine";
            operation.TentacleHostname = "Mymachine.test.com";
            operation.CommunicationStyle = CommunicationStyle.TentaclePassive;
            operation.EnvironmentNames = new[] { "Production" };
            operation.AllowOverwrite = true;
            operation.MachinePolicy = "Machine Policy 2";

            await operation.ExecuteAsync(serverEndpoint).ConfigureAwait(false);

            await client.Received().Update("/machines/whatever/1", Arg.Is<MachineResource>(m =>
                m.Id == "machines/84"
                && m.Name == "Mymachine"
                && m.EnvironmentIds.First() == "environments-2"
                && m.MachinePolicyId == "MachinePolicies-2"), Arg.Any<object>(), Arg.Is(default(CancellationToken))).ConfigureAwait(false);
        }
    }
}