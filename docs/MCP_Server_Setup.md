# Quick MCP Server Setup for A3sist

## Option 1: Simple Node.js MCP Server (Recommended for Testing)

### 1. Create MCP Server

Create a new directory and setup:

```bash
mkdir a3sist-mcp-server
cd a3sist-mcp-server
npm init -y
npm install express cors
```

### 2. Create server.js

```javascript
const express = require('express');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json());

// Available tools
const tools = [
    {
        name: "code_analysis",
        description: "Analyze code for issues and improvements",
        parameters: {
            type: "object",
            properties: {
                code: { type: "string" },
                language: { type: "string" },
                analysisLevel: { type: "string" }
            }
        }
    },
    {
        name: "documentation_search", 
        description: "Search documentation and knowledge bases",
        parameters: {
            type: "object",
            properties: {
                query: { type: "string" },
                scope: { type: "string" }
            }
        }
    },
    {
        name: "git_operations",
        description: "Perform Git operations",
        parameters: {
            type: "object", 
            properties: {
                operation: { type: "string" },
                path: { type: "string" }
            }
        }
    },
    {
        name: "file_operations",
        description: "Read and write files safely",
        parameters: {
            type: "object",
            properties: {
                operation: { type: "string" },
                path: { type: "string" },
                content: { type: "string" }
            }
        }
    }
];

// MCP endpoints
app.post('/mcp', async (req, res) => {
    const { method, params } = req.body;
    
    try {
        switch (method) {
            case 'llm/chat':
                return handleLLMChat(req, res);
            case 'llm/completion':
                return handleLLMCompletion(req, res);
            case 'tools/list':
                return res.json({ success: true, tools });
            case 'tools/execute':
                return handleToolExecution(req, res);
            default:
                return res.status(400).json({ 
                    success: false, 
                    error: `Unknown method: ${method}` 
                });
        }
    } catch (error) {
        res.status(500).json({ 
            success: false, 
            error: error.message 
        });
    }
});

async function handleLLMChat(req, res) {
    const { params } = req.body;
    const { messages, model, provider, tools: requestedTools } = params;
    
    // Simple mock response - replace with actual LLM integration
    const response = {
        success: true,
        content: `Mock response for: ${messages[0]?.content?.substring(0, 100)}...`,
        actualProvider: provider || "Mock",
        actualModel: model || "mock-model-1",
        tokensUsed: 150,
        finishReason: "completed"
    };
    
    res.json(response);
}

async function handleLLMCompletion(req, res) {
    const { params } = req.body;
    
    const response = {
        success: true,
        completion: `Mock completion for prompt: ${params.prompt?.substring(0, 50)}...`,
        actualProvider: params.provider || "Mock",
        actualModel: params.model || "mock-model-1",
        tokensUsed: 75
    };
    
    res.json(response);
}

async function handleToolExecution(req, res) {
    const { params } = req.body;
    const { name, parameters } = params;
    
    let result = "";
    
    switch (name) {
        case 'code_analysis':
            result = analyzeCode(parameters);
            break;
        case 'documentation_search':
            result = searchDocumentation(parameters);
            break;
        case 'git_operations':
            result = performGitOperation(parameters);
            break;
        case 'file_operations':
            result = performFileOperation(parameters);
            break;
        default:
            return res.status(400).json({
                success: false,
                error: `Unknown tool: ${name}`
            });
    }
    
    res.json({
        success: true,
        result,
        metadata: {
            toolName: name,
            executionTime: Date.now(),
            parameters
        }
    });
}

function analyzeCode(params) {
    const { code, language = "unknown", analysisLevel = "basic" } = params;
    
    // Mock code analysis
    const issues = [];
    
    if (code.includes("var ")) {
        issues.push("Consider using specific types instead of 'var' for better readability");
    }
    
    if (code.includes("public class") && !code.includes("namespace")) {
        issues.push("Class should be in a namespace");
    }
    
    if (code.length > 1000) {
        issues.push("Large code block detected - consider refactoring into smaller methods");
    }
    
    return JSON.stringify({
        language,
        analysisLevel,
        issuesFound: issues.length,
        issues,
        suggestions: [
            "Add XML documentation comments",
            "Consider adding error handling",
            "Review method complexity"
        ],
        codeQuality: issues.length === 0 ? "Good" : issues.length < 3 ? "Fair" : "Needs Improvement"
    });
}

function searchDocumentation(params) {
    const { query, scope = "general" } = params;
    
    // Mock documentation search
    const results = [
        {
            title: `Documentation for: ${query}`,
            content: `Here's what you need to know about ${query}...`,
            url: `https://docs.example.com/${query.toLowerCase().replace(/\s+/g, '-')}`,
            relevance: 0.9
        },
        {
            title: `Best Practices: ${query}`,
            content: `Best practices when working with ${query}...`,
            url: `https://bestpractices.example.com/${query}`,
            relevance: 0.8
        }
    ];
    
    return JSON.stringify({
        query,
        scope,
        resultsFound: results.length,
        results
    });
}

