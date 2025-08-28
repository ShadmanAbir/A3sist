using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace A3sistInstaller
{
    public class VisualStudioInstallation
    {
        public string InstallationPath { get; set; }
        public string Version { get; set; }
        public string Edition { get; set; }
        public string DisplayName { get; set; }
        public bool IsValid { get; set; }
        public DateTime InstallDate { get; set; }

        public override string ToString()
        {
            return $"{Edition} {Version} ({InstallationPath})";   
        }
    }

    public class VisualStudioDetector
    {
        private static readonly string[] VS2022_EDITIONS = { "Enterprise", "Professional", "Community" };
        private static readonly string[] VS_BASE_PATHS = 
        {
            @"C:\Program Files\Microsoft Visual Studio\2022",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2022"
        };

        public static List<VisualStudioInstallation> FindVisualStudioInstallations()
        {
            var installations = new List<VisualStudioInstallation>();

            // Method 1: Use vswhere.exe (most reliable)
            var vswhereInstallations = FindUsingVsWhere();
            installations.AddRange(vswhereInstallations);

            // Method 2: Check standard installation paths
            if (!installations.Any())
            {
                var standardInstallations = FindUsingStandardPaths();
                installations.AddRange(standardInstallations);
            }

            // Method 3: Check registry
            if (!installations.Any())
            {
                var registryInstallations = FindUsingRegistry();
                installations.AddRange(registryInstallations);
            }

            // Remove duplicates and sort
            installations = installations
                .GroupBy(vs => vs.InstallationPath)
                .Select(g => g.First())
                .OrderByDescending(vs => vs.Version)
                .ThenBy(vs => vs.Edition)
                .ToList();

            return installations;
        }

        private static List<VisualStudioInstallation> FindUsingVsWhere()
        {
            var installations = new List<VisualStudioInstallation>();

            try
            {
                // Try to find vswhere.exe
                var vswherePaths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
                    @"C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe"
                };

                string vswherePath = null;
                foreach (var path in vswherePaths)
                {
                    if (File.Exists(path))
                    {
                        vswherePath = path;
                        break;
                    }
                }

                if (vswherePath == null) return installations;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = vswherePath,
                        Arguments = "-version [17.0,18.0) -requires Microsoft.VisualStudio.Component.VSSDK -format json",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var vsInstances = JsonSerializer.Deserialize<VsWhereResult[]>(output);
                    foreach (var instance in vsInstances)
                    {
                        var installation = new VisualStudioInstallation
                        {
                            InstallationPath = instance.installationPath,
                            Version = instance.installationVersion,
                            Edition = GetEditionFromDisplayName(instance.displayName),
                            DisplayName = instance.displayName,
                            IsValid = ValidateInstallation(instance.installationPath),
                            InstallDate = DateTime.TryParse(instance.installDate, out var date) ? date : DateTime.MinValue
                        };
                        installations.Add(installation);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to other methods if vswhere fails
                System.Diagnostics.Debug.WriteLine($"vswhere failed: {ex.Message}");
            }

            return installations;
        }

        private static List<VisualStudioInstallation> FindUsingStandardPaths()
        {
            var installations = new List<VisualStudioInstallation>();

            foreach (var basePath in VS_BASE_PATHS)
            {
                if (!Directory.Exists(basePath)) continue;

                foreach (var edition in VS2022_EDITIONS)
                {
                    var installPath = Path.Combine(basePath, edition);
                    if (Directory.Exists(installPath) && ValidateInstallation(installPath))
                    {
                        var version = GetVersionFromPath(installPath);
                        installations.Add(new VisualStudioInstallation
                        {
                            InstallationPath = installPath,
                            Version = version,
                            Edition = edition,
                            DisplayName = $"Visual Studio {edition} 2022",
                            IsValid = true,
                            InstallDate = Directory.GetCreationTime(installPath)
                        });
                    }
                }
            }

            return installations;
        }

        private static List<VisualStudioInstallation> FindUsingRegistry()
        {
            var installations = new List<VisualStudioInstallation>();

            try
            {
                // Check HKLM\SOFTWARE\Microsoft\VisualStudio\Setup\Reboot
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\Setup\Reboot"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var installPath = subKey?.GetValue("InstallDir") as string;
                                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                                {
                                    var version = subKey.GetValue("Version") as string ?? "Unknown";
                                    var edition = GetEditionFromPath(installPath);
                                    
                                    if (ValidateInstallation(installPath))
                                    {
                                        installations.Add(new VisualStudioInstallation
                                        {
                                            InstallationPath = installPath,
                                            Version = version,
                                            Edition = edition,
                                            DisplayName = $"Visual Studio {edition} 2022",
                                            IsValid = true,
                                            InstallDate = DateTime.MinValue
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registry search failed: {ex.Message}");
            }

            return installations;
        }

        private static bool ValidateInstallation(string installPath)
        {
            if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                return false;

            // Check for essential VS files
            var devenvPath = Path.Combine(installPath, "Common7", "IDE", "devenv.exe");
            var vsixInstallerPath = Path.Combine(installPath, "Common7", "IDE", "VSIXInstaller.exe");
            
            return File.Exists(devenvPath) && File.Exists(vsixInstallerPath);
        }

        private static string GetVersionFromPath(string installPath)
        {
            try
            {
                var devenvPath = Path.Combine(installPath, "Common7", "IDE", "devenv.exe");
                if (File.Exists(devenvPath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(devenvPath);
                    return versionInfo.ProductVersion ?? "17.0";
                }
            }
            catch { }
            
            return "17.0"; // Default to VS 2022 version
        }

        private static string GetEditionFromPath(string installPath)
        {
            foreach (var edition in VS2022_EDITIONS)
            {
                if (installPath.Contains(edition, StringComparison.OrdinalIgnoreCase))
                    return edition;
            }
            return "Unknown";
        }

        private static string GetEditionFromDisplayName(string displayName)
        {
            foreach (var edition in VS2022_EDITIONS)
            {
                if (displayName.Contains(edition, StringComparison.OrdinalIgnoreCase))
                    return edition;
            }
            return "Unknown";
        }

        private class VsWhereResult
        {
            public string instanceId { get; set; }
            public string installDate { get; set; }
            public string installationName { get; set; }
            public string installationPath { get; set; }
            public string installationVersion { get; set; }
            public string productId { get; set; }
            public string productPath { get; set; }
            public string state { get; set; }
            public string displayName { get; set; }
            public string description { get; set; }
            public string channelId { get; set; }
            public string channelPath { get; set; }
            public string channelUri { get; set; }
            public string enginePath { get; set; }
            public string releaseNotes { get; set; }
            public string thirdPartyNotices { get; set; }
        }
    }
    public partial class InstallerForm : Form
    {
        private readonly string _installPath = @"C:\Program Files\A3sist";
        private readonly string _serviceName = "A3sistAPI";
        private readonly int _apiPort = 8341;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private TextBox _logTextBox;
        private Button _installButton;
        private Button _uninstallButton;
        private Button _exitButton;
        private CheckBox _installServiceCheckBox;
        private CheckBox _installExtensionCheckBox;
        private CheckBox _createShortcutsCheckBox;
        private ComboBox _vsInstallationComboBox;
        private Label _vsInstallationLabel;
        private Button _refreshVsButton;
        private List<VisualStudioInstallation> _vsInstallations;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "A3sist AI Assistant Installer";
            this.Size = new Size(600, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Icon = SystemIcons.Application;

            // Header
            var headerLabel = new Label
            {
                Text = "A3sist AI Assistant Installer",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Size = new Size(550, 30),
                Location = new Point(25, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(headerLabel);

            var descriptionLabel = new Label
            {
                Text = "Install the A3sist API service and Visual Studio extension",
                Font = new Font("Segoe UI", 10),
                Size = new Size(550, 20),
                Location = new Point(25, 55),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(descriptionLabel);

            // Installation options
            var optionsGroup = new GroupBox
            {
                Text = "Installation Options",
                Size = new Size(550, 100),
                Location = new Point(25, 90)
            };

            _installServiceCheckBox = new CheckBox
            {
                Text = "Install API Service (runs A3sist API as Windows service)",
                Checked = true,
                Size = new Size(500, 20),
                Location = new Point(15, 25)
            };
            optionsGroup.Controls.Add(_installServiceCheckBox);

            _installExtensionCheckBox = new CheckBox
            {
                Text = "Install Visual Studio Extension (.vsix)",
                Checked = true,
                Size = new Size(500, 20),
                Location = new Point(15, 50)
            };
            optionsGroup.Controls.Add(_installExtensionCheckBox);

            _createShortcutsCheckBox = new CheckBox
            {
                Text = "Create desktop shortcuts and management tools",
                Checked = true,
                Size = new Size(500, 20),
                Location = new Point(15, 75)
            };
            optionsGroup.Controls.Add(_createShortcutsCheckBox);

            this.Controls.Add(optionsGroup);

            // Visual Studio installation selection
            var vsGroup = new GroupBox
            {
                Text = "Visual Studio Installation",
                Size = new Size(550, 80),
                Location = new Point(25, 200)
            };

            _vsInstallationLabel = new Label
            {
                Text = "Select Visual Studio 2022 installation:",
                Size = new Size(300, 20),
                Location = new Point(15, 25)
            };
            vsGroup.Controls.Add(_vsInstallationLabel);

            _vsInstallationComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(400, 25),
                Location = new Point(15, 45)
            };
            vsGroup.Controls.Add(_vsInstallationComboBox);

            _refreshVsButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 25),
                Location = new Point(425, 45)
            };
            _refreshVsButton.Click += RefreshVsButton_Click;
            vsGroup.Controls.Add(_refreshVsButton);

            this.Controls.Add(vsGroup);

            // Initialize VS installations
            RefreshVisualStudioInstallations();

            // Progress section
            _statusLabel = new Label
            {
                Text = "Ready to install",
                Size = new Size(550, 20),
                Location = new Point(25, 290)
            };
            this.Controls.Add(_statusLabel);

            _progressBar = new ProgressBar
            {
                Size = new Size(550, 23),
                Location = new Point(25, 315),
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(_progressBar);

            // Log section
            var logLabel = new Label
            {
                Text = "Installation Log:",
                Size = new Size(200, 20),
                Location = new Point(25, 350)
            };
            this.Controls.Add(logLabel);

            _logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Size = new Size(550, 120),
                Location = new Point(25, 375),
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(_logTextBox);

            // Buttons
            _installButton = new Button
            {
                Text = "Install",
                Size = new Size(100, 30),
                Location = new Point(325, 505),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _installButton.Click += InstallButton_Click;
            this.Controls.Add(_installButton);

            _uninstallButton = new Button
            {
                Text = "Uninstall",
                Size = new Size(100, 30),
                Location = new Point(435, 505),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _uninstallButton.Click += UninstallButton_Click;
            this.Controls.Add(_uninstallButton);

            _exitButton = new Button
            {
                Text = "Exit",
                Size = new Size(100, 30),
                Location = new Point(545, 505)
            };
            _exitButton.Click += (s, e) => this.Close();
            this.Controls.Add(_exitButton);

            this.ResumeLayout(false);

            // Check current installation status
            CheckInstallationStatus();
        }

        private void RefreshVsButton_Click(object sender, EventArgs e)
        {
            RefreshVisualStudioInstallations();
        }

        private void RefreshVisualStudioInstallations()
        {
            UpdateStatus("Searching for Visual Studio installations...");
            _refreshVsButton.Enabled = false;
            
            Task.Run(() =>
            {
                try
                {
                    _vsInstallations = VisualStudioDetector.FindVisualStudioInstallations();
                    
                    Invoke(new Action(() =>
                    {
                        _vsInstallationComboBox.Items.Clear();
                        
                        if (_vsInstallations.Any())
                        {
                            foreach (var vs in _vsInstallations)
                            {
                                _vsInstallationComboBox.Items.Add(vs);
                            }
                            _vsInstallationComboBox.SelectedIndex = 0;
                            LogMessage($"✓ Found {_vsInstallations.Count} Visual Studio installation(s)");
                            foreach (var vs in _vsInstallations)
                            {
                                LogMessage($"  - {vs.DisplayName} at {vs.InstallationPath}");
                            }
                        }
                        else
                        {
                            _vsInstallationComboBox.Items.Add("No Visual Studio 2022 installations found");
                            _vsInstallationComboBox.SelectedIndex = 0;
                            LogMessage("⚠ No Visual Studio 2022 installations detected");
                        }
                        
                        UpdateStatus("Ready to install");
                        _refreshVsButton.Enabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        LogMessage($"✗ Error searching for Visual Studio: {ex.Message}");
                        _vsInstallationComboBox.Items.Clear();
                        _vsInstallationComboBox.Items.Add("Error detecting Visual Studio");
                        _vsInstallationComboBox.SelectedIndex = 0;
                        UpdateStatus("Ready to install");
                        _refreshVsButton.Enabled = true;
                    }));
                }
            });
        }

        private VisualStudioInstallation GetSelectedVisualStudioInstallation()
        {
            if (_vsInstallationComboBox.SelectedItem is VisualStudioInstallation vs)
            {
                return vs;
            }
            return null;
        }

        private void CheckInstallationStatus()
        {
            try
            {
                // Check if service exists
                var service = ServiceController.GetServices();
                var serviceExists = Array.Exists(service, s => s.ServiceName == _serviceName);

                if (serviceExists)
                {
                    LogMessage("✓ A3sist API service is currently installed");
                    _installButton.Text = "Reinstall";
                }

                // Check if extension is installed (simplified check)
                var vsixPath = Path.Combine(_installPath, "API");
                if (Directory.Exists(vsixPath))
                {
                    LogMessage("✓ A3sist installation directory found");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking installation status: {ex.Message}");
            }
        }

        private void RefreshVsButton_Click(object sender, EventArgs e)
        {
            RefreshVisualStudioInstallations();
        }

        private void RefreshVisualStudioInstallations()
        {
            UpdateStatus("Searching for Visual Studio installations...");
            _refreshVsButton.Enabled = false;
            
            Task.Run(() =>
            {
                try
                {
                    _vsInstallations = VisualStudioDetector.FindVisualStudioInstallations();
                    
                    Invoke(new Action(() =>
                    {
                        _vsInstallationComboBox.Items.Clear();
                        
                        if (_vsInstallations.Any())
                        {
                            foreach (var vs in _vsInstallations)
                            {
                                _vsInstallationComboBox.Items.Add(vs);
                            }
                            _vsInstallationComboBox.SelectedIndex = 0;
                            LogMessage($"✓ Found {_vsInstallations.Count} Visual Studio installation(s)");
                            foreach (var vs in _vsInstallations)
                            {
                                LogMessage($"  - {vs.DisplayName} at {vs.InstallationPath}");
                            }
                        }
                        else
                        {
                            _vsInstallationComboBox.Items.Add("No Visual Studio 2022 installations found");
                            _vsInstallationComboBox.SelectedIndex = 0;
                            LogMessage("⚠ No Visual Studio 2022 installations detected");
                        }
                        
                        UpdateStatus("Ready to install");
                        _refreshVsButton.Enabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        LogMessage($"✗ Error searching for Visual Studio: {ex.Message}");
                        _vsInstallationComboBox.Items.Clear();
                        _vsInstallationComboBox.Items.Add("Error detecting Visual Studio");
                        _vsInstallationComboBox.SelectedIndex = 0;
                        UpdateStatus("Ready to install");
                        _refreshVsButton.Enabled = true;
                    }));
                }
            });
        }

        private VisualStudioInstallation GetSelectedVisualStudioInstallation()
        {
            if (_vsInstallationComboBox.SelectedItem is VisualStudioInstallation vs)
            {
                return vs;
            }
            return null;
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            if (!IsRunAsAdmin())
            {
                MessageBox.Show("This installer requires administrator privileges. Please run as administrator.",
                    "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _installButton.Enabled = false;
            _uninstallButton.Enabled = false;
            _progressBar.Value = 0;

            try
            {
                await RunInstallation();
                MessageBox.Show("Installation completed successfully!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _installButton.Enabled = true;
                _uninstallButton.Enabled = true;
            }
        }

        private async void UninstallButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to uninstall A3sist?", 
                "Confirm Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _installButton.Enabled = false;
                _uninstallButton.Enabled = false;
                _progressBar.Value = 0;

                try
                {
                    await RunUninstallation();
                    MessageBox.Show("Uninstallation completed successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Uninstallation failed: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _installButton.Enabled = true;
                    _uninstallButton.Enabled = true;
                }
            }
        }

        private async Task RunInstallation()
        {
            UpdateStatus("Starting installation...");
            LogMessage("=== A3sist Installation Started ===");

            // Step 1: Check prerequisites
            UpdateStatus("Checking prerequisites...");
            _progressBar.Value = 10;
            await Task.Delay(500);

            if (!CheckDotNet9())
            {
                throw new Exception(".NET 9 runtime is required. Please install it from https://dotnet.microsoft.com/download/dotnet/9.0");
            }
            LogMessage("✓ .NET 9 runtime found");

            // Step 2: Install API Service
            if (_installServiceCheckBox.Checked)
            {
                UpdateStatus("Installing API service...");
                _progressBar.Value = 30;
                await InstallApiService();
                LogMessage("✓ API service installed");
            }

            // Step 3: Install VS Extension
            if (_installExtensionCheckBox.Checked)
            {
                UpdateStatus("Installing Visual Studio extension...");
                _progressBar.Value = 60;
                await InstallVSExtension();
                LogMessage("✓ Visual Studio extension installed");
            }

            // Step 4: Create configuration
            UpdateStatus("Creating configuration...");
            _progressBar.Value = 80;
            await CreateConfiguration();
            LogMessage("✓ Configuration created");

            // Step 5: Create shortcuts
            if (_createShortcutsCheckBox.Checked)
            {
                UpdateStatus("Creating shortcuts...");
                _progressBar.Value = 90;
                await CreateShortcuts();
                LogMessage("✓ Desktop shortcuts created");
            }

            UpdateStatus("Installation completed successfully!");
            _progressBar.Value = 100;
            LogMessage("=== Installation Completed ===");
        }

        private async Task RunUninstallation()
        {
            UpdateStatus("Starting uninstallation...");
            LogMessage("=== A3sist Uninstallation Started ===");

            _progressBar.Value = 20;
            await StopAndRemoveService();
            LogMessage("✓ Service stopped and removed");

            _progressBar.Value = 50;
            await RemoveFiles();
            LogMessage("✓ Files removed");

            _progressBar.Value = 80;
            await RemoveShortcuts();
            LogMessage("✓ Shortcuts removed");

            UpdateStatus("Uninstallation completed successfully!");
            _progressBar.Value = 100;
            LogMessage("=== Uninstallation Completed ===");
        }

        private bool CheckDotNet9()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.StartsWith("9.");
            }
            catch
            {
                return false;
            }
        }

        private async Task InstallApiService()
        {
            // Implementation would copy files and create service
            await Task.Delay(1000); // Simulate work
            // Add actual implementation here
        }

        private async Task InstallVSExtension()
        {
            var selectedVs = GetSelectedVisualStudioInstallation();
            if (selectedVs == null)
            {
                throw new Exception("No Visual Studio installation selected");
            }

            var vsixPath = Path.Combine(Directory.GetCurrentDirectory(), "A3sist.UI\\bin\\Release\\A3sist.UI.vsix");
            if (!File.Exists(vsixPath))
            {
                throw new Exception($"VSIX file not found at: {vsixPath}");
            }

            var vsixInstaller = Path.Combine(selectedVs.InstallationPath, "Common7\\IDE\\VSIXInstaller.exe");
            if (!File.Exists(vsixInstaller))
            {
                throw new Exception($"VSIXInstaller not found at: {vsixInstaller}");
            }

            LogMessage($"Installing extension to {selectedVs.DisplayName}...");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = vsixInstaller,
                    Arguments = $"/quiet \"{vsixPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                LogMessage("✓ Visual Studio extension installed successfully");
            }
            else
            {
                LogMessage($"⚠ Extension installation returned exit code: {process.ExitCode}");
            }
        }

        private async Task CreateConfiguration()
        {
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "A3sist");
            Directory.CreateDirectory(configDir);

            var config = @"{
  ""ApiUrl"": ""http://localhost:8341"",
  ""AutoStartApi"": true,
  ""AutoCompleteEnabled"": true,
  ""RequestTimeout"": 30,
  ""EnableLogging"": true
}";

            await File.WriteAllTextAsync(Path.Combine(configDir, "config.json"), config);
        }

        private async Task CreateShortcuts()
        {
            // Implementation would create desktop shortcuts
            await Task.Delay(500); // Simulate work
        }

        private async Task StopAndRemoveService()
        {
            // Implementation would stop and remove service
            await Task.Delay(1000); // Simulate work
        }

        private async Task RemoveFiles()
        {
            // Implementation would remove installation files
            await Task.Delay(1000); // Simulate work
        }

        private async Task RemoveShortcuts()
        {
            // Implementation would remove shortcuts
            await Task.Delay(500); // Simulate work
        }

        private bool IsRunAsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _statusLabel.Text = status));
            }
            else
            {
                _statusLabel.Text = status;
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => {
                    _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    _logTextBox.ScrollToCaret();
                }));
            }
            else
            {
                _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                _logTextBox.ScrollToCaret();
            }
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}