using System;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents.CSharp.Services;
using FluentAssertions;
using Xunit;

namespace A3sist.Core.Tests.Agents.Language
{
    /// <summary>
    /// Unit tests for the C# Analyzer service
    /// </summary>
    public class CSharpAnalyzerTests : IDisposable
    {
        private readonly Analyzer _analyzer;

        public CSharpAnalyzerTests()
        {
            _analyzer = new Analyzer();
        }

        [Fact]
        public async Task InitializeAsync_ShouldCompleteSuccessfully()
        {
            // Act
            await _analyzer.InitializeAsync();

            // Assert
            // Should complete without throwing
            _analyzer.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithValidCode_ShouldReturnAnalysis()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello World"");
        }
    }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithSyntaxError_ShouldReportError()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
public class TestClass
{
    public void TestMethod(
    {
        Console.WriteLine(""Hello World"");
    }
}"; // Missing closing parenthesis

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Syntax");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithComplexCode_ShouldProvideDetailedAnalysis()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        private List<string> _items = new List<string>();
        
        public void ComplexMethod()
        {
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    _items.Add(i.ToString());
                }
                else
                {
                    _items.Remove(i.ToString());
                }
            }
        }
        
        public string GetFirstItem()
        {
            return _items.FirstOrDefault();
        }
    }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Code Structure");
            result.Should().Contain("Code Metrics");
            result.Should().Contain("Classes: 1");
            result.Should().Contain("Methods:");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithHighComplexityMethod_ShouldReportComplexity()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
public class TestClass
{
    public void HighComplexityMethod(int value)
    {
        if (value > 0)
        {
            if (value < 10)
            {
                for (int i = 0; i < value; i++)
                {
                    if (i % 2 == 0)
                    {
                        switch (i)
                        {
                            case 0:
                                break;
                            case 2:
                                break;
                            case 4:
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        try
                        {
                            // Some operation
                        }
                        catch (Exception ex)
                        {
                            // Handle exception
                        }
                    }
                }
            }
        }
    }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("complexity");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithMultipleNamespaces_ShouldReportNamespaces()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
using System;
using System.Collections.Generic;

namespace FirstNamespace
{
    public class FirstClass { }
}

namespace SecondNamespace
{
    public class SecondClass { }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Namespaces:");
            result.Should().Contain("FirstNamespace");
            result.Should().Contain("SecondNamespace");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithNullOrEmptyCode_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _analyzer.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.AnalyzeCodeAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.AnalyzeCodeAsync(""));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.AnalyzeCodeAsync("   "));
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithValidCodeNoIssues_ShouldReturnSuccessMessage()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
using System;

public class SimpleClass
{
    public void SimpleMethod()
    {
        Console.WriteLine(""Hello"");
    }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            // Should contain structure and metrics information even if no issues
            result.Should().Contain("Code Structure");
        }

        [Fact]
        public async Task ShutdownAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            await _analyzer.InitializeAsync();

            // Act
            await _analyzer.ShutdownAsync();

            // Assert
            // Should complete without throwing
            _analyzer.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            _analyzer.Invoking(a => a.Dispose()).Should().NotThrow();
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithInterfaceAndProperties_ShouldCountCorrectly()
        {
            // Arrange
            await _analyzer.InitializeAsync();
            var code = @"
using System;

public interface ITestInterface
{
    string Name { get; set; }
    void DoSomething();
}

public class TestClass : ITestInterface
{
    public string Name { get; set; }
    private int _value;
    
    public void DoSomething()
    {
        // Implementation
    }
}";

            // Act
            var result = await _analyzer.AnalyzeCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Classes: 1");
            result.Should().Contain("Interfaces: 1");
            result.Should().Contain("Properties:");
            result.Should().Contain("Fields:");
        }

        public void Dispose()
        {
            _analyzer?.Dispose();
        }
    }
}