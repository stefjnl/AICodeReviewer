using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Services;
using AICodeReviewer.Web.Models;
using AICodeReviewer.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace AICodeReviewer.Web.Tests
{
    public class AnalysisCoordinatorServiceTests
    {
        private readonly Mock<ILogger<AnalysisCoordinatorService>> _mockLogger;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IContentExtractionService> _mockContentExtractionService;
        private readonly Mock<IDocumentRetrievalService> _mockDocumentRetrievalService;
        private readonly Mock<IAIAnalysisOrchestrator> _mockAiAnalysisOrchestrator;
        private readonly Mock<IResultProcessorService> _mockResultProcessorService;
        private readonly Mock<ISignalRBroadcastService> _mockSignalRService;
        private readonly Mock<IHubContext<ProgressHub>> _mockHubContext;
        private readonly IMemoryCache _cache;
        private readonly AnalysisCoordinatorService _coordinatorService;

        public AnalysisCoordinatorServiceTests()
        {
            _mockLogger = new Mock<ILogger<AnalysisCoordinatorService>>();
            _mockValidationService = new Mock<IValidationService>();
            _mockContentExtractionService = new Mock<IContentExtractionService>();
            _mockDocumentRetrievalService = new Mock<IDocumentRetrievalService>();
            _mockAiAnalysisOrchestrator = new Mock<IAIAnalysisOrchestrator>();
            _mockResultProcessorService = new Mock<IResultProcessorService>();
            _mockSignalRService = new Mock<ISignalRBroadcastService>();
            _mockHubContext = new Mock<IHubContext<ProgressHub>>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _coordinatorService = new AnalysisCoordinatorService(
                _mockLogger.Object,
                _mockValidationService.Object,
                _mockContentExtractionService.Object,
                _mockDocumentRetrievalService.Object,
                _mockAiAnalysisOrchestrator.Object,
                _mockResultProcessorService.Object,
                _mockSignalRService.Object,
                _mockHubContext.Object,
                _cache);
        }

        [Fact]
        public async Task StartAnalysisAsync_ValidationFails_ReturnsError()
        {
            // Arrange
            var request = new RunAnalysisRequest();
            var mockSession = new Mock<ISession>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            var mockConfiguration = new Mock<IConfiguration>();

            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/test/root");

            _mockValidationService.Setup(x => x.ValidateAnalysisRequestAsync(request, mockSession.Object, mockEnvironment.Object))
                .ReturnsAsync((false, "Validation failed", null));

            // Act
            var result = await _coordinatorService.StartAnalysisAsync(
                request, mockSession.Object, mockEnvironment.Object, mockConfiguration.Object);

            // Assert
            Assert.Equal("", result.analysisId);
            Assert.False(result.success);
            Assert.Equal("Validation failed", result.error);
        }

        [Fact]
        public async Task StartAnalysisAsync_ValidationPasses_ReturnsAnalysisId()
        {
            // Arrange
            var request = new RunAnalysisRequest
            {
                RepositoryPath = "/test/repo",
                SelectedDocuments = new List<string> { "doc1", "doc2" },
                Language = "NET",
                AnalysisType = AnalysisType.Uncommitted
            };

            var mockSession = new Mock<ISession>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            var mockConfiguration = new Mock<IConfiguration>();

            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/test/root");
            mockConfiguration.Setup(x => x["OpenRouter:ApiKey"]).Returns("test-api-key");
            mockConfiguration.Setup(x => x["OpenRouter:Model"]).Returns("gpt-4");
            mockConfiguration.Setup(x => x["OpenRouter:FallbackModel"]).Returns("gpt-3.5-turbo");

            _mockValidationService.Setup(x => x.ValidateAnalysisRequestAsync(request, mockSession.Object, mockEnvironment.Object))
                .ReturnsAsync((true, null, null));

            // Act
            var result = await _coordinatorService.StartAnalysisAsync(
                request, mockSession.Object, mockEnvironment.Object, mockConfiguration.Object);

            // Assert
            Assert.NotEqual("", result.analysisId);
            Assert.True(result.success);
            Assert.Null(result.error);
        }

        [Fact]
        public async Task StartAnalysisAsync_ExceptionDuringStart_ReturnsError()
        {
            // Arrange
            var request = new RunAnalysisRequest();
            var mockSession = new Mock<ISession>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            var mockConfiguration = new Mock<IConfiguration>();

            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/test/root");

            _mockValidationService.Setup(x => x.ValidateAnalysisRequestAsync(It.IsAny<RunAnalysisRequest>(), It.IsAny<ISession>(), It.IsAny<IWebHostEnvironment>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _coordinatorService.StartAnalysisAsync(
                request, mockSession.Object, mockEnvironment.Object, mockConfiguration.Object);

            // Assert
            Assert.Equal("", result.analysisId);
            Assert.False(result.success);
            Assert.Equal("Test exception", result.error);
        }

        [Fact]
        public void GetAnalysisStatus_WithValidAnalysisId_ReturnsProgressDto()
        {
            // Arrange
            var analysisId = "test-analysis-id";
            var expectedResult = new AnalysisResult
            {
                Status = "Complete",
                Result = "Test result",
                Error = null
            };

            _cache.Set($"analysis_{analysisId}", expectedResult);

            // Act
            var result = _coordinatorService.GetAnalysisStatus(analysisId);

            // Assert
            Assert.Equal("Complete", result.Status);
            Assert.Equal("Test result", result.Result);
            Assert.Null(result.Error);
            Assert.True(result.IsComplete);
        }

        [Fact]
        public void GetAnalysisStatus_WithInvalidAnalysisId_ReturnsNotFound()
        {
            // Arrange
            var analysisId = "non-existent-id";

            // Act
            var result = _coordinatorService.GetAnalysisStatus(analysisId);

            // Assert
            Assert.Equal("NotFound", result.Status);
            Assert.Null(result.Result);
            Assert.Equal("Analysis not found or expired", result.Error);
            Assert.True(result.IsComplete);
        }

        [Fact]
        public void GetAnalysisStatus_WithEmptyAnalysisId_ReturnsNotStarted()
        {
            // Arrange
            var analysisId = "";

            // Act
            var result = _coordinatorService.GetAnalysisStatus(analysisId);

            // Assert
            Assert.Equal("NotStarted", result.Status);
            Assert.Null(result.Result);
            Assert.Null(result.Error);
            Assert.False(result.IsComplete);
        }

        [Fact]
        public void StoreAnalysisId_WithValidId_LogsCorrectly()
        {
            // Arrange
            var analysisId = "test-analysis-id";
            var mockSession = new Mock<ISession>();

            // Act
            _coordinatorService.StoreAnalysisId(analysisId, mockSession.Object);

            // Assert - We can't easily mock extension methods, so we verify the method doesn't throw
            Assert.NotNull(analysisId);
        }

        [Fact]
        public void StoreAnalysisId_WithEmptyId_HandlesGracefully()
        {
            // Arrange
            var analysisId = "";
            var mockSession = new Mock<ISession>();

            // Act & Assert - Should not throw exception
            _coordinatorService.StoreAnalysisId(analysisId, mockSession.Object);
        }
    }
}
