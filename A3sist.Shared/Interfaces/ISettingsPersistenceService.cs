using A3sist.Shared.Models;

namespace A3sist.Shared.Interfaces;

/// <summary>
/// Interface for settings persistence and migration service
/// </summary>
public interface ISettingsPersistenceService
{
    /// <summary>
    /// Saves settings to persistent storage
    /// </summary>
    /// <param name="settings">Settings to save</param>
    Task SaveSettingsAsync(Dictionary<string, object> settings);

    /// <summary>
    /// Loads settings from persistent storage
    /// </summary>
    /// <returns>Dictionary of settings</returns>
    Task<Dictionary<string, object>> LoadSettingsAsync();

    /// <summary>
    /// Creates a backup of current settings
    /// </summary>
    /// <returns>Path to the backup file, or null if backup failed</returns>
    Task<string> CreateBackupAsync();

    /// <summary>
    /// Restores settings from a backup file
    /// </summary>
    /// <param name="backupFilePath">Path to the backup file</param>
    /// <returns>True if restore was successful, false otherwise</returns>
    Task<bool> RestoreFromBackupAsync(string backupFilePath);

    /// <summary>
    /// Gets a list of available backup files
    /// </summary>
    /// <returns>List of backup information</returns>
    Task<List<BackupInfo>> GetAvailableBackupsAsync();

    /// <summary>
    /// Validates the integrity of settings data
    /// </summary>
    /// <returns>Validation result</returns>
    Task<SettingsValidationResult> ValidateSettingsAsync();
}