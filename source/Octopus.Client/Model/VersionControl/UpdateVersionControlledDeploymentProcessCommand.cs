using System.ComponentModel.DataAnnotations;

namespace Octopus.Client.Model.VersionControl
{
    public class UpdateVersionControlledDeploymentProcessCommand
    {
        [Required]
        public string CommitMessage { get; set; }

        [Required]
        public DeploymentProcessResource Resource { get; set; }
    }
}