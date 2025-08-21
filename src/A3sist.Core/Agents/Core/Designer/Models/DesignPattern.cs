namespace A3sist.Orchastrator.Agents.Designer.Models
{
    public class DesignPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Benefit { get; set; }
        public string WhenToUse { get; set; }
        public List<string> Components { get; set; } = new List<string>();
        public List<Relationship> Relationships { get; set; } = new List<Relationship>();
    }
}