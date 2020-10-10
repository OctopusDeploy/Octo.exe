using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IUserRepository :
        IPaginate<UserResource>,
        IGet<UserResource>,
        IModify<UserResource>,
        IDelete<UserResource>,
        ICreate<UserResource>
    {
        Task<UserResource> FindByUsername(string username, CancellationToken token = default);
        Task<UserResource> Create(string username, string displayName, string password = null, string emailAddress = null, CancellationToken token = default);
        Task<UserResource> CreateServiceAccount(string username, string displayName, CancellationToken token = default);
        Task<UserResource> Register(RegisterCommand registerCommand, CancellationToken token = default);
        Task SignIn(LoginCommand loginCommand, CancellationToken token = default);
        Task SignIn(string username, string password, bool rememberMe = false, CancellationToken token = default);
        Task SignOut(CancellationToken token = default);
        Task<UserResource> GetCurrent(CancellationToken token = default);
        Task<SpaceResource[]> GetSpaces(UserResource user, CancellationToken token = default);
        Task<ApiKeyResource> CreateApiKey(UserResource user, string purpose = null, CancellationToken token = default);
        Task<List<ApiKeyResource>> GetApiKeys(UserResource user, CancellationToken token = default);
        Task RevokeApiKey(ApiKeyResource apiKey, CancellationToken token = default);
        [Obsolete("Use the " + nameof(IUserInvitesRepository) + " instead", false)]
        Task<InvitationResource> Invite(string addToTeamId, CancellationToken token = default);
        [Obsolete("Use the " + nameof(IUserInvitesRepository) + " instead", false)]
        Task<InvitationResource> Invite(ReferenceCollection addToTeamIds, CancellationToken token = default);
    }

    class UserRepository : BasicRepository<UserResource>, IUserRepository
    {
        readonly BasicRepository<InvitationResource> invitations;

        public UserRepository(IOctopusAsyncRepository repository)
            : base(repository, "Users")
        {
            invitations = new LegacyInvitationRepository(Repository);
        }

        public Task<UserResource> FindByUsername(string username, CancellationToken token = default) 
            => FindOne(u => u.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase), pathParameters: new {filter = username}, token: token);

        public Task<UserResource> Create(string username, string displayName, string password = null, string emailAddress = null, CancellationToken token = default)
        {
            return Create(new UserResource
            {
                Username = username,
                DisplayName = displayName,
                Password = password,
                EmailAddress = emailAddress,
                IsActive = true,
                IsService = false
            });
        }

        public Task<UserResource> CreateServiceAccount(string username, string displayName, CancellationToken token = default)
        {
            return Create(new UserResource
            {
                Username = username,
                DisplayName = displayName,
                IsActive = true,
                IsService = true
            }, token: token);
        }

        public async Task<UserResource> Register(RegisterCommand registerCommand, CancellationToken token = default)
        {
            return await Client.Post<UserResource,UserResource>(await Repository.Link("Register").ConfigureAwait(false), registerCommand, token: token).ConfigureAwait(false);
        }

        public async Task SignIn(LoginCommand loginCommand, CancellationToken token = default)
        {
            await Client.SignIn(loginCommand, token).ConfigureAwait(false);
        }

        public Task SignIn(string username, string password, bool rememberMe = false, CancellationToken token = default)
        {
            return SignIn(new LoginCommand() {Username = username, Password = password, RememberMe = rememberMe}, token);
        }

        public Task SignOut(CancellationToken token = default)
        {
            return Client.SignOut(token);
        }

        public async Task<UserResource> GetCurrent(CancellationToken token = default)
        {
            return await Client.Get<UserResource>(await Repository.Link("CurrentUser").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public Task<SpaceResource[]> GetSpaces(UserResource user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Client.Get<SpaceResource[]>(user.Link("Spaces"), token: token);
        }

        public Task<ApiKeyResource> CreateApiKey(UserResource user, string purpose = null, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException("user");
            return Client.Post<object, ApiKeyResource>(user.Link("ApiKeys"), new
            {
                Purpose = purpose ?? "Requested by Octopus.Client"
            }, token: token);
        }

        public async Task<List<ApiKeyResource>> GetApiKeys(UserResource user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException("user");
            var resources = new List<ApiKeyResource>();

            await Client.Paginate<ApiKeyResource>(user.Link("ApiKeys"), page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public Task RevokeApiKey(ApiKeyResource apiKey, CancellationToken token = default)
        {
            return Client.Delete(apiKey.Link("Self"), token: token);
        }

        [Obsolete("Use the " + nameof(IUserInvitesRepository) + " instead", false)]
        public Task<InvitationResource> Invite(string addToTeamId, CancellationToken token = default)
        {
            if (addToTeamId == null) throw new ArgumentNullException("addToTeamId");
            return Invite(new ReferenceCollection { addToTeamId }, token);
        }

        [Obsolete("Use the " + nameof(IUserInvitesRepository) + " instead", false)]
        public Task<InvitationResource> Invite(ReferenceCollection addToTeamIds, CancellationToken token = default)
        {
            return invitations.Create(new InvitationResource { AddToTeamIds = addToTeamIds ?? new ReferenceCollection() }, token: token);
        }
    }
}
