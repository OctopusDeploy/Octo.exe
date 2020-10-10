using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface ITeamsRepository :
        ICreate<TeamResource>,
        IModify<TeamResource>,
        IDelete<TeamResource>,
        IFindByName<TeamResource>,
        IGet<TeamResource>,
        ICanExtendSpaceContext<ITeamsRepository>
    {
        Task<List<ScopedUserRoleResource>> GetScopedUserRoles(TeamResource team, CancellationToken token = default);
    }

    class TeamsRepository : MixedScopeBaseRepository<TeamResource>, ITeamsRepository
    {
        public TeamsRepository(IOctopusAsyncRepository repository)
            : base(repository, "Teams")
        {
            MinimumCompatibleVersion("2019.1.0");
        }

        TeamsRepository(IOctopusAsyncRepository repository, SpaceContext spaceContext)
            : base(repository, "Teams", spaceContext)
        {
            MinimumCompatibleVersion("2019.1.0");
        }

        public async Task<List<ScopedUserRoleResource>> GetScopedUserRoles(TeamResource team, CancellationToken token = default)
        {
            await ThrowIfServerVersionIsNotCompatible();
            
            if (team == null) throw new ArgumentNullException(nameof(team));
            var resources = new List<ScopedUserRoleResource>();

            await Client.Paginate<ScopedUserRoleResource>(team.Link("ScopedUserRoles"), GetAdditionalQueryParameters(), page =>
            {
                resources.AddRange(page.Items);
                return true;
            }, token).ConfigureAwait(false);

            return resources;
        }

        public ITeamsRepository UsingContext(SpaceContext spaceContext)
        {
            return new TeamsRepository(Repository, spaceContext);
        }
    }
}
