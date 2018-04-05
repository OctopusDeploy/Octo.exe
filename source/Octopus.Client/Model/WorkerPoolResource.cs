﻿using Octopus.Client.Extensibility;
using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model
{
    /// <summary>
    /// Represents a pool of worker machines. WorkerPools are user-defined and map to pools of machines that
    /// can do work as part of a deployment: for example, running scripts and deploying to Azure.
    /// </summary>
    public class WorkerPoolResource : Resource, INamedResource
    {
        /// <summary>
        /// Gets or sets the name of this pool. This should be short, preferably 5-20 characters.
        /// </summary>
        [Writeable]
        [Trim]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a short description that can be used to explain the purpose of
        /// the pool to other users. This field may contain markdown.
        /// </summary>
        [Writeable]
        [Trim]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a number indicating the priority of this pool in sort order. WorkerPools with
        /// a lower sort order will appear in the UI before items with a higher sort order.
        /// </summary>
        [Writeable]
        public int SortOrder { get; set; }
    }
}