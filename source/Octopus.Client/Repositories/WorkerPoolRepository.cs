using System.Collections.Generic;
using Octopus.Client.Editors;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories
{
    public interface IWorkerPoolRepository : IFindByName<WorkerPoolResource>, IGet<WorkerPoolResource>, ICreate<WorkerPoolResource>, IModify<WorkerPoolResource>, IDelete<WorkerPoolResource>, IGetAll<WorkerPoolResource>
    {
        List<WorkerMachineResource> GetMachines(WorkerPoolResource workerpool,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null);
        WorkerPoolsSummaryResource Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            bool? hideEmptypools = false);
        void Sort(string[] workerpoolIdsInOrder);
        WorkerPoolEditor CreateOrModify(string name);
        WorkerPoolEditor CreateOrModify(string name, string description);
    }

    class WorkerPoolRepository : BasicRepository<WorkerPoolResource>, IWorkerPoolRepository
    {
        public WorkerPoolRepository(IOctopusClient client)
            : base(client, "WorkerPools")
        {
        }

        public List<WorkerMachineResource> GetMachines(WorkerPoolResource pool,
            int? skip = 0,
            int? take = null,
            string partialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null)
        {
            var resources = new List<WorkerMachineResource>();

            Client.Paginate<WorkerMachineResource>(pool.Link("WorkerMachines"), new
            {
                skip,
                take,
                partialName,
                isDisabled,
                healthStatuses,
                commStyles
            }, page =>
            {
                resources.AddRange(page.Items);
                return true;
            });

            return resources;
        }

        public WorkerPoolsSummaryResource Summary(
            string ids = null,
            string partialName = null,
            string machinePartialName = null,
            bool? isDisabled = false,
            string healthStatuses = null,
            string commStyles = null,
            bool? hideEmptyPools = false)
        {
            return Client.Get<WorkerPoolsSummaryResource>(Client.RootDocument.Link("WorkerPoolsSummary"), new
            {
                ids,
                partialName,
                machinePartialName,
                isDisabled,
                healthStatuses,
                commStyles,
                hideEmptyPools,
            });
        }

        public void Sort(string[] workerpoolIdsInOrder)
        {
            Client.Put(Client.RootDocument.Link("WorkerPoolsSortOrder"), workerpoolIdsInOrder);
        }

        public WorkerPoolEditor CreateOrModify(string name)
        {
            return new WorkerPoolEditor(this).CreateOrModify(name);
        }

        public WorkerPoolEditor CreateOrModify(string name, string description)
        {
            return new WorkerPoolEditor(this).CreateOrModify(name, description);
        }
    }
}