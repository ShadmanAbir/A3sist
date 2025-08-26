# A3sist MCP Server Ecosystem

## Overview

A3sist integrates with **5 specialized MCP servers** that provide comprehensive development assistance through the Model Context Protocol. This ecosystem enhances your Visual Studio extension with powerful AI-driven tools for code analysis, project management, documentation, version control, and quality assurance.

## 🏗️ Server Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   A3sist Core   │────│  MCPOrchestrator │────│   LLM Providers │
│   Extension     │    │                  │    │   (Multi-AI)    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                               │
                ┌──────────────┼──────────────┐
                │              │              │
         ┌──────▼──────┐ ┌─────▼─────┐ ┌─────▼─────┐
         │Core Dev     │ │VS Integ   │ │Knowledge  │
         │:3001        │ │:3002      │ │:3003      │
         └─────────────┘ └───────────┘ └───────────┘
                │              │              │
         ┌──────▼──────┐ ┌─────▼─────┐
         │Git DevOps   │ │Testing QA │
         │:3004        │ │:3005      │
         └─────────────┘ └───────────┘
```

## 🚀 MCP Servers

### 1. **Core Development Server** (Port 3001)
**Purpose**: Code analysis, refactoring, validation, and language conversion

**Tools**:
- `code_analysis` - Comprehensive code analysis for C#, JavaScript, Python
- `code_refactor` - Intelligent refactoring suggestions
- `code_validation` - Syntax and compilation validation
- `language_conversion` - Convert code between languages

**Use Cases**:
- Real-time code quality analysis
- Automated refactoring suggestions
- Cross-language code migration
- Compile-time error detection

### 2. **Visual Studio Integration Server** (Port 3002)
**Purpose**: VS-specific operations, project management, solution analysis

**Tools**:
- `project_analysis` - Analyze VS project structure and dependencies
- `solution_management` - Build, clean, restore operations
- `nuget_operations` - Package management
- `msbuild_operations` - MSBuild integration
- `extension_integration` - A3sist extension status and diagnostics

**Use Cases**:
- Project structure optimization
- Dependency management
- Build automation
- Extension health monitoring

### 3. **Knowledge & Documentation Server** (Port 3003)
**Purpose**: Documentation search, best practices, code examples

**Tools**:
- `documentation_search` - Search technical docs and API references
- `best_practices` - Get coding patterns and best practices
- `code_examples` - Find relevant code snippets
- `knowledge_update` - Update knowledge base

**Use Cases**:
- Context-aware documentation
- Learning and discovery
- Code pattern suggestions
- Knowledge base management

### 4. **Git & DevOps Server** (Port 3004)
**Purpose**: Version control, CI/CD integration, deployment

**Tools**:
- `git_operations` - Git status, log, diff, branch operations
- `ci_cd_integration` - CI/CD pipeline integration
- `deployment_analysis` - Deployment readiness analysis

**Use Cases**:
- Smart commit analysis
- Deployment automation
- CI/CD pipeline optimization
- Release management

### 5. **Testing & Quality Assurance Server** (Port 3005)
**Purpose**: Test generation, quality metrics, performance analysis

**Tools**:
- `test_generation` - Generate unit tests
- `quality_metrics` - Code quality and complexity metrics
- `performance_analysis` - Performance bottleneck analysis

**Use Cases**:
- Automated test generation
- Code quality monitoring
- Performance optimization
- Technical debt tracking

## 🔧 Quick Setup

### Prerequisites
- Node.js 18+ 
- .NET 6.0+ SDK
- Git
- Docker (optional)

### Option 1: Individual Server Setup

1. **Install dependencies for each server**:
```bash
cd mcp-servers/core-development && npm install
cd ../vs-integration && npm install  
cd ../knowledge && npm install
cd ../git-devops && npm install
cd ../testing-quality && npm install
```

2. **Start all servers**:
```bash
# Terminal 1
cd core-development && npm start

# Terminal 2  
cd vs-integration && npm start

# Terminal 3
cd knowledge && npm start

# Terminal 4
cd git-devops && npm start

