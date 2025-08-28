using Microsoft.AspNetCore.Mvc;
using A3sist.API.Models;
using A3sist.API.Services;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutoCompleteController : ControllerBase
{
    private readonly IAutoCompleteService _autoCompleteService;
    private readonly ILogger<AutoCompleteController> _logger;

    public AutoCompleteController(
        IAutoCompleteService autoCompleteService,
        ILogger<AutoCompleteController> logger)
    {
        _autoCompleteService = autoCompleteService;
        _logger = logger;
    }

    /// <summary>
    /// Get code completion suggestions
    /// </summary>
    [HttpPost("suggestions")]
    public async Task<ActionResult<IEnumerable<CompletionItem>>> GetCompletionSuggestions(
        [FromBody] GetCompletionSuggestionsRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (string.IsNullOrEmpty(request.Language))
                return BadRequest(new { error = "Language is required" });

            if (request.Position < 0 || request.Position > request.Code.Length)
                return BadRequest(new { error = "Invalid position" });

            var suggestions = await _autoCompleteService.GetCompletionSuggestionsAsync(
                request.Code, request.Position, request.Language);
            
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion suggestions");
            return StatusCode(500, new { error = "Failed to get completion suggestions" });
        }
    }

    /// <summary>
    /// Check if autocomplete is enabled
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<bool>> IsAutoCompleteEnabled()
    {
        try
        {
            var isEnabled = await _autoCompleteService.IsAutoCompleteEnabledAsync();
            return Ok(new { enabled = isEnabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking autocomplete status");
            return StatusCode(500, new { error = "Failed to check autocomplete status" });
        }
    }

    /// <summary>
    /// Enable or disable autocomplete
    /// </summary>
    [HttpPut("enabled")]
    public async Task<ActionResult<bool>> SetAutoCompleteEnabled([FromBody] SetAutoCompleteEnabledRequest request)
    {
        try
        {
            var success = await _autoCompleteService.SetAutoCompleteEnabledAsync(request.Enabled);
            
            if (success)
            {
                return Ok(new { success = true, message = $"Autocomplete {(request.Enabled ? "enabled" : "disabled")}" });
            }
            else
            {
                return BadRequest(new { error = "Failed to update autocomplete status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting autocomplete status");
            return StatusCode(500, new { error = "Failed to set autocomplete status" });
        }
    }

    /// <summary>
    /// Get autocomplete settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<CompletionSettings>> GetSettings()
    {
        try
        {
            var settings = await _autoCompleteService.GetSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete settings");
            return StatusCode(500, new { error = "Failed to get autocomplete settings" });
        }
    }

    /// <summary>
    /// Update autocomplete settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<bool>> UpdateSettings([FromBody] CompletionSettings settings)
    {
        try
        {
            if (settings == null)
                return BadRequest(new { error = "Settings are required" });

            var success = await _autoCompleteService.UpdateSettingsAsync(settings);
            
            if (success)
            {
                return Ok(new { success = true, message = "Autocomplete settings updated" });
            }
            else
            {
                return BadRequest(new { error = "Failed to update autocomplete settings" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating autocomplete settings");
            return StatusCode(500, new { error = "Failed to update autocomplete settings" });
        }
    }
}

// Request models for AutoComplete operations
public class GetCompletionSuggestionsRequest
{
    public string Code { get; set; } = "";
    public int Position { get; set; }
    public string Language { get; set; } = "";
}

public class SetAutoCompleteEnabledRequest
{
    public bool Enabled { get; set; }
}