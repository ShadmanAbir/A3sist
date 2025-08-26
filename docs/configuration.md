# ⚙️ A3sist Configuration Guide

## Overview
A3sist offers extensive customization options to tailor the AI assistant to your development workflow, preferences, and team requirements.

## 📊 Settings Access
**Main Path**: `Tools → Options → A3sist`
**Alternative**: A3sist Chat → Settings icon → Preferences

## 🤖 AI Provider Configuration

### Primary AI Models
**Path**: `A3sist → AI Models → Primary Provider`

| Provider | Models | Best For |
|----------|--------|----------|
| **OpenAI** | GPT-4, GPT-3.5-turbo | General development, code generation |
| **Anthropic** | Claude-3, Claude-2 | Code analysis, documentation |
| **Azure OpenAI** | Enterprise GPT models | Corporate environments, compliance |

### Configuration Example
```json
{
  "primaryProvider": "OpenAI",
  "model": "gpt-4",
  "apiKey": "[Your API Key]",
  "endpoint": "https://api.openai.com/v1",
  "maxTokens": 4096,
  "temperature": 0.3
}
```

### Fallback Configuration
**Purpose**: Ensure reliability when primary provider is unavailable

```json
{
  "fallbackEnabled": true,
  "fallbackProvider": "Anthropic",
  "fallbackModel": "claude-3-sonnet",
  "maxRetries": 3,
  "retryDelay": "2s"
}
```

## 💬 Chat Behavior Settings

### Response Styles
**Path**: `A3sist → Chat → Response Style`

| Style | Description | Use Case |
|-------|-------------|----------|
| **Concise** | Brief, to-the-point answers | Quick questions, experienced developers |
| **Detailed** | Comprehensive explanations | Learning, complex topics |
| **Conversational** | Natural, friendly dialogue | Exploration, brainstorming |

### Context Management
**Path**: `A3sist → Chat → Context Attachment`

**Options:**
- **Automatic** ✅ Recommended - Intelligently includes relevant context
- **Manual Approval** - Prompts before including context
- **Disabled** - No automatic context attachment

**Context Types:**
```
✅ Current file and selection
✅ Compilation errors and warnings  
✅ Project structure and dependencies
✅ Git status and recent changes
✅ Open documents and cursor position
```

### Conversation Management
```json
{
  "maxHistoryLength": 50,
  "autoSaveHistory": true,
  "historyRetentionDays": 30,
  "clearHistoryOnStartup": false
}
```

## 🎨 Appearance Customization

### Theme Integration
**Path**: `A3sist → Appearance → Theme`

**Options:**
- **Follow Visual Studio Theme** ✅ Recommended
- **Light Theme Override** - Always use light theme
- **Dark Theme Override** - Always use dark theme
- **Custom Theme** - Define custom colors

### Font Settings
```json
{
  "chatFont": {
    "family": "Segoe UI",
    "size": 12,
    "weight": "Normal"
  },
  "codeFont": {
    "family": "Consolas", 
    "size": 11,
    "weight": "Normal"
  }
}
```

### Layout Options
**Window Docking:**
- Tabbed with Solution Explorer
- Floating window
- Bottom panel
- Custom position

## 🔧 MCP Server Configuration

### Server Management
**Path**: `A3sist → MCP Servers`

**Available Servers:**
```json
{
  "coreDevServer": {
    "enabled": true,
    "port": 3001,
    "timeout": 30000,
    "retries": 3
  },
  "vsIntegrationServer": {
    "enabled": true,
    "port": 3002,
    "timeout": 15000,
    "retries": 2
  },
  "knowledgeServer": {
    "enabled": true,
    "port": 3003,
    "timeout": 45000,
    "retries": 3
  }
}
```

### Server Health Monitoring
- **Health Check Interval**: 60 seconds
- **Auto Restart**: On failure detection
- **Failover Mode**: Graceful degradation to available servers
- **Logging Level**: Info, Warning, Error, Debug

## 🛡️ Privacy & Security Settings

### Data Handling
**Path**: `A3sist → Privacy → Data Management`

**Options:**
```json
{
  "dataRetention": {
    "chatHistory": "30 days",
    "contextData": "session only", 
    "errorLogs": "7 days"
  },
  "dataTransmission": {
    "requireApproval": true,
    "includePersonalData": false,
    "anonymizeCode": true
  }
}
```

### Security Features
- **Code Anonymization** - Remove sensitive identifiers before sending to AI
- **Approval Prompts** - Confirm before transmitting code
- **Local Processing** - Process sensitive data locally when possible
- **Encryption** - Encrypt stored chat history and settings

