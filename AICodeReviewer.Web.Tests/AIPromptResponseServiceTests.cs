using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.Collections.Generic;
using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Tests
{
    public class AIPromptResponseServiceTests
    {
        private readonly Mock<ILogger<AIPromptResponseService>> _mockLogger;
        private readonly AIPromptResponseService _responseService;

        public AIPromptResponseServiceTests()
        {
            _mockLogger = new Mock<ILogger<AIPromptResponseService>>();
            _responseService = new AIPromptResponseService(_mockLogger.Object);
        }

        [Fact]
        public void ParseAIResponse_WithEmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            var rawResponse = "";

            // Act
            var result = _responseService.ParseAIResponse(rawResponse);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ParseAIResponse_WithNumberedIssues_ParsesCorrectly()
        {
            // Arrange
            var rawResponse = "1. This is a critical issue in file.cs line 10. Suggestion: fix it.\n2. This is a warning.";

            // Act
            var result = _responseService.ParseAIResponse(rawResponse);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(Severity.Critical, result[0].Severity);
            Assert.Equal("file.cs", result[0].FilePath);
            Assert.Equal(10, result[0].LineNumber);
            Assert.Equal("fix it", result[0].Suggestion);
            Assert.Equal(Severity.Warning, result[1].Severity);
        }

        [Fact]
        public void ParseAIResponse_WithBulletedIssues_ParsesCorrectly()
        {
            // Arrange
            var rawResponse = "- This is a style issue.\n- This is a suggestion.";

            // Act
            var result = _responseService.ParseAIResponse(rawResponse);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(Severity.Style, result[0].Severity);
            Assert.Equal(Severity.Suggestion, result[1].Severity);
        }

        [Fact]
        public void ParseAIResponse_WithNoRecognizedFormat_ReturnsGeneralFeedback()
        {
            // Arrange
            var rawResponse = "This is a general feedback.";

            // Act
            var result = _responseService.ParseAIResponse(rawResponse);

            // Assert
            Assert.Single(result);
            Assert.Equal(Category.General, result[0].Category);
            Assert.Equal("This is a general feedback.", result[0].Message);
        }
    }
}