function performGitOperation(params) {
    const { operation, path } = params;
    
    // Mock Git operations
    switch (operation) {
        case 'status':
            return JSON.stringify({
                operation,
                path,
                status: "clean",
                branch: "main",
                uncommittedChanges: 0
            });
        case 'log':
            return JSON.stringify({
                operation,
                commits: [
                    { hash: "abc123", message: "Latest commit", author: "Developer", date: new Date() },
                    { hash: "def456", message: "Previous commit", author: "Developer", date: new Date(Date.now() - 86400000) }
                ]
            });
        default:
            return JSON.stringify({ error: `Unknown git operation: ${operation}` });
    }
}

function performFileOperation(params) {
    const { operation, path, content } = params;
    
    // Mock file operations
    switch (operation) {
        case 'read':
            return JSON.stringify({
                operation,
                path,
                content: `Mock content for file: ${path}`,
                size: 1024,
                lastModified: new Date()
            });
        case 'analyze':
            return JSON.stringify({
                operation,
                path,
                fileType: path.split('.').pop(),
                lineCount: 42,
                characterCount: 1024
            });
        default:
            return JSON.stringify({ error: `Unknown file operation: ${operation}` });
    }
}

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`🚀 A3sist MCP Server running on http://localhost:${PORT}`);
    console.log(`📚 Available tools: ${tools.map(t => t.name).join(', ')}`);
});
```

### 3. Start the Server

```bash
node server.js
```

## Option 2: Using Docker (Quick Setup)

### 1. Create Dockerfile

```dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY server.js ./

EXPOSE 3000

CMD ["node", "server.js"]
```

### 2. Build and Run

```bash
docker build -t a3sist-mcp-server .
docker run -p 3000:3000 a3sist-mcp-server
```

## Option 3: Using Existing MCP Servers

### Continue.dev MCP Server
```bash
npx @continue/mcp-server --port 3000
```

### Custom MCP Integration
```bash
# Install existing MCP server
npm install -g some-mcp-server
some-mcp-server --port 3000 --tools code-analysis,git-ops
```

## Testing Your MCP Setup

### 1. Test with curl
```bash
# Test tools list
curl -X POST http://localhost:3000/mcp \
  -H "Content-Type: application/json" \
  -d '{"method": "tools/list", "params": {}}'

# Test tool execution
curl -X POST http://localhost:3000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/execute",
    "params": {
      "name": "code_analysis",
      "parameters": {
        "code": "public class Test { }",
        "language": "csharp"
      }
    }
  }'
```

### 2. Configure A3sist

Update your A3sist configuration:

```json
{
  "A3sist": {
    "LLM": {
      "MCP": {
        "Enabled": true,
        "ServerEndpoint": "http://localhost:3000",
        "EnableTools": true
      }
    }
  }
}
```

### 3. Test in Visual Studio

Your MCP server is now ready! The A3sist extension will automatically detect and use your MCP tools when processing requests.

## Production Considerations

For production use, consider:

1. **Security**: Add authentication, rate limiting
2. **Performance**: Implement proper caching, connection pooling
3. **Monitoring**: Add logging, metrics collection
4. **Scalability**: Use proper LLM providers, load balancing
5. **Error Handling**: Robust error handling and fallbacks

## Real LLM Integration

Replace the mock responses with actual LLM providers:

```javascript
// Example OpenAI integration
const OpenAI = require('openai');
const openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

async function handleLLMChat(req, res) {
    const { messages, model = "gpt-4" } = req.body.params;
    
    const completion = await openai.chat.completions.create({
        model,
        messages,
        max_tokens: 4000
    });
    
    res.json({
        success: true,
        content: completion.choices[0].message.content,
        actualProvider: "OpenAI",
        actualModel: model,
        tokensUsed: completion.usage.total_tokens
    });
}
```

Your MCP server is now ready to power your A3sist Visual Studio extension! 🎉