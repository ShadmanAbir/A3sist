namespace A3sist.Orchastrator.Agents.Designer.Models
{
    public class Component
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Responsibility { get; set; }
        public List<string> Technologies { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}