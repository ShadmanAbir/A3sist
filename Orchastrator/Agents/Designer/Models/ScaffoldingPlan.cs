namespace A3sist;
public class ScaffoldingPlan
    {
        public string ProjectName { get; set; }
        public string Language { get; set; }
        public string Framework { get; set; }
        public Dictionary<string, List<string>> Structure { get; set; } = new Dictionary<string, List<string>>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
    }
