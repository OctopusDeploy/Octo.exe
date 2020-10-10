using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface IActionTemplateRepository : ICreate<ActionTemplateResource>, IModify<ActionTemplateResource>, IDelete<ActionTemplateResource>, IGet<ActionTemplateResource>, IFindByName<ActionTemplateResource>, IGetAll<ActionTemplateResource>
    {
        Task<List<ActionTemplateSearchResource>> Search(CancellationToken token = default);
        Task<ActionTemplateResource> GetVersion(ActionTemplateResource resource, int version, CancellationToken token = default);
        Task<ActionUpdateResultResource[]> UpdateActions(ActionTemplateResource actionTemplate, ActionsUpdateResource update, CancellationToken token = default);
        Task SetLogo(ActionTemplateResource resource, string fileName, Stream contents, CancellationToken token = default);
    }

    class ActionTemplateRepository : BasicRepository<ActionTemplateResource>, IActionTemplateRepository
    {
        public ActionTemplateRepository(IOctopusAsyncRepository repository) : base(repository, "ActionTemplates")
        {
        }

        public async Task<List<ActionTemplateSearchResource>> Search(CancellationToken token = default)
        {
            return await Client.Get<List<ActionTemplateSearchResource>>(await Repository.Link("ActionTemplatesSearch").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public async Task<List<ActionTemplateCategoryResource>> Categories(CancellationToken token = default)
        {
            return await Client.Get<List<ActionTemplateCategoryResource>>(await Repository.Link("ActionTemplatesCategories").ConfigureAwait(false), token: token).ConfigureAwait(false);
        }

        public Task<ActionTemplateResource> GetVersion(ActionTemplateResource resource, int version, CancellationToken token = default)
        {
            return Client.Get<ActionTemplateResource>(resource.Links["Versions"], new { version }, token);
        }

        public Task<ActionUpdateResultResource[]> UpdateActions(ActionTemplateResource actionTemplate, ActionsUpdateResource update, CancellationToken token = default)
        {
            return Client.Post<ActionsUpdateResource, ActionUpdateResultResource[]>(actionTemplate.Links["ActionsUpdate"], update, new { actionTemplate.Id }, token);
        }
        
        public Task SetLogo(ActionTemplateResource resource, string fileName, Stream contents, CancellationToken token = default)
        {
            return Client.Post(resource.Link("Logo"), new FileUpload { Contents = contents, FileName = fileName }, false, token);
        }
    }
}