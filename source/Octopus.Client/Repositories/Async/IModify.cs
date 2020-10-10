using System;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Repositories.Async
{
    public interface IModify<TResource>
    {
        Task<TResource> Modify(TResource resource, CancellationToken token = default);
    }
}