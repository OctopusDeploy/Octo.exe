using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Operations
{
    /// <summary>
    /// Base for registering Deployment Targets and Workers
    /// </summary>
    public interface IRegisterMachineOperationBase
    {
        /// <summary>
        /// Gets or sets the name of the machine that will be used within Octopus to identify this machine.
        /// </summary>
        string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the machine policy that applies to this machine.
        /// </summary>
        string MachinePolicy { get; set; }

        /// <summary>
        /// Gets or sets the hostname that Octopus should use when communicating with the Tentacle.
        /// </summary>
        string TentacleHostname { get; set; }

        /// <summary>
        /// Gets or sets the TCP port that Octopus should use when communicating with the Tentacle.
        /// </summary>
        int TentaclePort { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint that Octopus should expect when communicating with the Tentacle.
        /// </summary>
        string TentacleThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the name of the proxy that Octopus should use when communicating with the Tentacle.
        /// </summary>
        string ProxyName { get; set; }

        /// <summary>
        /// If a machine with the same name already exists, it won't be overwritten by default (instead, an
        /// <see cref="ArgumentException" /> will be thrown).
        /// Set this property to <c>true</c> if you do want the existing machine to be overwritten.
        /// </summary>
        bool AllowOverwrite { get; set; }

        /// <summary>
        /// The communication style to use with the Tentacle. Allowed values are: TentacleActive, in which case the
        /// Tentacle will connect to the Octopus Server for instructions; or, TentaclePassive, in which case the
        /// Tentacle will listen for commands from the server (default).
        /// </summary>
        CommunicationStyle CommunicationStyle { get; set; }

        Uri SubscriptionId { get; set; }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="serverEndpoint">The Octopus Deploy server endpoint.</param>
        void Execute(OctopusServerEndpoint serverEndpoint);

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy repository.</param>
        void Execute(OctopusRepository repository);

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy repository.</param>
        void Execute(IOctopusSpaceRepository repository);

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="serverEndpoint">The Octopus Deploy server endpoint.</param>
        /// <param name="token">A cancellation token</param>
        Task ExecuteAsync(OctopusServerEndpoint serverEndpoint, CancellationToken token = default);

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy repository.</param>
        /// <param name="token">A cancellation token</param>
        Task ExecuteAsync(OctopusAsyncRepository repository, CancellationToken token = default);

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy repository.</param>
        /// <param name="token">A cancellation token</param>
        Task ExecuteAsync(IOctopusSpaceAsyncRepository repository, CancellationToken token = default);
    }
}