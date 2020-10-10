using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client.Editors.Async
{
    public interface IResourceEditor<out TResource, TResourceBuilder> : IResourceBuilder
        where TResource : Resource
        where TResourceBuilder : IResourceBuilder
    {
        TResource Instance { get; }
        TResourceBuilder Customize(Action<TResource> customize);
        Task<TResourceBuilder> Save(CancellationToken token = default);
    }

    public interface IResourceBuilder
    {
        
    }
}