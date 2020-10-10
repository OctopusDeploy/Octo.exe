using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{ 
    public interface IUserInvitesRepository
    {
        Task<InvitationResource> Invite(string addToTeamId, CancellationToken token = default);
        Task<InvitationResource> Invite(ReferenceCollection addToTeamIds, CancellationToken token = default);
    }
    
    class UserInvitesRepository : MixedScopeBaseRepository<InvitationResource>, IUserInvitesRepository
    {
        public UserInvitesRepository(IOctopusAsyncRepository repository) : base(repository, "Invitations")
        {
        }

        public Task<InvitationResource> Invite(string addToTeamId, CancellationToken token = default)
        {
            if (addToTeamId == null) throw new ArgumentNullException(nameof(addToTeamId));
            return Invite(new ReferenceCollection { addToTeamId }, token);
        }

        public Task<InvitationResource> Invite(ReferenceCollection addToTeamIds, CancellationToken token = default)
        {
            var invitationResource = new InvitationResource { AddToTeamIds = addToTeamIds ?? new ReferenceCollection() };
            EnrichSpaceId(invitationResource);
            return Create(invitationResource, token: token);
        }
    }
}