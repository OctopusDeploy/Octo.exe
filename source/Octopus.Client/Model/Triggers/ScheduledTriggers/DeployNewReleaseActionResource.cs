﻿using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model.Triggers.ScheduledTriggers
{
    public class DeployNewReleaseActionResource : ScheduledTriggerActionResource
    {
        public override TriggerActionType ActionType => TriggerActionType.DeployNewRelease;

        [Writeable]
        public string EnvironmentId { get; set; }
    }
}
