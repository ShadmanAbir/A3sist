using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;
using A3sist.API.Services;
using A3sist.API.Hubs;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentModeService _agentService;
    private readonly IHubContext<A3sistHub> _hubContext;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentModeService agentService,
        IHubContext<A3sistHub> hubContext,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _hubContext = hubContext;
        _logger = logger;
        
        // Subscribe to agent events for real-time updates
        _agentService.ProgressChanged += OnProgressChanged;
        _agentService.IssueFound += OnIssueFound;
        _agentService.AnalysisCompleted += OnAnalysisCompleted;
    }

    /// <summary>
    /// Start agent analysis on a workspace
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<bool>> StartAnalysis([FromBody] StartAnalysisRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.WorkspacePath))
                return BadRequest(new { error = "Workspace path is required" });

            if (!Directory.Exists(request.WorkspacePath))
                return BadRequest(new { error = "Workspace path does not exist" });

            // Check if analysis is already running
            var isRunning = await _agentService.IsAnalysisRunningAsync();
            if (isRunning)
                return BadRequest(new { error = "Agent analysis is already running" });

            _logger.LogInformation("Starting agent analysis for workspace: {WorkspacePath}", request.WorkspacePath);

            var success = await _agentService.StartAnalysisAsync(request.WorkspacePath);
            
            if (success)
            {
                return Ok(new { success = true, message = "Agent analysis started successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to start agent analysis" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting agent analysis for workspace: {WorkspacePath}", request?.WorkspacePath);
            return StatusCode(500, new { error = "Failed to start agent analysis" });
        }
    }

    /// <summary>
    /// Stop the current agent analysis
    /// </summary>
    [HttpPost("stop")]
    public async Task<ActionResult<bool>> StopAnalysis()
    {
        try
        {
            var success = await _agentService.StopAnalysisAsync();
            
            if (success)
            {
                _logger.LogInformation("Agent analysis stopped");
                return Ok(new { success = true, message = "Agent analysis stopped successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to stop agent analysis or no analysis is running" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping agent analysis");
            return StatusCode(500, new { error = "Failed to stop agent analysis" });
        }
    }

    /// <summary>
    /// Get the current agent analysis status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<AgentStatusResponse>> GetStatus()
    {
        try
        {
            var isRunning = await _agentService.IsAnalysisRunningAsync();
            var report = await _agentService.GetCurrentReportAsync();
            
            var response = new AgentStatusResponse
            {
                IsRunning = isRunning,
                Status = report?.Status ?? AgentAnalysisStatus.NotStarted,
                WorkspacePath = report?.WorkspacePath ?? "",
                FilesAnalyzed = report?.FilesAnalyzed ?? 0,
                TotalFiles = report?.TotalFiles ?? 0,
                ProgressPercentage = report?.TotalFiles > 0 ? (double)report.FilesAnalyzed / report.TotalFiles * 100 : 0,
                FindingsCount = report?.Findings?.Count ?? 0,
                RecommendationsCount = report?.Recommendations?.Count ?? 0,
                StartTime = report?.StartTime,
                ElapsedTime = report?.StartTime != null ? DateTime.UtcNow - report.StartTime : null
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent status");
            return StatusCode(500, new { error = "Failed to get agent status" });
        }
    }

    /// <summary>
    /// Get the current analysis report
    /// </summary>
    [HttpGet("report")]
    public async Task<ActionResult<AgentAnalysisReport>> GetReport()
    {
        try
        {
            var report = await _agentService.GetCurrentReportAsync();
            
            if (report == null)
                return NotFound(new { error = "No analysis report available" });
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent analysis report");
            return StatusCode(500, new { error = "Failed to get analysis report" });
        }
    }

    /// <summary>
    /// Get analysis findings with optional filtering
    /// </summary>
    [HttpGet("findings")]
    public async Task<ActionResult<IEnumerable<AgentFinding>>> GetFindings(
        [FromQuery] AgentFindingType? type = null,
        [FromQuery] AgentSeverity? severity = null,
        [FromQuery] string? filePath = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var report = await _agentService.GetCurrentReportAsync();
            
            if (report?.Findings == null)
                return Ok(Enumerable.Empty<AgentFinding>());

            var findings = report.Findings.AsEnumerable();

            // Apply filters
            if (type.HasValue)
                findings = findings.Where(f => f.Type == type.Value);

            if (severity.HasValue)
                findings = findings.Where(f => f.Severity == severity.Value);

            if (!string.IsNullOrEmpty(filePath))
                findings = findings.Where(f => f.FilePath.Contains(filePath, StringComparison.OrdinalIgnoreCase));

            // Apply pagination
            var totalCount = findings.Count();
            var pagedFindings = findings
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(pagedFindings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent findings");
            return StatusCode(500, new { error = "Failed to get findings" });
        }
    }

    /// <summary>
    /// Get analysis recommendations with optional filtering
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<ActionResult<IEnumerable<AgentRecommendation>>> GetRecommendations(
        [FromQuery] AgentRecommendationType? type = null,
        [FromQuery] int? minPriority = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var report = await _agentService.GetCurrentReportAsync();
            
            if (report?.Recommendations == null)
                return Ok(Enumerable.Empty<AgentRecommendation>());

            var recommendations = report.Recommendations.AsEnumerable();

            // Apply filters
            if (type.HasValue)
                recommendations = recommendations.Where(r => r.Type == type.Value);

            if (minPriority.HasValue)
                recommendations = recommendations.Where(r => r.Priority >= minPriority.Value);

            // Sort by priority (descending) and impact score
            recommendations = recommendations
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.ImpactScore);

            // Apply pagination
            var totalCount = recommendations.Count();
            var pagedRecommendations = recommendations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(pagedRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent recommendations");
            return StatusCode(500, new { error = "Failed to get recommendations" });
        }
    }

    /// <summary>
    /// Get analysis statistics summary
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AgentStatistics>> GetStatistics()
    {
        try
        {
            var report = await _agentService.GetCurrentReportAsync();
            
            if (report == null)
                return NotFound(new { error = "No analysis report available" });

            var statistics = new AgentStatistics
            {
                TotalFiles = report.TotalFiles,
                FilesAnalyzed = report.FilesAnalyzed,
                TotalFindings = report.Findings?.Count ?? 0,
                FindingsByType = report.Findings?.GroupBy(f => f.Type)
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<AgentFindingType, int>(),
                FindingsBySeverity = report.Findings?.GroupBy(f => f.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<AgentSeverity, int>(),
                TotalRecommendations = report.Recommendations?.Count ?? 0,
                RecommendationsByType = report.Recommendations?.GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<AgentRecommendationType, int>(),
                AnalysisTime = report.TotalTime,
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                Status = report.Status
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent statistics");
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    private async void OnProgressChanged(object? sender, AgentProgressEventArgs e)
    {
        try
        {
            // Notify all connected clients about agent progress
            await _hubContext.Clients.All.SendAsync("AgentProgressChanged", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting agent progress");
        }
    }

    private async void OnIssueFound(object? sender, AgentIssueFoundEventArgs e)
    {
        try
        {
            // Notify all connected clients about new issues found
            await _hubContext.Clients.All.SendAsync("AgentIssueFound", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting agent issue found");
        }
    }

    private async void OnAnalysisCompleted(object? sender, AgentAnalysisCompletedEventArgs e)
    {
        try
        {
            // Notify all connected clients about analysis completion
            await _hubContext.Clients.All.SendAsync("AgentAnalysisCompleted", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting agent analysis completion");
        }
    }
}

// Request/Response models for Agent operations
public class StartAnalysisRequest
{
    public string WorkspacePath { get; set; } = "";
}

public class AgentStatusResponse
{
    public bool IsRunning { get; set; }
    public AgentAnalysisStatus Status { get; set; }
    public string WorkspacePath { get; set; } = "";
    public int FilesAnalyzed { get; set; }
    public int TotalFiles { get; set; }
    public double ProgressPercentage { get; set; }
    public int FindingsCount { get; set; }
    public int RecommendationsCount { get; set; }
    public DateTime? StartTime { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
}

public class AgentStatistics
{
    public int TotalFiles { get; set; }
    public int FilesAnalyzed { get; set; }
    public int TotalFindings { get; set; }
    public Dictionary<AgentFindingType, int> FindingsByType { get; set; } = new();
    public Dictionary<AgentSeverity, int> FindingsBySeverity { get; set; } = new();
    public int TotalRecommendations { get; set; }
    public Dictionary<AgentRecommendationType, int> RecommendationsByType { get; set; } = new();
    public TimeSpan AnalysisTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public AgentAnalysisStatus Status { get; set; }
}