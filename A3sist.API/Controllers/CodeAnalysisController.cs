using Microsoft.AspNetCore.Mvc;
using A3sist.API.Models;
using A3sist.API.Services;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeAnalysisController : ControllerBase
{
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly ILogger<CodeAnalysisController> _logger;

    public CodeAnalysisController(
        ICodeAnalysisService codeAnalysisService,
        ILogger<CodeAnalysisController> logger)
    {
        _codeAnalysisService = codeAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Detect the programming language of code
    /// </summary>
    [HttpPost("detect-language")]
    public async Task<ActionResult<string>> DetectLanguage([FromBody] DetectLanguageRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            var language = await _codeAnalysisService.DetectLanguageAsync(request.Code, request.FileName);
            return Ok(new { language = language });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting language");
            return StatusCode(500, new { error = "Failed to detect language" });
        }
    }

    /// <summary>
    /// Extract code context at a specific position
    /// </summary>
    [HttpPost("context")]
    public async Task<ActionResult<CodeContext>> ExtractContext([FromBody] ExtractContextRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (request.Position < 0 || request.Position > request.Code.Length)
                return BadRequest(new { error = "Invalid position" });

            var context = await _codeAnalysisService.ExtractContextAsync(request.Code, request.Position);
            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting context");
            return StatusCode(500, new { error = "Failed to extract context" });
        }
    }

    /// <summary>
    /// Analyze code for issues and problems
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<IEnumerable<CodeIssue>>> AnalyzeCode([FromBody] AnalyzeCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (string.IsNullOrEmpty(request.Language))
                return BadRequest(new { error = "Language is required" });

            var issues = await _codeAnalysisService.AnalyzeCodeAsync(request.Code, request.Language);
            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing code");
            return StatusCode(500, new { error = "Failed to analyze code" });
        }
    }

    /// <summary>
    /// Get syntax tree for code
    /// </summary>
    [HttpPost("syntax-tree")]
    public async Task<ActionResult<SyntaxTree>> GetSyntaxTree([FromBody] GetSyntaxTreeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Code))
                return BadRequest(new { error = "Code is required" });

            if (string.IsNullOrEmpty(request.Language))
                return BadRequest(new { error = "Language is required" });

            var syntaxTree = await _codeAnalysisService.GetSyntaxTreeAsync(request.Code, request.Language);
            return Ok(syntaxTree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting syntax tree");
            return StatusCode(500, new { error = "Failed to get syntax tree" });
        }
    }

    /// <summary>
    /// Get supported programming languages
    /// </summary>
    [HttpGet("languages")]
    public async Task<ActionResult<IEnumerable<string>>> GetSupportedLanguages()
    {
        try
        {
            var languages = await _codeAnalysisService.GetSupportedLanguagesAsync();
            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported languages");
            return StatusCode(500, new { error = "Failed to get supported languages" });
        }
    }
}

// Request models for CodeAnalysis operations
public class DetectLanguageRequest
{
    public string Code { get; set; } = "";
    public string? FileName { get; set; }
}

public class ExtractContextRequest
{
    public string Code { get; set; } = "";
    public int Position { get; set; }
}

public class AnalyzeCodeRequest
{
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
}

public class GetSyntaxTreeRequest
{
    public string Code { get; set; } = "";
    public string Language { get; set; } = "";
}