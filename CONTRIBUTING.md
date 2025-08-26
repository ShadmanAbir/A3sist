# Contributing to A3sist

Thank you for your interest in contributing to A3sist! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

### Prerequisites
- Visual Studio 2022 or later
- .NET SDK 6.0 or later
- Git
- Basic knowledge of C# and Visual Studio extensibility

### Getting Started
1. **Fork the repository**
2. **Clone your fork**: `git clone https://github.com/your-username/A3sist.git`
3. **Create a feature branch**: `git checkout -b feature/your-feature-name`
4. **Make your changes**
5. **Test thoroughly**
6. **Submit a pull request**

## 📋 Development Guidelines

### Code Standards
- **Follow SOLID principles**
- **Use async/await patterns** for asynchronous operations
- **Include comprehensive XML documentation** for public APIs
- **Follow C# naming conventions**
- **Keep methods focused and single-purpose**
- **Use dependency injection** for service dependencies

### Code Style
```csharp
// ✅ Good
public async Task<AgentResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default)
{
    if (request == null)
        throw new ArgumentNullException(nameof(request));

    // Implementation here
    return AgentResult.CreateSuccess("Processing completed");
}

// ❌ Avoid
public AgentResult ProcessRequest(AgentRequest request)
{
    // Synchronous processing without proper validation
}
```

### Testing Requirements
- **Unit tests** for all new functionality
- **Integration tests** for complex workflows
- **Minimum 80% code coverage**
- **Use xUnit, FluentAssertions, and Moq**

```csharp
[Fact]
public async Task ProcessRequestAsync_WithValidRequest_ShouldReturnSuccess()
{
    // Arrange
    var mockService = new Mock<IService>();
    var agent = new TestAgent(mockService.Object);
    var request = new AgentRequest { /* valid data */ };

    // Act
    var result = await agent.ProcessRequestAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

## 🏗️ Architecture Guidelines

### Adding New Agents
1. **Inherit from BaseAgent**
2. **Implement required abstract methods**
3. **Register in DI container**
4. **Add configuration section**
5. **Include comprehensive tests**

```csharp
public class MyCustomAgent : BaseAgent
{
    public override string Name => "MyCustomAgent";
    public override AgentType Type => AgentType.Custom;

    public MyCustomAgent(
        ILogger<MyCustomAgent> logger,
        IAgentConfiguration configuration,
        IValidationService? validationService = null,
        IPerformanceMonitoringService? performanceService = null)
        : base(logger, configuration, validationService, performanceService)
    {
    }

    protected override async Task<AgentResult> HandleRequestAsync(
        AgentRequest request, CancellationToken cancellationToken)
    {
        // Implement agent logic
        return AgentResult.CreateSuccess("Task completed");
    }
}
```

### Service Registration
```csharp
// In ServiceCollectionExtensions.cs
services.AddTransient<MyCustomAgent>();
```

### Configuration
```json
{
  "A3sist": {
    "Agents": {
      "MyCustomAgent": {
        "Enabled": true,
        "Timeout": "00:02:00",
        "MaxRetries": 3
      }
    }
  }
}
```

## 📝 Documentation Standards

### XML Documentation
```csharp
/// <summary>
/// Processes an agent request with validation and error handling
/// </summary>
/// <param name="request">The request to process</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result of the processing operation</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null</exception>
public async Task<AgentResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default)
```

### README Updates
- Update relevant documentation when adding features
- Include usage examples
- Document breaking changes

## 🧪 Testing Guidelines

### Test Organization
```
tests/
├── A3sist.Core.Tests/
│   ├── Agents/
│   ├── Services/
│   └── Extensions/
├── A3sist.Integration.Tests/
└── A3sist.TestUtilities/
```

### Test Naming
```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public async Task ProcessRequestAsync_WithValidRequest_ShouldReturnSuccess()

[Fact]
public async Task ProcessRequestAsync_WithNullRequest_ShouldThrowArgumentNullException()
```

### Mock Usage
```csharp
var mockLogger = new Mock<ILogger<TestAgent>>();
var mockConfig = new Mock<IAgentConfiguration>();
var agent = new TestAgent(mockLogger.Object, mockConfig.Object);
```

## 🔍 Code Review Process

### Pull Request Requirements
- [ ] **Clear description** of changes
- [ ] **Tests included** and passing
- [ ] **Documentation updated**
- [ ] **No merge conflicts**
- [ ] **CI/CD checks passing**

### Review Checklist
- [ ] Code follows established patterns
- [ ] Proper error handling
- [ ] Performance considerations
- [ ] Security implications reviewed
- [ ] Backward compatibility maintained

## 🐛 Bug Reports

### Required Information
- **A3sist version**
- **Visual Studio version**
- **Steps to reproduce**
- **Expected vs actual behavior**
- **Error messages/logs**
- **Sample code (if applicable)**

### Template
```markdown
**Environment:**
- A3sist Version: 1.0.0
- Visual Studio: 2022 17.4.0
- .NET SDK: 6.0.12

**Description:**
Brief description of the issue

**Steps to Reproduce:**
1. Step one
2. Step two
3. Step three

**Expected Behavior:**
What should happen

**Actual Behavior:**
What actually happens

**Error Messages:**
```
Any error messages or logs
```

**Additional Context:**
Any other relevant information
```

## 🚀 Feature Requests

### Guidelines
- **Check existing issues** before creating new ones
- **Provide clear use case** and justification
- **Consider implementation complexity**
- **Align with project goals**

### Template
```markdown
**Feature Description:**
Clear description of the proposed feature

**Use Case:**
Why is this feature needed?

**Proposed Implementation:**
High-level approach (optional)

**Alternatives Considered:**
Other approaches you've considered

**Additional Context:**
Any other relevant information
```

## 📋 Release Process

### Version Numbering
- **Major.Minor.Patch** (Semantic Versioning)
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes

### Release Checklist
- [ ] All tests passing
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Version numbers updated
- [ ] VSIX package tested
- [ ] Release notes prepared

## 💬 Communication

### Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and ideas
- **Pull Request Comments**: Code-specific discussions

### Etiquette
- Be respectful and constructive
- Provide clear, detailed information
- Search existing issues before creating new ones
- Follow the project's code of conduct

## 🎯 Development Priorities

### High Priority
- Security improvements
- Performance optimizations
- Bug fixes
- Test coverage improvements

### Medium Priority
- New agent implementations
- UI/UX enhancements
- Documentation improvements

### Future Considerations
- Additional language support
- Advanced AI features
- Integration with other tools

---

Thank you for contributing to A3sist! Your efforts help make development more productive for everyone.