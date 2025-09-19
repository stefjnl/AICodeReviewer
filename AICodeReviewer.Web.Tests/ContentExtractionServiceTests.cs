
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.Threading.Tasks;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using System.IO;

namespace AICodeReviewer.Web.Tests
{
    public class ContentExtractionServiceTests
    {
        private readonly Mock<ILogger<ContentExtractionService>> _mockLogger;
        private readonly Mock<IRepositoryManagementService> _mockRepositoryManagementService;
        private readonly ContentExtractionService _contentExtractionService;

        public ContentExtractionServiceTests()
        {
            _mockLogger = new Mock<ILogger<ContentExtractionService>>();
            _mockRepositoryManagementService = new Mock<IRepositoryManagementService>();
            _contentExtractionService = new ContentExtractionService(_mockLogger.Object, _mockRepositoryManagementService.Object);
        }

        [Fact]
        public async Task ExtractContentAsync_SingleFile_ReturnsFileContent()
        {
            // Arrange
            var filePath = Path.GetTempFileName();
            var fileContent = "test content";
            File.WriteAllText(filePath, fileContent);

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.SingleFile, filePath: filePath);

            // Assert
            Assert.False(contentError);
            Assert.True(isFileContent);
            Assert.Equal(fileContent, content);
            Assert.Null(error);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task ExtractContentAsync_Commit_ReturnsCommitDiff()
        {
            // Arrange
            _mockRepositoryManagementService.Setup(s => s.GetCommitDiff(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(("commit diff", false));

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.Commit, commitId: "123");

            // Assert
            Assert.False(contentError);
            Assert.False(isFileContent);
            Assert.Equal("commit diff", content);
            Assert.Null(error);
        }

        [Fact]
        public async Task ExtractContentAsync_Staged_ReturnsStagedDiff()
        {
            // Arrange
            _mockRepositoryManagementService.Setup(s => s.ExtractStagedDiff(It.IsAny<string>()))
                .Returns(("staged diff", false));

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.Staged);

            // Assert
            Assert.False(contentError);
            Assert.False(isFileContent);
            Assert.Equal("staged diff", content);
            Assert.Null(error);
        }

        [Fact]
        public async Task ExtractContentAsync_Uncommitted_ReturnsUncommittedDiff()
        {
            // Arrange
            _mockRepositoryManagementService.Setup(s => s.ExtractDiff(It.IsAny<string>()))
                .Returns(("uncommitted diff", false));

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.Uncommitted);

            // Assert
            Assert.False(contentError);
            Assert.False(isFileContent);
            Assert.Equal("uncommitted diff", content);
            Assert.Null(error);
        }

        [Fact]
        public async Task ExtractContentAsync_RepoError_ReturnsError()
        {
            // Arrange
            _mockRepositoryManagementService.Setup(s => s.ExtractDiff(It.IsAny<string>()))
                .Returns(("git error", true));

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.Uncommitted);

            // Assert
            Assert.True(contentError);
            Assert.Equal("git error", error);
        }

        [Fact]
        public async Task ExtractContentAsync_EmptyContent_ReturnsError()
        {
            // Arrange
            _mockRepositoryManagementService.Setup(s => s.ExtractDiff(It.IsAny<string>()))
                .Returns(("", false));

            // Act
            var (content, contentError, isFileContent, error) = await _contentExtractionService.ExtractContentAsync(
                "/repo", AnalysisType.Uncommitted);

            // Assert
            Assert.True(contentError);
            Assert.Equal("No content extracted", error);
        }
    }
}
