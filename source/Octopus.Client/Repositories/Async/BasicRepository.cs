﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octopus.Client.Extensibility;
using Octopus.Client.Model;
using Octopus.Client.Util;

namespace Octopus.Client.Repositories.Async
{
    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable UnusedMember.Local
    // ReSharper disable MemberCanBeProtected.Local
    abstract class BasicRepository<TResource> where TResource : class, IResource
    {
        private readonly Func<IOctopusAsyncRepository, Task<string>> getCollectionLinkName;
        protected string CollectionLinkName;

        protected BasicRepository(IOctopusAsyncRepository repository, string collectionLinkName, Func<IOctopusAsyncRepository, Task<string>> getCollectionLinkName = null)
        {
            Client = repository.Client;
            Repository = repository;
            CollectionLinkName = collectionLinkName;
            this.getCollectionLinkName = getCollectionLinkName;
        }

        protected virtual Dictionary<string, object> GetAdditionalQueryParameters()
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public IOctopusAsyncClient Client { get; }
        public IOctopusAsyncRepository Repository { get; }

        private void AssertSpaceIdMatchesResource(TResource resource, bool isEmptySpaceIdAllowed = false)
        {
            if (resource is IHaveSpaceResource spaceResource)
            {
                Repository.Scope
                    .Apply(space =>
                        {
                            if (isEmptySpaceIdAllowed && string.IsNullOrWhiteSpace(spaceResource.SpaceId))
                                return (string) null;
                            
                            var errorMessageTemplate = $"The resource has a different space specified than the one specified by the repository scope. Either change the {nameof(IHaveSpaceResource.SpaceId)} on the resource to {space.Id}, or use a repository that is scoped to";
            
                            if (string.IsNullOrWhiteSpace(spaceResource.SpaceId) && !space.IsDefault)
                                throw new ArgumentException(
                                    $"{errorMessageTemplate} the default space.");

                            if (!string.IsNullOrWhiteSpace(spaceResource.SpaceId) && spaceResource.SpaceId != space.Id)
                                throw new ArgumentException(
                                    $"{errorMessageTemplate} {spaceResource.SpaceId}.");
                            
                            return (string) null;
                        },
                        () => null,
                        () => null);
            }
        }
        
        public virtual async Task<TResource> Create(TResource resource, object pathParameters = null)
        {
            var link = await ResolveLink().ConfigureAwait(false);
            AssertSpaceIdMatchesResource(resource, true);
            EnrichSpaceId(resource);
            return await Client.Create(link, resource, pathParameters).ConfigureAwait(false);
        }

        public virtual Task<TResource> Modify(TResource resource)
        {
            AssertSpaceIdMatchesResource(resource);
            return Client.Update(resource.Links["Self"], resource);
        }

        public Task Delete(TResource resource)
        {
            AssertSpaceIdMatchesResource(resource);
            return Client.Delete(resource.Links["Self"]);
        }

        public async Task Paginate(Func<ResourceCollection<TResource>, bool> getNextPage, string path = null, object pathParameters = null)
        {
            var link = await ResolveLink().ConfigureAwait(false);
            var parameters = ParameterHelper.CombineParameters(GetAdditionalQueryParameters(), pathParameters);
            await Client.Paginate(path ?? link, parameters, getNextPage).ConfigureAwait(false);
        }

        public async Task<TResource> FindOne(Func<TResource, bool> search, string path = null, object pathParameters = null)
        {
            TResource resource = null;
            await Paginate(page =>
            {
                resource = page.Items.FirstOrDefault(search);
                return resource == null;
            }, path, pathParameters)
                .ConfigureAwait(false);
            return resource;
        }

        public async Task<List<TResource>> FindMany(Func<TResource, bool> search, string path = null, object pathParameters = null)
        {
            var resources = new List<TResource>();
            await Paginate(page =>
            {
                resources.AddRange(page.Items.Where(search));
                return true;
            }, path, pathParameters)
                .ConfigureAwait(false);
            return resources;
        }

