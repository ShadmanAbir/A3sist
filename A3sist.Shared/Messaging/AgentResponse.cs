using System;

namespace A3sist.Shared.Messaging
{
    public class AgentResponse
    {
        public Guid ResponseId { get; set; }
        public Guid RequestId { get; set; }
        public string AgentName { get; set; }
        public string TaskName { get; set; }
        public string Result { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }

        public AgentResponse()
        {
            ResponseId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}