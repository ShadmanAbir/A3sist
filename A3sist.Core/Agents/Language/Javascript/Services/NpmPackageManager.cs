using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace A3sist.Orchastrator.Agents.JavaScript.Services
{
    /// <summary>
    /// Provides npm package management capabilities for JavaScript/TypeScript projects
    /// </summary>
    public class NpmPackageManager : IDisposable
    {
        private bool _disposed = false;
        private string _workingDirectory;

        public NpmPackageManager(string? workingDirectory = null)
        {
            _workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Initializes the npm package manager asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            // Check if npm is available
            try
            {
                var result = await ExecuteNpmCommandAsync("--version");
                if (!result.Success)
                {
                    throw new InvalidOperationException("npm is not available or not installed");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize npm package manager", ex);
            }
        }

        /// <summary>
        /// Executes an npm command
        /// </summary>
        /// <param name="command">The npm command to execute</param>
        /// <param name="packageName">Optional package name for package-specific commands</param>
        /// <returns>The command execution result</returns>
        public async Task<string> ExecuteCommandAsync(string command, string? packageName = null)
        {
            try
            {
                var npmCommand = command.ToLower() switch
                {
                    "install" when !string.IsNullOrEmpty(packageName) => $"install {packageName}",
                    "install" => "install",
                    "uninstall" when !string.IsNullOrEmpty(packageName) => $"uninstall {packageName}",
                    "update" when !string.IsNullOrEmpty(packageName) => $"update {packageName}",
                    "update" => "update",
                    "list" => "list --depth=0",
                    "outdated" => "outdated",
                    "audit" => "audit",
                    "audit-fix" => "audit fix",
                    "init" => "init -y",
                    "version" => "--version",
                    _ => command
                };

                var result = await ExecuteNpmCommandAsync(npmCommand);
                
                if (result.Success)
                {
                    return FormatNpmOutput(command, result.Output);
                }
                else
                {
                    return $"npm {command} failed: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing npm {command}: {ex.Message}";
            }
        }

        /// <summary>
        /// Runs tests using npm test command
        /// </summary>
        /// <param name="testPattern">Optional test pattern to run specific tests</param>
        /// <returns>Test execution results</returns>
        public async Task<string> RunTestsAsync(string? testPattern = null)
        {
            try
            {
                var testCommand = string.IsNullOrEmpty(testPattern) ? "test" : $"test -- --testPathPattern={testPattern}";
                var result = await ExecuteNpmCommandAsync(testCommand);
                
                if (result.Success)
                {
                    return $"Test execution completed:\n{result.Output}";
                }
                else
                {
                    return $"Test execution failed:\n{result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error running tests: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets package information from package.json
        /// </summary>
        /// <returns>Package information</returns>
        public async Task<string> GetPackageInfoAsync()
        {
            try
            {
                var packageJsonPath = Path.Combine(_workingDirectory, "package.json");
                
                if (!File.Exists(packageJsonPath))
                {
                    return "No package.json found in the current directory";
                }

                var packageJsonContent = File.ReadAllText(packageJsonPath);
                var packageInfo = JsonSerializer.Deserialize<JsonElement>(packageJsonContent);

                var info = new List<string>
                {
                    "=== Package Information ==="
                };

                if (packageInfo.TryGetProperty("name", out var name))
                {
                    info.Add($"Name: {name.GetString()}");
                }

                if (packageInfo.TryGetProperty("version", out var version))
                {
                    info.Add($"Version: {version.GetString()}");
                }

                if (packageInfo.TryGetProperty("description", out var description))
                {
                    info.Add($"Description: {description.GetString()}");
                }

                if (packageInfo.TryGetProperty("dependencies", out var dependencies))
                {
                    info.Add($"Dependencies: {dependencies.EnumerateObject().Count()}");
                }

                if (packageInfo.TryGetProperty("devDependencies", out var devDependencies))
                {
                    info.Add($"Dev Dependencies: {devDependencies.EnumerateObject().Count()}");
                }

                if (packageInfo.TryGetProperty("scripts", out var scripts))
                {
                    info.Add("Available Scripts:");
                    foreach (var script in scripts.EnumerateObject())
                    {
                        info.Add($"  {script.Name}: {script.Value.GetString()}");
                    }
                }

                return string.Join(Environment.NewLine, info);
            }
            catch (Exception ex)
            {
                return $"Error reading package information: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks for security vulnerabilities in dependencies
        /// </summary>
        /// <returns>Security audit results</returns>
        public async Task<string> SecurityAuditAsync()
        {
            try
            {
                var result = await ExecuteNpmCommandAsync("audit");
                
                if (result.Success)
                {
                    return $"Security audit completed:\n{result.Output}";
                }
                else
                {
                    // npm audit returns non-zero exit code when vulnerabilities are found
                    return $"Security vulnerabilities found:\n{result.Error}";
                }
            }
            catch (Exception ex)
            {
                return $"Error running security audit: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes an npm command and returns the result
        /// </summary>
        private async Task<(bool Success, string Output, string Error)> ExecuteNpmCommandAsync(string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
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
        /// Formats npm command output for better readability
        /// </summary>
        private string FormatNpmOutput(string command, string output)
        {
            var formatted = new List<string>
            {
                $"=== npm {command} ==="
            };

            if (string.IsNullOrWhiteSpace(output))
            {
                formatted.Add("Command completed successfully (no output)");
            }
            else
            {
                // Clean up common npm output formatting
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    if (!string.IsNullOrEmpty(cleanLine) && 
                        !cleanLine.StartsWith("npm WARN") && 
                        !cleanLine.StartsWith("npm notice"))
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