
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AICodeReviewer.Web.Infrastructure.Services;
using System.IO;
using System;
using LibGit2Sharp;
using System.Linq;

namespace AICodeReviewer.Web.Tests
{
    public class RepositoryManagementServiceTests
    {
        public class GitRepositoryFixture : IDisposable
        {
            public string RepoPath { get; }
            public Repository Repo { get; }
            public Commit InitialCommit { get; }

            public GitRepositoryFixture()
            {
                RepoPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Repository.Init(RepoPath);
                Repo = new Repository(RepoPath);

                File.WriteAllText(Path.Combine(RepoPath, "test.txt"), "initial content");
                Commands.Stage(Repo, "test.txt");
                var author = new Signature("test", "test@test.com", DateTimeOffset.Now);
                InitialCommit = Repo.Commit("Initial commit", author, author);
            }

            public void Dispose()
            {
                Repo.Dispose();
                // Recursively delete the directory
                var directory = new DirectoryInfo(RepoPath) { Attributes = FileAttributes.Normal };
                foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }
                directory.Delete(true);
            }
        }

        public class RepositoryManagementServiceTests_WithFixture : IClassFixture<GitRepositoryFixture>
        {
            private readonly GitRepositoryFixture _fixture;
            private readonly RepositoryManagementService _repositoryManagementService;
            private readonly Mock<ILogger<RepositoryManagementService>> _mockLogger;

            public RepositoryManagementServiceTests_WithFixture(GitRepositoryFixture fixture)
            {
                _fixture = fixture;
                _mockLogger = new Mock<ILogger<RepositoryManagementService>>();
                _repositoryManagementService = new RepositoryManagementService(
                    _mockLogger.Object);
                _fixture.Repo.Reset(ResetMode.Hard);
            }

            [Fact]
            public void DetectRepository_WithValidRepo_ReturnsBranchName()
            {
                // Act
                var (branchInfo, isError) = _repositoryManagementService.DetectRepository(_fixture.RepoPath);

                // Assert
                Assert.False(isError);
                Assert.True(branchInfo == "main" || branchInfo == "master");
            }

            [Fact]
            public void DetectRepository_WithInvalidPath_ReturnsNoRepoFound()
            {
                // Arrange
                var invalidPath = Path.GetTempPath();

                // Act
                var (branchInfo, isError) = _repositoryManagementService.DetectRepository(invalidPath);

                // Assert
                Assert.False(isError);
                Assert.Equal("No git repository found", branchInfo);
            }

            [Fact]
            public void ExtractDiff_WithUncommittedChanges_ReturnsDiff()
            {
                // Arrange
                File.WriteAllText(Path.Combine(_fixture.RepoPath, "test.txt"), "new content");

                // Act
                var (diff, isError) = _repositoryManagementService.ExtractDiff(_fixture.RepoPath);

                // Assert
                Assert.False(isError);
                Assert.Contains("-initial content", diff);
                Assert.Contains("+new content", diff);
            }

            [Fact]
            public void GetCommitDiff_WithValidCommit_ReturnsDiff()
            {
                // Arrange
                var commitId = _fixture.InitialCommit.Sha;

                // Act
                var (diff, isError) = _repositoryManagementService.GetCommitDiff(_fixture.RepoPath, commitId);

                // Assert
                Assert.False(isError);
                Assert.Contains("+initial content", diff);
            }
        [Fact]
            public void ValidateCommit_WithValidCommit_ReturnsTrue()
            {
                // Arrange
                var commitId = _fixture.InitialCommit.Sha;

                // Act
                var (isValid, message, error) = _repositoryManagementService.ValidateCommit(_fixture.RepoPath, commitId);

                // Assert
                Assert.True(isValid);
                Assert.NotNull(message);
                Assert.Null(error);
            }

            [Fact]
            public void ValidateRepositoryForAnalysis_WithValidRepo_ReturnsTrue()
            {
                // Act
                var (isValid, error) = _repositoryManagementService.ValidateRepositoryForAnalysis(_fixture.RepoPath);

                // Assert
                Assert.True(isValid);
                Assert.Null(error);
            }

            [Fact]
            public void ExtractStagedDiff_WithStagedChanges_ReturnsDiff()
            {
                // Arrange
                File.WriteAllText(Path.Combine(_fixture.RepoPath, "staged.txt"), "staged content");
                Commands.Stage(_fixture.Repo, "staged.txt");

                // Act
                var (diff, isError) = _repositoryManagementService.ExtractStagedDiff(_fixture.RepoPath);

                // Assert
                Assert.False(isError);
                Assert.Contains("+staged content", diff);
            }

            [Fact]
            public void HasStagedChanges_WithStagedChanges_ReturnsTrue()
            {
                // Arrange
                File.WriteAllText(Path.Combine(_fixture.RepoPath, "staged.txt"), "staged content");
                Commands.Stage(_fixture.Repo, "staged.txt");

                // Act
                var (hasStaged, error) = _repositoryManagementService.HasStagedChanges(_fixture.RepoPath);

                // Assert
                Assert.True(hasStaged);
                Assert.Null(error);

                // Cleanup
                _fixture.Repo.Reset(ResetMode.Hard);
            }

            [Fact]
            public void GetAnalysisOptions_WithRepo_ReturnsOptions()
            {
                // Act
                var (commits, branches, modifiedFiles, stagedFiles) = _repositoryManagementService.GetAnalysisOptions(_fixture.RepoPath);

                // Assert
                Assert.Single(commits);
                Assert.Single(branches);
                Assert.Empty(modifiedFiles);
                Assert.Empty(stagedFiles);
            }

            [Fact]
            public void PreviewChanges_WithUncommittedChanges_ReturnsSummary()
            {
                // Arrange
                File.WriteAllText(Path.Combine(_fixture.RepoPath, "test.txt"), "new content");

                // Act
                var (summary, isValid, error) = _repositoryManagementService.PreviewChanges(_fixture.RepoPath, "uncommitted");

                // Assert
                Assert.True(isValid);
                Assert.Null(error);
                var summaryType = summary.GetType();
                var filesModified = (int)summaryType.GetProperty("filesModified").GetValue(summary, null);
                var additions = (int)summaryType.GetProperty("additions").GetValue(summary, null);
                var deletions = (int)summaryType.GetProperty("deletions").GetValue(summary, null);

                Assert.Equal(1, filesModified);
                Assert.Equal(1, additions);
                Assert.Equal(1, deletions);
            }
        }
    }
}

