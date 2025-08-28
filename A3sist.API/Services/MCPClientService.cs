using A3sist.API.Models;
using A3sist.API.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A3sist.API.Services;

public class MCPClientService : IMCPClientService, IDisposable
{
    private readonly ILogger<MCPClientService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, MCPServerInfo> _servers;
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat;
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _heartbeatTimer;
    private bool _disposed;

    public event EventHandler<MCPServerStatusChangedEventArgs>? ServerStatusChanged;

    public MCPClientService(ILogger<MCPClientService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _servers = new ConcurrentDictionary<string, MCPServerInfo>();
        _lastHeartbeat = new ConcurrentDictionary<string, DateTime>();
        _semaphore = new SemaphoreSlim(1, 1);
        
        // Initialize heartbeat timer for keep-alive
        _heartbeatTimer = new Timer(CheckServerHeartbeats, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        // Load default servers
        _ = Task.Run(LoadDefaultServersAsync);
    }

    public async Task<bool> ConnectToServerAsync(MCPServerInfo serverInfo)
    {
        if (serverInfo == null || string.IsNullOrEmpty(serverInfo.Id))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Connecting to MCP server: {ServerName}", serverInfo.Name);

            // Test connection
            var connectionResult = await TestServerConnectionInternalAsync(serverInfo);
            if (!connectionResult)
            {
                _logger.LogWarning("Failed to connect to MCP server: {ServerName}", serverInfo.Name);
                return false;
            }

            // Update server status
            serverInfo.IsConnected = true;
            _servers.AddOrUpdate(serverInfo.Id, serverInfo, (key, existing) => serverInfo);
            _lastHeartbeat[serverInfo.Id] = DateTime.UtcNow;

            // Raise status change event
            ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
            {
                ServerId = serverInfo.Id,
                Status = MCPServerStatus.Connected,
                IsConnected = true,
                Message = "Connected successfully",
                StatusMessage = "Connected"
            });

            _logger.LogInformation("Successfully connected to MCP server: {ServerName}", serverInfo.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DisconnectFromServerAsync(string serverId)
    {
        if (string.IsNullOrEmpty(serverId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            if (!_servers.TryGetValue(serverId, out var serverInfo))
                return false;

            _logger.LogInformation("Disconnecting from MCP server: {ServerName}", serverInfo.Name);

            // Send disconnect request if applicable
            try
            {
                await SendDisconnectRequestAsync(serverInfo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during graceful disconnect from {ServerName}", serverInfo.Name);
            }

            // Update server status
            serverInfo.IsConnected = false;
            _lastHeartbeat.TryRemove(serverId, out _);

            // Raise status change event
            ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
            {
                ServerId = serverId,
                Status = MCPServerStatus.Disconnected,
                IsConnected = false,
                Message = "Disconnected",
                StatusMessage = "Disconnected"
            });

            _logger.LogInformation("Disconnected from MCP server: {ServerName}", serverInfo.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<MCPResponse> SendRequestAsync(MCPRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Method))
        {
            return new MCPResponse
            {
                Success = false,
                Error = new MCPError { Code = -1, Message = "Invalid request" }
            };
        }

        try
        {
            // For now, we'll send the request to the first connected server
            // In a real implementation, you might want to specify which server to use
            var connectedServer = _servers.Values.FirstOrDefault(s => s.IsConnected);
            if (connectedServer == null)
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = new MCPError { Code = -2, Message = "No connected MCP servers" }
                };
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(connectedServer.TimeoutSeconds);

            // Add authentication if required
            if (connectedServer.RequiresAuth && !string.IsNullOrEmpty(connectedServer.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {connectedServer.ApiKey}");
            }

            // Add custom headers if specified
            if (!string.IsNullOrEmpty(connectedServer.CustomHeaders))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(connectedServer.CustomHeaders);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing custom headers for server {ServerName}", connectedServer.Name);
                }
            }

            // Build MCP request payload
            var mcpPayload = new
            {
                jsonrpc = "2.0",
                id = request.Id ?? Guid.NewGuid().ToString(),
                method = request.Method,
                @params = request.Parameters ?? new Dictionary<string, object>()
            };

            var jsonContent = JsonSerializer.Serialize(mcpPayload);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending MCP request to {ServerName}: {Method}", connectedServer.Name, request.Method);

            var response = await httpClient.PostAsync(connectedServer.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Update last heartbeat
                _lastHeartbeat[connectedServer.Id] = DateTime.UtcNow;

                // Parse MCP response
                var mcpResponse = await ParseMCPResponseAsync(responseContent, request.Id);
                return mcpResponse;
            }
            else
            {
                _logger.LogError("MCP request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new MCPResponse
                {
                    Success = false,
                    Error = new MCPError
                    {
                        Code = (int)response.StatusCode,
                        Message = $"HTTP {response.StatusCode}: {responseContent}"
                    },
                    Id = request.Id
                };
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("MCP request timed out for method: {Method}", request.Method);
            return new MCPResponse
            {
                Success = false,
                Error = new MCPError { Code = -3, Message = "Request timed out" },
                Id = request.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MCP request: {Method}", request.Method);
            return new MCPResponse
            {
                Success = false,
                Error = new MCPError { Code = -4, Message = ex.Message },
                Id = request.Id
            };
        }
    }

    public async Task<IEnumerable<MCPServerInfo>> GetAvailableServersAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _servers.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId)
    {
        if (string.IsNullOrEmpty(serverId))
            return Enumerable.Empty<string>();

        await _semaphore.WaitAsync();
        try
        {
            if (_servers.TryGetValue(serverId, out var serverInfo))
            {
                return serverInfo.SupportedTools ?? Enumerable.Empty<string>();
            }
            return Enumerable.Empty<string>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> TestServerConnectionAsync(string serverId)
    {
        if (string.IsNullOrEmpty(serverId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            if (!_servers.TryGetValue(serverId, out var serverInfo))
                return false;

            return await TestServerConnectionInternalAsync(serverInfo);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> AddServerAsync(MCPServerInfo serverInfo)
    {
        if (serverInfo == null || string.IsNullOrEmpty(serverInfo.Id))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            // Initialize supported tools if not set
            if (serverInfo.SupportedTools == null)
                serverInfo.SupportedTools = new List<string>();

            _servers.AddOrUpdate(serverInfo.Id, serverInfo, (key, existing) => serverInfo);
            
            _logger.LogInformation("Added MCP server: {ServerName}", serverInfo.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveServerAsync(string serverId)
    {
        if (string.IsNullOrEmpty(serverId))
            return false;

        await _semaphore.WaitAsync();
        try
        {
            // Disconnect first if connected
            if (_servers.TryGetValue(serverId, out var serverInfo) && serverInfo.IsConnected)
            {
                await DisconnectFromServerAsync(serverId);
            }

            var removed = _servers.TryRemove(serverId, out var removedServer);
            _lastHeartbeat.TryRemove(serverId, out _);

            if (removed)
                _logger.LogInformation("Removed MCP server: {ServerName}", removedServer?.Name);

            return removed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<bool> TestServerConnectionInternalAsync(MCPServerInfo serverInfo)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Quick test timeout

            // Add authentication if required
            if (serverInfo.RequiresAuth && !string.IsNullOrEmpty(serverInfo.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {serverInfo.ApiKey}");
            }

            // Send a simple ping/info request
            var pingRequest = new
            {
                jsonrpc = "2.0",
                id = "ping",
                method = "ping",
                @params = new { }
            };

            var jsonContent = JsonSerializer.Serialize(pingRequest);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(serverInfo.Endpoint, content);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("MCP server connection test failed for {ServerName}: {Error}", serverInfo.Name, ex.Message);
            return false;
        }
    }

    private async Task<MCPResponse> ParseMCPResponseAsync(string responseContent, string? requestId)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            var mcpResponse = new MCPResponse
            {
                Id = requestId,
                Success = true
            };

            // Parse response according to JSON-RPC 2.0 specification
            if (root.TryGetProperty("result", out var result))
            {
                // Success response
                mcpResponse.Result = JsonSerializer.Deserialize<object>(result.GetRawText());
            }
            else if (root.TryGetProperty("error", out var error))
            {
                // Error response
                mcpResponse.Success = false;
                mcpResponse.Error = new MCPError();

                if (error.TryGetProperty("code", out var code))
                {
                    mcpResponse.Error.Code = code.GetInt32();
                }

                if (error.TryGetProperty("message", out var message))
                {
                    mcpResponse.Error.Message = message.GetString() ?? "";
                }

                if (error.TryGetProperty("data", out var data))
                {
                    mcpResponse.Error.Data = JsonSerializer.Deserialize<object>(data.GetRawText());
                }
            }

            // Extract response ID if present
            if (root.TryGetProperty("id", out var id))
            {
                mcpResponse.Id = id.GetString();
            }

            return mcpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MCP response");
            return new MCPResponse
            {
                Success = false,
                Error = new MCPError { Code = -5, Message = "Failed to parse response" },
                Id = requestId
            };
        }
    }

    private async Task SendDisconnectRequestAsync(MCPServerInfo serverInfo)
    {
        try
        {
            // Send a graceful disconnect request
            var disconnectRequest = new MCPRequest
            {
                Method = "disconnect",
                Parameters = new Dictionary<string, object>(),
                Id = Guid.NewGuid().ToString()
            };

            await SendRequestAsync(disconnectRequest);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error sending disconnect request to {ServerName}: {Error}", serverInfo.Name, ex.Message);
        }
    }

    private void CheckServerHeartbeats(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var now = DateTime.UtcNow;
                var serversToCheck = _servers.Values.Where(s => s.IsConnected).ToList();

                foreach (var server in serversToCheck)
                {
                    if (_lastHeartbeat.TryGetValue(server.Id, out var lastHeartbeat))
                    {
                        var timeSinceLastHeartbeat = now - lastHeartbeat;
                        
                        // Check if server has been silent for too long
                        if (timeSinceLastHeartbeat.TotalSeconds > server.KeepAliveInterval * 2)
                        {
                            _logger.LogWarning("MCP server {ServerName} appears to be unresponsive", server.Name);
                            
                            // Try to reconnect if auto-reconnect is enabled
                            if (server.AutoReconnect)
                            {
                                _logger.LogInformation("Attempting to reconnect to {ServerName}", server.Name);
                                var reconnected = await TestServerConnectionInternalAsync(server);
                                
                                if (reconnected)
                                {
                                    _lastHeartbeat[server.Id] = now;
                                    _logger.LogInformation("Successfully reconnected to {ServerName}", server.Name);
                                }
                                else
                                {
                                    // Mark as disconnected
                                    server.IsConnected = false;
                                    _lastHeartbeat.TryRemove(server.Id, out _);
                                    
                                    ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                                    {
                                        ServerId = server.Id,
                                        Status = MCPServerStatus.Error,
                                        IsConnected = false,
                                        Message = "Connection lost",
                                        StatusMessage = "Connection lost"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during heartbeat check");
            }
        });
    }

    private async Task LoadDefaultServersAsync()
    {
        try
        {
            // Add some default MCP servers
            var defaultServers = new[]
            {
                new MCPServerInfo
                {
                    Id = "local-mcp-server",
                    Name = "Local MCP Server",
                    Description = "Local Model Context Protocol server",
                    Endpoint = "http://localhost:8080/mcp",
                    Type = MCPServerType.Local,
                    IsConnected = false,
                    SupportedTools = new List<string> { "code_analysis", "file_operations", "git_operations" },
                    Port = 8080,
                    Protocol = "HTTP",
                    RequiresAuth = false,
                    TimeoutSeconds = 30,
                    RetryCount = 3,
                    MaxConcurrentRequests = 10,
                    KeepAliveInterval = 60,
                    EnableLogging = true,
                    AutoReconnect = true
                }
            };

            foreach (var server in defaultServers)
            {
                await AddServerAsync(server);
            }

            _logger.LogInformation("Loaded {Count} default MCP servers", defaultServers.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading default MCP servers");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _heartbeatTimer?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}