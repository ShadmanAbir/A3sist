# A3sist - AI-Powered Code Assistant for Visual Studio

A3sist is a comprehensive Visual Studio extension that provides intelligent code assistance through AI-powered features including chat, autocomplete, refactoring, and code suggestions. The extension offers flexible integration with both local and remote Language Learning Models (LLMs) through Model Control Protocol (MCP) and hybrid Retrieval-Augmented Generation (RAG) capabilities.

## Features

### Core Capabilities
- **Hybrid AI Integration**: Support for both local LLMs (Ollama, LM Studio) and remote APIs (OpenAI, Anthropic, etc.)
- **Multi-Model Support**: Use multiple local models simultaneously with smart routing and model switching
- **Intelligent Model Selection**: One model at a time with optional multi-model toggle for advanced users
- **Advanced Refactoring Engine**: AI-powered code refactoring with preview and rollback capabilities
- **Code Cleanup Tools**: Automated code formatting, optimization, and best practice enforcement
- **Flexible RAG Options**: Local knowledge bases and/or remote vector databases
- **Intelligent Code Completion**: Context-aware autocomplete functionality
- **Chat Interface**: Interactive AI chat for development assistance with model switching
- **IntelliSense Integration**: Bulb-like quick fix and suggestion interface
- **Multi-Language Support**: Primary focus on C#/.NET with extensible language detection
- **Privacy Control**: Users choose between local privacy or remote capabilities

### UI Components
- **Chat Window**: Interactive chat interface with model selection and history
- **Configuration Hub**: Comprehensive settings for models, MCP servers, and RAG configuration
- **Model Management**: Easy setup and testing of local and remote AI models
- **MCP Configuration**: Setup and management of Model Control Protocol servers
- **RAG Configuration**: Local and remote knowledge base configuration

## Installation

### Prerequisites
- Visual Studio 2022 (Community, Professional, or Enterprise)
- .NET Framework 4.7.2 or later
- For local models: Ollama, LM Studio, or compatible local AI server

### Building from Source
1. Clone the repository
2. Open `A3sist.csproj` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution
5. The VSIX package will be generated in the output directory

### Installation
1. Download the latest A3sist.vsix from releases
2. Close all Visual Studio instances
3. Double-click the .vsix file to install
4. Restart Visual Studio
5. Find A3sist menu in Tools > A3sist

## Configuration

### Setting Up Local Models

#### Ollama
1. Install Ollama from https://ollama.ai/
2. Download a model: `ollama pull codellama`
3. Start Ollama service
4. In A3sist Configuration > Models, add:
   - Name: CodeLlama
   - Provider: Ollama
   - Endpoint: http://localhost:11434
   - Model Name: codellama

#### LM Studio
1. Install LM Studio
2. Download and load a model
3. Start the local server
4. In A3sist Configuration > Models, add:
   - Name: Local Model
   - Provider: LM Studio
   - Endpoint: http://localhost:1234
   - Model Name: (your loaded model)

### Setting Up Remote Models

#### OpenAI
1. Get API key from OpenAI
2. In A3sist Configuration > Models, add:
   - Name: GPT-4
   - Provider: OpenAI
   - Endpoint: https://api.openai.com
   - API Key: your-api-key
   - Model Name: gpt-4

### MCP Server Configuration
Model Control Protocol servers provide extended capabilities:

1. Local MCP Server: Configure local tools and integrations
2. Remote MCP Server: Connect to cloud-based MCP services
3. Auto-discovery: Automatically find local MCP servers

### RAG Configuration
Configure knowledge bases for contextual AI responses:

#### Local RAG
- Vector Store: Choose between Simple Text, SQLite Vector, or Chroma
- Index Path: Specify workspace to index
- Auto-indexing: Enable automatic indexing of workspace changes

#### Remote RAG
- Provider: Pinecone, Qdrant, Weaviate, or custom
- API Configuration: Endpoint and API key
- Index synchronization with local knowledge base

## Usage

### Chat Assistant
1. Open chat: Tools > A3sist > Open Chat Assistant
2. Select active model from dropdown
3. Type questions or paste code for assistance
4. Use Ctrl+Enter for multi-line input

### Code Refactoring
1. Select code in editor
2. Right-click > A3sist > Refactor with AI
3. Review suggestions and apply changes

### AutoComplete
- Enable/disable: Tools > A3sist > Toggle AutoComplete
- Automatic suggestions while typing
- AI-powered context-aware completions

### Configuration
- Access: Tools > A3sist > Configure A3sist
- Configure models, MCP servers, and RAG settings
- Test connections and manage settings

## Architecture

### Core Components
- **A3sistPackage**: Main extension package and entry point
- **Service Layer**: Modular services for different capabilities
- **Model Management**: Handle local and remote AI models
- **MCP Client**: Model Control Protocol integration
- **RAG Engine**: Retrieval-Augmented Generation system
- **Code Analysis**: Language detection and code analysis
- **UI Components**: WPF-based user interfaces

### Service Architecture
```
A3sistPackage
├── ModelManagementService
├── MCPClientService
├── RAGEngineService
├── CodeAnalysisService
├── ChatService
├── AutoCompleteService
├── RefactoringService
└── ConfigurationService
```

## Development

### Project Structure
```
A3sist/
├── A3sist.csproj              # Main project file
├── A3sistPackage.cs           # Extension package
├── Services/                  # Core services
│   ├── Interfaces.cs          # Service interfaces
│   ├── ModelManagementService.cs
│   ├── MCPClientService.cs
│   ├── RAGEngineService.cs
│   ├── CodeAnalysisService.cs
│   ├── ChatService.cs
│   └── A3sistConfigurationService.cs
├── Commands/                  # VS command handlers
│   └── Commands.cs
├── UI/                        # WPF user interfaces
│   ├── ChatWindow.xaml
│   ├── ChatWindow.xaml.cs
│   ├── ConfigurationWindow.xaml
│   └── ConfigurationWindow.xaml.cs
├── A3sistPackage.vsct        # VS command table
└── source.extension.vsixmanifest
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Troubleshooting

### Common Issues

#### Models Not Connecting
- Check if local AI server is running
- Verify endpoint URLs and API keys
- Test connection in configuration dialog

#### AutoComplete Not Working
- Ensure AutoComplete is enabled
- Check if active model is available
- Verify workspace is indexed for RAG

#### RAG Not Finding Results
- Index workspace in RAG configuration
- Check similarity threshold settings
- Verify indexed document count

### Log Files
A3sist logs are stored in:
- Windows: `%APPDATA%\A3sist\logs\`
- Configuration: `%APPDATA%\A3sist\config.json`

## Roadmap

### Planned Features
- [ ] Agent Mode for autonomous code analysis
- [ ] IntelliSense bulb integration
- [ ] Additional language support
- [ ] Plugin system for extensibility
- [ ] Advanced code metrics and analysis
- [ ] Integration with Git for commit messages
- [ ] Code review assistance
- [ ] Documentation generation

### Version History
- v1.0.0: Initial release with core features
  - Chat interface with model selection
  - Local and remote model support
  - Basic MCP integration
  - Local RAG implementation
  - Configuration management

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, bug reports, and feature requests:
- GitHub Issues: https://github.com/a3sist/a3sist/issues
- Documentation: https://github.com/a3sist/a3sist/wiki

## Acknowledgments

- Visual Studio SDK team for the excellent extension framework
- Ollama team for local AI model hosting
- OpenAI for API access and model development
- Microsoft CodeAnalysis (Roslyn) team for code analysis capabilities