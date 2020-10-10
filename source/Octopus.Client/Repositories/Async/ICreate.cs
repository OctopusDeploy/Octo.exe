using System;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Repositories.Async
{
    public interface ICreate<TResource>
    {
        Task<TResource> Create(TResource resource, object pathParameters = null, CancellationToken token = default);
    }
}