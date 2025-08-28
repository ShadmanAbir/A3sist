using Microsoft.AspNetCore.Mvc;
using A3sist.API.Models;
using A3sist.API.Services;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefactoringController : ControllerBase
{
    private readonly IRefactoringService _refactoringService;
    private readonly ILogger<RefactoringController> _logger;

    public RefactoringController(
        IRefactoringService refactoringService,
        ILogger<RefactoringController> logger)
    {
        _refactoringService = refactoringService;
        _logger = logger;
    }

    /// <summary>
    /// Get refactoring suggestions for code
    /// </summary>
    [HttpPost("suggestions")]
    public async Task<ActionResult<IEnumerable<RefactoringSuggestion>>> GetRefactoringSuggestions(
        [FromBody] GetRefactoringSuggestionsRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (string.IsNullOrEmpty(request.Language))
                return BadRequest(new { error = "Language is required" });

            var suggestions = await _refactoringService.GetRefactoringSuggestionsAsync(request.Code, request.Language);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refactoring suggestions");
            return StatusCode(500, new { error = "Failed to get refactoring suggestions" });
        }
    }

    /// <summary>
    /// Apply a specific refactoring
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<RefactoringResult>> ApplyRefactoring([FromBody] ApplyRefactoringRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.SuggestionId))
                return BadRequest(new { error = "Suggestion ID is required" });

            if (string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            var result = await _refactoringService.ApplyRefactoringAsync(request.SuggestionId, request.Code);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying refactoring");
            return StatusCode(500, new { error = "Failed to apply refactoring" });
        }
    }

    /// <summary>
    /// Preview a refactoring before applying it
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<RefactoringPreview>> PreviewRefactoring([FromBody] PreviewRefactoringRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.SuggestionId))
                return BadRequest(new { error = "Suggestion ID is required" });

            if (string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            var preview = await _refactoringService.PreviewRefactoringAsync(request.SuggestionId, request.Code);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing refactoring");
            return StatusCode(500, new { error = "Failed to preview refactoring" });
        }
    }

    /// <summary>
    /// Get code cleanup suggestions
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult<IEnumerable<CodeCleanupSuggestion>>> GetCleanupSuggestions(
        [FromBody] GetCleanupSuggestionsRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (string.IsNullOrEmpty(request.Language))
                return BadRequest(new { error = "Language is required" });

            var suggestions = await _refactoringService.GetCleanupSuggestionsAsync(request.Code, request.Language);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cleanup suggestions");
            return StatusCode(500, new { error = "Failed to get cleanup suggestions" });
        }
    }
}

// Request models for Refactoring operations
public class GetRefactoringSuggestionsRequest
{
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
}

public class ApplyRefactoringRequest
{
    public string SuggestionId { get; set; } = "";
    public string Code { get; set; } = "";
}

public class PreviewRefactoringRequest
{
    public string SuggestionId { get; set; } = "";
    public string Code { get; set; } = "";
}

public class GetCleanupSuggestionsRequest
{
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
}