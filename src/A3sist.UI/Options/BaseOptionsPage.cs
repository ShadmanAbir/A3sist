using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace A3sist.UI.Options;

/// <summary>
/// Base class for A3sist options pages
/// </summary>
[ComVisible(true)]
public abstract class BaseOptionsPage : DialogPage
{
    /// <summary>
    /// Gets the category name for this options page
    /// </summary>
    public abstract string CategoryName { get; }

    /// <summary>
    /// Gets the page name for this options page
    /// </summary>
    public abstract string PageName { get; }

    /// <summary>
    /// Called when the options page is loaded
    /// </summary>
    public override void LoadSettingsFromStorage()
    {
        base.LoadSettingsFromStorage();
        OnSettingsLoaded();
    }

    /// <summary>
    /// Called when the options page is saved
    /// </summary>
    public override void SaveSettingsToStorage()
    {
        OnSettingsSaving();
        base.SaveSettingsToStorage();
        OnSettingsSaved();
    }

    /// <summary>
    /// Called when settings are loaded from storage
    /// </summary>
    protected virtual void OnSettingsLoaded()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called before settings are saved to storage
    /// </summary>
    protected virtual void OnSettingsSaving()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called after settings are saved to storage
    /// </summary>
    protected virtual void OnSettingsSaved()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Validates the current settings
    /// </summary>
    /// <returns>True if settings are valid, false otherwise</returns>
    public virtual bool ValidateSettings()
    {
        return true;
    }

    /// <summary>
    /// Resets settings to default values
    /// </summary>
    public virtual void ResetToDefaults()
    {
        ResetSettings();
    }
}