using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Tests
{
    public class AIAnalysisOrchestratorTests
    {
        private readonly Mock<ILogger<AIAnalysisOrchestrator>> _mockLogger;
        private readonly Mock<IAIService> _mockAIService;
        private readonly AIAnalysisOrchestrator _orchestrator;

        public AIAnalysisOrchestratorTests()
        {
            _mockLogger = new Mock<ILogger<AIAnalysisOrchestrator>>();
            _mockAIService = new Mock<IAIService>();
            _orchestrator = new AIAnalysisOrchestrator(_mockLogger.Object, _mockAIService.Object);
        }

        [Fact]
        public async Task AnalyzeAsync_SuccessfulAnalysis_ReturnsExpectedResult()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1", "standard2" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("analysis result", false, null));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("analysis result", result.analysis);
            Assert.False(result.error);
            Assert.Null(result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_TimeoutException_HandlesGracefully()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal("AI analysis timed out after 60 seconds", result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_RateLimitError_UsesFallbackModel()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            // Primary model fails with rate limit
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, "429 Too Many Requests"));

            // Fallback model succeeds
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, fallbackModel, language, isFileContent))
                .ReturnsAsync(("fallback analysis result", false, null));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("fallback analysis result", result.analysis);
            Assert.False(result.error);
            Assert.Null(result.errorMsg);
            Assert.Equal(fallbackModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_RateLimitError_NoFallbackModelSpecified_UsesPrimaryModel()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            string? fallbackModel = null;
            var language = "NET";
            var isFileContent = false;

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, "429 Too Many Requests"));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal("429 Too Many Requests", result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_RateLimitError_FallbackModelAlsoFails_ReturnsFallbackError()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            // Primary model fails with rate limit
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, "429 Too Many Requests"));

            // Fallback model also fails
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, fallbackModel, language, isFileContent))
                .ReturnsAsync(("", true, "Fallback model error"));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal("Fallback model error", result.errorMsg);
            Assert.Equal(fallbackModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_UnexpectedException_HandlesGracefully()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Contains("Unexpected error calling AI service: Unexpected error", result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }

        [Theory]
        [InlineData("429 Too Many Requests")]
        [InlineData("rate limit exceeded")]
        [InlineData("too many requests")]
        [InlineData("Rate limit detected")]
        [InlineData("RATE LIMIT")]
        public async Task AnalyzeAsync_VariousRateLimitMessages_TriggersFallback(string rateLimitMessage)
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            // Primary model fails with rate limit
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, rateLimitMessage));

            // Fallback model succeeds
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, fallbackModel, language, isFileContent))
                .ReturnsAsync(("fallback analysis result", false, null));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("fallback analysis result", result.analysis);
            Assert.False(result.error);
            Assert.Null(result.errorMsg);
            Assert.Equal(fallbackModel, result.usedModel);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("some other error")]
        [InlineData("network timeout")]
        [InlineData("invalid api key")]
        public async Task AnalyzeAsync_NonRateLimitErrors_DoNotTriggerFallback(string errorMessage)
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            // Primary model fails with non-rate-limit error
            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, errorMessage));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal(errorMessage, result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);

            // Verify fallback was not called
            _mockAIService.Verify(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, fallbackModel, language, isFileContent),
                Times.Never);
        }

        [Fact]
        public async Task AnalyzeAsync_EmptyApiKey_ReturnsError()
        {
            // Arrange
            var content = "test content";
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = ""; // Empty API key
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = false;

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, "OpenRouter API key not configured"));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal("OpenRouter API key not configured", result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }

        [Fact]
        public async Task AnalyzeAsync_EmptyContent_ReturnsError()
        {
            // Arrange
            var content = ""; // Empty content
            var codingStandards = new List<string> { "standard1" };
            var requirements = "test requirements";
            var apiKey = "test-api-key";
            var primaryModel = "gpt-4";
            var fallbackModel = "gpt-3.5-turbo";
            var language = "NET";
            var isFileContent = true; // File content should not be empty

            _mockAIService.Setup(x => x.AnalyzeCodeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent))
                .ReturnsAsync(("", true, "No file content to analyze"));

            // Act
            var result = await _orchestrator.AnalyzeAsync(
                content, codingStandards, requirements, apiKey, primaryModel, fallbackModel, language, isFileContent);

            // Assert
            Assert.Equal("", result.analysis);
            Assert.True(result.error);
            Assert.Equal("No file content to analyze", result.errorMsg);
            Assert.Equal(primaryModel, result.usedModel);
        }
    }
}
