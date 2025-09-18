
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.IO;
using System;
using System.Linq;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Tests
{
    public class DirectoryBrowsingServiceTests
    {
        public class DirectoryBrowsingFixture : IDisposable
        {
            public string TempDirectory { get; }
            public string TestDir { get; }
            public string SubDir { get; }
            public string GitDir { get; }

            public DirectoryBrowsingFixture()
            {
                TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(TempDirectory);

                TestDir = Path.Combine(TempDirectory, "test_dir");
                Directory.CreateDirectory(TestDir);

                GitDir = Path.Combine(TestDir, ".git");
                Directory.CreateDirectory(GitDir);

                SubDir = Path.Combine(TestDir, "sub_dir");
                Directory.CreateDirectory(SubDir);

                File.WriteAllText(Path.Combine(SubDir, "test.cs"), "");
                File.WriteAllText(Path.Combine(TestDir, "test.py"), "");
                File.WriteAllText(Path.Combine(TestDir, "test.txt"), "");
                File.WriteAllText(Path.Combine(TestDir, "image.jpg"), "");
            }

            public void Dispose()
            {
                Directory.Delete(TempDirectory, true);
            }
        }

        public class DirectoryBrowsingServiceTests_WithFixture : IClassFixture<DirectoryBrowsingFixture>
        {
            private readonly DirectoryBrowsingFixture _fixture;
            private readonly DirectoryBrowsingService _directoryBrowsingService;
            private readonly Mock<ILogger<DirectoryBrowsingService>> _mockLogger;

            public DirectoryBrowsingServiceTests_WithFixture(DirectoryBrowsingFixture fixture)
            {
                _fixture = fixture;
                _mockLogger = new Mock<ILogger<DirectoryBrowsingService>>();
                _directoryBrowsingService = new DirectoryBrowsingService(_mockLogger.Object);
            }

            [Fact]
            public void BrowseDirectory_WithValidPath_ReturnsContent()
            {
                // Arrange
                var path = _fixture.TestDir;

                // Act
                var response = _directoryBrowsingService.BrowseDirectory(path);

                // Assert
                Assert.Null(response.Error);
                Assert.True(response.IsGitRepository);
                Assert.Equal(path, response.CurrentPath);
                Assert.Equal(2, response.Directories.Count);
                Assert.Contains(response.Directories, d => d.Name == "sub_dir");
                Assert.Contains(response.Directories, d => d.Name == ".git");
                Assert.Equal(2, response.Files.Count);
                Assert.Contains(response.Files, f => f.Name == "test.py");
                Assert.Contains(response.Files, f => f.Name == "test.txt");
            }

            [Fact]
            public void BrowseDirectory_WithNonExistentPath_ReturnsError()
            {
                // Arrange
                var path = Path.Combine(_fixture.TempDirectory, "non-existent");

                // Act
                var response = _directoryBrowsingService.BrowseDirectory(path);

                // Assert
                Assert.NotNull(response.Error);
                Assert.Equal("Directory not found", response.Error);
            }

            [Fact]
            public void IsGitRepository_WithGitDir_ReturnsTrue()
            {
                // Arrange
                var path = _fixture.TestDir;

                // Act
                var isGitRepo = _directoryBrowsingService.IsGitRepository(path);

                // Assert
                Assert.True(isGitRepo);
            }

            [Fact]
            public void IsGitRepository_WithoutGitDir_ReturnsFalse()
            {
                // Arrange
                var path = _fixture.SubDir;

                // Act
                var isGitRepo = _directoryBrowsingService.IsGitRepository(path);

                // Assert
                Assert.False(isGitRepo);
            }

            [Fact]
            public void GetParentPath_WithRegularPath_ReturnsParent()
            {
                // Arrange
                var path = _fixture.SubDir;
                var expectedParent = _fixture.TestDir;

                // Act
                var parentPath = _directoryBrowsingService.GetParentPath(path);

                // Assert
                Assert.Equal(expectedParent, parentPath);
            }

            [Fact]
            public void ValidateDirectoryPath_WithValidPath_ReturnsTrue()
            {
                // Arrange
                var path = _fixture.TestDir;

                // Act
                var (isValid, error) = _directoryBrowsingService.ValidateDirectoryPath(path);

                // Assert
                Assert.True(isValid);
                Assert.Null(error);
            }

            [Fact]
            public void ValidateDirectoryPath_WithNonExistentPath_ReturnsFalse()
            {
                // Arrange
                var path = Path.Combine(_fixture.TempDirectory, "non-existent");

                // Act
                var (isValid, error) = _directoryBrowsingService.ValidateDirectoryPath(path);

                // Assert
                Assert.False(isValid);
                Assert.Equal($"Directory not found: {path}", error);
            }

            [Fact]
            public void ValidateDirectoryPath_WithEmptyPath_ReturnsFalse()
            {
                // Arrange
                var path = "";

                // Act
                var (isValid, error) = _directoryBrowsingService.ValidateDirectoryPath(path);

                // Assert
                Assert.False(isValid);
                Assert.Equal("Directory path cannot be empty", error);
            }
        [Fact]
            public void GetRootDrives_ReturnsDrives()
            {
                // Act
                var drives = _directoryBrowsingService.GetRootDrives();

                // Assert
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    Assert.NotEmpty(drives);
                }
                else
                {
                    Assert.Single(drives);
                    Assert.Equal("/", drives[0].Name);
                }
            }

            [Fact]
            public void GetParentPath_WithRootPath_ReturnsNull()
            {
                // Arrange
                var path = (Environment.OSVersion.Platform == PlatformID.Win32NT) ? "C:\\" : "/";

                // Act
                var parentPath = _directoryBrowsingService.GetParentPath(path);

                // Assert
                Assert.Null(parentPath);
            }
        }
    }
}
