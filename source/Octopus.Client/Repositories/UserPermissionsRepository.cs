using System;
using System.Collections.Generic;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories
{
    public interface IUserPermissionsRepository :
        ICanLimitToSpaces<IUserPermissionsRepository>
    {
        UserPermissionSetResource Get(UserResource user);
    }
    
    class UserPermissionsRepository : MixedScopeBaseRepository<UserPermissionSetResource>, IUserPermissionsRepository
    {
        public UserPermissionsRepository(IOctopusClient client)
            : base(client, null, null)
        {
        }

        UserPermissionsRepository(IOctopusClient client, SpaceQueryParameters spaceQueryParameters)
            : base(client, null, spaceQueryParameters)
        {
        }

        public UserPermissionSetResource Get(UserResource user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Client.Get<UserPermissionSetResource>(user.Link("Permissions"), SpaceQueryParameters);
        }

        public IUserPermissionsRepository LimitTo(bool includeGlobal, params string[] spaceIds)
        {
            var newParameters = this.CreateParameters(includeGlobal, spaceIds);
            return new UserPermissionsRepository(Client, newParameters);
        }
    }
}