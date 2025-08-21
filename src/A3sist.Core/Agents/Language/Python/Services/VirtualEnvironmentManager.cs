using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Language.Python.Services
{
    /// <summary>
    /// Provides virtual environment management capabilities for Python projects
    /// </summary>
    public class VirtualEnvironmentManager : IDisposable
    {
        private bool _disposed = false;
        private string _workingDirectory;

        public VirtualEnvironmentManager(string workingDirectory = null)
        {
            _workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Initializes the virtual environment manager asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Check if Python venv module is available
            try
            {
                var result = await ExecutePythonCommandAsync("-m venv --help");
                if (!result.Success)
                {
                    throw new InvalidOperationException("Python venv module is not available");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize virtual environment manager", ex);
            }
        }

        /// <summary>
        /// Executes a virtual environment command
        /// </summary>
        /// <param name="command">The venv command to execute</param>
        /// <param name="envName">Optional environment name</param>
        /// <returns>The command execution result</returns>
        public async Task<string> ExecuteCommandAsync(string command, string envName = null)
        {
            try
            {
                return command.ToLower() switch
                {
                    "create" when !string.IsNullOrEmpty(envName) => await CreateVirtualEnvironmentAsync(envName),
                    "create" => await CreateVirtualEnvironmentAsync("venv"),
                    "activate" when !string.IsNullOrEmpty(envName) => await GetActivationInstructionsAsync(envName),
                    "activate" => await GetActivationInstructionsAsync("venv"),
                    "deactivate" => await GetDeactivationInstructionsAsync(),
                    "list" => await ListVirtualEnvironmentsAsync(),
                    "status" => await GetVirtualEnvironmentStatusAsync(),
                    "delete" when !string.IsNullOrEmpty(envName) => await DeleteVirtualEnvironmentAsync(envName),
                    "info" when !string.IsNullOrEmpty(envName) => await GetEnvironmentInfoAsync(envName),
                    "info" => await GetEnvironmentInfoAsync("venv"),
                    _ => $"Unknown virtual environment command: {command}"
                };
            }
            catch (Exception ex)
            {
                return $"Error executing venv {command}: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a new virtual environment
        /// </summary>
        /// <param name="envName">Name of the virtual environment</param>
        /// <returns>Creation result</returns>
        private async Task<string> CreateVirtualEnvironmentAsync(string envName)
        {
            try
            {
                var envPath = Path.Combine(_workingDirectory, envName);
                
                if (Directory.Exists(envPath))
                {
                    return $"Virtual environment '{envName}' already exists at {envPath}";
                }

                var result = await ExecutePythonCommandAsync($"-m venv {envName}");
                
                if (result.Success)
                {
                    var activationScript = GetActivationScriptPath(envName);
                    return $"Virtual environment '{envName}' created successfully at {envPath}\n" +
                           $"To activate: {GetActivationCommand(envName)}";
                }
                else
                {
                    return $"Failed to create virtual environment '{envName}': {result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error creating virtual environment '{envName}': {ex.Message}";
            }
        }

        /// <summary>
        /// Gets activation instructions for a virtual environment
        /// </summary>
        /// <param name="envName">Name of the virtual environment</param>
        /// <returns>Activation instructions</returns>
        private async Task<string> GetActivationInstructionsAsync(string envName)
        {
            try
            {
                var envPath = Path.Combine(_workingDirectory, envName);
                
                if (!Directory.Exists(envPath))
                {
                    return $"Virtual environment '{envName}' does not exist. Create it first with 'create' command.";
                }

                var activationCommand = GetActivationCommand(envName);
                var info = new List<string>
                {
                    $"=== Virtual Environment Activation ===",
                    $"Environment: {envName}",
                    $"Location: {envPath}",
                    $"",
                    $"To activate this virtual environment:",
                    $"  {activationCommand}",
                    $"",
                    $"To deactivate:",
                    $"  deactivate",
                    $"",
                    $"Note: Activation must be done in your terminal/command prompt."
                };

                return await Task.FromResult(string.Join(Environment.NewLine, info));
            }
            catch (Exception ex)
            {
                return $"Error getting activation instructions: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets deactivation instructions
        /// </summary>
        /// <returns>Deactivation instructions</returns>
        private async Task<string> GetDeactivationInstructionsAsync()
        {
            var info = new List<string>
            {
                "=== Virtual Environment Deactivation ===",
                "",
                "To deactivate the current virtual environment:",
                "  deactivate",
                "",
                "This will return you to the system Python environment."
            };

            return await Task.FromResult(string.Join(Environment.NewLine, info));
        }

        /// <summary>
        /// Lists available virtual environments in the current directory
        /// </summary>
        /// <returns>List of virtual environments</returns>
        private async Task<string> ListVirtualEnvironmentsAsync()
        {
            try
            {
                var info = new List<string>
                {
                    "=== Virtual Environments ==="
                };

                var directories = Directory.GetDirectories(_workingDirectory);
                var venvDirs = new List<string>();

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    var activationScript = GetActivationScriptPath(dirName);
                    
                    if (File.Exists(activationScript))
                    {
                        venvDirs.Add(dirName);
                    }
                }

                if (venvDirs.Any())
                {
                    info.Add($"Found {venvDirs.Count} virtual environment(s):");
                    foreach (var venv in venvDirs)
                    {
                        var envPath = Path.Combine(_workingDirectory, venv);
                        var pythonExe = GetPythonExecutablePath(venv);
                        var isActive = await IsEnvironmentActiveAsync(venv);
                        var status = isActive ? " (active)" : "";
                        
                        info.Add($"  {venv}{status} - {envPath}");
                        
                        if (File.Exists(pythonExe))
                        {
                            var versionResult = await ExecuteCommandInEnvironmentAsync(venv, "--version");
                            if (versionResult.Success)
                            {
                                info.Add($"    Python: {versionResult.Output.Trim()}");
                            }
                        }
                    }
                }
                else
                {
                    info.Add("No virtual environments found in current directory.");
                    info.Add("Create one with: venv create <env_name>");
                }

                return await Task.FromResult(string.Join(Environment.NewLine, info));
            }
            catch (Exception ex)
            {
                return $"Error listing virtual environments: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the current virtual environment status
        /// </summary>
        /// <returns>Status information</returns>
        private async Task<string> GetVirtualEnvironmentStatusAsync()
        {
            try
            {
                var info = new List<string>
                {
                    "=== Virtual Environment Status ==="
                };

                // Check if we're in a virtual environment
                var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
                if (!string.IsNullOrEmpty(virtualEnv))
                {
                    info.Add($"Currently active: {Path.GetFileName(virtualEnv)}");
                    info.Add($"Location: {virtualEnv}");
                    
                    var pythonResult = await ExecutePythonCommandAsync("--version");
                    if (pythonResult.Success)
                    {
                        info.Add($"Python version: {pythonResult.Output.Trim()}");
                    }
                    
                    var pipResult = await ExecuteCommandAsync("pip", "list");
                    if (pipResult.Contains("pip list"))
                    {
                        var lines = pipResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        var packageCount = Math.Max(0, lines.Length - 3); // Subtract header and command lines
                        info.Add($"Installed packages: {packageCount}");
                    }
                }
                else
                {
                    info.Add("No virtual environment is currently active.");
                    info.Add("Using system Python environment.");
                    
                    var pythonResult = await ExecutePythonCommandAsync("--version");
                    if (pythonResult.Success)
                    {
                        info.Add($"System Python version: {pythonResult.Output.Trim()}");
                    }
                }

                return await Task.FromResult(string.Join(Environment.NewLine, info));
            }
            catch (Exception ex)
            {
                return $"Error getting virtual environment status: {ex.Message}";
            }
        }

        /// <summary>
        /// Deletes a virtual environment
        /// </summary>
        /// <param name="envName">Name of the virtual environment to delete</param>
        /// <returns>Deletion result</returns>
        private async Task<string> DeleteVirtualEnvironmentAsync(string envName)
        {
            try
            {
                var envPath = Path.Combine(_workingDirectory, envName);
                
                if (!Directory.Exists(envPath))
                {
                    return $"Virtual environment '{envName}' does not exist.";
                }

                // Verify it's actually a virtual environment
                var activationScript = GetActivationScriptPath(envName);
                if (!File.Exists(activationScript))
                {
                    return $"'{envName}' does not appear to be a virtual environment.";
                }

                Directory.Delete(envPath, true);
                return await Task.FromResult($"Virtual environment '{envName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                return $"Error deleting virtual environment '{envName}': {ex.Message}";
            }
        }

        /// <summary>
        /// Gets detailed information about a virtual environment
        /// </summary>
        /// <param name="envName">Name of the virtual environment</param>
        /// <returns>Environment information</returns>
        private async Task<string> GetEnvironmentInfoAsync(string envName)
        {
            try
            {
                var envPath = Path.Combine(_workingDirectory, envName);
                
                if (!Directory.Exists(envPath))
                {
                    return $"Virtual environment '{envName}' does not exist.";
                }

                var info = new List<string>
                {
                    $"=== Virtual Environment Info: {envName} ===",
                    $"Location: {envPath}",
                    $"Created: {Directory.GetCreationTime(envPath)}"
                };

                var pythonExe = GetPythonExecutablePath(envName);
                if (File.Exists(pythonExe))
                {
                    var versionResult = await ExecuteCommandInEnvironmentAsync(envName, "--version");
                    if (versionResult.Success)
                    {
                        info.Add($"Python version: {versionResult.Output.Trim()}");
                    }

                    var pipResult = await ExecuteCommandInEnvironmentAsync(envName, "-m pip list");
                    if (pipResult.Success)
                    {
                        var lines = pipResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        var packageCount = Math.Max(0, lines.Length - 2); // Subtract header lines
                        info.Add($"Installed packages: {packageCount}");
                        
                        if (packageCount > 0)
                        {
                            info.Add("Recent packages:");
                            foreach (var line in lines.Skip(2).Take(5))
                            {
                                info.Add($"  {line}");
                            }
                            if (packageCount > 5)
                            {
                                info.Add($"  ... and {packageCount - 5} more packages");
                            }
                        }
                    }
                }

                return await Task.FromResult(string.Join(Environment.NewLine, info));
            }
            catch (Exception ex)
            {
                return $"Error getting environment info: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the activation script path for a virtual environment
        /// </summary>
        private string GetActivationScriptPath(string envName)
        {
            var envPath = Path.Combine(_workingDirectory, envName);
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Path.Combine(envPath, "Scripts", "activate.bat");
            }
            else
            {
                return Path.Combine(envPath, "bin", "activate");
            }
        }

        /// <summary>
        /// Gets the Python executable path for a virtual environment
        /// </summary>
        private string GetPythonExecutablePath(string envName)
        {
            var envPath = Path.Combine(_workingDirectory, envName);
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Path.Combine(envPath, "Scripts", "python.exe");
            }
            else
            {
                return Path.Combine(envPath, "bin", "python");
            }
        }

        /// <summary>
        /// Gets the activation command for a virtual environment
        /// </summary>
        private string GetActivationCommand(string envName)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{envName}\\Scripts\\activate";
            }
            else
            {
                return $"source {envName}/bin/activate";
            }
        }

        /// <summary>
        /// Checks if a virtual environment is currently active
        /// </summary>
        private async Task<bool> IsEnvironmentActiveAsync(string envName)
        {
            var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
            if (string.IsNullOrEmpty(virtualEnv))
                return false;

            var envPath = Path.Combine(_workingDirectory, envName);
            return await Task.FromResult(string.Equals(virtualEnv, envPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Executes a command within a specific virtual environment
        /// </summary>
        private async Task<(bool Success, string Output, string Error)> ExecuteCommandInEnvironmentAsync(string envName, string arguments)
        {
            var pythonExe = GetPythonExecutablePath(envName);
            
            if (!File.Exists(pythonExe))
            {
                return (false, "", "Python executable not found in virtual environment");
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = arguments,
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        /// <summary>
        /// Executes a Python command and returns the result
        /// </summary>
        private async Task<(bool Success, string Output, string Error)> ExecutePythonCommandAsync(string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = arguments,
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode == 0, output, error);
            }
            catch (Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        /// <summary>
        /// Shuts down the virtual environment manager asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the virtual environment manager and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
                _disposed = true;
            }
        }
    }
}