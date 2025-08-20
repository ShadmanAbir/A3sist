using System;
using A3sist.Shared.Enums;

namespace A3sist.Shared.Messaging
{
    public class TaskMessage
    {
        public Guid MessageId { get; set; }
        public string TaskName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public TaskStatus Status { get; set; }

        public TaskMessage()
        {
            MessageId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}