using System;
using System.Collections.Generic;
using CodeAssist.Shared.Enums;

namespace CodeAssist.Agents.Dispatcher.Models
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

    public enum WorkflowPriority
    {
        Critical,
        High,
        Normal,
        Low
    }
}