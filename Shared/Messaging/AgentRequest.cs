using System;

namespace A3sist.Shared.Messaging
{
    public class AgentRequest
    {
        public Guid RequestId { get; set; }
        public string AgentName { get; set; }
        public string TaskName { get; set; }
        public string Context { get; set; }
        public DateTime Timestamp { get; set; }
        public string Requester { get; set; }
        public string FilePath { get; set; }
        public object Id { get; set; }
        public object Prompt { get; set; }
        public object LLMOptions { get; set; }

        public AgentRequest()
        {
            RequestId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}