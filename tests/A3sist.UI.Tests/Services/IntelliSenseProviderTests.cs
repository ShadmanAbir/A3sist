using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
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
    public class IntelliSenseProviderTests
    {
        private Mock<ITextView> _mockTextView;
        private Mock<ITextDocumentFactoryService> _mockTextDocumentFactoryService;
        private Mock<ITextDocument> _mockTextDocument;
        private Mock<IOrchestrator> _mockOrchestrator;
        private Mock<ILogger<A3sistCompletionSource>> _mockLogger;
        private IntelliSenseProvider _intelliSenseProvider;

        [TestInitialize]
        public void Setup()
        {
            _mockTextView = new Mock<ITextView>();
            _mockTextDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            _mockTextDocument = new Mock<ITextDocument>();
            _mockOrchestrator = new Mock<IOrchestrator>();
            _mockLogger = new Mock<ILogger<A3sistCompletionSource>>();

            _intelliSenseProvider = new IntelliSenseProvider
            {
                TextDocumentFactoryService = _mockTextDocumentFactoryService.Object
            };

            // Setup basic mocks
            _mockTextDocument.Setup(d => d.FilePath).Returns("test.cs");
        }

        [TestMethod]
        public void GetOrCreate_WithValidTextView_ReturnsCompletionSource()
        {
            // Act
            var source = _intelliSenseProvider.GetOrCreate(_mockTextView.Object);

            // Assert
            Assert.IsNotNull(source);
            Assert.IsInstanceOfType(source, typeof(A3sistCompletionSource));
        }

        [TestMethod]
        public void GetOrCreate_WithNullTextView_ReturnsNull()
        {
            // Act
            var source = _intelliSenseProvider.GetOrCreate(null);

            // Assert
            Assert.IsNull(source);
        }
    }
}