## ⌨️ Keyboard Shortcuts

### Default Shortcuts
**Path**: `A3sist → Keyboard → Shortcuts`

| Action | Default | Customizable |
|--------|---------|-------------|
| Open Chat | `Ctrl+Shift+F8` | ✅ |
| Analyze Code | `Ctrl+Shift+F11` | ✅ |
| Refactor Code | `Ctrl+Shift+F10` | ✅ |
| Fix Code | `Ctrl+Shift+F9` | ✅ |
| Main Menu | `Ctrl+Shift+F12` | ✅ |

### Custom Shortcuts
```json
{
  "customShortcuts": {
    "generateTests": "Ctrl+Alt+T",
    "addDocumentation": "Ctrl+Alt+D",
    "explainCode": "Ctrl+Alt+E"
  }
}
```

## 🏢 Team & Enterprise Configuration

### Shared Settings
**Export Configuration:**
```powershell
# Export team settings
Export-A3sistConfig -Path "team-config.json" -IncludePersonal:$false

# Import team settings  
Import-A3sistConfig -Path "team-config.json" -MergeMode "TeamOnly"
```

**Shared Configuration Files:**
```
team-settings/
├── ai-models.json          # AI provider configurations
├── chat-preferences.json   # Response styles and behavior
├── shortcuts.json          # Team keyboard shortcuts
└── mcp-servers.json       # MCP server configurations
```

### Enterprise Features
- **Centralized Configuration Management**
- **Usage Analytics and Reporting**
- **Compliance and Audit Logging**
- **Custom AI Model Integration**
- **SSO and Identity Provider Integration**

## 🔄 Advanced Configuration

### Performance Tuning
```json
{
  "performance": {
    "maxConcurrentRequests": 3,
    "requestTimeout": 30000,
    "cacheEnabled": true,
    "cacheSize": "100MB",
    "backgroundProcessing": true
  }
}
```

### Debugging Settings
```json
{
  "debugging": {
    "enableLogging": true,
    "logLevel": "Info",
    "logPath": "%LOCALAPPDATA%/A3sist/logs",
    "enableTelemetry": false,
    "verboseMode": false
  }
}
```

### Experimental Features
**Path**: `A3sist → Advanced → Experimental`

**Available Features:**
- **Enhanced Context Analysis** - Deeper project understanding
- **Predictive Suggestions** - Proactive code recommendations
- **Multi-Model Consensus** - Use multiple AI models for complex queries
- **Code Generation Workflows** - Automated development patterns

## 📁 Configuration File Locations

### Windows Paths
```
User Settings: %APPDATA%/A3sist/user-settings.json
Team Settings: %PROGRAMDATA%/A3sist/team-settings.json
Chat History: %LOCALAPPDATA%/A3sist/conversations/
Logs: %LOCALAPPDATA%/A3sist/logs/
Cache: %TEMP%/A3sist/cache/
```

### Configuration Backup
```powershell
# Backup all settings
Backup-A3sistConfig -Path "backup-$(Get-Date -Format 'yyyyMMdd').zip"

# Restore from backup
Restore-A3sistConfig -Path "backup-20240101.zip" -Confirm:$false
```

## 🔧 Troubleshooting Configuration

### Reset to Defaults
```powershell
# Reset all settings
Reset-A3sistConfig -All

# Reset specific category
Reset-A3sistConfig -Category "ChatSettings"
```

### Validate Configuration
```powershell
# Test AI provider connection
Test-A3sistConnection -Provider "OpenAI"

# Validate MCP servers
Test-MCPServers -All

# Check configuration integrity
Test-A3sistConfig -Repair
```

### Common Issues
| Issue | Solution |
|-------|----------|
| AI not responding | Check API keys and internet connection |
| Settings not saving | Run VS as administrator, check file permissions |
| Shortcuts not working | Reset keyboard bindings, check for conflicts |
| MCP servers failing | Restart servers, check port availability |

## 📚 Configuration Examples

### Development Team Setup
```json
{
  "teamName": "Development Team",
  "aiProvider": "OpenAI",
  "responseStyle": "Detailed", 
  "contextAutoAttach": true,
  "shortcuts": {
    "analyzeCode": "Ctrl+Shift+A",
    "generateTests": "Ctrl+Shift+T"
  }
}
```

### Enterprise Setup
```json
{
  "organization": "Enterprise Corp",
  "aiProvider": "Azure OpenAI",
  "complianceMode": true,
  "dataRetention": 7,
  "auditLogging": true,
  "ssoEnabled": true
}
```

---

**Need help with configuration?** Use A3sist chat to ask: "How do I configure [specific setting]?"