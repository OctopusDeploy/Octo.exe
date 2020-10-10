using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface IUserPermissionsRepository :
        ICanExtendSpaceContext<IUserPermissionsRepository>
    {
        Task<UserPermissionSetResource> Get(UserResource user, CancellationToken token = default);
        Task<UserPermissionSetResource> GetConfiguration(UserResource user, CancellationToken token = default);
        Task<Stream> Export(UserPermissionSetResource userPermissions, CancellationToken token = default);
    }
    
    class UserPermissionsRepository : MixedScopeBaseRepository<UserPermissionSetResource>, IUserPermissionsRepository
    {
        public UserPermissionsRepository(IOctopusAsyncRepository repository)
            : base(repository, null)
        {
        }

        UserPermissionsRepository(IOctopusAsyncRepository repository, SpaceContext spaceContext)
            : base(repository, null, spaceContext)
        {
        }

        public async Task<UserPermissionSetResource> Get(UserResource user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await Client.Get<UserPermissionSetResource>(user.Link("Permissions"), GetAdditionalQueryParameters(), token).ConfigureAwait(false);
        }

        public async Task<UserPermissionSetResource> GetConfiguration(UserResource user, CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await Client.Get<UserPermissionSetResource>(user.Link("PermissionsConfiguration"), GetAdditionalQueryParameters(), token).ConfigureAwait(false);
        }

        public async Task<Stream> Export(UserPermissionSetResource userPermissions, CancellationToken token = default)
        {
            if (userPermissions == null) throw new ArgumentNullException(nameof(userPermissions));
            return await Client.GetContent(userPermissions.Link("Export"), GetAdditionalQueryParameters(), token).ConfigureAwait(false);
        }

        public IUserPermissionsRepository UsingContext(SpaceContext spaceContext)
        {
            return new UserPermissionsRepository(Repository, spaceContext);
        }
    }
}