
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Tests
{
    public class DocumentManagementServiceTests
    {
        public class DocumentFolderFixture : IDisposable
        {
            public string TempDirectory { get; }
            public string DocumentsPath { get; }

            public DocumentFolderFixture()
            {
                TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(TempDirectory);

                DocumentsPath = Path.Combine(TempDirectory, "Documents");
                Directory.CreateDirectory(DocumentsPath);

                File.WriteAllText(Path.Combine(DocumentsPath, "01-test-doc.md"), "Test content 1");
                File.WriteAllText(Path.Combine(DocumentsPath, "02_another-doc.md"), "Test content 2");
            }

            public void Dispose()
            {
                Directory.Delete(TempDirectory, true);
            }
        }

        public class DocumentManagementServiceTests_WithFixture : IClassFixture<DocumentFolderFixture>
        {
            private readonly DocumentFolderFixture _fixture;
            private readonly DocumentManagementService _documentManagementService;
            private readonly Mock<ILogger<DocumentManagementService>> _mockLogger;

            public DocumentManagementServiceTests_WithFixture(DocumentFolderFixture fixture)
            {
                _fixture = fixture;
                _mockLogger = new Mock<ILogger<DocumentManagementService>>();
                _documentManagementService = new DocumentManagementService(_mockLogger.Object);
            }

            [Fact]
            public void ScanDocumentsFolder_WithValidFolder_ReturnsFiles()
            {
                // Act
                var (files, isError) = _documentManagementService.ScanDocumentsFolder(_fixture.DocumentsPath);

                // Assert
                Assert.False(isError);
                Assert.Equal(2, files.Count);
                Assert.Equal("01-test-doc", files[0]);
                Assert.Equal("02_another-doc", files[1]);
            }

            [Fact]
            public void LoadDocument_WithValidFile_ReturnsContent()
            {
                // Act
                var (content, isError) = _documentManagementService.LoadDocument("01-test-doc", _fixture.DocumentsPath);

                // Assert
                Assert.False(isError);
                Assert.Equal("Test content 1", content);
            }

            [Fact]
            public async Task LoadDocumentAsync_WithValidFile_ReturnsContent()
            {
                // Act
                var (content, isError) = await _documentManagementService.LoadDocumentAsync("01-test-doc", _fixture.DocumentsPath);

                // Assert
                Assert.False(isError);
                Assert.Equal("Test content 1", content);
            }

            [Fact]
            public void GetDocumentDisplayName_WithFileName_ReturnsDisplayName()
            {
                // Arrange
                var fileName = "01-test_doc-name";

                // Act
                var displayName = _documentManagementService.GetDocumentDisplayName(fileName);

                // Assert
                Assert.Equal("01 test doc name", displayName);
            }

            [Fact]
            public void ValidateDocumentsFolder_WithValidFolder_ReturnsTrue()
            {
                // Act
                var (isValid, normalizedPath, error) = _documentManagementService.ValidateDocumentsFolder(_fixture.DocumentsPath);

                // Assert
                Assert.True(isValid);
                Assert.Equal(_fixture.DocumentsPath, normalizedPath);
                Assert.Null(error);
            }
        }
    }
}
