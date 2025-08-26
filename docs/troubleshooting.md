# 🔧 A3sist Troubleshooting Guide

## Quick Diagnostics

### Health Check Commands
```powershell
# Run in A3sist chat window:
"/health"        # Check overall system status
"/test-ai"       # Test AI provider connection  
"/test-mcp"      # Test MCP server connections
"/test-config"   # Validate configuration
"/system-info"   # Show system information
```

## 🚨 Common Issues & Solutions

### 1. A3sist Chat Won't Open

**Symptoms:**
- Menu item is grayed out
- Keyboard shortcut doesn't work
- Error message when trying to open

**Solutions:**

**Check Visual Studio Version**
```
Required: Visual Studio 2022 (17.9 or later)
Current: Help → About Microsoft Visual Studio
```

**Verify Extension Installation**
```
1. Extensions → Manage Extensions
2. Search for "A3sist"
3. Ensure status shows "Installed"
4. If not installed: Download and install from marketplace
```

**Reset Extension State**
```
1. Tools → Options → A3sist → Advanced
2. Click "Reset All Settings"
3. Restart Visual Studio
4. Try opening chat again
```

**Run as Administrator** (if permissions issues)
```
1. Close Visual Studio
2. Right-click VS icon → "Run as administrator"
3. Try A3sist features again
```

### 2. AI Responses Are Slow or Failing

**Symptoms:**
- Long wait times for responses
- "Connection timeout" errors
- No response from AI

**Solutions:**

**Check Internet Connection**
```powershell
# Test connectivity
Test-NetConnection api.openai.com -Port 443
Test-NetConnection api.anthropic.com -Port 443
```

**Switch AI Provider**
```
1. Tools → Options → A3sist → AI Models
2. Try different provider (OpenAI ↔ Anthropic)
3. Test with simple question
```

**Reduce Context Size**
```
1. Tools → Options → A3sist → Chat
2. Set Context Attachment to "Manual"
3. Try query without code context
```

**Check API Keys**
```
1. Tools → Options → A3sist → AI Models
2. Verify API key is correct and active
3. Test key independently: https://platform.openai.com/playground
```

### 3. MCP Servers Not Working

**Symptoms:**
- "MCP server unavailable" messages
- Missing specialized AI capabilities  
- Server timeout errors

**Solutions:**

**Check Server Status**
```powershell
# In A3sist chat:
"/mcp status"

# Expected output:
# ✅ Core Development (Port 3001): Running
# ✅ VS Integration (Port 3002): Running  
# ✅ Knowledge (Port 3003): Running
# ❌ Git DevOps (Port 3004): Stopped
```

**Restart MCP Servers**
```powershell
# Navigate to MCP servers directory
cd "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\A3sist\mcp-servers"

# Restart individual server
cd core-development
npm start

# Or restart all servers
.\restart-all-servers.ps1
```

**Check Port Availability**
```powershell
# Check if ports are in use
netstat -an | findstr "3001 3002 3003 3004 3005"

# Kill processes using A3sist ports if needed
Get-Process -Name "node" | Where-Object {$_.CommandLine -like "*mcp-server*"} | Stop-Process
```

**Reinstall MCP Dependencies**
```bash
# In each mcp-server directory:
npm ci  # Clean install dependencies
npm test  # Verify server works
```

### 4. Context Not Loading Properly

**Symptoms:**
- A3sist doesn't understand current file
- Selected code not included in responses
- "No context available" messages

**Solutions:**

**Verify File is Saved**
```
1. Ensure current file is saved (Ctrl+S)
2. Check file appears in Solution Explorer
3. Verify file type is supported (.cs, .js, .py, etc.)
```

**Check Context Settings**
```
1. Tools → Options → A3sist → Chat
2. Ensure "Context Attachment" is set to "Automatic"
3. Verify context types are enabled
```

**Manual Context Attachment**
```
# In chat, use @ mentions:
@currentfile     # Include current file
@selection       # Include selected code
@project         # Include project info
@errors          # Include compilation errors
```

**Reset Context Service**
```
1. Tools → Options → A3sist → Advanced
2. Click "Reset Context Service"
3. Restart Visual Studio
```

### 5. Performance Issues

**Symptoms:**
- Visual Studio becomes slow when A3sist is active
- High memory usage
- UI freezing during AI requests

**Solutions:**

**Adjust Performance Settings**
```json
{
  "performance": {
    "maxConcurrentRequests": 2,  // Reduce from 3
    "enableBackgroundProcessing": false,
    "cacheSize": "50MB",  // Reduce from 100MB
    "asyncProcessing": true
  }
}
```

