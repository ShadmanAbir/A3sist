using System;
using System.Collections.Generic;

namespace A3sist.Agents.Designer.Models
{
    public class ArchitecturePlan
    {
        public string ProjectName { get; set; }
        public string ArchitectureStyle { get; set; }
        public List<Component> Components { get; set; } = new List<Component>();
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
        public List<PatternRecommendation> RecommendedPatterns { get; set; } = new List<PatternRecommendation>();
        public List<string> SuggestedImprovements { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Component
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Responsibility { get; set; }
        public List<string> Technologies { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    public class Dependency
    {
        public string SourceComponent { get; set; }
        public string TargetComponent { get; set; }
        public string Type { get; set; } // e.g., "Uses", "DependsOn", "CommunicatesWith"
        public string Description { get; set; }
    }

    public class PatternRecommendation
    {
        public string PatternName { get; set; }
        public string Description { get; set; }
        public string Benefit { get; set; }
        public string WhenToUse { get; set; }
        public List<string> ComponentsToApplyTo { get; set; } = new List<string>();
    }
}