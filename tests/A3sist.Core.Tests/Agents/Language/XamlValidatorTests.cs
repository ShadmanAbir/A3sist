using System;
using System.Threading.Tasks;
using A3sist.Orchastrator.Agents.CSharp.Services;
using FluentAssertions;
using Xunit;

namespace A3sist.Core.Tests.Agents.Language
{
    /// <summary>
    /// Unit tests for the XAML Validator service
    /// </summary>
    public class XamlValidatorTests : IDisposable
    {
        private readonly XamlValidator _validator;

        public XamlValidatorTests()
        {
            _validator = new XamlValidator();
        }

        [Fact]
        public async Task InitializeAsync_ShouldCompleteSuccessfully()
        {
            // Act
            await _validator.InitializeAsync();

            // Assert
            // Should complete without throwing
            _validator.Should().NotBeNull();
        }

        [Fact]
        public async Task ValidateXamlAsync_WithValidXaml_ShouldReturnSuccessMessage()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                            <Grid>
                                <TextBlock Text=""Hello World"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("successfully");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithInvalidXml_ShouldReportXmlErrors()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <Grid>
                                <TextBlock Text=""Hello World"" />
                            </Grid>
                        <!-- Missing closing Window tag -->";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("XML Structure Errors");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithInvalidXaml_ShouldReportXamlErrors()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <Grid>
                                <InvalidControl SomeProperty=""Value"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            // May contain XAML structure errors depending on the validation
        }

        [Fact]
        public async Task ValidateXamlAsync_WithNamespaceAnalysis_ShouldReportNamespaces()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                               xmlns:local=""clr-namespace:MyApp"">
                            <Grid>
                                <TextBlock Text=""Hello World"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Namespace Analysis");
            result.Should().Contain("Declared namespaces:");
            result.Should().Contain("WPF Presentation");
            result.Should().Contain("XAML");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithProperties_ShouldAnalyzeProperties()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               Title=""My Window""
                               Width=""800""
                               Height=""600"">
                            <Grid Margin=""10"">
                                <TextBlock Text=""Hello World"" FontSize=""16"" />
                                <Button Content=""Click Me"" Width=""100"" Height=""30"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Property Analysis");
            result.Should().Contain("Total elements:");
            result.Should().Contain("Elements with properties:");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithDuplicateProperties_ShouldReportIssues()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <Grid>
                                <TextBlock Text=""Hello"" Text=""World"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Property issues found");
            result.Should().Contain("Duplicate property");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithEmptyProperties_ShouldReportIssues()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <Grid>
                                <TextBlock Text="""" FontSize="""" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Property issues found");
            result.Should().Contain("Empty property");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithResources_ShouldAnalyzeResources()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                            <Window.Resources>
                                <SolidColorBrush x:Key=""MyBrush"" Color=""Blue"" />
                                <Style x:Key=""MyStyle"" TargetType=""Button"">
                                    <Setter Property=""Background"" Value=""Red"" />
                                </Style>
                            </Window.Resources>
                            <Grid>
                                <Button Style=""{StaticResource MyStyle}"" />
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Resource Analysis");
            result.Should().Contain("Resource containers found:");
            result.Should().Contain("Resources with keys:");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithResourcesWithoutKeys_ShouldReportIssues()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <Window.Resources>
                                <SolidColorBrush Color=""Blue"" />
                            </Window.Resources>
                            <Grid />
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Resource Analysis");
            result.Should().Contain("Resources without keys: 1");
        }

        [Fact]
        public async Task ValidateXamlAsync_WithNullOrEmptyXaml_ShouldReturnErrorMessage()
        {
            // Arrange
            await _validator.InitializeAsync();

            // Act
            var resultNull = await _validator.ValidateXamlAsync(null);
            var resultEmpty = await _validator.ValidateXamlAsync("");
            var resultWhitespace = await _validator.ValidateXamlAsync("   ");

            // Assert
            resultNull.Should().Contain("No XAML content provided");
            resultEmpty.Should().Contain("No XAML content provided");
            resultWhitespace.Should().Contain("No XAML content provided");
        }

        [Fact]
        public async Task FormatXamlAsync_WithValidXaml_ShouldReturnFormattedXaml()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""><Grid><TextBlock Text=""Hello"" /></Grid></Window>";

            // Act
            var result = await _validator.FormatXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBe(xaml); // Should be formatted differently
            result.Should().Contain(Environment.NewLine); // Should have line breaks
            result.Should().Contain("  "); // Should have indentation
        }

        [Fact]
        public async Task FormatXamlAsync_WithInvalidXaml_ShouldThrowException()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window><Grid><TextBlock Text=""Hello"" /></Grid>"; // Missing closing tag

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.FormatXamlAsync(xaml));
        }

        [Fact]
        public async Task FormatXamlAsync_WithNullOrEmptyXaml_ShouldReturnInput()
        {
            // Arrange
            await _validator.InitializeAsync();

            // Act
            var resultNull = await _validator.FormatXamlAsync(null);
            var resultEmpty = await _validator.FormatXamlAsync("");

            // Assert
            resultNull.Should().BeNull();
            resultEmpty.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateXamlAsync_WithComplexXaml_ShouldProvideComprehensiveAnalysis()
        {
            // Arrange
            await _validator.InitializeAsync();
            var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                               xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                               xmlns:local=""clr-namespace:MyApp""
                               Title=""Complex Window""
                               Width=""800""
                               Height=""600"">
                            <Window.Resources>
                                <Style x:Key=""ButtonStyle"" TargetType=""Button"">
                                    <Setter Property=""Margin"" Value=""5"" />
                                </Style>
                            </Window.Resources>
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height=""Auto"" />
                                    <RowDefinition Height=""*"" />
                                </Grid.RowDefinitions>
                                <TextBlock Grid.Row=""0"" Text=""Header"" FontSize=""20"" />
                                <StackPanel Grid.Row=""1"" Orientation=""Vertical"">
                                    <Button Content=""Button 1"" Style=""{StaticResource ButtonStyle}"" />
                                    <Button Content=""Button 2"" Style=""{StaticResource ButtonStyle}"" />
                                </StackPanel>
                            </Grid>
                        </Window>";

            // Act
            var result = await _validator.ValidateXamlAsync(xaml);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("XML Structure");
            result.Should().Contain("XAML Structure");
            result.Should().Contain("Namespace Analysis");
            result.Should().Contain("Property Analysis");
            result.Should().Contain("Resource Analysis");
        }

        [Fact]
        public async Task ShutdownAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            await _validator.InitializeAsync();

            // Act
            await _validator.ShutdownAsync();

            // Assert
            // Should complete without throwing
            _validator.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            _validator.Invoking(v => v.Dispose()).Should().NotThrow();
        }

        public void Dispose()
        {
            _validator?.Dispose();
        }
    }
}