# A3sist API Documentation

## Overview
The A3sist API provides endpoints for context routing and real-time communication between agents in the A3sist system.

## Base URL
```
https://localhost:5001/api
```

## Authentication
All API endpoints require authentication. Use the following header:
```
Authorization: Bearer <your_token>
```

## Endpoints

### Context Routing

#### Route Context
```
POST /context/route
```

**Request Body:**
```json
{
  "contextType": "string",
  "serializedContext": "string"
}
```

**Response:**
```json
{
  "status": "Success",
  "contextType": "string",
  "processingTime": 0
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
  "serializedContext": "string"
}
```

**Response:**
```json
{
  "status": "Success",
  "contextType": "string",
  "processingTime": 0
}
```

#### Get Registered Context Types
```
GET /context/context-types
```

**Response:**
```json
[
  "string"
]
```

#### Get Agents for Context
```
GET /context/agents/{contextType}
```

**Response:**
```json
[
  "string"
]
```

### WebSocket API

#### Connect to WebSocket
```
wss://localhost:5001/ws
```

**Message Format:**
```json
{
  "type": "string",
  "payload": "string"
}
```

**Example Message:**
```json
{
  "type": "route_context",
  "payload": {
    "contextType": "string",
    "serializedContext": "string"
  }
}
```

## Error Handling

All endpoints return appropriate HTTP status codes:

- 200 OK: Success
- 400 Bad Request: Invalid input
- 401 Unauthorized: Authentication failed
- 404 Not Found: Resource not found
- 500 Internal Server Error: Server error

**Error Response Format:**
```json
{
  "error": "string",
  "details": "string"
}
```

## Rate Limiting

The API implements rate limiting. If you exceed the limit, you'll receive a 429 Too Many Requests response.

## Versioning

The API uses semantic versioning. The current version is v1.0.

## Examples

### Route Context Example
```bash
curl -X POST "https://localhost:5001/api/context/route" \
-H "Authorization: Bearer your_token" \
-H "Content-Type: application/json" \
-d '{
  "contextType": "code_analysis",
  "serializedContext": "{\"code\": \"public class Test {}\"}"
}'
```

### WebSocket Example
```javascript
const socket = new WebSocket('wss://localhost:5001/ws');

socket.onopen = function(e) {
  console.log("Connection established");
  socket.send(JSON.stringify({
    type: "route_context",
    payload: {
      contextType: "code_analysis",
      serializedContext: "{\"code\": \"public class Test {}\"}"
    }
  }));
};

socket.onmessage = function(event) {
  console.log("Message from server: ", event.data);
};
```

## Changelog

### v1.0 (Current)
- Initial release
- Context routing endpoints
- WebSocket API
- Basic authentication
- Rate limiting
