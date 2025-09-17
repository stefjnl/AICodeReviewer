using AICodeReviewer.Web.Domain.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for managing git repositories and operations
/// </summary>
public class RepositoryManagementService : IRepositoryManagementService
{
    private readonly ILogger<RepositoryManagementService> _logger;

    public RepositoryManagementService(ILogger<RepositoryManagementService> logger)
    {
        _logger = logger;
    }

    public (string branchInfo, bool isError) DetectRepository(string startPath)
    {
        try
        {
            // Discover git repository starting from the given path
            var repositoryPath = Repository.Discover(startPath);

            if (string.IsNullOrEmpty(repositoryPath))
            {
                return ("No git repository found", false);
            }

            using (var repo = new Repository(repositoryPath))
            {
                // Handle empty repository (no commits yet)
                if (repo.Head.Tip == null)
                {
                    return ($"{repo.Head.FriendlyName} (no commits)", false);
                }

                // Handle detached HEAD state
                if (repo.Head.IsRemote)
                {
                    var sha = repo.Head.Tip?.Sha.Substring(0, 7) ?? "unknown";
                    return ($"Detached HEAD ({sha})", false);
                }

                // Normal branch state
                return (repo.Head.FriendlyName, false);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Repository access denied at path: {Path}", startPath);
            return ("Repository access denied", true);
        }
        catch (LibGit2SharpException ex)
        {
            _logger.LogError(ex, "Git detection unavailable at path: {Path}", startPath);
            return ("Git detection unavailable", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git detection error at path: {Path}", startPath);
            return ("Git detection error", true);
        }
    }

    public (string diff, bool isError) ExtractDiff(string repositoryPath)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Get both staged and unstaged changes
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree, 
                DiffTargets.Index | DiffTargets.WorkingDirectory);
            
            var diffContent = diff.Content;
            
            // Simple size check - 200KB limit
            if (diffContent.Length > 204800) // 100KB
            {
                _logger.LogWarning("Diff too large: {Size} bytes in repository: {Path}", diffContent.Length, repositoryPath);
                return ($"Diff too large ({diffContent.Length} bytes > 100KB). Commit some changes first.", true);
            }
            
            // Include current branch information
            var branchInfo = $"Branch: {repo.Head.FriendlyName}\n\n";
            
            return string.IsNullOrEmpty(diffContent) 
                ? (branchInfo + "No changes detected.", false)  // Empty repo graceful handling
                : (branchInfo + diffContent, false);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Not a git repository at path: {Path}", repositoryPath);
            return ("Not a git repository.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git error while extracting diff from: {Path}", repositoryPath);
            return ($"Git error: {ex.Message}", true);
        }
    }

    public (string diff, bool isError) GetCommitDiff(string repositoryPath, string commitId)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Lookup commit by ID (full or partial hash)
            var commit = repo.Lookup<Commit>(commitId);
            if (commit == null)
            {
                _logger.LogWarning("Commit '{CommitId}' not found in repository: {Path}", commitId, repositoryPath);
                return ($"Commit '{commitId}' not found.", true);
            }
            
            // Handle initial commit (no parent)
            if (!commit.Parents.Any())
            {
                // Compare against empty tree for initial commit
                var emptyTree = repo.Lookup<Tree>("4b825dc642cb6eb9a060e54bf8d69288fbee4904"); // Empty tree hash
                var diff = repo.Diff.Compare<Patch>(emptyTree, commit.Tree);
                var branchInfo = $"Branch: {repo.Head.FriendlyName}\nCommit: {commit.Sha.Substring(0, 7)} - {commit.MessageShort}\n\n";
                return (branchInfo + diff.Content, false);
            }
            
            // Normal case: compare with parent
            var parentCommit = commit.Parents.First();
            var compareOptions = new CompareOptions
            {
                Similarity = SimilarityOptions.Renames,
                IncludeUnmodified = false
            };
            
            var commitDiff = repo.Diff.Compare<Patch>(parentCommit.Tree, commit.Tree, compareOptions);
            var diffContent = commitDiff.Content;
            
            // Size check - 100KB limit
            if (diffContent.Length > 102400)
            {
                _logger.LogWarning("Commit diff too large: {Size} bytes in repository: {Path}", diffContent.Length, repositoryPath);
                return ($"Commit diff too large ({diffContent.Length} bytes > 100KB).", true);
            }
            
            // Include commit information
            var commitInfo = $"Branch: {repo.Head.FriendlyName}\nCommit: {commit.Sha.Substring(0, 7)} - {commit.MessageShort}\n\n";
            
            return string.IsNullOrEmpty(diffContent)
                ? (commitInfo + "No changes in this commit.", false)
                : (commitInfo + diffContent, false);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Not a git repository at path: {Path}", repositoryPath);
            return ("Not a git repository.", true);
        }
        catch (AmbiguousSpecificationException)
        {
            _logger.LogWarning("Ambiguous commit ID '{CommitId}' in repository: {Path}", commitId, repositoryPath);
            return ($"Ambiguous commit ID '{commitId}'. Please provide a more specific hash.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git error while getting commit diff for '{CommitId}' from: {Path}", commitId, repositoryPath);
            return ($"Git error: {ex.Message}", true);
        }
    }

    public (bool isValid, string? message, string? error) ValidateCommit(string repositoryPath, string commitId)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            var commit = repo.Lookup<Commit>(commitId);
            
            if (commit == null)
            {
                _logger.LogWarning("Commit '{CommitId}' not found in repository: {Path}", commitId, repositoryPath);
                return (false, null, $"Commit '{commitId}' not found");
            }
            
            var message = $"{commit.Sha.Substring(0, 7)} - {commit.MessageShort}";
            _logger.LogInformation("Validated commit '{CommitId}' in repository: {Path} - {Message}", commitId, repositoryPath, message);
            return (true, message, null);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogError("Repository not found at path: {Path}", repositoryPath);
            return (false, null, "Not a git repository");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating commit '{CommitId}' in repository: {Path}", commitId, repositoryPath);
            return (false, null, $"Validation error: {ex.Message}");
        }
    }

    public (bool isValid, string? error) ValidateRepositoryForAnalysis(string repositoryPath)
    {
        try
        {
            var (branchInfo, isGitError) = DetectRepository(repositoryPath);
            
            if (isGitError || branchInfo == "No git repository found")
            {
                _logger.LogWarning("No valid git repository found at path: {Path}", repositoryPath);
                return (false, "No valid git repository found at the specified path. Please select a valid git repository.");
            }
            
            _logger.LogInformation("Validated repository for analysis: {Path} - Branch: {Branch}", repositoryPath, branchInfo);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating repository for analysis at path: {Path}", repositoryPath);
            return (false, $"Repository validation error: {ex.Message}");
        }
    }

    public (string diff, bool isError) ExtractStagedDiff(string repositoryPath)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Get only staged changes (HEAD vs Index)
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index);
            
            var diffContent = diff.Content;
            
            // Simple size check - 100KB limit
            if (diffContent.Length > 102400)
            {
                _logger.LogWarning("Staged diff too large: {Size} bytes in repository: {Path}", diffContent.Length, repositoryPath);
                return ($"Staged diff too large ({diffContent.Length} bytes > 100KB).", true);
            }
            
            // Include current branch information
            var branchInfo = $"Branch: {repo.Head.FriendlyName}\n\n";
            
            return string.IsNullOrEmpty(diffContent)
                ? (branchInfo + "No staged changes detected.", false)  // No staged changes graceful handling
                : (branchInfo + diffContent, false);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Not a git repository at path: {Path}", repositoryPath);
            return ("Not a git repository.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git error while extracting staged diff from: {Path}", repositoryPath);
            return ($"Git error: {ex.Message}", true);
        }
    }

    public (bool hasStaged, string? error) HasStagedChanges(string repositoryPath)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Check if there are any staged changes by comparing HEAD tree with index
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index);
            
            var hasStaged = !string.IsNullOrEmpty(diff.Content);
            
            _logger.LogInformation("Checked for staged changes in repository: {Path} - Has staged: {HasStaged}", repositoryPath, hasStaged);
            return (hasStaged, null);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogError("Repository not found at path: {Path}", repositoryPath);
            return (false, "Not a git repository");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for staged changes in repository: {Path}", repositoryPath);
            return (false, $"Error checking staged changes: {ex.Message}");
        }
    }
}