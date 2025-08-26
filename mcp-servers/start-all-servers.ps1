# A3sist MCP Servers Startup Script (Windows PowerShell)

Write-Host "🚀 Starting A3sist MCP Server Ecosystem..." -ForegroundColor Green

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js is not installed. Please install Node.js 18+ first." -ForegroundColor Red
    exit 1
}

# Check if npm is installed
try {
    $npmVersion = npm --version
    Write-Host "✅ npm found: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ npm is not installed. Please install npm first." -ForegroundColor Red
    exit 1
}

# Function to start a server
function Start-Server {
    param(
        [string]$ServerName,
        [int]$Port
    )
    
    Write-Host "📦 Starting $ServerName server on port $Port..." -ForegroundColor Cyan
    
    if (-not (Test-Path $ServerName)) {
        Write-Host "❌ Directory $ServerName not found!" -ForegroundColor Red
        return $false
    }
    
    Push-Location $ServerName
    
    # Install dependencies if needed
    if (-not (Test-Path "node_modules")) {
        Write-Host "📥 Installing dependencies for $ServerName..." -ForegroundColor Yellow
        npm install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to install dependencies for $ServerName" -ForegroundColor Red
            Pop-Location
            return $false
        }
    }
    
    # Set environment variables
    $env:PORT = $Port
    $env:NODE_ENV = "production"
    
    # Start server
    $logFile = "..\logs\$ServerName.log"
    $process = Start-Process -FilePath "npm" -ArgumentList "start" -RedirectStandardOutput $logFile -RedirectStandardError $logFile -PassThru -WindowStyle Hidden
    
    # Save process ID
    $process.Id | Out-File "..\logs\$ServerName.pid"
    
    Pop-Location
    
    # Wait and check if server started
    Start-Sleep -Seconds 3
    if ($process.HasExited) {
        Write-Host "❌ Failed to start $ServerName server" -ForegroundColor Red
        return $false
    } else {
        Write-Host "✅ $ServerName server started successfully (PID: $($process.Id))" -ForegroundColor Green
        return $true
    }
}

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    
    try {
        $listener = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
        $portInUse = $listener | Where-Object { $_.Port -eq $Port }
        return -not $portInUse
    } catch {
        return $true
    }
}

# Create logs directory
if (-not (Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" | Out-Null
}

# Check all required ports
Write-Host "🔍 Checking port availability..." -ForegroundColor Cyan
$ports = @(3001, 3002, 3003, 3004, 3005)
$portsAvailable = $true

foreach ($port in $ports) {
    if (-not (Test-Port $port)) {
        Write-Host "⚠️  Port $port is already in use" -ForegroundColor Yellow
        $portsAvailable = $false
    }
}

if (-not $portsAvailable) {
    Write-Host "❌ Some ports are in use. Please free up the ports or modify the configuration." -ForegroundColor Red
    exit 1
}

Write-Host "✅ All ports are available" -ForegroundColor Green

# Start all servers
$servers = @(
    @{ Name = "core-development"; Port = 3001 },
    @{ Name = "vs-integration"; Port = 3002 },
    @{ Name = "knowledge"; Port = 3003 },
    @{ Name = "git-devops"; Port = 3004 },
    @{ Name = "testing-quality"; Port = 3005 }
)

$allStarted = $true
foreach ($server in $servers) {
    if (-not (Start-Server -ServerName $server.Name -Port $server.Port)) {
        $allStarted = $false
    }
}

if (-not $allStarted) {
    Write-Host "❌ Some servers failed to start. Check the logs for details." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 All A3sist MCP servers started successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Server Status:" -ForegroundColor Cyan
Write-Host "  • Core Development:    http://localhost:3001" -ForegroundColor White
Write-Host "  • VS Integration:      http://localhost:3002" -ForegroundColor White
Write-Host "  • Knowledge:           http://localhost:3003" -ForegroundColor White
Write-Host "  • Git DevOps:          http://localhost:3004" -ForegroundColor White
Write-Host "  • Testing Quality:     http://localhost:3005" -ForegroundColor White
Write-Host ""
Write-Host "📝 Logs available in:" -ForegroundColor Cyan
foreach ($server in $servers) {
    Write-Host "  • $($server.Name): logs\$($server.Name).log" -ForegroundColor White
}
Write-Host ""
Write-Host "🛑 To stop all servers, run: .\stop-all-servers.ps1" -ForegroundColor Yellow
Write-Host ""

# Test server health
Write-Host "🔍 Testing server health..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

foreach ($port in $ports) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$port/health" -Method Get -TimeoutSec 5
        Write-Host "✅ Server on port $port is healthy" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Server on port $port health check failed (may still be starting)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎯 A3sist MCP Ecosystem is ready for use!" -ForegroundColor Green
Write-Host "   Configure your A3sist extension to use MCP servers." -ForegroundColor White