# Terminal 5
cd testing-quality && npm start
```

### Option 2: Docker Compose (Recommended)

```bash
# From the mcp-servers directory
docker-compose up -d
```

### Option 3: Automated Startup Script

**Windows (PowerShell)**:
```powershell
.\start-all-servers.ps1
```

**Linux/Mac**:
```bash
./start-all-servers.sh
```

## ⚙️ Configuration

Update your A3sist configuration (`appsettings.json`):

```json
{
  "A3sist": {
    "LLM": {
      "Provider": "OpenAI",
      "Model": "gpt-4",
      "MCP": {
        "Enabled": true,
        "EnableOrchestration": true,
        "EnableTools": true,
        "MaxToolExecutions": 10,
        "HealthCheckInterval": "00:05:00",
        "Servers": {
          "CoreDevelopment": {
            "Enabled": true,
            "Endpoint": "http://localhost:3001",
            "Priority": 10
          },
          "VSIntegration": {
            "Enabled": true,
            "Endpoint": "http://localhost:3002",
            "Priority": 9
          },
          "Knowledge": {
            "Enabled": true,
            "Endpoint": "http://localhost:3003",
            "Priority": 8
          },
          "GitDevOps": {
            "Enabled": true,
            "Endpoint": "http://localhost:3004",
            "Priority": 7
          },
          "TestingQuality": {
            "Enabled": true,
            "Endpoint": "http://localhost:3005",
            "Priority": 6
          }
        }
      }
    }
  }
}
```

## 🧪 Testing Your Setup

### 1. Health Check
```bash
curl -X POST http://localhost:3001/mcp \
  -H "Content-Type: application/json" \
  -d '{"method": "tools/list"}'
```

### 2. Test Code Analysis
```bash
curl -X POST http://localhost:3001/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/execute",
    "params": {
      "name": "code_analysis",
      "parameters": {
        "code": "public class Test { public void Method() { } }",
        "language": "csharp",
        "analysisLevel": "full"
      }
    }
  }'
```

### 3. Test VS Integration
```bash
curl -X POST http://localhost:3002/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/execute", 
    "params": {
      "name": "extension_integration",
      "parameters": {
        "operation": "status"
      }
    }
  }'
```

## 📊 Monitoring & Troubleshooting

### Server Status Dashboard
Each server provides status at `/status`:
- http://localhost:3001/status
- http://localhost:3002/status
- etc.

### Logs
Server logs are available in the console output or log files:
- `logs/core-development.log`
- `logs/vs-integration.log`
- etc.

### Common Issues

1. **Port conflicts**: Ensure ports 3001-3005 are available
2. **Missing dependencies**: Run `npm install` in each server directory
3. **Permission issues**: Ensure proper file/directory permissions
4. **VS Integration**: Ensure Visual Studio SDK is available

## 🔄 Development Mode

For development, use nodemon for auto-restart:

```bash
npm run dev
```

## 📈 Performance Optimization

- **Caching**: All servers implement intelligent caching
- **Load Balancing**: Multiple instances can run behind a load balancer
- **Health Checks**: Automatic health monitoring with failover
- **Rate Limiting**: Built-in rate limiting for API protection

## 🚀 Production Deployment

### Docker Production Setup
```yaml
version: '3.8'
services:
  core-dev:
    image: a3sist/core-development:latest
    ports: ["3001:3001"]
    environment:
      - NODE_ENV=production
      - LOG_LEVEL=info
    restart: unless-stopped
    
  # ... other services
```

### Kubernetes Deployment
See `k8s/` directory for Kubernetes manifests.

### Monitoring
- Prometheus metrics available at `/metrics`
- Grafana dashboards in `monitoring/`
- Health checks at `/health`

## 🎯 Integration Examples

### Using with A3sist Extension

```csharp
// Get orchestrator service
var orchestrator = serviceProvider.GetRequiredService<MCPOrchestrator>();

// Process request across multiple servers
var request = new AgentRequest 
{
    Prompt = "Analyze this code and suggest improvements",
    Content = sourceCode,
    FilePath = "MyClass.cs"
};

var result = await orchestrator.ProcessRequestAsync(request);

// Result contains synthesized output from multiple servers
Console.WriteLine(result.Content);
Console.WriteLine($"Used servers: {string.Join(", ", result.ServersUsed)}");
```

### Direct Tool Execution

```csharp
// Execute specific tool on specific server
var mcpClient = serviceProvider.GetRequiredService<MCPLLMClient>();

var analysisResult = await mcpClient.ExecuteToolAsync("code_analysis", new {
    code = sourceCode,
    language = "csharp", 
    analysisLevel = "comprehensive"
});
```

## 📚 API Documentation

Detailed API documentation for each server:
- [Core Development API](./core-development/API.md)
- [VS Integration API](./vs-integration/API.md)
- [Knowledge API](./knowledge/API.md)
- [Git DevOps API](./git-devops/API.md)
- [Testing Quality API](./testing-quality/API.md)

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Add tests for new tools
4. Submit pull request

## 📄 License

MIT License - see LICENSE file for details.

---

**🎉 Your A3sist MCP ecosystem is ready!** 

This comprehensive setup provides your Visual Studio extension with powerful AI assistance across all aspects of software development.