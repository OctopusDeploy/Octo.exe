using System;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Repositories.Async
{
    public interface IDelete<in TResource>
    {
        Task Delete(TResource resource, CancellationToken token = default);
    }
}