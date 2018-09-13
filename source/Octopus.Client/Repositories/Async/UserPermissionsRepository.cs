using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface IUserPermissionsRepository :
        ICanLimitToSpaces<IUserPermissionsRepository>
    {
        Task<UserPermissionSetResource> Get(UserResource user);
    }
    
    class UserPermissionsRepository : MixedScopeBaseRepository<UserPermissionSetResource>, IUserPermissionsRepository
    {
        public UserPermissionsRepository(IOctopusAsyncClient client)
            : base(client, null, null)
        {
        }

        UserPermissionsRepository(IOctopusAsyncClient client, SpaceQueryParameters spaceQueryParameters)
            : base(client, null, spaceQueryParameters)
        {
        }

        public Task<UserPermissionSetResource> Get(UserResource user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Client.Get<UserPermissionSetResource>(user.Link("Permissions"));
        }

        public IUserPermissionsRepository LimitTo(bool includeGlobal, params string[] spaceIds)
        {
            var newParameters = this.CreateParameters(includeGlobal, spaceIds);
            return new UserPermissionsRepository(Client, newParameters);
        }
    }
}