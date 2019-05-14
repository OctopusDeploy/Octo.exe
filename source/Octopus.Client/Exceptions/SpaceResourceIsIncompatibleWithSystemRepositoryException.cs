using System;
using Octopus.Client.Extensibility;

namespace Octopus.Client.Exceptions
{
    public class SpaceResourceIsIncompatibleWithSystemRepositoryException : Exception
    {
        public SpaceResourceIsIncompatibleWithSystemRepositoryException(IHaveSpaceResource spaceResource)
            : base($"The space scoped resource {((IResource)spaceResource).Id} cannot be modified by a System scoped repository. Try again using a repository scoped to {spaceResource.SpaceId}.")
        {
        }
        
        public SpaceResourceIsIncompatibleWithSystemRepositoryException()
            : base($"Access to space scoped resources cannot be achieved by a System scoped repository. Try again using a repository scoped to a Space.")
        {
        }
    }
}