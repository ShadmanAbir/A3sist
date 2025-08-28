using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;
using A3sist.API.Services;
using A3sist.API.Hubs;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MCPController : ControllerBase
{
    private readonly IMCPClientService _mcpService;
    private readonly IHubContext<A3sistHub> _hubContext;
    private readonly ILogger<MCPController> _logger;

    public MCPController(
        IMCPClientService mcpService,
        IHubContext<A3sistHub> hubContext,
        ILogger<MCPController> logger)
    {
        _mcpService = mcpService;
        _hubContext = hubContext;
        _logger = logger;
        
        // Subscribe to server status events
        _mcpService.ServerStatusChanged += OnServerStatusChanged;
    }

    /// <summary>
    /// Get all available MCP servers
    /// </summary>
    [HttpGet("servers")]
    public async Task<ActionResult<IEnumerable<MCPServerInfo>>> GetServers()
    {
        try
        {
            var servers = await _mcpService.GetAvailableServersAsync();
            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP servers");
            return StatusCode(500, new { error = "Failed to get MCP servers" });
        }
    }

    /// <summary>
    /// Add a new MCP server
    /// </summary>
    [HttpPost("servers")]
    public async Task<ActionResult<bool>> AddServer([FromBody] MCPServerInfo serverInfo)
    {
        try
        {
            if (serverInfo == null)
                return BadRequest(new { error = "Server information is required" });

            if (string.IsNullOrEmpty(serverInfo.Id) || string.IsNullOrEmpty(serverInfo.Name))
                return BadRequest(new { error = "Server ID and Name are required" });

            if (string.IsNullOrEmpty(serverInfo.Endpoint))
                return BadRequest(new { error = "Server endpoint is required" });

            var success = await _mcpService.AddServerAsync(serverInfo);
            
            if (success)
            {
                _logger.LogInformation("MCP server {ServerName} added via API", serverInfo.Name);
                return Ok(new { success = true, message = "MCP server added successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to add MCP server" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding MCP server {ServerName}", serverInfo?.Name);
            return StatusCode(500, new { error = "Failed to add MCP server" });
        }
    }

    /// <summary>
    /// Update an existing MCP server
    /// </summary>
    [HttpPut("servers/{serverId}")]
    public async Task<ActionResult<bool>> UpdateServer(string serverId, [FromBody] MCPServerInfo serverInfo)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            if (serverInfo == null)
                return BadRequest(new { error = "Server information is required" });

            // Ensure the server ID matches
            serverInfo.Id = serverId;

            var success = await _mcpService.AddServerAsync(serverInfo); // AddServer handles updates too
            
            if (success)
            {
                _logger.LogInformation("MCP server {ServerName} updated via API", serverInfo.Name);
                return Ok(new { success = true, message = "MCP server updated successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to update MCP server" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to update MCP server" });
        }
    }

    /// <summary>
    /// Remove an MCP server
    /// </summary>
    [HttpDelete("servers/{serverId}")]
    public async Task<ActionResult<bool>> RemoveServer(string serverId)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            var success = await _mcpService.RemoveServerAsync(serverId);
            
            if (success)
            {
                _logger.LogInformation("MCP server {ServerId} removed via API", serverId);
                return Ok(new { success = true, message = "MCP server removed successfully" });
            }
            else
            {
                return NotFound(new { error = "MCP server not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to remove MCP server" });
        }
    }

    /// <summary>
    /// Connect to an MCP server
    /// </summary>
    [HttpPost("servers/{serverId}/connect")]
    public async Task<ActionResult<bool>> ConnectToServer(string serverId)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            var servers = await _mcpService.GetAvailableServersAsync();
            var server = servers.FirstOrDefault(s => s.Id == serverId);
            
            if (server == null)
                return NotFound(new { error = "MCP server not found" });

            var success = await _mcpService.ConnectToServerAsync(server);
            
            if (success)
            {
                _logger.LogInformation("Connected to MCP server {ServerName}", server.Name);
                return Ok(new { success = true, message = "Connected to MCP server successfully" });
            }
            else
            {
                return BadRequest(new { error = "Failed to connect to MCP server" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to connect to MCP server" });
        }
    }

    /// <summary>
    /// Disconnect from an MCP server
    /// </summary>
    [HttpPost("servers/{serverId}/disconnect")]
    public async Task<ActionResult<bool>> DisconnectFromServer(string serverId)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            var success = await _mcpService.DisconnectFromServerAsync(serverId);
            
            if (success)
            {
                _logger.LogInformation("Disconnected from MCP server {ServerId}", serverId);
                return Ok(new { success = true, message = "Disconnected from MCP server successfully" });
            }
            else
            {
                return NotFound(new { error = "MCP server not found or not connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to disconnect from MCP server" });
        }
    }

    /// <summary>
    /// Test connection to an MCP server
    /// </summary>
    [HttpPost("servers/{serverId}/test")]
    public async Task<ActionResult<bool>> TestServer(string serverId)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            var success = await _mcpService.TestServerConnectionAsync(serverId);
            
            return Ok(new { 
                success = success, 
                message = success ? "MCP server connection successful" : "MCP server connection failed" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to test MCP server connection" });
        }
    }

    /// <summary>
    /// Get available tools for a specific MCP server
    /// </summary>
    [HttpGet("servers/{serverId}/tools")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableTools(string serverId)
    {
        try
        {
            if (string.IsNullOrEmpty(serverId))
                return BadRequest(new { error = "Server ID is required" });

            var tools = await _mcpService.GetAvailableToolsAsync(serverId);
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tools for MCP server {ServerId}", serverId);
            return StatusCode(500, new { error = "Failed to get available tools" });
        }
    }

    /// <summary>
    /// Get all available tools from all connected servers
    /// </summary>
    [HttpGet("tools")]
    public async Task<ActionResult<IEnumerable<MCPToolInfo>>> GetAllAvailableTools()
    {
        try
        {
            var servers = await _mcpService.GetAvailableServersAsync();
            var connectedServers = servers.Where(s => s.IsConnected);
            
            var allTools = new List<MCPToolInfo>();
            
            foreach (var server in connectedServers)
            {
                var tools = await _mcpService.GetAvailableToolsAsync(server.Id);
                foreach (var tool in tools)
                {
                    allTools.Add(new MCPToolInfo
                    {
                        Name = tool,
                        ServerId = server.Id,
                        ServerName = server.Name,
                        Description = $"Tool from {server.Name}"
                    });
                }
            }
            
            return Ok(allTools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all available tools");
            return StatusCode(500, new { error = "Failed to get available tools" });
        }
    }

    /// <summary>
    /// Execute an MCP tool
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<MCPResponse>> ExecuteTool([FromBody] MCPRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request is required" });

            if (string.IsNullOrEmpty(request.Method))
                return BadRequest(new { error = "Method is required" });

            _logger.LogInformation("Executing MCP tool: {Method}", request.Method);

            var response = await _mcpService.SendRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool: {Method}", request?.Method);
            return StatusCode(500, new { error = "Failed to execute MCP tool" });
        }
    }

    private async void OnServerStatusChanged(object? sender, MCPServerStatusChangedEventArgs e)
    {
        try
        {
            // Notify all connected clients about server status changes
            await _hubContext.Clients.All.SendAsync("MCPServerStatusChanged", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting MCP server status change");
        }
    }
}

// Response model for MCP tool information
public class MCPToolInfo
{
    public string Name { get; set; } = "";
    public string ServerId { get; set; } = "";
    public string ServerName { get; set; } = "";
    public string Description { get; set; } = "";
}