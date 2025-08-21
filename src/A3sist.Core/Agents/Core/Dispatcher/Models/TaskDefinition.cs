namespace A3sist.Orchastrator.Agents.Dispatcher.Models
{
    public class TaskDefinition
    {
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string AgentName { get; set; }
        public string TaskType { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}