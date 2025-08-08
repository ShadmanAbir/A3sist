using System;

namespace CodeAssist.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AgentCapabilityAttribute : Attribute
    {
        public string CapabilityName { get; }
        public string Description { get; }
        public string[] SupportedLanguages { get; }
        public string[] RequiredContextTypes { get; }

        public AgentCapabilityAttribute(string capabilityName, string description, string[] supportedLanguages, string[] requiredContextTypes)
        {
            CapabilityName = capabilityName ?? throw new ArgumentNullException(nameof(capabilityName));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            SupportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
            RequiredContextTypes = requiredContextTypes ?? throw new ArgumentNullException(nameof(requiredContextTypes));
        }
    }
}