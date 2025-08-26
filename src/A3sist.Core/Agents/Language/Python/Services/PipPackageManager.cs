using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Core.Agents.Language.Python.Services
{
    /// <summary>
    /// Provides pip package management capabilities for Python projects
    /// </summary>
    public class PipPackageManager : IDisposable
    {
        private bool _disposed = false;
        private string _workingDirectory;

        public PipPackageManager(string workingDirectory = null)
        {
            _workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Initializes the pip package manager asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Check if pip is available
            try
            {
                var result = await ExecutePipCommandAsync("--version");
                if (!result.Success)
                {
                    throw new InvalidOperationException("pip is not available or not installed");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize pip package manager", ex);
            }
        }

        /// <summary>
        /// Executes a pip command
        /// </summary>
        /// <param name="command">The pip command to execute</param>
        /// <param name="packageName">Optional package name for package-specific commands</param>
        /// <returns>The command execution result</returns>
        public async Task<string> ExecuteCommandAsync(string command, string packageName = null)
        {
            try
            {
                var pipCommand = command.ToLower() switch
                {
                    "install" when !string.IsNullOrEmpty(packageName) => $"install {packageName}",
                    "install" => "install -r requirements.txt",
                    "uninstall" when !string.IsNullOrEmpty(packageName) => $"uninstall {packageName} -y",
                    "upgrade" when !string.IsNullOrEmpty(packageName) => $"install --upgrade {packageName}",
                    "list" => "list",
                    "freeze" => "freeze",
                    "show" when !string.IsNullOrEmpty(packageName) => $"show {packageName}",
                    "search" when !string.IsNullOrEmpty(packageName) => $"search {packageName}",
                    "outdated" => "list --outdated",
                    "check" => "check",
                    "version" => "--version",
                    _ => command
                };

                var result = await ExecutePipCommandAsync(pipCommand);
                
                if (result.Success)
                {
                    return FormatPipOutput(command, result.Output);
                }
                else
                {
                    return $"pip {command} failed: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing pip {command}: {ex.Message}";
            }
        }

        /// <summary>
        /// Runs tests using pytest or unittest
        /// </summary>
        /// <param name="testPattern">Optional test pattern to run specific tests</param>
        /// <returns>Test execution results</returns>
        public async Task<string> RunTestsAsync(string testPattern = null)
        {
            try
            {
                // Try pytest first, then fall back to unittest
                var pytestResult = await ExecutePythonCommandAsync($"-m pytest {testPattern ?? ""}");
                if (pytestResult.Success)
                {
                    return $"Test execution completed (pytest):\n{pytestResult.Output}";
                }

                // Fall back to unittest
                var unittestResult = await ExecutePythonCommandAsync($"-m unittest discover {testPattern ?? ""}");
                if (unittestResult.Success)
                {
                    return $"Test execution completed (unittest):\n{unittestResult.Output}";
                }
                else
                {
                    return $"Test execution failed:\n{unittestResult.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error running tests: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets package information from requirements.txt or setup.py
        /// </summary>
        /// <returns>Package information</returns>
        public async Task<string> GetPackageInfoAsync()
        {
            try
            {
                var info = new List<string>
                {
                    "=== Python Package Information ==="
                };

                // Check requirements.txt
                var requirementsPath = Path.Combine(_workingDirectory, "requirements.txt");
                if (File.Exists(requirementsPath))
                {
                    var requirements = await File.ReadAllLinesAsync(requirementsPath);
                    info.Add($"Requirements.txt found with {requirements.Length} packages:");
                    foreach (var req in requirements.Take(10)) // Show first 10
                    {
                        info.Add($"  {req}");
                    }
                    if (requirements.Length > 10)
                    {
                        info.Add($"  ... and {requirements.Length - 10} more packages");
                    }
                }

                // Check setup.py
                var setupPath = Path.Combine(_workingDirectory, "setup.py");
                if (File.Exists(setupPath))
                {
                    info.Add("setup.py found");
                }

                // Check pyproject.toml
                var pyprojectPath = Path.Combine(_workingDirectory, "pyproject.toml");
                if (File.Exists(pyprojectPath))
                {
                    info.Add("pyproject.toml found");
                }

                // Get installed packages
                var pipListResult = await ExecutePipCommandAsync("list");
                if (pipListResult.Success)
                {
                    var lines = pipListResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var packageCount = lines.Length - 2; // Subtract header lines
                    info.Add($"Installed packages: {Math.Max(0, packageCount)}");
                }

                return string.Join(Environment.NewLine, info);
            }
            catch (Exception ex)
            {
                return $"Error reading package information: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks for outdated packages
        /// </summary>
        /// <returns>Outdated packages information</returns>
        public async Task<string> CheckOutdatedPackagesAsync()
        {
            try
            {
                var result = await ExecutePipCommandAsync("list --outdated");
                
                if (result.Success)
                {
                    return $"Outdated packages check completed:\n{result.Output}";
                }
                else
                {
                    return $"Failed to check outdated packages:\n{result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error checking outdated packages: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a requirements.txt file from currently installed packages
        /// </summary>
        /// <returns>Operation result</returns>
        public async Task<string> FreezeRequirementsAsync()
        {
            try
            {
                var result = await ExecutePipCommandAsync("freeze");
                
                if (result.Success)
                {
                    var requirementsPath = Path.Combine(_workingDirectory, "requirements.txt");
                    await File.WriteAllTextAsync(requirementsPath, result.Output);
                    
                    var packageCount = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                    return $"Requirements.txt created with {packageCount} packages";
                }
                else
                {
                    return $"Failed to freeze requirements: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error freezing requirements: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes a pip command and returns the result
        /// </summary>
        private async Task<(bool Success, string Output, string Error)> ExecutePipCommandAsync(string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pip",
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
        /// Formats pip command output for better readability
        /// </summary>
        private string FormatPipOutput(string command, string output)
        {
            var formatted = new List<string>
            {
                $"=== pip {command} ==="
            };

            if (string.IsNullOrWhiteSpace(output))
            {
                formatted.Add("Command completed successfully (no output)");
            }
            else
            {
                // Clean up common pip output formatting
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    if (!string.IsNullOrEmpty(cleanLine) && 
                        !cleanLine.StartsWith("WARNING:") && 
                        !cleanLine.StartsWith("DEPRECATION:"))
                    {
                        formatted.Add(cleanLine);
                    }
                }
            }

            return string.Join(Environment.NewLine, formatted);
        }

        /// <summary>
        /// Shuts down the package manager asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            // Clean up resources
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the package manager and releases resources
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