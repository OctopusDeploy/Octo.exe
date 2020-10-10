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
    public class RegisterMachineOperation : RegisterMachineOperationBase, IRegisterMachineOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterMachineOperation" /> class.
        /// </summary>
        public RegisterMachineOperation() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterMachineOperation" /> class.
        /// </summary>
        /// <param name="clientFactory">The client factory.</param>
        public RegisterMachineOperation(IOctopusClientFactory clientFactory) : base(clientFactory)
        {

        }

        /// <summary>
        /// Gets or sets the environments that this machine should be added to.
        /// </summary>
        public string[] EnvironmentNames { get; set; }

        /// <summary>
        /// Gets or sets the roles that this machine belongs to.
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// Gets or sets the tenants that this machine is linked to.
        /// </summary>
        public string[] Tenants { get; set; }

        /// <summary>
        /// Gets or sets the tenant tags that this machine is linked to.
        /// </summary>
        public string[] TenantTags { get; set; }


        /// <summary>
        /// How the machine should participate in Tenanted Deployments.
        /// Allowed values are Untenanted, TenantedOrUntenanted or Tenanted
        /// </summary>
        public TenantedDeploymentMode TenantedDeploymentParticipation { get; set; }

        /// <summary>
        /// Executes the operation against the specified Octopus Deploy server.
        /// </summary>
        /// <param name="repository">The Octopus Deploy server repository.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public override void Execute(IOctopusSpaceRepository repository)
        {
            var selectedEnvironments = GetEnvironments(repository);
            var machinePolicy = GetMachinePolicy(repository);
            var machine = GetMachine(repository);
            var tenants = GetTenants(repository);
            ValidateTenantTags(repository);
            var proxy = GetProxy(repository);

            ApplyBaseChanges(machine, machinePolicy, proxy);
            ApplyDeploymentTargetChanges(machine, selectedEnvironments, tenants);

            if (machine.Id != null)
                repository.Machines.Modify(machine);
            else
                repository.Machines.Create(machine);
        }

        List<TenantResource> GetTenants(IOctopusSpaceRepository repository)
        {
            if (Tenants == null || !Tenants.Any())
            {
                return new List<TenantResource>();
            }
            var tenantsByName = repository.Tenants.FindByNames(Tenants);
            var missing = Tenants.Except(tenantsByName.Select(e => e.Name), StringComparer.OrdinalIgnoreCase).ToArray();


            var tenantsById = repository.Tenants.Get(missing);
            missing = missing.Except(tenantsById.Select(e => e.Id), StringComparer.OrdinalIgnoreCase).ToArray();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("tenant", missing));

            return tenantsById.Concat(tenantsByName).ToList();
        }

        void ValidateTenantTags(IOctopusSpaceRepository repository)
        {
            if (TenantTags == null || !TenantTags.Any())
                return;

            var tagSets = repository.TagSets.FindAll();
            var missingTags = TenantTags.Where(tt => !tagSets.Any(ts => ts.Tags.Any(t => t.CanonicalTagName.Equals(tt, StringComparison.OrdinalIgnoreCase)))).ToList();

            if (missingTags.Any())
                throw new ArgumentException(CouldNotFindMessage("tag", missingTags.ToArray()));
        }

        List<EnvironmentResource> GetEnvironments(IOctopusSpaceRepository repository)
        {
            var selectedEnvironments = repository.Environments.FindByNames(EnvironmentNames);

            var missing = EnvironmentNames.Except(selectedEnvironments.Select(e => e.Name), StringComparer.OrdinalIgnoreCase).ToList();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("environment", missing.ToArray()));

            return selectedEnvironments;
        }

        MachineResource GetMachine(IOctopusSpaceRepository repository)
        {
            var existing = default(MachineResource);
            try
            {
                existing = repository.Machines.FindByName(MachineName);
                if (!AllowOverwrite && existing?.Id != null)
                    throw new ArgumentException($"A machine named '{MachineName}' already exists on the Octopus Server in the target space. Use the 'force' parameter if you intended to update the existing machine.");
            }
            catch (OctopusDeserializationException) // eat it, probably caused by resource incompatability between versions
            {
            }
            return existing ?? new MachineResource();
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
            var selectedEnvironments = GetEnvironments(repository, token).ConfigureAwait(false);
            var machinePolicy = GetMachinePolicy(repository, token).ConfigureAwait(false);
            var machineTask = GetMachine(repository, token).ConfigureAwait(false);
            var tenants = GetTenants(repository, token).ConfigureAwait(false);
            await ValidateTenantTags(repository, token).ConfigureAwait(false);
            var proxy = GetProxy(repository, token).ConfigureAwait(false);

            var machine = await machineTask;
            ApplyBaseChanges(machine, await machinePolicy, await proxy);
            ApplyDeploymentTargetChanges(machine, await selectedEnvironments, await tenants);

            if (machine.Id != null)
                await repository.Machines.Modify(machine, token).ConfigureAwait(false);
            else
                await repository.Machines.Create(machine, token).ConfigureAwait(false);
        }

        async Task<List<TenantResource>> GetTenants(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            if (Tenants == null || !Tenants.Any())
            {
                return new List<TenantResource>();
            }

            var tenantsByName = new List<TenantResource>();
            foreach (var tenantName in Tenants)
            {
                var tenant = await repository.Tenants.FindByName(tenantName, token: token).ConfigureAwait(false);
                if (tenant != null)
                    tenantsByName.Add(tenant);
            }

            var missing = Tenants.Except(tenantsByName.Select(e => e.Name), StringComparer.OrdinalIgnoreCase).ToArray();

            var tenantsById = await repository.Tenants.Get(token, missing).ConfigureAwait(false);
            missing = missing.Except(tenantsById.Select(e => e.Id), StringComparer.OrdinalIgnoreCase).ToArray();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("tenant", missing));

            return tenantsById.Concat(tenantsByName).ToList();
        }

        async Task ValidateTenantTags(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            if (TenantTags == null || !TenantTags.Any())
                return;

            var tagSets = await repository.TagSets.FindAll(token: token).ConfigureAwait(false);
            var missingTags = TenantTags.Where(tt => !tagSets.Any(ts => ts.Tags.Any(t => t.CanonicalTagName.Equals(tt, StringComparison.OrdinalIgnoreCase)))).ToList();

            if (missingTags.Any())
                throw new ArgumentException(CouldNotFindMessage("tag", missingTags.ToArray()));
        }

        async Task<List<EnvironmentResource>> GetEnvironments(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var selectedEnvironments = new List<EnvironmentResource>();
            foreach (var environmentName in EnvironmentNames)
            {
                var environment = await repository.Environments.FindByName(environmentName, token: token).ConfigureAwait(false);
                if (environment != null)
                    selectedEnvironments.Add(environment);
            }

            var missing = EnvironmentNames.Except(selectedEnvironments.Select(e => e.Name), StringComparer.OrdinalIgnoreCase).ToList();

            if (missing.Any())
                throw new ArgumentException(CouldNotFindMessage("environment", missing.ToArray()));

            return selectedEnvironments;
        }

        async Task<MachineResource> GetMachine(IOctopusSpaceAsyncRepository repository, CancellationToken token = default)
        {
            var existing = default(MachineResource);
            try
            {
                existing = await repository.Machines.FindByName(MachineName, token: token).ConfigureAwait(false);
                if (!AllowOverwrite && existing?.Id != null)
                    throw new ArgumentException($"A machine named '{MachineName}' already exists in the environment. Use the 'force' parameter if you intended to update the existing machine.");
            }
            catch (OctopusDeserializationException) // eat it, probably caused by resource incompatability between versions
            {
            }
            return existing ?? new MachineResource();
        }

        void ApplyDeploymentTargetChanges(MachineResource machine, IEnumerable<EnvironmentResource> environment, IEnumerable<TenantResource> tenants)
        {
            machine.EnvironmentIds = new ReferenceCollection(environment.Select(e => e.Id).ToArray());
            machine.TenantIds = new ReferenceCollection(tenants.Select(t => t.Id).ToArray());
            machine.TenantTags = new ReferenceCollection(TenantTags);
            machine.Roles = new ReferenceCollection(Roles);
            machine.TenantedDeploymentParticipation = TenantedDeploymentParticipation;
        }
    }
}