using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;
using A3sist.API.Services;
using A3sist.API.Hubs;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RAGController : ControllerBase
{
    private readonly IRAGEngineService _ragService;
    private readonly IHubContext<A3sistHub> _hubContext;
    private readonly ILogger<RAGController> _logger;

    public RAGController(
        IRAGEngineService ragService,
        IHubContext<A3sistHub> hubContext,
        ILogger<RAGController> logger)
    {
        _ragService = ragService;
        _hubContext = hubContext;
        _logger = logger;
        
        // Subscribe to indexing progress events
        _ragService.IndexingProgress += OnIndexingProgress;
    }

    /// <summary>
    /// Index a workspace directory
    /// </summary>
    [HttpPost("index")]
    public async Task<ActionResult<bool>> IndexWorkspace([FromBody] IndexWorkspaceRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.WorkspacePath))
                return BadRequest(new { error = "Workspace path is required" });

            if (!Directory.Exists(request.WorkspacePath))
                return BadRequest(new { error = "Workspace path does not exist" });

            _logger.LogInformation("Starting workspace indexing for: {WorkspacePath}", request.WorkspacePath);
            
            // Start indexing in background
            var success = await _ragService.IndexWorkspaceAsync(request.WorkspacePath);
            
            if (success)
            {
                return Ok(new { success = true, message = "Workspace indexing started" });
            }
            else
            {
                return BadRequest(new { error = "Failed to start workspace indexing" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing workspace {WorkspacePath}", request?.WorkspacePath);
            return StatusCode(500, new { error = "Failed to index workspace" });
        }
    }

    /// <summary>
    /// Search indexed content
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<IEnumerable<SearchResult>>> Search([FromBody] SearchRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Query))
                return BadRequest(new { error = "Search query is required" });

            var maxResults = request.MaxResults > 0 ? request.MaxResults : 10;
            if (maxResults > 100) maxResults = 100; // Limit max results

            _logger.LogDebug("Searching for: {Query} (max results: {MaxResults})", request.Query, maxResults);

            var results = await _ragService.SearchAsync(request.Query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for query: {Query}", request?.Query);
            return StatusCode(500, new { error = "Failed to search" });
        }
    }

    /// <summary>
    /// Get current indexing status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<IndexingStatus>> GetIndexingStatus()
    {
        try
        {
            var status = await _ragService.GetIndexingStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexing status");
            return StatusCode(500, new { error = "Failed to get indexing status" });
        }
    }

    /// <summary>
    /// Add a document to the index
    /// </summary>
    [HttpPost("documents")]
    public async Task<ActionResult<bool>> AddDocument([FromBody] AddDocumentRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.DocumentPath) || string.IsNullOrEmpty(request.Content))
                return BadRequest(new { error = "Document path and content are required" });

            var success = await _ragService.AddDocumentAsync(request.DocumentPath, request.Content);
            
            if (success)
            {
                _logger.LogInformation("Document added to index: {DocumentPath}", request.DocumentPath);
                return Ok(new { success = true, message = "Document added to index" });
            }
            else
            {
                return BadRequest(new { error = "Failed to add document to index" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document to index: {DocumentPath}", request?.DocumentPath);
            return StatusCode(500, new { error = "Failed to add document" });
        }
    }

    /// <summary>
    /// Remove a document from the index
    /// </summary>
    [HttpDelete("documents")]
    public async Task<ActionResult<bool>> RemoveDocument([FromBody] RemoveDocumentRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.DocumentPath))
                return BadRequest(new { error = "Document path is required" });

            var success = await _ragService.RemoveDocumentAsync(request.DocumentPath);
            
            if (success)
            {
                _logger.LogInformation("Document removed from index: {DocumentPath}", request.DocumentPath);
                return Ok(new { success = true, message = "Document removed from index" });
            }
            else
            {
                return NotFound(new { error = "Document not found in index" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing document from index: {DocumentPath}", request?.DocumentPath);
            return StatusCode(500, new { error = "Failed to remove document" });
        }
    }

    /// <summary>
    /// Configure local RAG settings
    /// </summary>
    [HttpPut("config/local")]
    public async Task<ActionResult<bool>> ConfigureLocalRAG([FromBody] LocalRAGConfig config)
    {
        try
        {
            if (config == null)
                return BadRequest(new { error = "Configuration is required" });

            var success = await _ragService.ConfigureLocalRAGAsync(config);
            
            if (success)
            {
                _logger.LogInformation("Local RAG configuration updated");
                return Ok(new { success = true, message = "Local RAG configuration updated" });
            }
            else
            {
                return BadRequest(new { error = "Failed to update local RAG configuration" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring local RAG");
            return StatusCode(500, new { error = "Failed to configure local RAG" });
        }
    }

    /// <summary>
    /// Configure remote RAG settings
    /// </summary>
    [HttpPut("config/remote")]
    public async Task<ActionResult<bool>> ConfigureRemoteRAG([FromBody] RemoteRAGConfig config)
    {
        try
        {
            if (config == null)
                return BadRequest(new { error = "Configuration is required" });

            var success = await _ragService.ConfigureRemoteRAGAsync(config);
            
            if (success)
            {
                _logger.LogInformation("Remote RAG configuration updated for endpoint: {Endpoint}", config.ApiEndpoint);
                return Ok(new { success = true, message = "Remote RAG configuration updated" });
            }
            else
            {
                return BadRequest(new { error = "Failed to update remote RAG configuration" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring remote RAG");
            return StatusCode(500, new { error = "Failed to configure remote RAG" });
        }
    }

    private async void OnIndexingProgress(object? sender, IndexingProgressEventArgs e)
    {
        try
        {
            // Notify all connected clients about indexing progress
            await _hubContext.Clients.All.SendAsync("RAGIndexingProgress", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting indexing progress");
        }
    }
}

// Request/Response models for RAG operations
public class IndexWorkspaceRequest
{
    public string WorkspacePath { get; set; } = "";
}

public class SearchRequest
{
    public string Query { get; set; } = "";
    public int MaxResults { get; set; } = 10;
}

public class AddDocumentRequest
{
    public string DocumentPath { get; set; } = "";
    public string Content { get; set; } = "";
}

public class RemoveDocumentRequest
{
    public string DocumentPath { get; set; } = "";
}