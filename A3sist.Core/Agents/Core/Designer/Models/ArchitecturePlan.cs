namespace A3sist.Orchastrator.Agents.Designer.Models
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
}