using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using A3sist.Models;

namespace A3sist.Services
{
    public class MCPClientService : IMCPClientService
    {
        private readonly IA3sistConfigurationService _configService;
        private readonly HttpClient _httpClient;
        private readonly List<MCPServerInfo> _servers;
        private readonly Dictionary<string, MCPConnection> _connections;
        private readonly object _lockObject = new object();
        private readonly Timer _healthCheckTimer;

        public event EventHandler<MCPServerStatusChangedEventArgs> ServerStatusChanged;

        public MCPClientService(IA3sistConfigurationService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _servers = new List<MCPServerInfo>();
            _connections = new Dictionary<string, MCPConnection>();

            // Initialize health check timer
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            // Initialize with default servers
            InitializeDefaultServers();
        }

        public async Task<bool> ConnectToServerAsync(MCPServerInfo serverInfo)
        {
            try
            {
                var connection = new MCPConnection
                {
                    ServerId = serverInfo.Id,
                    ServerInfo = serverInfo,
                    IsConnected = false,
                    LastConnected = DateTime.UtcNow
                };

                // Test connection
                var testSuccess = await TestConnectionAsync(serverInfo);
                if (!testSuccess)
                {
                    return false;
                }

                lock (_lockObject)
                {
                    _connections[serverInfo.Id] = connection;
                    var server = _servers.FirstOrDefault(s => s.Id == serverInfo.Id);
                    if (server != null)
                    {
                        server.IsConnected = true;
                    }
                    else
                    {
                        serverInfo.IsConnected = true;
                        _servers.Add(serverInfo);
                    }
                }

                connection.IsConnected = true;

                ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                {
                    ServerId = serverInfo.Id,
                    IsConnected = true,
                    StatusMessage = "Connected successfully"
                });

                await SaveServersConfigurationAsync();
                return true;
            }
            catch (Exception ex)
            {
                ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                {
                    ServerId = serverInfo.Id,
                    IsConnected = false,
                    StatusMessage = $"Connection failed: {ex.Message}"
                });
                return false;
            }
        }

        public async Task<bool> DisconnectFromServerAsync(string serverId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_connections.TryGetValue(serverId, out var connection))
                    {
                        connection.IsConnected = false;
                        _connections.Remove(serverId);
                    }

                    var server = _servers.FirstOrDefault(s => s.Id == serverId);
                    if (server != null)
                    {
                        server.IsConnected = false;
                    }
                }

                ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                {
                    ServerId = serverId,
                    IsConnected = false,
                    StatusMessage = "Disconnected"
                });

                await SaveServersConfigurationAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<MCPResponse> SendRequestAsync(MCPRequest request)
        {
            // Find an available connected server
            MCPConnection connection = null;
            lock (_lockObject)
            {
                connection = _connections.Values.FirstOrDefault(c => c.IsConnected);
            }

            if (connection == null)
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = new MCPError
                    {
                        Code = -1,
                        Message = "No connected MCP servers available"
                    },
                    Id = request.Id
                };
            }

            try
            {
                if (connection.ServerInfo.Type == MCPServerType.Local)
                {
                    return await SendLocalRequestAsync(connection, request);
                }
                else
                {
                    return await SendRemoteRequestAsync(connection, request);
                }
            }
            catch (Exception ex)
            {
                return new MCPResponse
                {
                    Success = false,
                    Error = new MCPError
                    {
                        Code = -2,
                        Message = ex.Message
                    },
                    Id = request.Id
                };
            }
        }

        private async Task<MCPResponse> SendLocalRequestAsync(MCPConnection connection, MCPRequest request)
        {
            var mcpMessage = new
            {
                jsonrpc = "2.0",
                id = request.Id ?? Guid.NewGuid().ToString(),
                method = request.Method,
                @params = request.Parameters
            };

            var content = new StringContent(
                JsonSerializer.Serialize(mcpMessage),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(connection.ServerInfo.Endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("error", out var errorElement))
                {
                    var error = JsonSerializer.Deserialize<MCPError>(errorElement.GetRawText());
                    return new MCPResponse
                    {
                        Success = false,
                        Error = error,
                        Id = result.GetProperty("id").GetString()
                    };
                }

                return new MCPResponse
                {
                    Success = true,
                    Result = result.TryGetProperty("result", out var resultElement) ? 
                        JsonSerializer.Deserialize<object>(resultElement.GetRawText()) : null,
                    Id = result.GetProperty("id").GetString()
                };
            }

            throw new Exception($"HTTP request failed with status: {response.StatusCode}");
        }

        private async Task<MCPResponse> SendRemoteRequestAsync(MCPConnection connection, MCPRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, connection.ServerInfo.Endpoint);
            
            if (!string.IsNullOrEmpty(connection.ServerInfo.ApiKey))
            {
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", connection.ServerInfo.ApiKey);
            }

            var mcpMessage = new
            {
                jsonrpc = "2.0",
                id = request.Id ?? Guid.NewGuid().ToString(),
                method = request.Method,
                @params = request.Parameters
            };

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(mcpMessage),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("error", out var errorElement))
                {
                    var error = JsonSerializer.Deserialize<MCPError>(errorElement.GetRawText());
                    return new MCPResponse
                    {
                        Success = false,
                        Error = error,
                        Id = result.GetProperty("id").GetString()
                    };
                }

                return new MCPResponse
                {
                    Success = true,
                    Result = result.TryGetProperty("result", out var resultElement) ? 
                        JsonSerializer.Deserialize<object>(resultElement.GetRawText()) : null,
                    Id = result.GetProperty("id").GetString()
                };
            }

            throw new Exception($"HTTP request failed with status: {response.StatusCode}");
        }

        public async Task<IEnumerable<MCPServerInfo>> GetAvailableServersAsync()
        {
            lock (_lockObject)
            {
                return _servers.ToList();
            }
        }

        public async Task<IEnumerable<string>> GetAvailableToolsAsync(string serverId)
        {
            lock (_lockObject)
            {
                var server = _servers.FirstOrDefault(s => s.Id == serverId);
                return server?.SupportedTools ?? new List<string>();
            }
        }

        public async Task<bool> TestServerConnectionAsync(string serverId)
        {
            MCPServerInfo server;
            lock (_lockObject)
            {
                server = _servers.FirstOrDefault(s => s.Id == serverId);
            }

            if (server == null)
                return false;

            return await TestConnectionAsync(server);
        }

        private async Task<bool> TestConnectionAsync(MCPServerInfo server)
        {
            try
            {
                var testRequest = new MCPRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Method = "ping",
                    Parameters = new Dictionary<string, object>()
                };

                if (server.Type == MCPServerType.Local)
                {
                    var mcpMessage = new
                    {
                        jsonrpc = "2.0",
                        id = testRequest.Id,
                        method = testRequest.Method,
                        @params = testRequest.Parameters
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(mcpMessage),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync(server.Endpoint, content);
                    return response.IsSuccessStatusCode;
                }
                else
                {
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, server.Endpoint);
                    
                    if (!string.IsNullOrEmpty(server.ApiKey))
                    {
                        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", server.ApiKey);
                    }

                    var mcpMessage = new
                    {
                        jsonrpc = "2.0",
                        id = testRequest.Id,
                        method = testRequest.Method,
                        @params = testRequest.Parameters
                    };

                    httpRequest.Content = new StringContent(
                        JsonSerializer.Serialize(mcpMessage),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.SendAsync(httpRequest);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void InitializeDefaultServers()
        {
            // Add some common local MCP server configurations
            _servers.Add(new MCPServerInfo
            {
                Id = "local-mcp-server",
                Name = "Local MCP Server",
                Endpoint = "http://localhost:8080/mcp",
                Type = MCPServerType.Local,
                IsConnected = false,
                SupportedTools = new List<string> { "code_analysis", "completion", "refactoring" }
            });

            // Auto-discover local servers
            Task.Run(async () =>
            {
                await DiscoverLocalServersAsync();
            });
        }

        private async Task DiscoverLocalServersAsync()
        {
            // Common local MCP server ports
            var commonPorts = new[] { 8080, 8081, 8082, 3000, 3001, 5000, 5001 };
            var discoveredServers = new List<MCPServerInfo>();

            foreach (var port in commonPorts)
            {
                try
                {
                    var endpoint = $"http://localhost:{port}/mcp";
                    var testServer = new MCPServerInfo
                    {
                        Id = $"discovered-local-{port}",
                        Name = $"Discovered Local Server (Port {port})",
                        Endpoint = endpoint,
                        Type = MCPServerType.Local,
                        IsConnected = false,
                        SupportedTools = new List<string>()
                    };

                    if (await TestConnectionAsync(testServer))
                    {
                        // Try to get server capabilities
                        var capabilitiesRequest = new MCPRequest
                        {
                            Method = "initialize",
                            Parameters = new Dictionary<string, object>
                            {
                                ["protocolVersion"] = "2024-11-05",
                                ["capabilities"] = new Dictionary<string, object>
                                {
                                    ["roots"] = new Dictionary<string, object> { ["listChanged"] = true },
                                    ["sampling"] = new object()
                                }
                            }
                        };

                        // Add to discovered servers
                        discoveredServers.Add(testServer);
                    }
                }
                catch
                {
                    // Continue with next port
                }
            }

            // Add discovered servers
            lock (_lockObject)
            {
                foreach (var server in discoveredServers)
                {
                    if (!_servers.Any(s => s.Endpoint == server.Endpoint))
                    {
                        _servers.Add(server);
                    }
                }
            }

            if (discoveredServers.Any())
            {
                await SaveServersConfigurationAsync();
            }
        }

        private async void PerformHealthCheck(object state)
        {
            var serversToCheck = new List<MCPServerInfo>();
            
            lock (_lockObject)
            {
                serversToCheck = _servers.Where(s => s.IsConnected).ToList();
            }

            foreach (var server in serversToCheck)
            {
                var isHealthy = await TestConnectionAsync(server);
                
                if (!isHealthy && server.IsConnected)
                {
                    // Mark as disconnected
                    lock (_lockObject)
                    {
                        server.IsConnected = false;
                        if (_connections.ContainsKey(server.Id))
                        {
                            _connections[server.Id].IsConnected = false;
                        }
                    }

                    ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                    {
                        ServerId = server.Id,
                        IsConnected = false,
                        StatusMessage = "Connection lost during health check"
                    });
                }
                else if (isHealthy && !server.IsConnected)
                {
                    // Mark as reconnected
                    lock (_lockObject)
                    {
                        server.IsConnected = true;
                        if (_connections.ContainsKey(server.Id))
                        {
                            _connections[server.Id].IsConnected = true;
                        }
                    }

                    ServerStatusChanged?.Invoke(this, new MCPServerStatusChangedEventArgs
                    {
                        ServerId = server.Id,
                        IsConnected = true,
                        StatusMessage = "Reconnected during health check"
                    });
                }
            }
        }

        private async Task SaveServersConfigurationAsync()
        {
            List<MCPServerInfo> serversToSave;
            lock (_lockObject)
            {
                serversToSave = _servers.ToList();
            }

            await _configService.SetSettingAsync("mcp.servers", serversToSave);
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            _httpClient?.Dispose();
        }

        private class MCPConnection
        {
            public string ServerId { get; set; }
            public MCPServerInfo ServerInfo { get; set; }
            public bool IsConnected { get; set; }
            public DateTime LastConnected { get; set; }
        }
    }
}