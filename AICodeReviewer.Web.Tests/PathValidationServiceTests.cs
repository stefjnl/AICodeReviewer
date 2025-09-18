using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.IO;
using System;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Tests
{
    public class PathValidationServiceTests
    {
        private readonly Mock<ILogger<PathValidationService>> _mockLogger;
        private readonly PathValidationService _pathValidationService;

        public PathValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<PathValidationService>>();
            _pathValidationService = new PathValidationService(_mockLogger.Object);
        }

        [Fact]
        public void NormalizePath_WhenPathIsEmpty_ReturnsFalse()
        {
            // Arrange
            var path = "";
            var basePath = @"C:\base";

            // Act
            var (normalizedPath, isValid, error) = _pathValidationService.NormalizePath(path, basePath);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Path cannot be empty", error);
        }

        [Fact]
        public void NormalizePath_WhenPathIsRelative_ReturnsNormalizedPath()
        {
            // Arrange
            var path = "test.txt";
            var basePath = @"C:\base";
            var expectedPath = Path.Combine(basePath, path);

            // Act
            var (normalizedPath, isValid, error) = _pathValidationService.NormalizePath(path, basePath);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedPath, normalizedPath);
            Assert.Null(error);
        }

        [Fact]
        public void NormalizePath_WhenPathIsAbsolute_ReturnsSamePath()
        {
            // Arrange
            var path = @"C:\test.txt";
            var basePath = @"C:\base";

            // Act
            var (normalizedPath, isValid, error) = _pathValidationService.NormalizePath(path, basePath);

            // Assert
            Assert.True(isValid);
            Assert.Equal(path, normalizedPath);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateFileExtension_WithSupportedExtension_ReturnsTrue()
        {
            // Arrange
            var path = "test.cs";

            // Act
            var (isSupported, error) = _pathValidationService.ValidateFileExtension(path);

            // Assert
            Assert.True(isSupported);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateFileExtension_WithUnsupportedExtension_ReturnsFalse()
        {
            // Arrange
            var path = "test.txt";

            // Act
            var (isSupported, error) = _pathValidationService.ValidateFileExtension(path);

            // Assert
            Assert.False(isSupported);
            Assert.Contains("Unsupported file type", error);
        }

        [Fact]
        public void ValidateFileExtension_WithNoExtension_ReturnsFalse()
        {
            // Arrange
            var path = "test";

            // Act
            var (isSupported, error) = _pathValidationService.ValidateFileExtension(path);

            // Assert
            Assert.False(isSupported);
            Assert.Contains("Unsupported file type", error);
        }

        [Fact]
        public void ValidateDirectoryExists_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            // Act
            var (isValid, error) = _pathValidationService.ValidateDirectoryExists(tempDir);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);

            // Cleanup
            Directory.Delete(tempDir);
        }

        [Fact]
        public void ValidateDirectoryExists_WithNonExistingDirectory_ReturnsFalse()
        {
            // Arrange
            var path = @"C:\non-existing-dir";

            // Act
            var (isValid, error) = _pathValidationService.ValidateDirectoryExists(path);

            // Assert
            Assert.False(isValid);
            Assert.Equal($"Directory not found: {path}", error);
        }

        [Fact]
        public void ValidateDirectoryExists_WithEmptyPath_ReturnsFalse()
        {
            // Arrange
            var path = "";

            // Act
            var (isValid, error) = _pathValidationService.ValidateDirectoryExists(path);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Directory path cannot be empty", error);
        }
    }

    public class FileSystemFixture : IDisposable
    {
        public string TempDirectory { get; }
        public string RepoPath { get; }
        public string ContentRootPath { get; }

        public FileSystemFixture()
        {
            TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(TempDirectory);

            RepoPath = Path.Combine(TempDirectory, "repo");
            Directory.CreateDirectory(RepoPath);

            ContentRootPath = Path.Combine(TempDirectory, "content");
            Directory.CreateDirectory(ContentRootPath);

            File.WriteAllText(Path.Combine(RepoPath, "main.py"), "print('hello')");
            Directory.CreateDirectory(Path.Combine(RepoPath, "src"));
            File.WriteAllText(Path.Combine(RepoPath, "src", "app.js"), "console.log('hello');");
            File.WriteAllText(Path.Combine(ContentRootPath, "styles.css"), "body { color: red; }");
        }

        public void Dispose()
        {
            Directory.Delete(TempDirectory, true);
        }
    }

    public class PathValidationServiceTests_WithFileSystem : IClassFixture<FileSystemFixture>
    {
        private readonly FileSystemFixture _fixture;
        private readonly PathValidationService _pathValidationService;
        private readonly Mock<ILogger<PathValidationService>> _mockLogger;

        public PathValidationServiceTests_WithFileSystem(FileSystemFixture fixture)
        {
            _fixture = fixture;
            _mockLogger = new Mock<ILogger<PathValidationService>>();
            _pathValidationService = new PathValidationService(_mockLogger.Object);
        }

        [Fact]
        public void ValidateSingleFilePath_WithAbsolutePath_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine(_fixture.RepoPath, "main.py");

            // Act
            var (resolvedPath, isValid, error) = _pathValidationService.ValidateSingleFilePath(filePath, _fixture.RepoPath, _fixture.ContentRootPath);

            // Assert
            Assert.True(isValid);
            Assert.Equal(filePath, resolvedPath);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateSingleFilePath_WithRelativePath_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine("src", "app.js");
            var expectedPath = Path.Combine(_fixture.RepoPath, filePath);

            // Act
            var (resolvedPath, isValid, error) = _pathValidationService.ValidateSingleFilePath(filePath, _fixture.RepoPath, _fixture.ContentRootPath);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedPath, resolvedPath);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateSingleFilePath_WithFileNameInRepoRoot_ReturnsTrue()
        {
            // Arrange
            var filePath = "main.py";
            var expectedPath = Path.Combine(_fixture.RepoPath, filePath);

            // Act
            var (resolvedPath, isValid, error) = _pathValidationService.ValidateSingleFilePath(filePath, _fixture.RepoPath, _fixture.ContentRootPath);

            // Assert
            Assert.True(isValid);
            Assert.Equal(expectedPath, resolvedPath);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateSingleFilePath_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var filePath = "nonexistent.cs";

            // Act
            var (resolvedPath, isValid, error) = _pathValidationService.ValidateSingleFilePath(filePath, _fixture.RepoPath, _fixture.ContentRootPath);

            // Assert
            Assert.False(isValid);
                            Assert.Contains($"File '{filePath}' not found", error);
        }

        [Fact]
        public void ValidateSingleFilePath_WithUnsupportedExtension_ReturnsFalse()
        {
            // Arrange
            var filePath = Path.Combine(_fixture.ContentRootPath, "styles.css");

            // Act
            var (resolvedPath, isValid, error) = _pathValidationService.ValidateSingleFilePath(filePath, _fixture.RepoPath, _fixture.ContentRootPath);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Unsupported file type", error);
        }
    }
}
