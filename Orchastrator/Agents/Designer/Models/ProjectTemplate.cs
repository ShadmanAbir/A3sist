namespace A3sist;

public class ProjectTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, List<string>> Structure { get; set; } = new Dictionary<string, List<string>>();
        public List<string> Dependencies { get; set; } = new List<string>();
    }
