namespace A3sist;

public class PatternRecommendation
{
     public string PatternName { get; set; }
        public string Description { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
        public string WhenToUse { get; set; }
        public List<string> CommonComponents { get; set; } = new List<string>();
        public List<string> ExampleFrameworks { get; set; } = new List<string>();
        public float RelevanceScore { get; set; }
        public string Justification { get; set; }
        public List<string> ComponentsToApplyTo { get; internal set; }
        public string Benefit { get; internal set; }
}
