using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a shell command to be executed
    /// </summary>
    public class ShellCommand
    {
        public string CommandText { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool RequiresSandbox { get; set; } = true;
        public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Unknown;
    }

    /// <summary>
    /// Represents a command prepared for sandboxed execution
    /// </summary>
    public class SandboxedCommand
    {
        public string Executable { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public List<string> RestrictedPaths { get; set; } = new List<string>();
        public List<string> AllowedPaths { get; set; } = new List<string>();
        public bool NetworkAccessAllowed { get; set; } = false;
        public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Result of command validation
    /// </summary>
    public class CommandValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public SecurityRiskLevel SecurityRisk { get; set; } = SecurityRiskLevel.None;
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();
        public bool RequiresUserConfirmation { get; set; }
    }

    /// <summary>
    /// Result of command execution
    /// </summary>
    public class CommandExecutionResult
    {
        public string Command { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public SecurityRiskLevel SecurityRisk { get; set; } = SecurityRiskLevel.None;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Security risk levels for commands
    /// </summary>
    public enum SecurityRiskLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical,
        Unknown
    }

    /// <summary>
    /// Types of shell environments
    /// </summary>
    public enum ShellType
    {
        Cmd,
        PowerShell,
        Bash,
        Zsh,
        Fish,
        Unknown
    }

    /// <summary>
    /// Command execution context
    /// </summary>
    public class CommandExecutionContext
    {
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public ShellType ShellType { get; set; } = ShellType.Unknown;
        public string ProjectPath { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalContext { get; set; } = new Dictionary<string, object>();
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }
}