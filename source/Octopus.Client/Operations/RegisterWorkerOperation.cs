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
    public class RegisterWorkerOperation : RegisterMachineOperationBase, IRegisterWorkerOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterWorkerOperation" /> class.
        /// </summary>
        public RegisterWorkerOperation() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterWorkerOperation" /> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        public RegisterWorkerOperation(IOctopusClientFactory clientFactory) : base(clientFactory)
        {

        }

        /// <summary>
        /// Gets or sets the worker pools that this machine should be added to.
        /// </summary>
        public string[] WorkerPoolNames { get; set; }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public override void Execute(IOctopusSpaceRepository repository)
        {
            var selectedPools = GetWorkerPools(repository);
            var machinePolicy = GetMachinePolicy(repository);
            var machine = GetWorker(repository);
            var proxy = GetProxy(repository);

            ApplyBaseChanges(machine, machinePolicy, proxy);

            machine.WorkerPoolIds = new ReferenceCollection(selectedPools.Select(p => p.Id).ToArray());

            if (machine.Id != null)
                repository.Workers.Modify(machine);
            else
                repository.Workers.Create(machine);
        }

        List<WorkerPoolResource> GetWorkerPools(IOctopusSpaceRepository repository)
        {
            var selectedPools = repository.WorkerPools.FindByNames(WorkerPoolNames);

            var missing = WorkerPoolNames.Except(selectedPools.Select(p => p.Name), StringComparer.OrdinalIgnoreCase).ToList();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("worker pool", missing.ToArray()));

            return selectedPools;
        }

        WorkerResource GetWorker(IOctopusSpaceRepository repository)
        {
            var existing = default(WorkerResource);
            try
            {
                existing = repository.Workers.FindByName(MachineName);
                if (!AllowOverwrite && existing?.Id != null)
                    throw new ArgumentException($"A worker named '{MachineName}' already exists. Use the 'force' parameter if you intended to update the existing machine.");
            }
            catch (OctopusDeserializationException) // eat it, probably caused by resource incompatability between versions
            {
            }
            return existing ?? new WorkerResource();
        }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <param name="token">A cancellation token</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public override async Task ExecuteAsync(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var selectedPools = GetWorkerPools(repository, token).ConfigureAwait(false);
            var machinePolicy = GetMachinePolicy(repository, token).ConfigureAwait(false);
            var machineTask = GetWorker(repository, token).ConfigureAwait(false);
            var proxy = GetProxy(repository, token).ConfigureAwait(false);

            var machine = await machineTask;
            ApplyBaseChanges(machine, await machinePolicy, await proxy);

            machine.WorkerPoolIds = new ReferenceCollection((await selectedPools).Select(p => p.Id).ToArray());

            if (machine.Id != null)
                await repository.Workers.Modify(machine, token).ConfigureAwait(false);
            else
                await repository.Workers.Create(machine, token: token).ConfigureAwait(false);
        }

        async Task<List<WorkerPoolResource>> GetWorkerPools(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var selectedPools = await repository.WorkerPools.FindByNames(WorkerPoolNames, token: token).ConfigureAwait(false);

            var missing = WorkerPoolNames.Except(selectedPools.Select(p => p.Name), StringComparer.OrdinalIgnoreCase).ToList();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("worker pool", missing.ToArray()));

            return selectedPools;
        }

        async Task<WorkerResource> GetWorker(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var existing = default(WorkerResource);
            try
            {
                existing = await repository.Workers.FindByName(MachineName, token: token).ConfigureAwait(false);
                if (!AllowOverwrite && existing?.Id != null)
                    throw new ArgumentException($"A worker named '{MachineName}' already exists. Use the 'force' parameter if you intended to update the existing machine.");
            }
            catch (OctopusDeserializationException) // eat it, probably caused by resource incompatability between versions
            {
            }
            return existing ?? new WorkerResource();
        }
    }
}