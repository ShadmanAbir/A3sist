namespace A3sist.Orchastrator.Agents.IntentRouter.Services
{
    public class AgentConfig
    {
        public string AgentName { get; set; }
        public string AgentType { get; set; }
        public List<AgentCapabilityConfig> Capabilities { get; set; }
        public string EntryPoint { get; set; }
    }
}