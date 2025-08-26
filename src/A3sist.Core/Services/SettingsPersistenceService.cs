using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace A3sist.Core.Services;

/// <summary>
/// Service for persisting and migrating A3sist settings
/// </summary>
public class SettingsPersistenceService : ISettingsPersistenceService
{
    private readonly ILogger<SettingsPersistenceService> _logger;
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly string _backupDirectory;

    public SettingsPersistenceService(ILogger<SettingsPersistenceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "A3sist");
        
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        _backupDirectory = Path.Combine(_settingsDirectory, "backups");
        
        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Saves settings to persistent storage
    /// </summary>
    public async Task SaveSettingsAsync(Dictionary<string, object> settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        try
        {
            // Create backup before saving
            await CreateBackupAsync();

            var settingsData = new SettingsData
            {
                Version = GetCurrentVersion(),
                CreatedAt = DateTime.UtcNow,
                Settings = settings,
                Checksum = CalculateChecksum(settings)
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(settingsData, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _logger.LogInformation("Settings saved successfully to {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {FilePath}", _settingsFilePath);
            throw;
        }
    }

    /// <summary>
    /// Loads settings from persistent storage
    /// </summary>
    public async Task<Dictionary<string, object>> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file does not exist, returning empty settings");
                return new Dictionary<string, object>();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Settings file is empty");
                return new Dictionary<string, object>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var settingsData = JsonSerializer.Deserialize<SettingsData>(json, options);
            if (settingsData == null)
            {
                _logger.LogWarning("Failed to deserialize settings data");
                return new Dictionary<string, object>();
            }

            // Verify checksum
            var calculatedChecksum = CalculateChecksum(settingsData.Settings);
            if (calculatedChecksum != settingsData.Checksum)
            {
                _logger.LogWarning("Settings checksum mismatch, data may be corrupted");
            }

            // Check if migration is needed
            if (settingsData.Version != GetCurrentVersion())
            {
                _logger.LogInformation("Settings migration needed from version {OldVersion} to {NewVersion}", 
                    settingsData.Version, GetCurrentVersion());
                
                settingsData.Settings = await MigrateSettingsAsync(settingsData.Settings, settingsData.Version);
                
                // Save migrated settings
                await SaveSettingsAsync(settingsData.Settings);
            }

            _logger.LogInformation("Settings loaded successfully from {FilePath}", _settingsFilePath);
            return settingsData.Settings ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {FilePath}", _settingsFilePath);
            
            // Try to restore from backup
            var backupSettings = await TryRestoreFromBackupAsync();
            return backupSettings ?? new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Creates a backup of current settings
    /// </summary>
    public async Task<string> CreateBackupAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("No settings file to backup");
                return null;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"settings_backup_{timestamp}.json";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            await File.CopyToAsync(_settingsFilePath, backupFilePath);

            // Clean up old backups (keep only last 10)
            await CleanupOldBackupsAsync();

            _logger.LogInformation("Settings backup created at {BackupPath}", backupFilePath);
            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settings backup");
            return null;
        }
    }

