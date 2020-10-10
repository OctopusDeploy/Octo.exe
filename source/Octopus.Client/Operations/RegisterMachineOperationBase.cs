using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;

namespace Octopus.Client.Operations
{
    /// <summary>
    /// Encapsulates the operation for registering machines.
    /// </summary>
    public abstract  class RegisterMachineOperationBase : IRegisterMachineOperationBase
    {
        readonly IOctopusClientFactory clientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterMachineOperation" /> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        public RegisterMachineOperationBase(IOctopusClientFactory clientFactory)
        {
            this.clientFactory = clientFactory ?? new OctopusClientFactory();
        }


        /// <summary>
        /// Gets or sets the name of the machine that will be used within Octopus to identify this machine.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Get or sets the machine policy that applied to this machine.
        /// </summary>
        public string MachinePolicy { get; set; }

        /// <summary>
        /// Gets or sets the hostname that Octopus should use when communicating with the Tentacle.
        /// </summary>
        public string TentacleHostname { get; set; }

        /// <summary>
        /// Gets or sets the TCP port that Octopus should use when communicating with the Tentacle.
        /// </summary>
        public int TentaclePort { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint that Octopus should expect when communicating with the Tentacle.
        /// </summary>
        public string TentacleThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the name of the proxy that Octopus should use when communicating with the Tentacle.
        /// </summary>
        public string ProxyName { get; set; }

        /// <summary>
        /// If a machine with the same name already exists, it won't be overwritten by default (instead, an
        /// <see cref="ArgumentException" /> will be thrown).
        /// Set this property to <c>true</c> if you do want the existing machine to be overwritten.
        /// </summary>
        public bool AllowOverwrite { get; set; }

        /// <summary>
        /// The communication style to use with the Tentacle. Allowed values are: TentacleActive, in which case the
        /// Tentacle will connect to the Octopus Server for instructions; or, TentaclePassive, in which case the
        /// Tentacle will listen for commands from the server (default).
        /// </summary>
        public CommunicationStyle CommunicationStyle { get; set; }

        public Uri SubscriptionId { get; set; }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="serverEndpoint">The Octopus Deploy server endpoint.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public void Execute(OctopusServerEndpoint serverEndpoint)
        {
            using (var client = clientFactory.CreateClient(serverEndpoint))
            {
                var repository = new OctopusRepository(client);

                Execute(repository);
            }
        }


        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public void Execute(OctopusRepository repository)
        {
            Execute((IOctopusSpaceRepository) repository);
        }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public abstract void Execute(IOctopusSpaceRepository repository);

        protected MachinePolicyResource GetMachinePolicy(IOctopusSpaceRepository repository)
        {
            var machinePolicy = default(MachinePolicyResource);
            if (!string.IsNullOrEmpty(MachinePolicy))
            {
                machinePolicy = repository.MachinePolicies.FindByName(MachinePolicy);
                if (machinePolicy == null)
                    throw new ArgumentException(CouldNotFindMessage("machine policy", MachinePolicy));
            }
            return machinePolicy;
        }

        protected ProxyResource GetProxy(IOctopusSpaceRepository repository)
        {
            var proxy = default(ProxyResource);
            if (!string.IsNullOrEmpty(ProxyName))
            {
                proxy = repository.Proxies.FindByName(ProxyName);
                if (proxy == null)
                    throw new ArgumentException(CouldNotFindMessage("proxy name", ProxyName));
            }
            return proxy;
        }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="serverEndpoint">The Octopus Deploy server endpoint.</param>
        /// <param name="token">A cancellation token</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public async Task ExecuteAsync(OctopusServerEndpoint serverEndpoint, CancellationToken token = default)
        {
            using (var client = await clientFactory.CreateAsyncClient(serverEndpoint).ConfigureAwait(false))
            {
                var repository = new OctopusAsyncRepository(client);

                await ExecuteAsync(repository, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <param name="token">A cancellation token</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public async Task ExecuteAsync(OctopusAsyncRepository repository, CancellationToken token = default)
        {
            await ExecuteAsync((IOctopusSpaceAsyncRepository) repository, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <param name="token">A cancellation token</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public abstract Task ExecuteAsync(IOctopusSpaceAsyncRepository repository, CancellationToken token = default);

        protected async Task<MachinePolicyResource> GetMachinePolicy(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {

            var machinePolicy = default(MachinePolicyResource);
            if (!string.IsNullOrEmpty(MachinePolicy))
            {
                machinePolicy = await repository.MachinePolicies.FindByName(MachinePolicy, token: token).ConfigureAwait(false);
                if (machinePolicy == null)
                    throw new ArgumentException(CouldNotFindMessage("machine policy", MachinePolicy));
            }
            return machinePolicy;
        }

        protected async Task<ProxyResource> GetProxy(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var proxy = default(ProxyResource);
            if (!string.IsNullOrEmpty(ProxyName))
            {
                proxy = await repository.Proxies.FindByName(ProxyName, token: token).ConfigureAwait(false);
                if (proxy == null)
                    throw new ArgumentException(CouldNotFindMessage("proxy name", ProxyName));
            }
            return proxy;
        }


        protected void ApplyBaseChanges(MachineBasedResource machine, MachinePolicyResource machinePolicy, ProxyResource proxy)
        {
            machine.Name = MachineName;
            if (machinePolicy != null)
                machine.MachinePolicyId = machinePolicy.Id;

            if (CommunicationStyle == CommunicationStyle.TentaclePassive)
            {
                var listening = new ListeningTentacleEndpointResource();
                listening.Uri = new Uri("https://" + TentacleHostname.ToLowerInvariant() + ":" + TentaclePort.ToString(CultureInfo.InvariantCulture) + "/").ToString();
                listening.Thumbprint = TentacleThumbprint;
                listening.ProxyId = proxy?.Id;
                machine.Endpoint = listening;
            }
            else if (CommunicationStyle == CommunicationStyle.TentacleActive)
            {
                var polling = new PollingTentacleEndpointResource();
                polling.Uri = SubscriptionId.ToString();
                polling.Thumbprint = TentacleThumbprint;
                machine.Endpoint = polling;
            }
        }

        protected static string CouldNotFindMessage(string modelType, params string[] missing)
        {
            return missing.Length == 1
                ? $"Could not find the {modelType} named {missing.Single()} on the Octopus Server. Ensure the {modelType} exists and you have permission to access it."
                : $"Could not find the {modelType}s named: {string.Join(", ", missing)} on the Octopus Server. Ensure the {modelType}s exist and you have permission to access them.";
        }
    }
}