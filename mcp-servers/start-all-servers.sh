#!/bin/bash

# A3sist MCP Servers Startup Script (Linux/Mac)

echo "🚀 Starting A3sist MCP Server Ecosystem..."

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js 18+ first."
    exit 1
fi

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ npm is not installed. Please install npm first."
    exit 1
fi

# Function to start a server
start_server() {
    local server_name=$1
    local port=$2
    
    echo "📦 Starting $server_name server on port $port..."
    
    if [ ! -d "$server_name" ]; then
        echo "❌ Directory $server_name not found!"
        return 1
    fi
    
    cd "$server_name"
    
    # Install dependencies if needed
    if [ ! -d "node_modules" ]; then
        echo "📥 Installing dependencies for $server_name..."
        npm install
    fi
    
    # Start server in background
    export PORT=$port
    nohup npm start > "../logs/${server_name}.log" 2>&1 &
    echo $! > "../logs/${server_name}.pid"
    
    cd ..
    
    # Wait a moment and check if server started
    sleep 2
    if ps -p $(cat "logs/${server_name}.pid") > /dev/null; then
        echo "✅ $server_name server started successfully (PID: $(cat logs/${server_name}.pid))"
    else
        echo "❌ Failed to start $server_name server"
        return 1
    fi
}

# Function to check if port is available
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null; then
        echo "⚠️  Port $port is already in use"
        return 1
    fi
    return 0
}

# Create logs directory
mkdir -p logs

# Check all required ports
echo "🔍 Checking port availability..."
ports=(3001 3002 3003 3004 3005)
for port in "${ports[@]}"; do
    if ! check_port $port; then
        echo "❌ Port $port is in use. Please free up the port or modify the configuration."
        exit 1
    fi
done

echo "✅ All ports are available"

# Start all servers
start_server "core-development" 3001
start_server "vs-integration" 3002
start_server "knowledge" 3003
start_server "git-devops" 3004
start_server "testing-quality" 3005

echo ""
echo "🎉 All A3sist MCP servers started successfully!"
echo ""
echo "📊 Server Status:"
echo "  • Core Development:    http://localhost:3001"
echo "  • VS Integration:      http://localhost:3002"
echo "  • Knowledge:           http://localhost:3003"
echo "  • Git DevOps:          http://localhost:3004"
echo "  • Testing Quality:     http://localhost:3005"
echo ""
echo "📝 Logs available in:"
for server in core-development vs-integration knowledge git-devops testing-quality; do
    echo "  • $server: logs/${server}.log"
done
echo ""
echo "🛑 To stop all servers, run: ./stop-all-servers.sh"
echo ""

# Test server health
echo "🔍 Testing server health..."
sleep 5

for port in "${ports[@]}"; do
    if curl -s -f "http://localhost:$port/health" > /dev/null 2>&1; then
        echo "✅ Server on port $port is healthy"
    else
        echo "⚠️  Server on port $port health check failed (may still be starting)"
    fi
done

echo ""
echo "🎯 A3sist MCP Ecosystem is ready for use!"
echo "   Configure your A3sist extension to use MCP servers."