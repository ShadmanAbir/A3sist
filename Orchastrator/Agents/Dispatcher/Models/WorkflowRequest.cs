using System;
using System.Collections.Generic;
using A3sist.Shared.Enums;

namespace A3sist.Agents.Dispatcher.Models
{
    public class WorkflowRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public string WorkflowName { get; set; }
        public string Description { get; set; }
        public List<TaskDefinition> Tasks { get; set; } = new List<TaskDefinition>();
        public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Requester { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}