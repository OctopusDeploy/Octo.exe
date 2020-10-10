using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Exceptions;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    public interface ICommunityActionTemplateRepository : IGet<CommunityActionTemplateResource>
    {
        Task<ActionTemplateResource> GetInstalledTemplate(CommunityActionTemplateResource resource, CancellationToken token = default);
        Task Install(CommunityActionTemplateResource resource, CancellationToken token = default);
        Task UpdateInstallation(CommunityActionTemplateResource resource, CancellationToken token = default);
    }

    class CommunityActionTemplateRepository : BasicRepository<CommunityActionTemplateResource>, ICommunityActionTemplateRepository
    {
        public CommunityActionTemplateRepository(IOctopusAsyncRepository repository) : base(repository, "CommunityActionTemplates")
        {
        }

        public Task Install(CommunityActionTemplateResource resource, CancellationToken token = default)
        {
            var baseLink = resource.Links["Installation"];
            var spaceResource = Repository.Scope.Apply(space => space,
                () => throw new SpaceScopedOperationInSystemContextException(),
                () => null); // Link without a space id acts on the default space

            if (spaceResource == null)
            {
                return Client.Post(baseLink.ToString(), token: token);
            }

            return Client.Post<string>(baseLink.ToString(), null, new {spaceId = spaceResource.Id}, token);
        }

        public Task UpdateInstallation(CommunityActionTemplateResource resource, CancellationToken token = default)
        {
            var baseLink = resource.Links["Installation"];
            var spaceResource = Repository.Scope.Apply(space => space,
                () => throw new SpaceScopedOperationInSystemContextException(),
                () => null); // Link without a space id acts on the default space

            if (spaceResource == null)
            {
                return Client.Put(baseLink.ToString(), token);
            }

            return Client.Put<string>(baseLink.ToString(), null, new {spaceId = spaceResource.Id}, token);
        }

        public Task<ActionTemplateResource> GetInstalledTemplate(CommunityActionTemplateResource resource, CancellationToken token = default)
        {
            var baseLink = resource.Links["InstalledTemplate"];
            var spaceResource = Repository.Scope.Apply(space => space,
                () => throw new SpaceScopedOperationInSystemContextException(),
                () => null); // Link without a space id acts on the default space

            if (spaceResource == null)
            {
                return Client.Get<ActionTemplateResource>(baseLink.ToString(), token: token);
            }

            return Client.Get<ActionTemplateResource>(baseLink.ToString(), new {spaceId = spaceResource.Id}, token);
        }
    }
}