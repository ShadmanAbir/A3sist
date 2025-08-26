using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.Core;
using A3sist.Core.Services;
using A3sist.Shared.Models;
using A3sist.Shared.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace A3sist.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for the simplified A3sist architecture
    /// Tests the complete flow from request to response with RAG enhancement
    /// </summary>
    public class SimplifiedArchitectureIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;

        public SimplifiedArchitectureIntegrationTests()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Add A3sist core services
            services.AddRAGServices();
            services.AddTransient<EnhancedRequestRouter>();
            
            // Mock LLM service for testing
            services.AddSingleton<ILLMService, MockLLMService>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task EndToEndRequest_CodeAnalysis_ShouldWorkWithRAGEnhancement()
        {
            // Arrange
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();
            
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this C# method for performance issues",
                Context = new Dictionary<string, object>
                {
                    { "code", @"
                        public void ProcessData(List<string> data)
                        {
                            for(int i = 0; i < data.Count; i++)
                            {
                                data[i] = data[i].ToUpper();
                                Thread.Sleep(10);
                            }
                        }" },
                    { "language", "csharp" }
                }
            };

            // Act
            var response = await requestRouter.ProcessRequestAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("performance").Or.Contain("analysis");
        }

        [Fact]
        public async Task EndToEndRequest_Refactoring_ShouldIncludeModernPatterns()
        {
            // Arrange
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();
            
            var request = new AgentRequest
            {
                Type = RequestType.Refactoring,
                Content = "Modernize this legacy code for .NET 9",
                Context = new Dictionary<string, object>
                {
                    { "code", @"
                        public class LegacyDataService
                        {
                            public string GetUserData(int userId)
                            {
                                return File.ReadAllText($""user_{userId}.txt"");
                            }
                        }" },
                    { "targetFramework", "net9.0" }
                }
            };

            // Act
            var response = await requestRouter.ProcessRequestAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("modern").Or.Contain("async").Or.Contain("dependency");
        }

        [Fact]
        public async Task EndToEndRequest_Chat_ShouldProvideHelpfulResponse()
        {
            // Arrange
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();
            
            var request = new AgentRequest
            {
                Type = RequestType.Chat,
                Content = "How do I implement dependency injection in .NET 9?",
                Context = new Dictionary<string, object>()
            };

            // Act
            var response = await requestRouter.ProcessRequestAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Success.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("dependency injection").Or.Contain("DI").Or.Contain("service");
        }

        [Fact]
        public async Task ParallelRequests_ShouldHandleMultipleRequestsCorrectly()
        {
            // Arrange
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();
            
            var requests = new[]
            {
                new AgentRequest
                {
                    Type = RequestType.CodeAnalysis,
                    Content = "Analyze performance",
                    Context = new Dictionary<string, object> { { "code", "public void Test() {}" }, { "language", "csharp" } }
                },
                new AgentRequest
                {
                    Type = RequestType.Chat,
                    Content = "Explain async/await",
                    Context = new Dictionary<string, object>()
                },
                new AgentRequest
                {
                    Type = RequestType.Refactoring,
                    Content = "Modernize this code",
                    Context = new Dictionary<string, object> { { "code", "public class Old {}" } }
                }
            };

            // Act
            var tasks = requests.Select(r => requestRouter.ProcessRequestAsync(r));
            var responses = await Task.WhenAll(tasks);

            // Assert
            responses.Should().HaveCount(3);
            responses.All(r => r.Success).Should().BeTrue();
            responses.All(r => !string.IsNullOrEmpty(r.Content)).Should().BeTrue();
        }

        [Fact]
        public async Task ServiceResolution_AllCoreServices_ShouldResolveCorrectly()
        {
            // Act & Assert
            var ragService = _serviceProvider.GetService<IRAGService>();
            ragService.Should().NotBeNull();

            var requestRouter = _serviceProvider.GetService<EnhancedRequestRouter>();
            requestRouter.Should().NotBeNull();

            var llmService = _serviceProvider.GetService<ILLMService>();
            llmService.Should().NotBeNull();

            var logger = _serviceProvider.GetService<ILogger<SimplifiedArchitectureIntegrationTests>>();
            logger.Should().NotBeNull();
        }

        [Fact]
        public async Task RAGService_KnowledgeRetrieval_ShouldWorkInIntegratedEnvironment()
        {
            // Arrange
            var ragService = _serviceProvider.GetRequiredService<IRAGService>();

            // Act
            var knowledge = await ragService.RetrieveKnowledgeAsync("dependency injection patterns", 5);

            // Assert
            knowledge.Should().NotBeNull();
            // Note: In a real environment, this would return actual knowledge items
            // In our test environment, it may be empty but should not throw
        }

        [Fact]
        public async Task MemoryUsage_SimplifiedArchitecture_ShouldBeEfficient()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(false);
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();

            // Act
            var requests = Enumerable.Range(0, 10).Select(i => new AgentRequest
            {
                Type = RequestType.Chat,
                Content = $"Test request {i}",
                Context = new Dictionary<string, object>()
            });

            foreach (var request in requests)
            {
                await requestRouter.ProcessRequestAsync(request);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // Less than 50MB increase
        }

        [Fact]
        public async Task ErrorHandling_InvalidRequests_ShouldHandleGracefully()
        {
            // Arrange
            var requestRouter = _serviceProvider.GetRequiredService<EnhancedRequestRouter>();

            var invalidRequests = new[]
            {
                null, // Null request
                new AgentRequest { Type = RequestType.CodeAnalysis, Content = null }, // Null content
                new AgentRequest { Type = RequestType.CodeAnalysis, Content = "", Context = null } // Null context
            };

            // Act & Assert
            foreach (var request in invalidRequests)
            {
                var response = await requestRouter.ProcessRequestAsync(request!);
                response.Should().NotBeNull();
                response.Success.Should().BeFalse();
                response.ErrorMessage.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void DependencyInjection_ServiceLifetimes_ShouldBeCorrect()
        {
            // Act
            var ragService1 = _serviceProvider.GetService<IRAGService>();
            var ragService2 = _serviceProvider.GetService<IRAGService>();

            var router1 = _serviceProvider.GetService<EnhancedRequestRouter>();
            var router2 = _serviceProvider.GetService<EnhancedRequestRouter>();

            // Assert
            // RAGService should be singleton
            ragService1.Should().BeSameAs(ragService2);

            // RequestRouter should be transient
            router1.Should().NotBeSameAs(router2);
        }
    }

    /// <summary>
    /// Mock LLM service for testing purposes
    /// </summary>
    public class MockLLMService : ILLMService
    {
        public Task<AgentResponse> ProcessRequestAsync(string prompt)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = $"Mock response for: {prompt.Substring(0, Math.Min(50, prompt.Length))}...",
                Citations = new[] { "Mock Knowledge Source" }
            };

            return Task.FromResult(response);
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
        }

        public Task<string> GetModelInfoAsync()
        {
            return Task.FromResult("Mock LLM Model v1.0");
        }
    }
}