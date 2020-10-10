using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IInterruptionRepository : IGet<InterruptionResource>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take (First supported in Server 3.14.15)</param>
        /// <param name="pendingOnly"></param>
        /// <param name="regardingDocumentId"></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        Task<ResourceCollection<InterruptionResource>> List(int skip = 0, int? take = null, bool pendingOnly = false, string regardingDocumentId = null, CancellationToken token = default);
        Task Submit(InterruptionResource interruption, CancellationToken token = default);
        Task TakeResponsibility(InterruptionResource interruption, CancellationToken token = default);
        Task<UserResource> GetResponsibleUser(InterruptionResource interruption, CancellationToken token = default);
    }

    class InterruptionRepository : BasicRepository<InterruptionResource>, IInterruptionRepository
    {
        public InterruptionRepository(IOctopusAsyncRepository repository)
            : base(repository, "Interruptions")
        {
        }

        public async Task<ResourceCollection<InterruptionResource>> List(int skip = 0, int? take = null, bool pendingOnly = false, string regardingDocumentId = null, CancellationToken token = default)
        {
            return await Client.List<InterruptionResource>(await Repository.Link("Interruptions").ConfigureAwait(false), new { skip, take, pendingOnly, regarding = regardingDocumentId }, token).ConfigureAwait(false);
        }

        public Task Submit(InterruptionResource interruption, CancellationToken token = default)
        {
            return Client.Post(interruption.Link("Submit"), interruption.Form.Values, token: token);
        }

        public Task TakeResponsibility(InterruptionResource interruption, CancellationToken token = default)
        {
            return Client.Put(interruption.Link("Responsible"), (InterruptionResource)null, token: token);
        }

        public Task<UserResource> GetResponsibleUser(InterruptionResource interruption, CancellationToken token = default)
        {
            return Client.Get<UserResource>(interruption.Link("Responsible"), token: token);
        }
    }
}
