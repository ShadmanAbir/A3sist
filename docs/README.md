# CodeAssist

A modular, AI-powered multi-agent system for code assistance, refactoring, validation, and intelligent design planning.

## Overview

CodeAssist is a Visual Studio extension that enhances developer productivity by providing specialized agents for different aspects of software development. The system is designed to be modular, allowing for easy addition of new agents and features.

## Features

- **Multi-language support**: C#, JavaScript, and Python
- **AI-driven decision-making**: Agents use AI to make decisions and recover from failures
- **Modular architecture**: Agents are designed to be independent and scalable
- **Context routing**: Efficiently routes code context to the appropriate agents
- **Visual Studio integration**: Seamless integration with the Visual Studio IDE

## Architecture

The system is built around a core agent framework that provides:

1. **BaseAgent**: The foundation for all agents with common functionality
2. **AgentCommunication**: System for agent communication and message passing
3. **AgentLifecycleManager**: Management of agent lifecycle and status
4. **AgentConfiguration**: Configuration system for agents
5. **AgentTestFramework**: Basic testing framework for agents

## Getting Started

### Prerequisites

- Visual Studio 2022 or later
- .NET SDK 6.0 or later
- Node.js (for JavaScript support)

### Installation

1. Clone the repository:
   ```shell
   git clone https://github.com/yourusername/CodeAssist.git
   ```

2. Open the solution in Visual Studio:
   ```shell
   cd CodeAssist
   start CodeAssist.sln
   ```

3. Build the solution in Visual Studio

4. Run the extension in the experimental instance

## Usage

1. Install the extension in Visual Studio
2. Open a code file in the editor
3. Use the CodeAssist commands from the Visual Studio menu or context menu

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

# A3sist