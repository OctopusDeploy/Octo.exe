using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Repositories.Async
{
    public interface IGet<TResource>
    {
        Task<TResource> Get(string idOrHref, CancellationToken token = default);
        Task<List<TResource>> Get(CancellationToken token = default, params string[] ids);
        Task<TResource> Refresh(TResource resource, CancellationToken token = default);
    }
}