namespace A3sist.Orchastrator.Agents.IntentRouter.Services
{
    public class AgentCapability
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] SupportedLanguages { get; set; }
        public string[] RequiredContextTypes { get; set; }
    }
}