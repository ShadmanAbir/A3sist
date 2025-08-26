using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace A3sist.UI.Utilities;

/// <summary>
/// Utility for managing settings migration and backup/restore operations in the UI
/// </summary>
public class SettingsMigrationUtility
{
    private readonly ISettingsPersistenceService _persistenceService;
    private readonly ILogger<SettingsMigrationUtility> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SettingsMigrationUtility(
        ISettingsPersistenceService persistenceService,
        ILogger<SettingsMigrationUtility> logger,
        IServiceProvider serviceProvider)
    {
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Performs automatic settings migration on startup
    /// </summary>
    public async Task<bool> PerformStartupMigrationAsync()
    {
        try
        {
            _logger.LogInformation("Starting automatic settings migration check");

            // Validate current settings
            var validationResult = await _persistenceService.ValidateSettingsAsync();
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Settings validation failed with {ErrorCount} errors", validationResult.Errors.Count);
                
                // Try to restore from backup
                var restored = await TryRestoreFromLatestBackupAsync();
                if (!restored)
                {
                    // Show error to user and offer to reset to defaults
                    var shouldReset = ShowMigrationErrorDialog(validationResult);
                    if (shouldReset)
                    {
                        await ResetSettingsToDefaultsAsync();
                        return true;
                    }
                    return false;
                }
            }

            // Load settings to trigger migration if needed
            await _persistenceService.LoadSettingsAsync();
            
            _logger.LogInformation("Settings migration check completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings migration failed");
            
            // Show error dialog and offer to reset
            var shouldReset = ShowMigrationFailureDialog(ex);
            if (shouldReset)
            {
                await ResetSettingsToDefaultsAsync();
                return true;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Creates a manual backup of current settings
    /// </summary>
    public async Task<string> CreateManualBackupAsync()
    {
        try
        {
            var backupPath = await _persistenceService.CreateBackupAsync();
            
            if (!string.IsNullOrEmpty(backupPath))
            {
                ShowMessage("Backup created successfully.", "Backup Complete", OLEMSGICON.OLEMSGICON_INFO);
                _logger.LogInformation("Manual backup created at {BackupPath}", backupPath);
            }
            else
            {
                ShowMessage("Failed to create backup. Please check the logs for details.", "Backup Failed", OLEMSGICON.OLEMSGICON_WARNING);
                _logger.LogWarning("Manual backup creation failed");
            }
            
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual backup creation failed");
            ShowMessage($"Backup creation failed: {ex.Message}", "Backup Error", OLEMSGICON.OLEMSGICON_CRITICAL);
            return null;
        }
    }

    /// <summary>
    /// Shows a dialog to select and restore from a backup
    /// </summary>
    public async Task<bool> ShowRestoreBackupDialogAsync()
    {
        try
        {
            var backups = await _persistenceService.GetAvailableBackupsAsync();
            
            if (!backups.Any())
            {
                ShowMessage("No backup files found.", "No Backups", OLEMSGICON.OLEMSGICON_INFO);
                return false;
            }

            // Create a simple selection dialog (in a real implementation, you'd use a proper dialog)
            var backupList = string.Join("\n", backups.Select((b, i) => 
                $"{i + 1}. {b.FileName} - {b.CreatedAt:yyyy-MM-dd HH:mm:ss} ({b.Version})"));
            
            var message = $"Available backups:\n\n{backupList}\n\nThis is a simplified selection. In a full implementation, you would show a proper dialog with backup details.";
            
            var result = ShowMessage(message, "Select Backup", OLEMSGICON.OLEMSGICON_QUESTION, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL);
            
            if (result == 1) // OK
            {
                // For this example, restore the latest backup
                var latestBackup = backups.First();
                return await RestoreFromBackupAsync(latestBackup.FilePath);
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore backup dialog failed");
            ShowMessage($"Failed to show restore dialog: {ex.Message}", "Restore Error", OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    /// <summary>
    /// Restores settings from a specific backup file
    /// </summary>
    public async Task<bool> RestoreFromBackupAsync(string backupPath)
    {
        try
        {
            var confirmResult = ShowMessage(
                "This will replace your current settings with the backup. Do you want to continue?",
                "Confirm Restore", OLEMSGICON.OLEMSGICON_QUESTION, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
            
            if (confirmResult != 6) // Not Yes
            {
                return false;
            }

            var success = await _persistenceService.RestoreFromBackupAsync(backupPath);
            
            if (success)
            {
                ShowMessage("Settings restored successfully. Please restart Visual Studio for all changes to take effect.", 
                    "Restore Complete", OLEMSGICON.OLEMSGICON_INFO);
                _logger.LogInformation("Settings restored from backup: {BackupPath}", backupPath);
            }
            else
            {
                ShowMessage("Failed to restore settings from backup. Please check the logs for details.", 
                    "Restore Failed", OLEMSGICON.OLEMSGICON_WARNING);
                _logger.LogWarning("Settings restore failed for backup: {BackupPath}", backupPath);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings restore failed");
            ShowMessage($"Restore failed: {ex.Message}", "Restore Error", OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public async Task<bool> ResetSettingsToDefaultsAsync()
    {
        try
        {
            var confirmResult = ShowMessage(
                "This will reset all A3sist settings to their default values. Do you want to continue?",
                "Confirm Reset", OLEMSGICON.OLEMSGICON_QUESTION, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
            
            if (confirmResult != 6) // Not Yes
            {
                return false;
            }

            // Create backup before reset
            await _persistenceService.CreateBackupAsync();

            // Save empty settings to trigger default value loading
            await _persistenceService.SaveSettingsAsync(new Dictionary<string, object>());
            
            ShowMessage("Settings reset to defaults successfully.", "Reset Complete", OLEMSGICON.OLEMSGICON_INFO);
            _logger.LogInformation("Settings reset to defaults");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings reset failed");
            ShowMessage($"Reset failed: {ex.Message}", "Reset Error", OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    /// <summary>
    /// Validates current settings and shows results to user
    /// </summary>
    public async Task ShowSettingsValidationResultsAsync()
    {
        try
        {
            var validationResult = await _persistenceService.ValidateSettingsAsync();
            
            var message = $"Settings Validation Results:\n\n" +
                         $"Valid: {validationResult.IsValid}\n" +
                         $"Errors: {validationResult.Errors.Count}\n" +
                         $"Warnings: {validationResult.Warnings.Count}";
            
            if (validationResult.Errors.Any())
            {
                message += "\n\nErrors:\n" + string.Join("\n", validationResult.Errors.Select(e => $"- {e.Message}"));
            }
            
            if (validationResult.Warnings.Any())
            {
                message += "\n\nWarnings:\n" + string.Join("\n", validationResult.Warnings.Select(w => $"- {w.Message}"));
            }
            
            var icon = validationResult.IsValid ? OLEMSGICON.OLEMSGICON_INFO : OLEMSGICON.OLEMSGICON_WARNING;
            ShowMessage(message, "Settings Validation", icon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings validation display failed");
            ShowMessage($"Validation failed: {ex.Message}", "Validation Error", OLEMSGICON.OLEMSGICON_CRITICAL);
        }
    }

    private async Task<bool> TryRestoreFromLatestBackupAsync()
    {
        try
        {
            var backups = await _persistenceService.GetAvailableBackupsAsync();
            var latestBackup = backups.FirstOrDefault();
            
            if (latestBackup != null)
            {
                _logger.LogInformation("Attempting automatic restore from latest backup");
                return await _persistenceService.RestoreFromBackupAsync(latestBackup.FilePath);
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatic backup restore failed");
            return false;
        }
    }

    private bool ShowMigrationErrorDialog(SettingsValidationResult validationResult)
    {
        var errorMessage = "Settings validation failed with the following errors:\n\n" +
                          string.Join("\n", validationResult.Errors.Select(e => $"- {e.Message}")) +
                          "\n\nWould you like to reset settings to defaults?";
        
        var result = ShowMessage(errorMessage, "Settings Error", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
        return result == 6; // Yes
    }

    private bool ShowMigrationFailureDialog(Exception ex)
    {
        var message = $"Settings migration failed: {ex.Message}\n\nWould you like to reset settings to defaults?";
        var result = ShowMessage(message, "Migration Error", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);
        return result == 6; // Yes
    }

    private int ShowMessage(string message, string title, OLEMSGICON icon, OLEMSGBUTTON button = OLEMSGBUTTON.OLEMSGBUTTON_OK)
    {
        var uiShell = _serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
        var clsid = Guid.Empty;
        uiShell?.ShowMessageBox(0, ref clsid, title, message, string.Empty, 0, button, 
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, icon, 0, out var result);
        return result;
    }
}