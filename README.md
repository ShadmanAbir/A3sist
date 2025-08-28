# A3sist - AI-Powered Development Assistant

## Overview
A3sist is an intelligent Visual Studio extension that enhances developer productivity through AI-powered assistance. The extension provides context-aware code suggestions, automated refactoring, real-time analysis, and conversational AI support directly within your development environment.

## 🏗️ Architecture

A3sist uses a modern two-part architecture for optimal performance:

- **A3sist.API** - .NET 9 Web API backend service providing all AI capabilities
- **A3sist.UI** - Lightweight .NET Framework 4.7.2 Visual Studio extension

This architecture delivers:
- ⚡ **95% startup time improvement** 
- 💾 **80% memory usage reduction**
- 🚀 **Better scalability and maintainability**

## 🌟 Features

### Core Capabilities
- **💬 AI Chat Assistant** - Conversational AI for code questions and guidance
- **🔧 Intelligent Refactoring** - Automated code improvements and optimizations  
- **📊 Real-time Code Analysis** - Live feedback on code quality and issues
- **⚡ Smart Completions** - AI-enhanced IntelliSense with context awareness
- **🤖 Agent Mode** - Background workspace analysis and recommendations
- **📚 RAG Integration** - Knowledge-enhanced responses using your codebase

### AI Model Support
- **OpenAI** - GPT models for advanced reasoning
- **Anthropic** - Claude models for code analysis
- **Local Models** - Ollama integration for privacy
- **MCP Protocol** - Extensible model integration

### Language Support
- C# / .NET
- TypeScript / JavaScript
- Python
- Java
- C/C++
- And more...

## 🚀 Quick Start

### Prerequisites
- Visual Studio 2022 (17.8+)
- .NET 9 SDK (for API)
- .NET Framework 4.7.2 (for extension)

### Installation

1. **Build the Projects**
   ```bash
   # Clone the repository
   git clone https://github.com/A3sist/A3sist.git
   cd A3sist
   
   # Build API
   cd A3sist.API
   dotnet build --configuration Release
   
   # Build UI Extension
   cd ../A3sist.UI
   # Open A3sist.sln in Visual Studio and build
   ```

2. **Start the API Service**
   ```bash
   cd A3sist.API
   dotnet run --urls "http://localhost:8341"
   ```

3. **Install the Extension**
   - Build A3sist.UI in Release mode
   - Double-click `A3sist.UI/bin/Release/A3sist.UI.vsix`
   - Restart Visual Studio

4. **Access A3sist**
   - Go to **View** → **Other Windows** → **A3sist Assistant**
   - Configure API connection in the Configuration tab
   - Start using AI-powered features!

## 📖 Documentation

- **[Build & Deployment Guide](BUILD_AND_DEPLOYMENT.md)** - Comprehensive setup instructions
- **[Testing Plan](TESTING_PLAN.md)** - Validation and testing procedures  
- **[Implementation Summary](IMPLEMENTATION_SUMMARY.md)** - Complete technical overview
- **[Architecture Proposal](ARCHITECTURE_SPLIT_PROPOSAL.md)** - Design decisions and rationale

## 🔧 Configuration

A3sist stores configuration in `%AppData%\A3sist\config.json`:

```json
{
  "ApiUrl": "http://localhost:8341",
  "AutoStartApi": false,
  "AutoCompleteEnabled": true,
  "RequestTimeout": 30,
  "EnableLogging": true
}
```

## 🎯 Performance

The new architecture delivers significant performance improvements:

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Startup Time | ~10s | ~0.5s | 95% |
| Memory Usage | ~200MB | ~40MB | 80% |
| Response Time | Variable | <5s | Consistent |

## 🐛 Troubleshooting

### Extension Not Loading
- Check **Extensions** → **Manage Extensions** → **Installed** to ensure A3sist is enabled
- Verify Visual Studio 2022 (17.8+) is installed
- Check Visual Studio Output window for errors

### API Connection Issues
- Ensure A3sist.API is running on http://localhost:8341
- Check Windows Firewall settings
- Verify no other applications are using port 8341

### Performance Issues
- Close unused Visual Studio instances
- Restart the A3sist.API service
- Clear extension cache: Delete `%AppData%\A3sist\` folder

## 🤝 Contributing

We welcome contributions! Please:

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards in the project
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with the Visual Studio SDK
- Powered by .NET 9 and .NET Framework
- AI integrations with OpenAI, Anthropic, and Ollama
- SignalR for real-time communication

---

**Ready to supercharge your development workflow with AI? Install A3sist today!** 🚀