    /// <summary>
    /// Restores settings from a backup file
    /// </summary>
    public async Task<bool> RestoreFromBackupAsync(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
            throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));

        try
        {
            if (!File.Exists(backupFilePath))
            {
                _logger.LogError("Backup file does not exist: {BackupPath}", backupFilePath);
                return false;
            }

            // Create backup of current settings before restore
            await CreateBackupAsync();

            // Copy backup file to settings location
            await File.CopyToAsync(backupFilePath, _settingsFilePath, overwrite: true);

            _logger.LogInformation("Settings restored from backup: {BackupPath}", backupFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore settings from backup: {BackupPath}", backupFilePath);
            return false;
        }
    }

    /// <summary>
    /// Gets a list of available backup files
    /// </summary>
    public async Task<List<BackupInfo>> GetAvailableBackupsAsync()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
            {
                return new List<BackupInfo>();
            }

            var backupFiles = Directory.GetFiles(_backupDirectory, "settings_backup_*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            var backups = new List<BackupInfo>();
            
            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(backupFile);
                    var json = await File.ReadAllTextAsync(backupFile);
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var settingsData = JsonSerializer.Deserialize<SettingsData>(json, options);
                    
                    backups.Add(new BackupInfo
                    {
                        FilePath = backupFile,
                        FileName = fileInfo.Name,
                        CreatedAt = settingsData?.CreatedAt ?? fileInfo.CreationTime,
                        Version = settingsData?.Version ?? "Unknown",
                        Size = fileInfo.Length
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read backup file info: {BackupFile}", backupFile);
                }
            }

            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available backups");
            return new List<BackupInfo>();
        }
    }

    /// <summary>
    /// Validates the integrity of settings data
    /// </summary>
    public async Task<SettingsValidationResult> ValidateSettingsAsync()
    {
        var result = new SettingsValidationResult();

        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                result.AddWarning("File", "Settings file does not exist");
                return result;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                result.AddError("Content", "Settings file is empty");
                return result;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var settingsData = JsonSerializer.Deserialize<SettingsData>(json, options);
            if (settingsData == null)
            {
                result.AddError("Format", "Invalid settings file format");
                return result;
            }

            // Validate checksum
            if (!string.IsNullOrEmpty(settingsData.Checksum))
            {
                var calculatedChecksum = CalculateChecksum(settingsData.Settings);
                if (calculatedChecksum != settingsData.Checksum)
                {
                    result.AddError("Integrity", "Settings checksum mismatch - data may be corrupted");
                }
            }

            // Validate version
            if (string.IsNullOrEmpty(settingsData.Version))
            {
                result.AddWarning("Version", "Settings version is not specified");
            }
            else if (settingsData.Version != GetCurrentVersion())
            {
                result.AddWarning("Version", $"Settings version {settingsData.Version} differs from current version {GetCurrentVersion()}");
            }

            // Validate settings structure
            if (settingsData.Settings == null)
            {
                result.AddError("Structure", "Settings data is null");
            }
            else if (settingsData.Settings.Count == 0)
            {
                result.AddWarning("Structure", "Settings data is empty");
            }

            _logger.LogInformation("Settings validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings validation failed");
            result.AddError("Validation", $"Validation process failed: {ex.Message}");
        }

        return result;
    }

    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }

            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settings directories");
            throw;
        }
    }

    private async Task<Dictionary<string, object>> MigrateSettingsAsync(Dictionary<string, object> settings, string fromVersion)
    {
        _logger.LogInformation("Migrating settings from version {FromVersion} to {ToVersion}", fromVersion, GetCurrentVersion());

        try
        {
            var migratedSettings = new Dictionary<string, object>(settings);

            // Apply version-specific migrations
            switch (fromVersion)
            {
                case "1.0":
                    migratedSettings = await MigrateFrom1_0To1_1(migratedSettings);
                    goto case "1.1";
                
                case "1.1":
                    migratedSettings = await MigrateFrom1_1To1_2(migratedSettings);
                    break;
                
                default:
                    _logger.LogWarning("No migration path defined for version {Version}", fromVersion);
                    break;
            }

            _logger.LogInformation("Settings migration completed successfully");
            return migratedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings migration failed");
            throw;
        }
    }

    private async Task<Dictionary<string, object>> MigrateFrom1_0To1_1(Dictionary<string, object> settings)
    {
        // Example migration: Add new default settings introduced in version 1.1
        if (!settings.ContainsKey("EnableDataAnonymization"))
        {
            settings["EnableDataAnonymization"] = true;
        }

        if (!settings.ContainsKey("EnableAgentHealthMonitoring"))
        {
            settings["EnableAgentHealthMonitoring"] = true;
        }

        return await Task.FromResult(settings);
    }

    private async Task<Dictionary<string, object>> MigrateFrom1_1To1_2(Dictionary<string, object> settings)
    {
        // Example migration: Rename settings keys
        if (settings.ContainsKey("OldSettingName"))
        {
            settings["NewSettingName"] = settings["OldSettingName"];
            settings.Remove("OldSettingName");
        }

        return await Task.FromResult(settings);
    }

    private async Task<Dictionary<string, object>> TryRestoreFromBackupAsync()
    {
        try
        {
            var backups = await GetAvailableBackupsAsync();
            var latestBackup = backups.FirstOrDefault();
            
            if (latestBackup != null)
            {
                _logger.LogInformation("Attempting to restore from latest backup: {BackupPath}", latestBackup.FilePath);
                
                var json = await File.ReadAllTextAsync(latestBackup.FilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                
                var settingsData = JsonSerializer.Deserialize<SettingsData>(json, options);
                return settingsData?.Settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup");
        }

        return null;
    }

    private async Task CleanupOldBackupsAsync()
    {
        try
        {
            const int maxBackups = 10;
            
            var backupFiles = Directory.GetFiles(_backupDirectory, "settings_backup_*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (backupFiles.Count > maxBackups)
            {
                var filesToDelete = backupFiles.Skip(maxBackups);
                
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted old backup file: {FilePath}", file);
                }
                
                _logger.LogInformation("Cleaned up {Count} old backup files", filesToDelete.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backup files");
        }

        await Task.CompletedTask;
    }

    private static string CalculateChecksum(Dictionary<string, object> settings)
    {
        if (settings == null)
            return string.Empty;

        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hash);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetCurrentVersion()
    {
        return "1.2"; // Current settings version
    }
}

/// <summary>
/// Settings data structure for persistence
/// </summary>
public class SettingsData
{
    public string Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public string Checksum { get; set; }
}