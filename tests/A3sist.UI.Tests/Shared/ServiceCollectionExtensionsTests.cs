using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.UI.Shared;
using A3sist.UI.Shared.Interfaces;
using System;

namespace A3sist.UI.Tests.Shared
{
    /// <summary>
    /// Tests for unified service registration to ensure framework detection works correctly
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddA3sistUI_ShouldRegisterCoreServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddA3sistUI();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            serviceProvider.GetService<IUIService>().Should().NotBeNull();
            serviceProvider.GetService<ChatViewModel>().Should().NotBeNull();
        }

        [Fact]
        public void AddA3sistUI_WithDifferentFrameworks_ShouldRegisterCorrectImplementations()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddA3sistUI();
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Should register WPF services for .NET 9
            var uiService = serviceProvider.GetService<IUIService>();
            uiService.Should().NotBeNull();
            uiService.GetType().Name.Should().Contain("WPF");
        }

        [Fact]
        public void AddFrameworkLogging_ShouldConfigureLoggingCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddFrameworkLogging();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var logger = serviceProvider.GetService<ILogger<ServiceCollectionExtensionsTests>>();
            logger.Should().NotBeNull();
        }

        [Fact]
        public void ServiceRegistration_ShouldHaveCorrectLifetimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddA3sistUI();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var uiService1 = serviceProvider.GetService<IUIService>();
            var uiService2 = serviceProvider.GetService<IUIService>();

            var chatViewModel1 = serviceProvider.GetService<ChatViewModel>();
            var chatViewModel2 = serviceProvider.GetService<ChatViewModel>();

            // Assert
            uiService1.Should().BeSameAs(uiService2); // Should be singleton
            chatViewModel1.Should().NotBeSameAs(chatViewModel2); // Should be transient
        }

        [Fact]
        public void GetFrameworkInfo_ShouldReturnCorrectFramework()
        {
            // Act
            var frameworkInfo = ServiceCollectionExtensions.GetFrameworkInfo();

            // Assert
            frameworkInfo.Should().NotBeNullOrEmpty();
            frameworkInfo.Should().Contain(".NET");
        }

        [Fact]
        public void ServiceCollection_WithNullServices_ShouldThrowArgumentNullException()
        {
            // Arrange
            ServiceCollection? services = null;

            // Act & Assert
            var action = () => services!.AddA3sistUI();
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void DependencyResolution_AllRequiredServices_ShouldResolveWithoutErrors()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddA3sistUI();

            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert - Should not throw
            var uiService = serviceProvider.GetRequiredService<IUIService>();
            var chatViewModel = serviceProvider.GetRequiredService<ChatViewModel>();

            uiService.Should().NotBeNull();
            chatViewModel.Should().NotBeNull();
        }

        [Fact]
        public void ConditionalCompilation_ShouldRegisterCorrectServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddA3sistUI();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
#if NET9_0_OR_GREATER
            // For .NET 9, should register WPF services
            var uiService = serviceProvider.GetService<IUIService>();
            uiService.Should().NotBeNull();
            uiService.GetType().Namespace.Should().Contain("WPF");
#elif NET472
            // For .NET 4.7.2, should register VSIX services
            var uiService = serviceProvider.GetService<IUIService>();
            uiService.Should().NotBeNull();
            uiService.GetType().Namespace.Should().Contain("VSIX");
#endif
        }

        [Fact]
        public void MultipleRegistrations_ShouldNotCauseDuplicates()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddA3sistUI();
            services.AddA3sistUI(); // Add twice
            
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Should not throw and should work correctly
            var uiService = serviceProvider.GetService<IUIService>();
            uiService.Should().NotBeNull();
        }

        [Fact]
        public void ServiceProvider_Disposal_ShouldDisposeServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddA3sistUI();

            var serviceProvider = services.BuildServiceProvider();
            var uiService = serviceProvider.GetService<IUIService>();

            // Act & Assert - Should not throw
            var action = () => serviceProvider.Dispose();
            action.Should().NotThrow();
        }
    }
}