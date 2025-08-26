using A3sist.Shared.Attributes;
using A3sist.Shared.Enums;
using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Metadata information about an agent type
    /// </summary>
    public class AgentMetadata
    {
        /// <summary>
        /// The agent type
        /// </summary>
        public Type AgentType { get; set; } = null!;

        /// <summary>
        /// Name of the agent
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Agent type enum value
        /// </summary>
        public AgentType Type { get; set; }

        /// <summary>
        /// Description of the agent
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Version of the agent
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Author of the agent
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Capabilities of the agent
        /// </summary>
        public List<AgentCapabilityAttribute> Capabilities { get; set; } = new();

        /// <summary>
        /// Whether the agent is enabled by default
        /// </summary>
        public bool EnabledByDefault { get; set; } = true;

        /// <summary>
        /// Priority of the agent (higher values = higher priority)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// File extensions this agent can handle
        /// </summary>
        public List<string> SupportedFileExtensions { get; set; } = new();

        /// <summary>
        /// Keywords that trigger this agent
        /// </summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// Dependencies required by this agent
        /// </summary>
        public List<Type> Dependencies { get; set; } = new();

        /// <summary>
        /// Whether the agent is abstract (cannot be instantiated)
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Assembly where the agent is defined
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// Namespace of the agent
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// Full type name of the agent
        /// </summary>
        public string FullTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}