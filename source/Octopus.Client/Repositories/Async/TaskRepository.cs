using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;
using Octopus.Client.Validation;

namespace Octopus.Client.Repositories.Async
{
    public interface ITaskRepository : IPaginate<TaskResource>, IGet<TaskResource>, ICreate<TaskResource>, ICanExtendSpaceContext<ITaskRepository>
    {
        Task<TaskResource> ExecuteHealthCheck(string description = null, int timeoutAfterMinutes = 5, int machineTimeoutAfterMinutes = 1, string environmentId = null, string[] machineIds = null, string restrictTo = null, string workerpoolId = null, string[] workerIds = null, CancellationToken token = default);
        Task<TaskResource> ExecuteCalamariUpdate(string description = null, string[] machineIds = null, CancellationToken token = default);
        Task<TaskResource> ExecuteBackup(string description = null, CancellationToken token = default);
        Task<TaskResource> ExecuteTentacleUpgrade(string description = null, string environmentId = null, string[] machineIds = null, string restrictTo = null, string workerpooltId = null, string[] workerIds = null, CancellationToken token = default);
        Task<TaskResource> ExecuteAdHocScript(string scriptBody, string[] machineIds = null, string[] environmentIds = null, string[] targetRoles = null, string description = null, string syntax = "PowerShell", BuiltInTasks.AdHocScript.TargetType? targetType = null, CancellationToken token = default);
        Task<TaskDetailsResource> GetDetails(TaskResource resource, bool? includeVerboseOutput = null, int? tail = null, CancellationToken token = default);
        Task<TaskResource> ExecuteActionTemplate(ActionTemplateResource resource, Dictionary<string, PropertyValueResource> properties, string[] machineIds = null, string[] environmentIds = null, string[] targetRoles = null, string description = null, BuiltInTasks.AdHocScript.TargetType? targetType = null, CancellationToken token = default);
        Task<TaskResource> ExecuteCommunityActionTemplatesSynchronisation(string description = null, CancellationToken token = default);
        
        /// <summary>
        /// Gets all the active tasks (optionally limited to pageSize)
        /// </summary>
        /// <param name="pageSize">Number of items per page, setting to less than the total items still retreives all items, but uses multiple requests reducing memory load on the server</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<List<TaskResource>> GetAllActive(int pageSize = int.MaxValue, CancellationToken token = default);
        
        /// <summary>
        /// Returns all active tasks (optionally limited to pageSize) along with a count of all tasks in each status
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="skip"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<TaskResourceCollection> GetActiveWithSummary(int pageSize = int.MaxValue, int skip = 0, CancellationToken token = default);

        /// <summary>
        /// Returns all tasks (optionally limited to pageSize) along with a count of all tasks in each status
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="skip"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<TaskResourceCollection> GetAllWithSummary(int pageSize = int.MaxValue, int skip = 0, CancellationToken token = default);
        
