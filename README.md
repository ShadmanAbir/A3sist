# A3sist - AI Code Assistant for Visual Studio

A3sist is an AI-powered development assistant for Visual Studio that provides intelligent chat, code analysis, refactoring suggestions, and context-aware assistance with support for local and remote AI models.

## 🚀 Quick Start

### Building and Installing

1. **Build the Extension**:
   - Run `build_and_install.bat` (recommended) 
   - OR manually build using Visual Studio 2022 with the solution file `A3sist.sln`

2. **Install the Extension**:
   - Close all Visual Studio instances
   - Double-click the generated `A3sist.vsix` file (usually in `bin\Debug\`)
   - Follow the installation prompts

3. **Access the Sidebar Panel**:
   - Open Visual Studio
   - Go to **View** → **A3sist AI Assistant** 
   - OR go to **Tools** → **A3sist** → **Show A3sist Panel**

## 📍 Finding the A3sist Sidebar Panel

### Method 1: View Menu (Recommended)
1. Open Visual Studio
2. Click **View** in the top menu
3. Look for **"A3sist AI Assistant"**
4. Click to open the sidebar panel

### Method 2: Tools Menu  
1. Open Visual Studio
2. Click **Tools** in the top menu
3. Look for **"A3sist"** submenu
4. Click **"Show A3sist Panel"**

### Troubleshooting Panel Visibility
- Ensure the extension is enabled in **Extensions** → **Manage Extensions**
- Try restarting Visual Studio completely
- Check **View** → **Output** for any error messages
- Reset window layout: **Window** → **Reset Window Layout**

## 🎯 Features

The A3sist sidebar panel provides:

### Quick Actions
- **💬 Open Chat Assistant**: Launch AI-powered code chat
- **🔧 Refactor Selected Code**: AI-assisted code refactoring  
- **⚙️ Configure A3sist**: Extension settings and model configuration

### Agent Mode
- **Autonomous Analysis**: Let AI analyze your codebase automatically
- **Progress Tracking**: Visual feedback for ongoing AI operations
- **Start/Stop Control**: Manual control over agent activities

### Model Management
- **Active Model Display**: See which AI model is currently selected
- **Model Switching**: Change between configured AI models
- **Status Indicators**: Visual feedback for model availability

### Feature Toggles
- **AI AutoComplete**: Toggle intelligent code completion
- **Real-time Analysis**: Enable/disable live code analysis
- **Knowledge Base (RAG)**: Control retrieval-augmented generation features

## 🔧 Configuration

### AI Models
1. Click **"Configure A3sist"** in the sidebar
2. Go to the **Models** tab
3. Add your AI models (OpenAI, Anthropic, local models via Ollama, etc.)
4. Configure API keys and endpoints

### MCP Servers  
- Set up Model Context Protocol servers for enhanced capabilities
- Configure tools and resources for specific development workflows

### RAG Engine
- Enable knowledge base functionality
- Configure document indexing and retrieval settings

## 💡 Usage Tips

1. **First Time Setup**: Configure at least one AI model before using features
2. **Docking**: The sidebar can be docked anywhere in Visual Studio
3. **Multiple Instances**: The panel is designed as a singleton - only one instance
4. **Solution Context**: Some features work better with an open solution/project

## 🔍 Technical Details

- **Target Framework**: .NET Framework 4.7.2
- **Visual Studio**: 2022 (Community, Professional, Enterprise)
- **Architecture**: Modular service-based design with dependency injection
- **UI Framework**: WPF/XAML for Visual Studio integration

## 📁 Project Structure

```
A3sist/
├── Commands/           # Visual Studio command handlers
├── Services/          # Core AI and analysis services  
├── UI/               # User interface components
├── Models/           # Data models and configuration
├── Agent/            # Autonomous analysis capabilities
└── Resources/        # Icons and other assets
```

## 🐛 Troubleshooting

### Extension Not Loading
- Check Visual Studio Output window (General/Extensions channels)
- Verify extension is enabled in Extensions Manager
- Try safe mode: `devenv /safemode`

### Sidebar Not Appearing
- Use **View** → **A3sist AI Assistant** 
- Check for the panel in other dock locations
- Reset Visual Studio window layout

### Build Issues
- Ensure Visual Studio 2022 with VS SDK is installed
- Use the provided `build_and_install.bat` script
- Check for NuGet package restore issues

## 🆘 Support

If you're having trouble finding or using the sidebar panel:

1. Check the [HOW_TO_FIND_SIDEBAR.md](HOW_TO_FIND_SIDEBAR.md) guide
2. Look for error messages in **View** → **Output** 
3. Try the troubleshooting steps above
4. Ensure you have an active solution open

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
