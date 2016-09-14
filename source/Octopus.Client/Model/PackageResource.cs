﻿using System;

namespace Octopus.Client.Model
{
    public class PackageResource : Resource
    {
        public string PackageId { get; set; }

        public string FeedId { get; set; }

        public string Title { get; set; }
        public string Summary { get; set; }
        
        public string Version { get; set; }
        public string Description { get; set; }
        public string Published { get; set; }
        public string ReleaseNotes { get; set; }
        public string FileExtension { get; set; }
    }
}