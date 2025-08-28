# A3sist Installation Guide

## Overview

A3sist provides multiple installation methods to suit different user preferences and technical requirements. All installers set up both the A3sist API service and Visual Studio extension automatically.

## 🚀 Quick Start (Recommended)

### One-Command Setup
```batch
setup.bat
```
This master setup script will:
1. Build both projects automatically
2. Let you choose installation method
3. Complete the entire setup process

**Requirements:**
- Administrator privileges
- .NET 9 SDK
- Visual Studio 2022

## 📦 Installation Options

### 1. Master Setup Script (Easiest)
**File:** `setup.bat`
- ✅ Builds projects automatically
- ✅ Interactive installation method selection
- ✅ Complete end-to-end setup
- ✅ Beginner-friendly

```batch
# Run as Administrator
setup.bat
```

### 2. Standard Batch Installer
**File:** `install.bat`
- ✅ Simple batch script
- ✅ Works on all Windows versions
- ✅ Clear step-by-step output
- ✅ Automatic service creation

```batch
# First build the projects
build.bat

# Then install (run as Administrator)
install.bat
```

### 3. Advanced PowerShell Installer
**File:** `install.ps1`
- ✅ Advanced error handling
- ✅ Colored output and progress indicators
- ✅ Flexible configuration options
- ✅ Better prerequisite checking
- ✅ NSSM support for robust service management

```powershell
# Run PowerShell as Administrator
PowerShell -ExecutionPolicy Bypass -File install.ps1

# With custom options
PowerShell -ExecutionPolicy Bypass -File install.ps1 -InstallPath "D:\A3sist" -ApiPort 9000
```

### 4. GUI Installer (Advanced Users)
**File:** `A3sistInstaller.cs`
- ✅ User-friendly graphical interface
- ✅ Visual progress tracking
- ✅ Installation log display
- ✅ Selective component installation

First compile the GUI installer:
```batch
csc /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll A3sistInstaller.cs
A3sistInstaller.exe
```

### 5. Professional MSI Package
**File:** `A3sist.wxs`
- ✅ Enterprise-grade deployment
- ✅ Windows Installer technology
- ✅ Group Policy deployment support
- ✅ Automatic updates support

Requires WiX Toolset:
```batch
candle A3sist.wxs
light A3sist.wixobj
```

## 🛠️ What Gets Installed

### A3sist API Service
- **Location:** `C:\Program Files\A3sist\API\`
- **Service Name:** A3sistAPI
- **Port:** 8341 (configurable)
- **Auto-start:** Yes
- **Recovery:** Automatic restart on failure

### Visual Studio Extension
- **File:** `A3sist.UI.vsix`
- **Target:** Visual Studio 2022
- **Location:** `View → Other Windows → A3sist Assistant`

### Configuration
- **Location:** `%AppData%\A3sist\config.json`
- **Auto-created:** Yes
- **Editable:** Yes

### Desktop Shortcuts
- A3sist API Manager
- A3sist Configuration
- A3sist Documentation

### Firewall Rules
- Port 8341 inbound (for API access)
- Service communication allowed

## 🔧 Prerequisites

### Required
- **Windows 10/11** (x64)
- **.NET 9 Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (17.8+) - Community, Professional, or Enterprise
- **Administrator Privileges** (for service installation)

### Optional
- **NSSM** (for robust service management) - [Download](https://nssm.cc/)
- **WiX Toolset** (for MSI creation) - [Download](https://wixtoolset.org/)

## 📋 Installation Steps

### Option A: Complete Automated Setup
1. **Download/Clone** the A3sist repository
2. **Open Command Prompt as Administrator**
3. **Navigate** to the A3sist directory
4. **Run:** `setup.bat`
5. **Follow** the interactive prompts
6. **Done!** Open Visual Studio and find A3sist in View → Other Windows

### Option B: Manual Build + Install
1. **Build projects:**
   ```batch
   build.bat
   ```

2. **Install (choose one):**
   ```batch
   # Standard installation
   install.bat
   
   # OR Advanced installation
   PowerShell -ExecutionPolicy Bypass -File install.ps1
   ```

3. **Verify installation:**
   ```batch
   verify.bat
   ```

## ✅ Verification

After installation, run the verification script:
```batch
verify.bat
```

This checks:
- ✅ Service installation and status
- ✅ API endpoint responsiveness
- ✅ File installation
- ✅ Configuration creation
- ✅ Extension installation
- ✅ Network configuration

## 🎯 Usage

1. **Open Visual Studio 2022**
2. **Go to:** View → Other Windows → A3sist Assistant
3. **Configure** API connection if needed (usually auto-configured)
4. **Start coding** with AI assistance!

## 🔧 Configuration

### API Configuration
Edit `%AppData%\A3sist\config.json`:
```json
{
  "ApiUrl": "http://localhost:8341",
  "AutoStartApi": true,
  "AutoCompleteEnabled": true,
  "RequestTimeout": 30,
  "EnableLogging": true
}
```

### Service Management
```batch
# Start service
sc start A3sistAPI

# Stop service
sc stop A3sistAPI

# Check status
sc query A3sistAPI
```

## 🚨 Troubleshooting

### Service Won't Start
1. Check .NET 9 runtime installation
2. Verify port 8341 is not in use
3. Check Windows Event Logs
4. Try running API manually: `dotnet A3sist.API.dll`

### Extension Not Visible
1. Verify Visual Studio 2022 is version 17.8+
2. Check Extensions → Manage Extensions → Installed
3. Restart Visual Studio
4. Try manual VSIX installation

### API Not Responding
1. Check Windows Firewall settings
2. Verify service is running
3. Test endpoint: `curl http://localhost:8341/api/health`
4. Check logs in `%AppData%\A3sist\logs\`

### Port Conflicts
Change the API port:
```batch
# Stop service
sc stop A3sistAPI

# Edit config.json to use different port
# Update service configuration
# Restart service
```

## 🗑️ Uninstallation

To completely remove A3sist:
```batch
uninstall.bat
```

This removes:
- Windows service
- Installation files
- Configuration files
- Desktop shortcuts
- Firewall rules

**Note:** Visual Studio extension must be removed manually through VS Extensions Manager.

## 📁 File Structure

```
A3sist/
├── setup.bat              # Master setup script (recommended)
├── build.bat               # Build both projects
├── install.bat             # Standard installer
├── install.ps1             # Advanced PowerShell installer
├── uninstall.bat           # Complete uninstaller
├── verify.bat              # Installation verification
├── A3sistInstaller.cs      # GUI installer source
├── A3sist.wxs              # WiX MSI configuration
├── A3sist.API/             # .NET 9 API backend
├── A3sist.UI/              # .NET Framework VS extension
└── [documentation files]
```

## 🆘 Support

For issues and support:
1. **Check** the verification script output
2. **Review** the troubleshooting section above
3. **Check** logs in `%AppData%\A3sist\logs\`
4. **Consult** README.md and other documentation
5. **File an issue** on the project repository

## 🔄 Updates

To update A3sist:
1. **Download** the new version
2. **Run** `uninstall.bat` to remove old version
3. **Run** `setup.bat` to install new version

Or for in-place updates:
1. **Stop** the A3sist service
2. **Replace** files in `C:\Program Files\A3sist\API\`
3. **Start** the service
4. **Reinstall** the VSIX if the extension was updated

---

**Happy coding with AI assistance! 🤖✨**