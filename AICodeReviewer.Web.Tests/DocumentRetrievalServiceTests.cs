
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Tests
{
    public class DocumentRetrievalServiceTests
    {
        private readonly Mock<ILogger<DocumentRetrievalService>> _mockLogger;
        private readonly Mock<IDocumentManagementService> _mockDocumentManagementService;
        private readonly DocumentRetrievalService _documentRetrievalService;

        public DocumentRetrievalServiceTests()
        {
            _mockLogger = new Mock<ILogger<DocumentRetrievalService>>();
            _mockDocumentManagementService = new Mock<IDocumentManagementService>();
            _documentRetrievalService = new DocumentRetrievalService(_mockLogger.Object, _mockDocumentManagementService.Object);
        }

        [Fact]
        public async Task LoadDocumentsAsync_WithDocuments_ReturnsContent()
        {
            // Arrange
            var selectedDocuments = new List<string> { "doc1", "doc2" };
            var documentsFolder = "/docs";
            _mockDocumentManagementService.Setup(s => s.LoadDocumentAsync("doc1", documentsFolder))
                .ReturnsAsync(("content1", false));
            _mockDocumentManagementService.Setup(s => s.LoadDocumentAsync("doc2", documentsFolder))
                .ReturnsAsync(("content2", false));

            // Act
            var result = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("content1", result);
            Assert.Contains("content2", result);
        }

        [Fact]
        public async Task LoadDocumentsAsync_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var selectedDocuments = new List<string>();
            var documentsFolder = "/docs";

            // Act
            var result = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task LoadDocumentsAsync_WithSomeFailures_ReturnsOnlySuccessful()
        {
            // Arrange
            var selectedDocuments = new List<string> { "doc1", "doc2" };
            var documentsFolder = "/docs";
            _mockDocumentManagementService.Setup(s => s.LoadDocumentAsync("doc1", documentsFolder))
                .ReturnsAsync(("content1", false));
            _mockDocumentManagementService.Setup(s => s.LoadDocumentAsync("doc2", documentsFolder))
                .ReturnsAsync(("error", true));

            // Act
            var result = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);

            // Assert
            Assert.Single(result);
            Assert.Contains("content1", result);
        }
    }
}
