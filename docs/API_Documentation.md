# A3sist API Documentation

## Overview

The A3sist API provides comprehensive endpoints for agent coordination, context routing, and real-time communication between agents in the A3sist Visual Studio extension system.

## Base URL
```
https://localhost:5001/api
```

## Authentication
All API endpoints require authentication. Use the following header:
```
Authorization: Bearer <your_token>
```

## Core System APIs

### Agent Management

#### Get All Agents
```
GET /agents
```

**Response:**
```json
{
  "agents": [
    {
      "name": "string",
      "type": "string",
      "status": "string",
      "health": "string",
      "lastActivity": "datetime"
    }
  ]
}
```

#### Get Agent Status
```
GET /agents/{agentName}/status
```

**Response:**
```json
{
  "name": "string",
  "type": "string",
  "status": "Running|Stopped|Error",
  "health": "Healthy|Unhealthy|Critical",
  "taskMetrics": {
    "processed": 0,
    "succeeded": 0,
    "failed": 0,
    "averageProcessingTime": "timespan"
  }
}
```

### Context Routing

#### Route Context
```
POST /context/route
```

**Request Body:**
```json
{
  "contextType": "string",
  "serializedContext": "string",
  "priority": "Low|Normal|High|Critical"
}
```

**Response:**
```json
{
  "status": "Success|Failed",
  "contextType": "string",
  "assignedAgent": "string",
  "processingTime": "timespan",
  "result": {
    "success": true,
    "message": "string",
    "content": "string"
  }
}
```

#### Route Context with Retry
```
POST /context/route-with-retry?maxRetries=3
```

**Request Body:**
```json
{
  "contextType": "string",
  "serializedContext": "string",
  "retryPolicy": {
    "maxRetries": 3,
    "baseDelay": "00:00:01",
    "maxDelay": "00:00:30"
  }
}
```

#### Get Registered Context Types
```
GET /context/context-types
```

**Response:**
```json
[
  "code_analysis",
  "refactoring",
  "completion",
  "validation"
]
```

#### Get Agents for Context
```
GET /context/agents/{contextType}
```

**Response:**
```json
[
  "CSharpAgent",
  "JavaScriptAgent",
  "ValidationAgent"
]
```

### Task Management

#### Submit Task
```
POST /tasks
```

**Request Body:**
```json
{
  "id": "guid",
  "prompt": "string",
  "filePath": "string",
  "content": "string",
  "context": {},
  "preferredAgentType": "string",
  "priority": "Low|Normal|High|Critical",
  "timeout": "timespan"
}
```

#### Get Task Status
```
GET /tasks/{taskId}
```

**Response:**
```json
{
  "id": "guid",
  "status": "Pending|InProgress|Completed|Failed|Cancelled",
  "result": {
    "success": true,
    "message": "string",
    "content": "string",
    "agentName": "string",
    "processingTime": "timespan"
  }
}
```

#### Cancel Task
```
DELETE /tasks/{taskId}
```

### Performance Monitoring

#### Get System Metrics
```
GET /monitoring/metrics
```

**Response:**
```json
{
  "timestamp": "datetime",
  "totalMemoryUsage": 0,
  "cpuUsagePercent": 0.0,
  "activeAgents": 0,
  "totalRequestsProcessed": 0,
  "successfulRequests": 0,
  "failedRequests": 0,
  "averageResponseTime": "timespan",
  "cacheHitRatio": 0.0
}
```

#### Get Agent Performance Report
```
GET /monitoring/agents/{agentName}/performance
```

**Response:**
```json
{
  "agentName": "string",
  "totalExecutions": 0,
  "successfulExecutions": 0,
  "failedExecutions": 0,
  "averageExecutionTime": "timespan",
  "minExecutionTime": "timespan",
  "maxExecutionTime": "timespan",
  "successRate": 0.0,
  "lastExecution": "datetime"
}
```

### Configuration Management

#### Get Configuration
```
GET /config
```

**Response:**
```json
{
  "agents": {
    "defaultTimeout": "timespan",
    "maxConcurrentAgents": 0,
    "healthCheckInterval": "timespan"
  },
  "llm": {
    "provider": "string",
    "model": "string",
    "maxTokens": 0,
    "enableCaching": true
  },
  "performance": {
    "enableMonitoring": true,
    "maxMemoryUsageMB": 0
  }
}
```

#### Update Configuration
```
PUT /config
```

**Request Body:**
```json
{
  "section": "string",
  "configuration": {}
}
```

## WebSocket API

### Connect to WebSocket
```
wss://localhost:5001/ws
```

**Message Format:**
```json
{
  "type": "string",
  "payload": {},
  "correlationId": "string"
}
```

### Message Types

