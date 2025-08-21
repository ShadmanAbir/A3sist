using A3sist.Shared.Enums;
using System;

namespace A3sist.Shared.Attributes
{
    /// <summary>
    /// Attribute to define agent capabilities and metadata
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AgentCapabilityAttribute : Attribute
    {
        /// <summary>
        /// The capability name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the capability
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Agent type this capability applies to
        /// </summary>
        public AgentType AgentType { get; set; }

        /// <summary>
        /// Priority of this capability (higher values = higher priority)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether this capability is enabled by default
        /// </summary>
        public bool EnabledByDefault { get; set; } = true;

        /// <summary>
        /// File extensions this capability can handle (comma-separated)
        /// </summary>
        public string FileExtensions { get; set; } = string.Empty;

        /// <summary>
        /// Keywords that trigger this capability (comma-separated)
        /// </summary>
        public string Keywords { get; set; } = string.Empty;

        /// <summary>
        /// Minimum confidence level required for this capability (0.0 to 1.0)
        /// </summary>
        public double MinConfidence { get; set; } = 0.0;

        public AgentCapabilityAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}