        Task<string> GetRawOutputLog(TaskResource resource, CancellationToken token = default);
        Task<TaskTypeResource[]> GetTaskTypes(CancellationToken token = default);
        Task Rerun(TaskResource resource, CancellationToken token = default);
        Task Cancel(TaskResource resource, CancellationToken token = default);
        Task ModifyState(TaskResource resource, TaskState newState, string reason, CancellationToken token = default);
        Task<IReadOnlyList<TaskResource>> GetQueuedBehindTasks(TaskResource resource, CancellationToken token = default);
        Task WaitForCompletion(TaskResource task, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Action<TaskResource[]> interval = null, CancellationToken token = default);
        Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Action<TaskResource[]> interval = null, CancellationToken token = default);
        Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Func<TaskResource[], Task> interval = null, CancellationToken token = default);
        Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, TimeSpan? timeoutAfter = null, Func<TaskResource[], Task> interval = null, CancellationToken token = default);
    }

    class TaskRepository : MixedScopeBaseRepository<TaskResource>, ITaskRepository
    {
        public TaskRepository(IOctopusAsyncRepository repository)
            : base(repository, "Tasks")
        {
        }

        TaskRepository(IOctopusAsyncRepository repository, SpaceContext spaceContext)
            : base(repository, "Tasks", spaceContext)
        {
        }

        public Task<TaskResource> ExecuteHealthCheck(
            string description = null, int timeoutAfterMinutes = 5, int machineTimeoutAfterMinutes = 1, string environmentId = null, string[] machineIds = null,
            string restrictTo = null, string workerpoolId = null, string[] workerIds = null, CancellationToken token = default)
        {
            // Default space enabled -> Creates it in the default space
            // Default space disabled -> Fails
            EnsureSingleSpaceContext();
            var resource = new TaskResource
            {
                Name = BuiltInTasks.Health.Name,
                Description = string.IsNullOrWhiteSpace(description) ? "Manual health check" : description,
                Arguments = new Dictionary<string, object>
                {
                    {BuiltInTasks.Health.Arguments.Timeout, TimeSpan.FromMinutes(timeoutAfterMinutes)},
                    {BuiltInTasks.Health.Arguments.MachineTimeout, TimeSpan.FromMinutes(machineTimeoutAfterMinutes)},
                    {BuiltInTasks.Health.Arguments.EnvironmentId, environmentId},
                    {BuiltInTasks.Health.Arguments.WorkerpoolId, workerpoolId},
                    {BuiltInTasks.Health.Arguments.RestrictedTo, restrictTo},
                    {
                        BuiltInTasks.Health.Arguments.MachineIds,
                        machineIds?.Concat(workerIds ?? new string[0]).ToArray() ?? workerIds
                    }
                }
            };
            return Create(resource, token: token);
        }

        public Task<TaskResource> ExecuteCalamariUpdate(string description = null, string[] machineIds = null, CancellationToken token = default)
        {
            EnsureSingleSpaceContext();
            var resource = new TaskResource
            {
                Name = BuiltInTasks.UpdateCalamari.Name,
                Description = string.IsNullOrWhiteSpace(description) ? "Manual Calamari update" : description,
                Arguments = new Dictionary<string, object>
                {
                    {BuiltInTasks.UpdateCalamari.Arguments.MachineIds, machineIds}
                }
            };
            return Create(resource, token: token);
        }

        public Task<TaskResource> ExecuteBackup(string description = null, CancellationToken token = default)
        {
            EnsureSystemContext();
            var resource = new TaskResource
            {
                Name = BuiltInTasks.Backup.Name,
                Description = string.IsNullOrWhiteSpace(description) ? "Manual backup" : description
            };
            return CreateSystemTask(resource, token);
        }

        public async Task<TaskResource> ExecuteTentacleUpgrade(string description = null, string environmentId = null, string[] machineIds = null, string restrictTo = null, string workerpoolId = null, string[] workerIds = null, CancellationToken token = default)
        {
            EnsureSingleSpaceContext();
            var resource = new TaskResource
            {
                Name = BuiltInTasks.Upgrade.Name,
                Description = string.IsNullOrWhiteSpace(description) ? "Manual upgrade" : description,
                Arguments = new Dictionary<string, object>
                {
                    {BuiltInTasks.Upgrade.Arguments.EnvironmentId, environmentId},
                    {BuiltInTasks.Upgrade.Arguments.WorkerpoolId, workerpoolId},
                    {BuiltInTasks.Upgrade.Arguments.RestrictedTo, restrictTo},
                    {
                        BuiltInTasks.Upgrade.Arguments.MachineIds,
                        machineIds?.Concat(workerIds ?? new string[0]).ToArray() ?? workerIds
                    }
                }
            };
            return await Create(resource, token: token).ConfigureAwait(false);
        }

        public async Task<TaskResource> ExecuteAdHocScript(string scriptBody, string[] machineIds = null, string[] environmentIds = null, string[] targetRoles = null, string description = null, string syntax = "PowerShell", BuiltInTasks.AdHocScript.TargetType? targetType = null, CancellationToken token = default)
        {
            EnsureSingleSpaceContext();
            await EnsureValidTargetType(targetType, token);
            var resource = new TaskResource
            {
                Name = BuiltInTasks.AdHocScript.Name,
                Description = string.IsNullOrWhiteSpace(description) ? "Run ad-hoc PowerShell script" : description,
                Arguments = new Dictionary<string, object>
                {
                    {BuiltInTasks.AdHocScript.Arguments.EnvironmentIds, environmentIds},
                    {BuiltInTasks.AdHocScript.Arguments.TargetRoles, targetRoles},
                    {BuiltInTasks.AdHocScript.Arguments.MachineIds, machineIds},
                    {BuiltInTasks.AdHocScript.Arguments.ScriptBody, scriptBody},
                    {BuiltInTasks.AdHocScript.Arguments.Syntax, syntax},
                    {BuiltInTasks.AdHocScript.Arguments.TargetType, targetType},
                }
            };
            return await Create(resource, token: token).ConfigureAwait(false);
        }

        public async Task<TaskResource> ExecuteActionTemplate(
            ActionTemplateResource template,
            Dictionary<string, PropertyValueResource> properties,
            string[] machineIds = null,
            string[] environmentIds = null,
            string[] targetRoles = null,
            string description = null,
            BuiltInTasks.AdHocScript.TargetType? targetType = null, 
            CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(template?.Id)) throw new ArgumentException("The step template was either null, or has no ID");
            await EnsureValidTargetType(targetType, token);
            
            var resource = new TaskResource(){SpaceId = template.SpaceId};
            resource.Name = BuiltInTasks.AdHocScript.Name;
            resource.Description = string.IsNullOrWhiteSpace(description) ? "Run step template: " + template.Name : description;
            resource.Arguments = new Dictionary<string, object>
                {
                    {BuiltInTasks.AdHocScript.Arguments.EnvironmentIds, environmentIds},
                    {BuiltInTasks.AdHocScript.Arguments.TargetRoles, targetRoles},
                    {BuiltInTasks.AdHocScript.Arguments.MachineIds, machineIds},
                    {BuiltInTasks.AdHocScript.Arguments.ActionTemplateId, template.Id},
                    {BuiltInTasks.AdHocScript.Arguments.Properties, properties},
                    {BuiltInTasks.AdHocScript.Arguments.TargetType, targetType},
                };
            return await Create(resource, token: token);
        }

        private async Task EnsureValidTargetType(BuiltInTasks.AdHocScript.TargetType? targetType, CancellationToken token)
        {
            if (targetType == BuiltInTasks.AdHocScript.TargetType.OctopusServer)
            {
                var minimumRequiredVersion = new SemanticVersion("2019.13.5");
                await EnsureServerIsMinimumVersion(minimumRequiredVersion,
                    currentServerVersion => $"The version of the Octopus Server ('{currentServerVersion}') you are connecting to is not compatible with the TargetType value of 'OctopusServer' for this API call. Please upgrade your Octopus Server to version '{minimumRequiredVersion}' or greater.");
            }
        }

        public Task<TaskResource> ExecuteCommunityActionTemplatesSynchronisation(string description = null, CancellationToken token = default)
        {
            EnsureSystemContext();

            // SpaceId always need to be null, use a different Create method to handle that
            var resource = new TaskResource
            {
                Name = BuiltInTasks.SyncCommunityActionTemplates.Name,
                Description = description ?? "Run " + BuiltInTasks.SyncCommunityActionTemplates.Name
            };

            return CreateSystemTask(resource, token);
        }

        public async Task<TaskDetailsResource> GetDetails(TaskResource resource, bool? includeVerboseOutput = null, int? tail = null, CancellationToken token = default)
        {
            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (includeVerboseOutput.HasValue)
                args.Add("verbose", includeVerboseOutput.Value);

            if (tail.HasValue)
                args.Add("tail", tail.Value);
            var parameters = ParameterHelper.CombineParameters(GetAdditionalQueryParameters(), args);
            return await Client.Get<TaskDetailsResource>(resource.Link("Details"), parameters, token).ConfigureAwait(false);
        }

        public async Task<string> GetRawOutputLog(TaskResource resource, CancellationToken token = default)
        {
            return await Client.Get<string>(resource.Link("Raw"), GetAdditionalQueryParameters(), token).ConfigureAwait(false);
        }

        public async Task<TaskTypeResource[]> GetTaskTypes(CancellationToken token = default)
        {
            return await Client.Get<TaskTypeResource[]>((await Client.Repository.LoadRootDocument()).Links["TaskTypes"], token: token);
        }

        public async Task Rerun(TaskResource resource, CancellationToken token = default)
        {
            EnsureTaskCanRunInTheCurrentContext(resource);
            await Client.Post(resource.Link("Rerun"), (TaskResource)null, token: token).ConfigureAwait(false);
        }

        public async Task Cancel(TaskResource resource, CancellationToken token = default)
        {
            EnsureTaskCanRunInTheCurrentContext(resource);
            await Client.Post(resource.Link("Cancel"), (TaskResource)null, token: token).ConfigureAwait(false);
        }

        public async Task ModifyState(TaskResource resource, TaskState newState, string reason, CancellationToken token = default)
        {
            EnsureTaskCanRunInTheCurrentContext(resource);
            await Client.Post(resource.Link("State"), new { state = newState, reason = reason }, token: token).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<TaskResource>> GetQueuedBehindTasks(TaskResource resource, CancellationToken token = default)
        {
            return await Client.ListAll<TaskResource>(resource.Link("QueuedBehind"), GetAdditionalQueryParameters(), token).ConfigureAwait(false);
        }

        public Task WaitForCompletion(TaskResource task, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Action<TaskResource[]> interval = null, CancellationToken token = default)
        {
            return WaitForCompletion(new[] { task }, pollIntervalSeconds, timeoutAfterMinutes, interval, token);
        }

        public Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Action<TaskResource[]> interval = null, CancellationToken token = default)
        {
            Func<TaskResource[], Task> taskInterval = null;
            if (interval != null)
                taskInterval = tr => Task.Run(() => interval(tr));

            return WaitForCompletion(tasks, pollIntervalSeconds, timeoutAfterMinutes, taskInterval, token);
        }

        public Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, int timeoutAfterMinutes = 0, Func<TaskResource[], Task> interval = null, CancellationToken token = default)
            => WaitForCompletion(tasks, pollIntervalSeconds, TimeSpan.FromMinutes(timeoutAfterMinutes), interval, token);

        public async Task WaitForCompletion(TaskResource[] tasks, int pollIntervalSeconds = 4, TimeSpan? timeoutAfter = null, Func<TaskResource[], Task> interval = null, CancellationToken token = default)
        {
            var start = Stopwatch.StartNew();
            if (tasks == null || tasks.Length == 0)
                return;
            var additionalQueryParameters = GetAdditionalQueryParameters();
            while (true)
            {
                var stillRunning = await Task.WhenAll(
                        tasks.Select(t => Client.Get<TaskResource>(t.Link("Self"), additionalQueryParameters, token))
                    )
                    .ConfigureAwait(false);

                if (interval != null)
                    await interval(stillRunning).ConfigureAwait(false);

                if (stillRunning.All(t => t.IsCompleted))
                    return;

                if (timeoutAfter.HasValue && timeoutAfter > TimeSpan.Zero && start.Elapsed > timeoutAfter)
                {
                    throw new TimeoutException($"One or more tasks did not complete before the timeout was reached. We waited {start.Elapsed:hh\\:mm\\:ss}  for the tasks to complete.");
                }

                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), token).ConfigureAwait(false);
            }
        }

        public Task<List<TaskResource>> GetAllActive(int pageSize = int.MaxValue, CancellationToken token = default) => FindAll(pathParameters: new { active = true, take = pageSize }, token: token);

        public async Task<TaskResourceCollection> GetActiveWithSummary(int pageSize = int.MaxValue, int skip = 0, CancellationToken token = default)
            => await Client.Get<TaskResourceCollection>(await ResolveLink(), new {active = true, take = pageSize, skip}, token);

        public async Task<TaskResourceCollection> GetAllWithSummary(int pageSize = int.MaxValue, int skip = 0, CancellationToken token = default)
            => await Client.Get<TaskResourceCollection>(await ResolveLink(), new {take = pageSize, skip}, token);

        public ITaskRepository UsingContext(SpaceContext spaceContext)
        {
            return new TaskRepository(Repository, spaceContext);
        }

        void EnsureTaskCanRunInTheCurrentContext(TaskResource task)
        {
            if (string.IsNullOrEmpty(task.SpaceId))
                return;
            var spaceContext = GetCurrentSpaceContext();
            
            spaceContext.ApplySpaceSelection(spaces =>
            {
                if (spaces.All(space => space.Id != task.SpaceId))
                {
                    throw new SpaceScopedOperationOutsideOfCurrentSpaceContextException(task.SpaceId, spaceContext);
                }
            }, () => { });
        }

        async Task<TaskResource> CreateSystemTask(TaskResource task, CancellationToken token)
        {
            return await Client.Create(await Repository.Link(CollectionLinkName).ConfigureAwait(false), task, token: token).ConfigureAwait(false);
        }
    }

    public class SpaceScopedOperationOutsideOfCurrentSpaceContextException : Exception
    {
        public SpaceScopedOperationOutsideOfCurrentSpaceContextException(string spaceId, SpaceContext context) 
            : base($"Attempted to perform a space scoped operation within space {spaceId}, but your current space context does not contain that space id. " +
                   $"Current Space Context: {context.ApplySpaceSelection(spaces => string.Join(", ", spaces), () => "all spaces")}")
        {
        }
    }
}