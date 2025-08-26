namespace A3sist.Orchastrator.Agents.Designer.Models
{
    public class Dependency
    {
        public string ProjectName { get; set; }
        public string SourceComponent { get; set; }
        public string TargetComponent { get; set; }
        public string Type { get; set; } // e.g., "Uses", "DependsOn", "CommunicatesWith"
        public string Description { get; set; }
    }
}