using System;

namespace A3sist.Orchastrator.Agents.Designer.Models
{
    
    public class ArchitecturalPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
        public string WhenToUse { get; set; }
        public List<string> CommonComponents { get; set; } = new List<string>();
        public List<string> ExampleFrameworks { get; set; } = new List<string>();
    }
}