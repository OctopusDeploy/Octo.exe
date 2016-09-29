using System;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories
{
    public interface IUserRolesRepository : IFindByName<UserRoleResource>, IGet<UserRoleResource>, ICreate<TenantResource>, IModify<TenantResource>
    {
    }
}