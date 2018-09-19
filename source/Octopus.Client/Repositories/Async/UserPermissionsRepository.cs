using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface IUserPermissionsRepository :
        ICanExtendSpaceContext<IUserPermissionsRepository>
    {
        Task<UserPermissionSetResource> Get(UserResource user);
        Task<Stream> Export(UserPermissionSetResource userPermissions);
    }
    
    class UserPermissionsRepository : MixedScopeBaseRepository<UserPermissionSetResource>, IUserPermissionsRepository
    {
        public UserPermissionsRepository(IOctopusAsyncClient client)
            : base(client, null, null)
        {
        }

        UserPermissionsRepository(IOctopusAsyncClient client, SpaceContext spaceContext)
            : base(client, null, spaceContext)
        {
        }

        public Task<UserPermissionSetResource> Get(UserResource user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Client.Get<UserPermissionSetResource>(user.Link("Permissions"), AdditionalQueryParameters);
        }
        
        public Task<Stream> Export(UserPermissionSetResource userPermissions)
        {
            if (userPermissions == null) throw new ArgumentNullException(nameof(userPermissions));
            return Client.GetContent(userPermissions.Link("Export"), AdditionalQueryParameters);
        }

        public IUserPermissionsRepository Including(SpaceContext spaceContext)
        {
            return new UserPermissionsRepository(Client, base.ExtendSpaceContext(spaceContext));
        }
    }
}