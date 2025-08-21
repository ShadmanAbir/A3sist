using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.UI.Services;

namespace A3sist.UI.Tests.Services
{
    [TestClass]
    public class QuickActionProviderTests
    {
        private Mock<ITextView> _mockTextView;
        private Mock<ITextBuffer> _mockTextBuffer;
        private Mock<ITextDocumentFactoryService> _mockTextDocumentFactoryService;
        private Mock<ITextDocument> _mockTextDocument;
        private Mock<ISuggestionService> _mockSuggestionService;
        private Mock<ILogger<A3sistSuggestedActionsSource>> _mockLogger;
        private QuickActionProvider _quickActionProvider;

        [TestInitialize]
        public void Setup()
        {
            _mockTextView = new Mock<ITextView>();
            _mockTextBuffer = new Mock<ITextBuffer>();
            _mockTextDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            _mockTextDocument = new Mock<ITextDocument>();
            _mockSuggestionService = new Mock<ISuggestionService>();
            _mockLogger = new Mock<ILogger<A3sistSuggestedActionsSource>>();

            _quickActionProvider = new QuickActionProvider
            {
                TextDocumentFactoryService = _mockTextDocumentFactoryService.Object
            };

            // Setup basic mocks
            _mockTextDocument.Setup(d => d.FilePath).Returns("test.cs");
            _mockTextDocumentFactoryService
                .Setup(f => f.TryGetTextDocument(_mockTextBuffer.Object, out It.Ref<ITextDocument>.IsAny))
                .Returns((ITextBuffer buffer, out ITextDocument document) =>
                {
                    document = _mockTextDocument.Object;
                    return true;
                });
        }

        [TestMethod]
        public void CreateSuggestedActionsSource_WithValidParameters_ReturnsSource()
        {
            // Act
            var source = _quickActionProvider.CreateSuggestedActionsSource(_mockTextView.Object, _mockTextBuffer.Object);

            // Assert
            Assert.IsNotNull(source);
            Assert.IsInstanceOfType(source, typeof(A3sistSuggestedActionsSource));
        }

        [TestMethod]
        public void CreateSuggestedActionsSource_WithNullParameters_ReturnsNull()
        {
            // Act & Assert
            Assert.IsNull(_quickActionProvider.CreateSuggestedActionsSource(null, _mockTextBuffer.Object));
            Assert.IsNull(_quickActionProvider.CreateSuggestedActionsSource(_mockTextView.Object, null));
            Assert.IsNull(_quickActionProvider.CreateSuggestedActionsSource(null, null));
        }

        [TestMethod]
        public void A3sistSuggestedAction_DisplayText_ReturnsCorrectValue()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Title = "Test Suggestion",
                Description = "Test Description",
                Type = SuggestionType.CodeFix
            };

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act & Assert
            Assert.AreEqual("Test Suggestion", action.DisplayText);
        }

        [TestMethod]
        public void A3sistSuggestedAction_HasPreview_ReturnsTrueWhenPreviewTextExists()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Title = "Test Suggestion",
                PreviewText = "Preview content"
            };

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act & Assert
            Assert.IsTrue(action.HasPreview);
        }

        [TestMethod]
        public void A3sistSuggestedAction_HasPreview_ReturnsFalseWhenNoPreviewText()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Title = "Test Suggestion",
                PreviewText = null
            };

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act & Assert
            Assert.IsFalse(action.HasPreview);
        }

        [TestMethod]
        public void A3sistSuggestedAction_GetPreviewAsync_ReturnsPreviewText()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Title = "Test Suggestion",
                PreviewText = "Preview content"
            };

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act
            var preview = action.GetPreviewAsync(CancellationToken.None).Result;

            // Assert
            Assert.AreEqual("Preview content", preview);
        }

        [TestMethod]
        public void A3sistSuggestedAction_GetPreviewAsync_ReturnsNullWhenNoPreview()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Title = "Test Suggestion",
                PreviewText = null
            };

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act
            var preview = action.GetPreviewAsync(CancellationToken.None).Result;

            // Assert
            Assert.IsNull(preview);
        }

        [TestMethod]
        public void A3sistSuggestedAction_Invoke_CallsApplySuggestionAsync()
        {
            // Arrange
            var suggestion = new CodeSuggestion
            {
                Id = Guid.NewGuid(),
                Title = "Test Suggestion"
            };

            _mockSuggestionService
                .Setup(s => s.ApplySuggestionAsync(suggestion))
                .ReturnsAsync(true);

            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act
            action.Invoke(CancellationToken.None);

            // Assert
            _mockSuggestionService.Verify(s => s.ApplySuggestionAsync(suggestion), Times.Once);
        }

        [TestMethod]
        public void A3sistSuggestedAction_TryGetTelemetryId_ReturnsValidGuid()
        {
            // Arrange
            var suggestion = new CodeSuggestion { Title = "Test" };
            var action = new A3sistSuggestedAction(suggestion, _mockSuggestionService.Object, _mockLogger.Object);

            // Act
            var result = action.TryGetTelemetryId(out Guid telemetryId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreNotEqual(Guid.Empty, telemetryId);
        }

        [TestMethod]
        public void ServiceLocator_RegisterAndGetService_WorksCorrectly()
        {
            // Arrange
            var testService = new TestService();

            // Act
            ServiceLocator.RegisterService<ITestService>(testService);
            var retrievedService = ServiceLocator.GetService<ITestService>();

            // Assert
            Assert.IsNotNull(retrievedService);
            Assert.AreSame(testService, retrievedService);
        }

        [TestMethod]
        public void ServiceLocator_GetNonExistentService_ReturnsDefault()
        {
            // Act
            var service = ServiceLocator.GetService<INonExistentService>();

            // Assert
            Assert.IsNull(service);
        }

        // Helper interfaces and classes for testing
        public interface ITestService
        {
            string GetValue();
        }

        public interface INonExistentService
        {
        }

        public class TestService : ITestService
        {
            public string GetValue() => "test";
        }
    }
}