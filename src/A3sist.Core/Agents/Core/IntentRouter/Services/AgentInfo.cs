namespace A3sist.Orchastrator.Agents.IntentRouter.Services
{
    public class AgentInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> SupportedLanguages { get; set; }
        public List<AgentCapability> Capabilities { get; set; }
    }
}