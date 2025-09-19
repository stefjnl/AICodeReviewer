
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.IO;
using System;
using System.Linq;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;


using AICodeReviewer.Web.Infrastructure.Extensions;

namespace AICodeReviewer.Web.Tests
{

    

    public class ValidationServiceTests
    {
        private readonly Mock<ILogger<ValidationService>> _mockLogger;
        private readonly Mock<IRepositoryManagementService> _mockRepositoryService;
        private readonly Mock<IPathValidationService> _mockPathService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly TestSession _testSession;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly ValidationService _validationService;

        public ValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ValidationService>>();
            _mockRepositoryService = new Mock<IRepositoryManagementService>();
            _mockPathService = new Mock<IPathValidationService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _testSession = new TestSession();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockWebHostEnvironment.Setup(e => e.ContentRootPath).Returns("C:\\git\\AICodeReviewer");

            _validationService = new ValidationService(
                _mockLogger.Object,
                _mockRepositoryService.Object,
                _mockPathService.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_InvalidApiKey_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest();
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("");

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("API key not configured", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_NoSelectedDocuments_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest();
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("No coding standards selected", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_SingleFile_MissingFilePath_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.SingleFile };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("File path is required for single file analysis", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_SingleFile_InvalidFilePath_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.SingleFile, FilePath = "invalid/path" };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockPathService.Setup(p => p.ValidateSingleFilePath(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(("path", false, "Invalid path"));

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Invalid path", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_SingleFile_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.SingleFile, FilePath = "valid/path" };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockPathService.Setup(p => p.ValidateSingleFilePath(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(("resolved/path", true, null));

            // Act
            var (isValid, error, resolvedPath) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);
            Assert.Equal("resolved/path", resolvedPath);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_GitBased_InvalidRepo_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.Uncommitted };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockRepositoryService.Setup(r => r.ValidateRepositoryForAnalysis(It.IsAny<string>()))
                .Returns((false, "Invalid repo"));

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Invalid repo", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_Commit_MissingCommitId_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.Commit };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockRepositoryService.Setup(r => r.ValidateRepositoryForAnalysis(It.IsAny<string>()))
                .Returns((true, null));

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Commit ID is required for commit analysis", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_Staged_NoStagedChanges_ReturnsFalse()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.Staged };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockRepositoryService.Setup(r => r.ValidateRepositoryForAnalysis(It.IsAny<string>()))
                .Returns((true, null));
            _mockRepositoryService.Setup(r => r.HasStagedChanges(It.IsAny<string>()))
                .Returns((false, "No staged changes"));

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.False(isValid);
            Assert.Equal("No staged changes found. Use 'git add' to stage files for analysis.", error);
        }

        [Fact]
        public async Task ValidateAnalysisRequestAsync_GitBased_ValidRequest_ReturnsTrue()
        {
            // Arrange
            var request = new RunAnalysisRequest { AnalysisType = AnalysisType.Uncommitted };
            _mockConfiguration.Setup(c => c["OpenRouter:ApiKey"]).Returns("test_key");
            var documents = new List<string> { "doc1" };
            var documentsJson = System.Text.Json.JsonSerializer.Serialize(documents);
            var documentsBytes = System.Text.Encoding.UTF8.GetBytes(documentsJson);
            _testSession.Set("SelectedDocuments", documentsBytes);
            _mockRepositoryService.Setup(r => r.ValidateRepositoryForAnalysis(It.IsAny<string>()))
                .Returns((true, null));

            // Act
            var (isValid, error, _) = await _validationService.ValidateAnalysisRequestAsync(request, _testSession, _mockWebHostEnvironment.Object);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);
        }
    [Fact]
        public void TestSession_SetAndGet_Works()
        {
            // Arrange
            var session = new TestSession();
            var key = "test_key";
            var value = "test_value";
            var valueBytes = System.Text.Encoding.UTF8.GetBytes(value);

            // Act
            session.Set(key, valueBytes);
            byte[] retrievedValueBytes;
            var result = session.TryGetValue(key, out retrievedValueBytes);

            // Assert
            Assert.True(result);
            Assert.Equal(valueBytes, retrievedValueBytes);
        }
    }
}
