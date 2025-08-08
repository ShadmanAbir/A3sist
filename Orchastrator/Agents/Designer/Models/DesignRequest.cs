using System;
using System.Collections.Generic;

namespace CodeAssist.Agents.Designer.Models
{
    public class DesignRequest
    {
        public string ProjectName { get; set; }
        public string Language { get; set; }
        public string Framework { get; set; }
        public List<string> RequiredFeatures { get; set; } = new List<string>();
        public List<string> Constraints { get; set; } = new List<string>();
        public string ExistingCodebasePath { get; set; }
        public DesignPreferences Preferences { get; set; } = new DesignPreferences();
    }

    public class DesignPreferences
    {
        public string ArchitectureStyle { get; set; }
        public bool PreferSeparationOfConcerns { get; set; }
        public bool PreferTestability { get; set; }
        public bool PreferModularity { get; set; }
        public bool PreferPerformanceOptimizations { get; set; }
        public List<string> PreferredPatterns { get; set; } = new List<string>();
    }
}