        public Task<List<TResource>> FindAll(string path = null, object pathParameters = null)
        {
            return FindMany(r => true, path, pathParameters);
        }

        public async Task<List<TResource>> GetAll()
        {
            var link = await ResolveLink().ConfigureAwait(false);
            var parameters = ParameterHelper.CombineParameters(GetAdditionalQueryParameters(), new { id = IdValueConstant.IdAll });
            return await Client.Get<List<TResource>>(link, parameters).ConfigureAwait(false);
        }

        public Task<TResource> FindByName(string name, string path = null, object pathParameters = null)
        {
            name = (name ?? string.Empty).Trim();

            // Some endpoints allow a Name query param which greatly increases efficiency
            if (pathParameters == null)
                pathParameters = new { name = name };

            return FindOne(r =>
            {
                var named = r as INamedResource;
                if (named != null) return string.Equals((named.Name ?? string.Empty).Trim(), name, StringComparison.OrdinalIgnoreCase);
                return false;
            }, path, pathParameters);
        }

        public Task<List<TResource>> FindByNames(IEnumerable<string> names, string path = null, object pathParameters = null)
        {
            var nameSet = new HashSet<string>((names ?? new string[0]).Select(n => (n ?? string.Empty).Trim()), StringComparer.OrdinalIgnoreCase);
            return FindMany(r =>
            {
                var named = r as INamedResource;
                if (named != null) return nameSet.Contains((named.Name ?? string.Empty).Trim());
                return false;
            }, path, pathParameters);
        }

        public async Task<TResource> Get(string idOrHref)
        {
            if (string.IsNullOrWhiteSpace(idOrHref))
                return null;

            var link = await ResolveLink().ConfigureAwait(false);
            var additionalQueryParameters = GetAdditionalQueryParameters();
            var parameters = ParameterHelper.CombineParameters(additionalQueryParameters, new { id = idOrHref });
            var  getTask = idOrHref.StartsWith("/", StringComparison.OrdinalIgnoreCase)
                ? Client.Get<TResource>(idOrHref, additionalQueryParameters).ConfigureAwait(false)
                : Client.Get<TResource>(link, parameters).ConfigureAwait(false);
            return await getTask;
        }

        public virtual async Task<List<TResource>> Get(params string[] ids)
        {
            if (ids == null) return new List<TResource>();
            var actualIds = ids.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            if (actualIds.Length == 0) return new List<TResource>();

            var resources = new List<TResource>();

            var link = await ResolveLink().ConfigureAwait(false);
            if (!Regex.IsMatch(link, @"\{\?.*\Wids\W"))
                link += "{?ids}";

            var parameters = ParameterHelper.CombineParameters(GetAdditionalQueryParameters(), new { ids = actualIds });
            await Client.Paginate<TResource>(
                link,
                parameters,
                page =>
                {
                    resources.AddRange(page.Items);
                    return true;
                })
                .ConfigureAwait(false);

            return resources;
        }

        public Task<TResource> Refresh(TResource resource)
        {
            if (resource == null) throw new ArgumentNullException("resource");
            return Get(resource.Id);
        }

        protected virtual void EnrichSpaceId(TResource resource)
        {
            if (resource is IHaveSpaceResource spaceResource)
            {
                spaceResource.SpaceId = Repository.Scope.Apply(space => space.Id,
                    () => null,
                    () => spaceResource.SpaceId);
            }
        }

        protected async Task<string> ResolveLink()
        {
            if (CollectionLinkName == null && getCollectionLinkName != null)
                CollectionLinkName = await getCollectionLinkName(Repository).ConfigureAwait(false);
            return await Repository.Link(CollectionLinkName).ConfigureAwait(false);
        }
    }

    // ReSharper restore MemberCanBePrivate.Local
    // ReSharper restore UnusedMember.Local
    // ReSharper restore MemberCanBeProtected.Local
}
