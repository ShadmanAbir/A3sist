using System;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents.CSharp.Services;
using FluentAssertions;
using Xunit;

namespace A3sist.Core.Tests.Agents.Language
{
    /// <summary>
    /// Unit tests for the C# RefactorEngine service
    /// </summary>
    public class CSharpRefactorEngineTests : IDisposable
    {
        private readonly RefactorEngine _refactorEngine;

        public CSharpRefactorEngineTests()
        {
            _refactorEngine = new RefactorEngine();
        }

        [Fact]
        public async Task InitializeAsync_ShouldCompleteSuccessfully()
        {
            // Act
            await _refactorEngine.InitializeAsync();

            // Assert
            // Should complete without throwing
            _refactorEngine.Should().NotBeNull();
        }

        [Fact]
        public async Task RefactorCodeAsync_WithValidCode_ShouldReturnRefactoredCode()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            List<string> items = new List<string>();
            items.Add(""test"");
        }
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().NotBe(code); // Should be different after refactoring
        }

        [Fact]
        public async Task RefactorCodeAsync_WithVarCandidates_ShouldApplyVarKeyword()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        List<string> items = new List<string>();
        Dictionary<string, int> dict = new Dictionary<string, int>();
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("var");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithBooleanComparisons_ShouldSimplifyExpressions()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
public class TestClass
{
    public void TestMethod(bool condition)
    {
        if (condition == true)
        {
            // Do something
        }
        
        if (condition == false)
        {
            // Do something else
        }
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            // Should simplify boolean comparisons
            result.Should().NotContain("== true");
            result.Should().NotContain("== false");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithLinqWhereCount_ShouldOptimizeToCount()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System.Linq;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        var items = new List<string> { ""a"", ""b"", ""c"" };
        var count = items.Where(x => x.Length > 1).Count();
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            // Should optimize Where().Count() to Count(predicate)
            result.Should().Contain("Count(");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithFormattingIssues_ShouldFormatCode()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"public class TestClass{public void TestMethod(){var x=1;}}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(Environment.NewLine); // Should be properly formatted
            result.Should().Contain("    "); // Should have proper indentation
        }

        [Fact]
        public async Task RefactorCodeAsync_WithUnnecessaryUsings_ShouldRemoveUnusedUsings()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

public class TestClass
{
    public void TestMethod()
    {
        Console.WriteLine(""Hello"");
        var list = new List<string>();
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            // Should keep used usings
            result.Should().Contain("using System;");
            result.Should().Contain("using System.Collections.Generic;");
            // Should remove unused usings
            result.Should().NotContain("using System.IO;");
            result.Should().NotContain("using System.Net.Http;");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithNullOrEmptyCode_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _refactorEngine.RefactorCodeAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _refactorEngine.RefactorCodeAsync(""));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _refactorEngine.RefactorCodeAsync("   "));
        }

        [Fact]
        public async Task RefactorCodeAsync_WithSyntaxError_ShouldThrowInvalidOperationException()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
public class TestClass
{
    public void TestMethod(
    {
        Console.WriteLine(""Hello"");
    }
}"; // Missing closing parenthesis

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _refactorEngine.RefactorCodeAsync(code));
        }

        [Fact]
        public async Task RefactorCodeAsync_WithComplexCode_ShouldPreserveLogic()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public void ComplexMethod(List<int> numbers)
    {
        Dictionary<string, int> results = new Dictionary<string, int>();
        
        for (int i = 0; i < numbers.Count; i++)
        {
            if (numbers[i] % 2 == 0)
            {
                results.Add($""even_{i}"", numbers[i]);
            }
            else
            {
                results.Add($""odd_{i}"", numbers[i]);
            }
        }
        
        var evenCount = results.Where(x => x.Key.StartsWith(""even"")).Count();
        Console.WriteLine($""Even numbers: {evenCount}"");
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            // Should preserve the core logic
            result.Should().Contain("ComplexMethod");
            result.Should().Contain("numbers.Count");
            result.Should().Contain("results.Add");
            // Should apply refactoring improvements
            result.Should().Contain("var");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithSimpleTypes_ShouldNotUseVarForPrimitives()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
public class TestClass
{
    public void TestMethod()
    {
        int number = 42;
        string text = ""hello"";
        bool flag = true;
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            // Should not use var for simple primitive assignments
            result.Should().Contain("int number");
            result.Should().Contain("string text");
            result.Should().Contain("bool flag");
        }

        [Fact]
        public async Task ShutdownAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();

            // Act
            await _refactorEngine.ShutdownAsync();

            // Assert
            // Should complete without throwing
            _refactorEngine.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            _refactorEngine.Invoking(r => r.Dispose()).Should().NotThrow();
        }

        [Fact]
        public async Task RefactorCodeAsync_WithGenericTypes_ShouldUseVar()
        {
            // Arrange
            await _refactorEngine.InitializeAsync();
            var code = @"
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        Dictionary<string, List<int>> complexType = new Dictionary<string, List<int>>();
    }
}";

            // Act
            var result = await _refactorEngine.RefactorCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("var complexType");
        }

        public void Dispose()
        {
            _refactorEngine?.Dispose();
        }
    }
}