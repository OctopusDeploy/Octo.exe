using System;
using System.Threading;
using System.Threading.Tasks;
using Octopus.Client.Editors.Async;
using Octopus.Client.Model;

namespace Octopus.Client.Repositories.Async
{
    public interface ILibraryVariableSetRepository :
        ICreate<LibraryVariableSetResource>,
        IGet<LibraryVariableSetResource>,
        IModify<LibraryVariableSetResource>,
        IDelete<LibraryVariableSetResource>,
        IFindByName<LibraryVariableSetResource>
    {
        Task<LibraryVariableSetEditor> CreateOrModify(string name, CancellationToken token = default);
        Task<LibraryVariableSetEditor> CreateOrModify(string name, string description, CancellationToken token = default);
    }

    class LibraryVariableSetRepository : BasicRepository<LibraryVariableSetResource>, ILibraryVariableSetRepository
    {
        public LibraryVariableSetRepository(IOctopusAsyncRepository repository)
            : base(repository, "LibraryVariables")
        {
        }

        public Task<LibraryVariableSetEditor> CreateOrModify(string name, CancellationToken token = default)
        {
            return new LibraryVariableSetEditor(this, new VariableSetRepository(Repository)).CreateOrModify(name, token);
        }

        public Task<LibraryVariableSetEditor> CreateOrModify(string name, string description, CancellationToken token = default)
        {
            return new LibraryVariableSetEditor(this, new VariableSetRepository(Repository)).CreateOrModify(name, description, token);
        }
    }
}
