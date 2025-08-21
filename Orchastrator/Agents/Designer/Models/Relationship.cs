namespace Orchastrator.Agents.Designer.Models
{
    public class Relationship
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public string Type { get; set; } // e.g., "Uses", "DependsOn", "CommunicatesWith"
    }
}