#### Agent Status Updates
```json
{
  "type": "agent_status_update",
  "payload": {
    "agentName": "string",
    "previousStatus": "string",
    "newStatus": "string",
    "timestamp": "datetime"
  }
}
```

#### Task Progress Updates
```json
{
  "type": "task_progress",
  "payload": {
    "taskId": "guid",
    "status": "string",
    "progress": 0.0,
    "message": "string"
  }
}
```

#### System Alerts
```json
{
  "type": "system_alert",
  "payload": {
    "severity": "Low|Medium|High|Critical",
    "message": "string",
    "component": "string",
    "timestamp": "datetime"
  }
}
```

## Error Handling

### HTTP Status Codes
- **200 OK**: Success
- **201 Created**: Resource created successfully
- **400 Bad Request**: Invalid input or malformed request
- **401 Unauthorized**: Authentication failed
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **409 Conflict**: Resource conflict (e.g., duplicate agent name)
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Server error
- **503 Service Unavailable**: Service temporarily unavailable

### Error Response Format
```json
{
  "error": {
    "code": "string",
    "message": "string",
    "details": "string",
    "timestamp": "datetime",
    "correlationId": "string"
  }
}
```

### Common Error Codes
- `INVALID_INPUT`: Request validation failed
- `AGENT_NOT_FOUND`: Specified agent does not exist
- `AGENT_UNAVAILABLE`: Agent is not available for processing
- `CONTEXT_INVALID`: Context data is invalid or malformed
- `TASK_NOT_FOUND`: Task ID not found
- `CONFIGURATION_ERROR`: Configuration issue detected
- `RATE_LIMIT_EXCEEDED`: Too many requests
- `INTERNAL_ERROR`: Unexpected server error

## Rate Limiting

### Limits
- **General API**: 1000 requests per minute per client
- **WebSocket**: 100 messages per minute per connection
- **Task Submission**: 50 tasks per minute per client

### Headers
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640995200
```

## Versioning

The API uses semantic versioning with URL versioning:
- Current version: `v1`
- Base URL: `https://localhost:5001/api/v1`

## Examples

### Submit Code Analysis Task
```bash
curl -X POST "https://localhost:5001/api/v1/tasks" \
  -H "Authorization: Bearer your_token" \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Analyze this C# code for potential issues",
    "filePath": "TestClass.cs",
    "content": "public class TestClass { public void Method() { } }",
    "preferredAgentType": "CSharp",
    "priority": "Normal"
  }'
```

### WebSocket Connection Example
```javascript
const socket = new WebSocket('wss://localhost:5001/ws');

socket.onopen = function(e) {
  console.log("Connected to A3sist WebSocket API");
  
  // Subscribe to agent status updates
  socket.send(JSON.stringify({
    type: "subscribe",
    payload: {
      events: ["agent_status_update", "task_progress"]
    },
    correlationId: generateId()
  }));
};

socket.onmessage = function(event) {
  const message = JSON.parse(event.data);
  console.log("Message received:", message);
  
  switch(message.type) {
    case "agent_status_update":
      handleAgentStatusUpdate(message.payload);
      break;
    case "task_progress":
      handleTaskProgress(message.payload);
      break;
  }
};
```

### Performance Monitoring Example
```javascript
// Get system metrics
fetch('https://localhost:5001/api/v1/monitoring/metrics', {
  headers: {
    'Authorization': 'Bearer your_token'
  }
})
.then(response => response.json())
.then(metrics => {
  console.log('System Memory Usage:', metrics.totalMemoryUsage / 1024 / 1024, 'MB');
  console.log('Success Rate:', (metrics.successfulRequests / metrics.totalRequestsProcessed * 100).toFixed(2), '%');
  console.log('Cache Hit Ratio:', (metrics.cacheHitRatio * 100).toFixed(2), '%');
});
```

## SDK Support

### C# SDK
```csharp
var client = new A3sistApiClient("https://localhost:5001", "your_token");

var task = new AgentRequest 
{
    Prompt = "Refactor this method",
    FilePath = "MyClass.cs",
    Content = codeContent,
    PreferredAgentType = AgentType.CSharp
};

var result = await client.SubmitTaskAsync(task);
```

### JavaScript SDK
```javascript
import { A3sistClient } from '@a3sist/client';

const client = new A3sistClient({
  baseUrl: 'https://localhost:5001',
  apiKey: 'your_token'
});

const result = await client.submitTask({
  prompt: 'Generate unit tests for this function',
  filePath: 'utils.js',
  content: functionCode,
  preferredAgentType: 'JavaScript'
});
```

## Changelog

### v1.0 (Current)
- Initial release
- Agent management endpoints
- Context routing with retry logic
- Task submission and tracking
- Performance monitoring
- WebSocket API for real-time updates
- Configuration management
- Rate limiting
- Comprehensive error handling