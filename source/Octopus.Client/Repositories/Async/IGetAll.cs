using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Client.Repositories.Async
{
    public interface IGetAll<TResource>
    {
        Task<List<TResource>> GetAll(CancellationToken token = default);
    }
}