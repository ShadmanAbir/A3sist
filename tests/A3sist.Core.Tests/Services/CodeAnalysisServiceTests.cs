using A3sist.Core.Services;
using A3sist.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class CodeAnalysisServiceTests
    {
        private readonly Mock<ILogger<CodeAnalysisService>> _mockLogger;
        private readonly CodeAnalysisService _codeAnalysisService;

        public CodeAnalysisServiceTests()
        {
            _mockLogger = new Mock<ILogger<CodeAnalysisService>>();
            _codeAnalysisService = new CodeAnalysisService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act & Assert - Constructor should not throw
            Assert.NotNull(_codeAnalysisService);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CodeAnalysisService(null));
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithEmptyCode_ReturnsEmptyResult()
        {
            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync("", "test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("csharp", result.Language);
            Assert.Empty(result.Elements);
            Assert.Empty(result.Patterns);
            Assert.Empty(result.CodeSmells);
            Assert.NotNull(result.Complexity);
            Assert.NotNull(result.Dependencies);
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithValidCSharpCode_ReturnsAnalysisResult()
        {
            // Arrange
            var code = @"
using System;
using System.Collections.Generic;

public class TestClass
{
    private int count;
    
    public void LongMethod()
    {
        for (int i = 0; i < 100; i++)
        {
            if (i % 2 == 0)
            {
                Console.WriteLine(i);
            }
            else if (i % 3 == 0)
            {
                Console.WriteLine(""Multiple of 3"");
            }
            else
            {
                Console.WriteLine(""Other"");
            }
        }
    }
    
    public void MethodWithMagicNumbers()
    {
        var result = count * 42 + 1337;
        if (result > 9999)
        {
            Console.WriteLine(""Large result"");
        }
    }
}";

            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync(code, "TestClass.cs");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("csharp", result.Language);
            
            // Should detect code elements
            Assert.NotEmpty(result.Elements);
            Assert.Contains(result.Elements, e => e.Type == CodeElementType.Class && e.Name == "TestClass");
            
            // Should calculate complexity metrics
            Assert.NotNull(result.Complexity);
            Assert.True(result.Complexity.CyclomaticComplexity > 1);
            Assert.True(result.Complexity.LinesOfCode > 0);
            Assert.True(result.Complexity.NumberOfMethods > 0);
            Assert.True(result.Complexity.NumberOfClasses > 0);
            
            // Should detect code smells
            Assert.NotEmpty(result.CodeSmells);
            
            // Should detect dependencies
            Assert.NotNull(result.Dependencies);
            Assert.NotEmpty(result.Dependencies.Dependencies);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithEmptyCode_ReturnsEmpty()
        {
            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync("", "test.cs");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithLongMethod_DetectsLongMethodSmell()
        {
            // Arrange
            var code = @"
public class Test
{
    public void VeryLongMethod()
    {
        // This method has many lines to trigger the long method detection
        var line1 = 1;
        var line2 = 2;
        var line3 = 3;
        var line4 = 4;
        var line5 = 5;
        var line6 = 6;
        var line7 = 7;
        var line8 = 8;
        var line9 = 9;
        var line10 = 10;
        var line11 = 11;
        var line12 = 12;
        var line13 = 13;
        var line14 = 14;
        var line15 = 15;
        var line16 = 16;
        var line17 = 17;
        var line18 = 18;
        var line19 = 19;
        var line20 = 20;
        var line21 = 21;
        var line22 = 22;
        var line23 = 23;
        var line24 = 24;
        var line25 = 25;
        var line26 = 26;
        var line27 = 27;
        var line28 = 28;
        var line29 = 29;
        var line30 = 30;
        var line31 = 31;
        var line32 = 32;
        var line33 = 33;
        var line34 = 34;
        var line35 = 35;
        var line36 = 36;
        var line37 = 37;
        var line38 = 38;
        var line39 = 39;
        var line40 = 40;
        var line41 = 41;
        var line42 = 42;
        var line43 = 43;
        var line44 = 44;
        var line45 = 45;
        var line46 = 46;
        var line47 = 47;
        var line48 = 48;
        var line49 = 49;
        var line50 = 50;
        var line51 = 51;
        var line52 = 52;
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.LongMethod);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithMagicNumbers_DetectsMagicNumberSmells()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method()
    {
        var result = count * 42 + 1337;
        if (result > 9999)
        {
            Console.WriteLine(""Result: "" + result);
        }
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.MagicNumbers);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithLongParameterList_DetectsLongParameterListSmell()
        {
            // Arrange
            var code = @"
public class Test
{
    public void MethodWithManyParameters(int param1, string param2, bool param3, double param4, float param5, char param6, byte param7)
    {
        // Method with too many parameters
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.LongParameterList);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithComplexConditional_DetectsComplexConditionalSmell()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method()
    {
        if (condition1 && condition2 || condition3 && condition4 || condition5 && condition6)
        {
            // Complex conditional
        }
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.ComplexConditional);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithDeadCode_DetectsDeadCodeSmell()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method()
    {
        Console.WriteLine(""Before return"");
        return;
        Console.WriteLine(""This is dead code"");
        var deadVariable = 42;
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.DeadCode);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithPoorNaming_DetectsNamingIssues()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method()
    {
        var a = 5;
        var b = 10;
        var c = a + b;
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.UncommunicativeNames);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithException_ReturnsEmpty()
        {
            // Arrange - Create a scenario that might cause an exception
            var invalidCode = new string('x', 1000000); // Very large string that might cause issues

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(invalidCode, "test.cs");

            // Assert - Should not throw, should return empty or partial results
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CalculateComplexityAsync_WithEmptyCode_ReturnsDefaultMetrics()
        {
            // Act
            var result = await _codeAnalysisService.CalculateComplexityAsync("", "test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.LinesOfCode);
            Assert.Equal(0, result.EffectiveLinesOfCode);
            Assert.Equal(0, result.NumberOfMethods);
            Assert.Equal(0, result.NumberOfClasses);
        }

        [Fact]
        public async Task CalculateComplexityAsync_WithComplexCode_ReturnsAccurateMetrics()
        {
            // Arrange
            var code = @"
using System;

public class TestClass
{
    public void Method1()
    {
        if (condition1)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    Console.WriteLine(i);
                }
            }
        }
    }
    
    public void Method2()
    {
        while (condition2)
        {
            switch (value)
            {
                case 1:
                    break;
                case 2:
                    break;
                default:
                    break;
            }
        }
    }
}

public interface ITestInterface
{
    void InterfaceMethod();
}";

            // Act
            var result = await _codeAnalysisService.CalculateComplexityAsync(code, "test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.LinesOfCode > 0);
            Assert.True(result.EffectiveLinesOfCode > 0);
            Assert.True(result.CyclomaticComplexity > 1); // Should detect decision points
            Assert.Equal(2, result.NumberOfMethods);
            Assert.Equal(1, result.NumberOfClasses);
            Assert.Equal(1, result.NumberOfInterfaces);
            Assert.True(result.MaintainabilityIndex >= 0 && result.MaintainabilityIndex <= 100);
        }

        [Fact]
        public async Task CalculateComplexityAsync_WithException_ReturnsDefaultMetrics()
        {
            // This test is more about ensuring the method doesn't throw
            // In a real scenario, we might mock dependencies to force an exception

            // Act
            var result = await _codeAnalysisService.CalculateComplexityAsync("some code", "test.cs");

            // Assert - Should not throw
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AnalyzeDependenciesAsync_WithEmptyCode_ReturnsEmptyResult()
        {
            // Act
            var result = await _codeAnalysisService.AnalyzeDependenciesAsync("", "test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Dependencies);
            Assert.Empty(result.ExternalDependencies);
            Assert.Empty(result.UnusedDependencies);
            Assert.Empty(result.CircularDependencies);
        }

        [Fact]
        public async Task AnalyzeDependenciesAsync_WithUsingStatements_DetectsDependencies()
        {
            // Arrange
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using CustomNamespace;

public class Test
{
    public void Method()
    {
        var list = new List<string>();
        Console.WriteLine(list.Count);
    }
}";

            // Act
            var result = await _codeAnalysisService.AnalyzeDependenciesAsync(code, "test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Dependencies);
            Assert.NotEmpty(result.ExternalDependencies);
            
            // Should detect the custom namespace as external dependency
            Assert.Contains(result.ExternalDependencies, dep => dep == "CustomNamespace");
            
            // Should detect unused dependencies
            Assert.Contains(result.UnusedDependencies, dep => dep == "CustomNamespace");
        }

        [Theory]
        [InlineData("Test.cs", "csharp")]
        [InlineData("test.js", "javascript")]
        [InlineData("test.ts", "typescript")]
        [InlineData("test.py", "python")]
        [InlineData("Test.java", "java")]
        [InlineData("test.cpp", "cpp")]
        [InlineData("test.cc", "cpp")]
        [InlineData("test.cxx", "cpp")]
        [InlineData("test.c", "c")]
        [InlineData("unknown.xyz", "unknown")]
        [InlineData("", "unknown")]
        [InlineData(null, "unknown")]
        public async Task AnalyzeCodeAsync_WithVariousFileExtensions_DetectsCorrectLanguage(string filePath, string expectedLanguage)
        {
            // Arrange
            var code = "public class Test {}";

            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync(code, filePath);

            // Assert
            Assert.Equal(expectedLanguage, result.Language);
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithSingletonPattern_DetectsPattern()
        {
            // Arrange
            var code = @"
public class Singleton
{
    private static Singleton instance;
    
    private Singleton() { }
    
    public static Singleton GetInstance()
    {
        if (instance == null)
        {
            instance = new Singleton();
        }
        return instance;
    }
}";

            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync(code, "Singleton.cs");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Patterns);
            Assert.Contains(result.Patterns, p => p.Name == "Singleton Pattern");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithFactoryPattern_DetectsPattern()
        {
            // Arrange
            var code = @"
public class Factory
{
    public static IProduct CreateProduct(string type)
    {
        switch (type)
        {
            case ""A"":
                return new ProductA();
            case ""B"":
                return new ProductB();
            default:
                throw new ArgumentException(""Unknown type"");
        }
    }
}";

            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync(code, "Factory.cs");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Patterns);
            Assert.Contains(result.Patterns, p => p.Name == "Factory Pattern");
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithGuardClause_DetectsPattern()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }
        
        // Rest of method logic
        Console.WriteLine(input);
    }
}";

            // Act
            var result = await _codeAnalysisService.AnalyzeCodeAsync(code, "Test.cs");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Patterns);
            Assert.Contains(result.Patterns, p => p.Name == "Guard Clause");
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithLargeClass_DetectsLargeClassSmell()
        {
            // Arrange - Create a very large class
            var codeLines = new string[600]; // More than 500 lines threshold
            for (int i = 0; i < codeLines.Length; i++)
            {
                codeLines[i] = $"    // Line {i + 1}";
            }
            
            var code = "public class LargeClass\n{\n" + string.Join("\n", codeLines) + "\n}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.LargeClass);
        }

        [Fact]
        public async Task DetectCodeSmellsAsync_WithDuplicatedCode_DetectsDuplicationSmell()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method1()
    {
        Console.WriteLine(""Hello"");
        Console.WriteLine(""World"");
        Console.WriteLine(""From"");
        Console.WriteLine(""Method1"");
        Console.WriteLine(""End"");
    }
    
    public void Method2()
    {
        Console.WriteLine(""Hello"");
        Console.WriteLine(""World"");
        Console.WriteLine(""From"");
        Console.WriteLine(""Method2"");
        Console.WriteLine(""End"");
    }
}";

            // Act
            var result = await _codeAnalysisService.DetectCodeSmellsAsync(code, "test.cs");

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, smell => smell.Type == CodeSmellType.DuplicatedCode);
        }
    }
}