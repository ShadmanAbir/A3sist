using A3sist.Core.Agents.Base;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Utility.Shell
{
    /// <summary>
    /// Agent responsible for safe command execution with validation and sandboxing
    /// </summary>
    public class ShellAgent : BaseAgent
    {
        private readonly ICommandValidator _commandValidator;
        private readonly ICommandSandbox _commandSandbox;
        private readonly IShellConfiguration _shellConfiguration;
        private readonly HashSet<string> _allowedCommands;
        private readonly HashSet<string> _blockedCommands;

        public override string Name => "ShellAgent";
        public override AgentType Type => AgentType.Utility;

        public ShellAgent(
            ILogger<ShellAgent> logger,
            IAgentConfiguration configuration,
            ICommandValidator commandValidator,
            ICommandSandbox commandSandbox,
            IShellConfiguration shellConfiguration)
            : base(logger, configuration)
        {
            _commandValidator = commandValidator ?? throw new ArgumentNullException(nameof(commandValidator));
            _commandSandbox = commandSandbox ?? throw new ArgumentNullException(nameof(commandSandbox));
            _shellConfiguration = shellConfiguration ?? throw new ArgumentNullException(nameof(shellConfiguration));

            InitializeCommandLists();
        }

        public override async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request?.Prompt == null) return false;

            var prompt = request.Prompt.ToLowerInvariant();
            
            // Shell command keywords
            var shellKeywords = new[]
            {
                "run", "execute", "command", "shell", "cmd", "powershell",
                "build", "compile", "test", "install", "npm", "dotnet",
                "git", "mkdir", "copy", "move", "delete", "ls", "dir"
            };

            return shellKeywords.Any(keyword => prompt.Contains(keyword)) ||
                   ContainsCommandPattern(prompt);
        }

        protected override async System.Threading.Tasks.Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Processing shell command request: {RequestId}", request.Id);

                var commands = ExtractCommands(request.Prompt);
                if (!commands.Any())
                {
                    return new AgentResult
                    {
                        Success = false,
                        Message = "No valid commands found in the request",
                        AgentName = Name
                    };
                }

                var results = new List<CommandExecutionResult>();
                
                foreach (var command in commands)
                {
                    var validationResult = await _commandValidator.ValidateAsync(command, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        results.Add(new CommandExecutionResult
                        {
                            Command = command.CommandText,
                            Success = false,
                            Error = validationResult.ErrorMessage,
                            SecurityRisk = validationResult.SecurityRisk
                        });
                        continue;
                    }

                    var executionResult = await ExecuteCommandSafelyAsync(command, cancellationToken);
                    results.Add(executionResult);
                }

                var response = FormatExecutionResults(results);
                var overallSuccess = results.Any() && results.All(r => r.Success);

                return new AgentResult
                {
                    Success = overallSuccess,
                    Content = response,
                    Message = overallSuccess ? "Commands executed successfully" : "Some commands failed",
                    AgentName = Name,
                    Metadata = new Dictionary<string, object>
                    {
                        ["CommandCount"] = results.Count,
                        ["SuccessfulCommands"] = results.Count(r => r.Success),
                        ["FailedCommands"] = results.Count(r => !r.Success),
                        ["ExecutionTime"] = results.Sum(r => r.ExecutionTime.TotalMilliseconds)
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing shell command request: {RequestId}", request.Id);
                return new AgentResult
                {
                    Success = false,
                    Message = $"Failed to process shell command request: {ex.Message}",
                    Exception = ex,
                    AgentName = Name
                };
            }
        }

        private async Task<CommandExecutionResult> ExecuteCommandSafelyAsync(
            ShellCommand command, 
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Logger.LogInformation("Executing command: {Command}", command.CommandText);

                var sandboxedCommand = await _commandSandbox.PrepareCommandAsync(command, cancellationToken);
                
                using var process = new Process();
                ConfigureProcess(process, sandboxedCommand);

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeoutTask = Task.Delay(_shellConfiguration.CommandTimeout, cancellationToken);
                var processTask = Task.Run(() => process.WaitForExit(), cancellationToken);

                var completedTask = await Task.WhenAny(processTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    process.Kill();
                    return new CommandExecutionResult
                    {
                        Command = command.CommandText,
                        Success = false,
                        Error = "Command execution timed out",
                        ExecutionTime = stopwatch.Elapsed
                    };
                }

                var output = outputBuilder.ToString().Trim();
                var error = errorBuilder.ToString().Trim();

                return new CommandExecutionResult
                {
                    Command = command.CommandText,
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing command: {Command}", command.CommandText);
                return new CommandExecutionResult
                {
                    Command = command.CommandText,
                    Success = false,
                    Error = ex.Message,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private void ConfigureProcess(Process process, SandboxedCommand sandboxedCommand)
        {
            process.StartInfo.FileName = sandboxedCommand.Executable;
            process.StartInfo.Arguments = sandboxedCommand.Arguments;
            process.StartInfo.WorkingDirectory = sandboxedCommand.WorkingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;

            // Set environment variables
            foreach (var envVar in sandboxedCommand.EnvironmentVariables)
            {
                process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }
        }

        private List<ShellCommand> ExtractCommands(string prompt)
        {
            var commands = new List<ShellCommand>();

            // Look for explicit command blocks
            var codeBlockPattern = @"```(?:bash|cmd|powershell|shell)?\s*\n(.*?)\n```";
            var codeBlockMatches = Regex.Matches(prompt, codeBlockPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in codeBlockMatches)
            {
                var commandText = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(commandText))
                {
                    commands.Add(new ShellCommand { CommandText = commandText });
                }
            }

            // Look for inline commands
            if (!commands.Any())
            {
                var inlinePatterns = new[]
                {
                    @"run\s+(.+)",
                    @"execute\s+(.+)",
                    @"command:\s*(.+)",
                    @">\s*(.+)"
                };

                foreach (var pattern in inlinePatterns)
                {
                    var matches = Regex.Matches(prompt, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var commandText = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(commandText))
                        {
                            commands.Add(new ShellCommand { CommandText = commandText });
                        }
                    }
                }
            }

            return commands;
        }

        private bool ContainsCommandPattern(string prompt)
        {
            var commandPatterns = new[]
            {
                @"```(?:bash|cmd|powershell|shell)",
                @"run\s+\w+",
                @"execute\s+\w+",
                @"command:\s*\w+",
                @">\s*\w+"
            };

            return commandPatterns.Any(pattern => 
                Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase));
        }

        private string FormatExecutionResults(List<CommandExecutionResult> results)
        {
            var response = new StringBuilder();

            foreach (var result in results)
            {
                response.AppendLine($"**Command:** `{result.Command}`");
                response.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
                
                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    response.AppendLine("**Output:**");
                    response.AppendLine("```");
                    response.AppendLine(result.Output);
                    response.AppendLine("```");
                }

                if (!result.Success && !string.IsNullOrEmpty(result.Error))
                {
                    response.AppendLine("**Error:**");
                    response.AppendLine("```");
                    response.AppendLine(result.Error);
                    response.AppendLine("```");
                }

                if (result.SecurityRisk != SecurityRiskLevel.None)
                {
                    response.AppendLine($"**Security Risk:** {result.SecurityRisk}");
                }

                response.AppendLine($"**Execution Time:** {result.ExecutionTime.TotalMilliseconds:F0}ms");
                response.AppendLine();
            }

            return response.ToString().Trim();
        }

        private void InitializeCommandLists()
        {
            _allowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Build tools
                "dotnet", "msbuild", "npm", "yarn", "pip", "maven", "gradle",
                
                // Version control
                "git",
                
                // File operations (safe ones)
                "dir", "ls", "pwd", "cd", "mkdir", "type", "cat", "head", "tail",
                
                // Development tools
                "node", "python", "java", "javac", "gcc", "make",
                
                // Package managers
                "nuget", "composer", "gem"
            };

            _blockedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // System modification
                "rm", "del", "rmdir", "format", "fdisk", "diskpart",
                
                // Network operations
                "wget", "curl", "ftp", "telnet", "ssh",
                
                // System control
                "shutdown", "reboot", "halt", "systemctl", "service",
                
                // Registry operations
                "reg", "regedit",
                
                // Potentially dangerous
                "powershell", "cmd", "bash", "sh", "eval", "exec"
            };
        }

        protected override async System.Threading.Tasks.Task InitializeAgentAsync()
        {
            // Base initialization is handled by the base class
            Logger.LogInformation("ShellAgent initialized with {AllowedCount} allowed and {BlockedCount} blocked commands",
                _allowedCommands.Count, _blockedCommands.Count);
        }
    }
}