**Clear Cache**
```
1. Tools → Options → A3sist → Advanced
2. Click "Clear All Cache"
3. Restart Visual Studio
```

**Disable Heavy Features**
```
1. Tools → Options → A3sist → Features
2. Disable "Real-time Analysis"
3. Disable "Predictive Suggestions"  
4. Keep only essential features enabled
```

### 6. Extension Crashes

**Symptoms:**
- Visual Studio crashes when using A3sist
- "Extension error" notifications
- Features stop working suddenly

**Solutions:**

**Check Error Logs**
```
# A3sist logs location:
%LOCALAPPDATA%\A3sist\logs\

# Visual Studio logs:
%APPDATA%\Microsoft\VisualStudio\17.0\ActivityLog.xml
```

**Safe Mode Testing**
```powershell
# Start VS in safe mode
devenv /safemode

# If A3sist works in safe mode, conflict with other extension
```

**Reinstall Extension**
```
1. Extensions → Manage Extensions
2. Find A3sist → Uninstall
3. Restart Visual Studio
4. Install fresh copy from marketplace
```

**Report Crash**
```
1. Gather crash information:
   - Error logs
   - Steps to reproduce
   - Visual Studio version
   - System information
2. Report at: https://github.com/A3sist/A3sist/issues
```

## 📊 Diagnostic Information

### System Requirements Check
```
✅ Operating System: Windows 10/11 (x64)
✅ Visual Studio: 2022 version 17.9+
✅ .NET Runtime: 6.0 or later
✅ Memory: 4GB minimum (8GB recommended)
✅ Storage: 100MB free space
✅ Internet: Stable connection for AI providers
```

### Gather Diagnostic Info
```powershell
# In A3sist chat window:
"/diagnostic-info"

# Creates diagnostic report with:
# - System configuration
# - Extension version and settings
# - Recent error logs
# - Performance metrics
# - Network connectivity status
```

## 🔍 Advanced Troubleshooting

### Registry Issues (Windows)
```powershell
# Check Visual Studio extension registry
Get-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\VisualStudio\17.0\Extensions\A3sist*"

# Reset extension settings
Remove-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\VisualStudio\17.0\Extensions\A3sist*" -Recurse
```

### File Permission Issues
```powershell
# Check A3sist directory permissions
icacls "%LOCALAPPDATA%\A3sist" /T

# Grant full control if needed
icacls "%LOCALAPPDATA%\A3sist" /grant "%USERNAME%":F /T
```

### Network Configuration
```powershell
# Configure proxy settings if behind corporate firewall
netsh winhttp set proxy proxy-server:port
```

## 📞 Getting Help

### Self-Help Resources
1. **A3sist Chat**: Ask questions directly in the chat interface
2. **Documentation**: Check [GitHub Wiki](https://github.com/A3sist/A3sist/wiki)
3. **Community**: Join [Discord](https://discord.gg/a3sist) for peer support

### Reporting Issues
**GitHub Issues**: [https://github.com/A3sist/A3sist/issues](https://github.com/A3sist/A3sist/issues)

**Issue Template:**
```markdown
## Issue Description
[Clear description of the problem]

## Steps to Reproduce
1. Step one
2. Step two
3. Expected vs actual behavior

## Environment
- Visual Studio Version: 
- A3sist Version:
- Operating System:
- .NET Version:

## Diagnostic Information
[Paste output from "/diagnostic-info" command]

## Additional Context
[Any other relevant information]
```

### Priority Support
- **Community Support**: GitHub Issues, Discord (Free)
- **Email Support**: support@a3sist.com (Response within 48 hours)
- **Enterprise Support**: Available for enterprise customers

## 🛠️ Maintenance Tasks

### Regular Maintenance
```powershell
# Weekly maintenance routine:
1. Clear old chat history: Tools → Options → A3sist → Clear History
2. Update extension: Extensions → Updates
3. Clear cache: Tools → Options → A3sist → Clear Cache
4. Check for new features: Help → What's New
```

### Performance Monitoring
```powershell
# Monitor A3sist resource usage:
Get-Process | Where-Object {$_.ProcessName -like "*A3sist*" -or $_.ProcessName -like "*node*"}

# Check disk usage:
Get-ChildItem "$env:LOCALAPPDATA\A3sist" -Recurse | Measure-Object -Property Length -Sum
```

---

**Still need help?** Ask A3sist directly: "I'm having trouble with [specific issue]" or visit our [support community](https://discord.gg/a3sist).