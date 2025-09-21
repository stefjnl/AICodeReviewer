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

    /// <summary>
    /// Get analysis options for a repository
    /// </summary>
    public (List<object> commits, List<object> branches, List<string> modifiedFiles, List<string> stagedFiles) GetAnalysisOptions(string repositoryPath)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Get last 10 commits
            var commits = repo.Commits.Take(10).Select(c => new
            {
                id = c.Sha.Substring(0, 7),
                message = c.MessageShort,
                author = c.Author.Name,
                date = c.Author.When.ToString("yyyy-MM-dd HH:mm")
            }).ToList<object>();

            // Get branches
            var branches = repo.Branches.Where(b => !b.IsRemote).Select(b => new
            {
                name = b.FriendlyName,
                isCurrent = b.IsCurrentRepositoryHead
            }).ToList<object>();

            // Get modified/untracked files
            var status = repo.RetrieveStatus();
            var modifiedFiles = status.Where(s => s.State != FileStatus.Unaltered)
                                    .Select(s => s.FilePath)
                                    .ToList();

            // Get staged files - any files with changes
            var stagedFiles = status.Where(s => s.State != FileStatus.Unaltered)
                                  .Select(s => s.FilePath)
                                  .ToList();

            _logger.LogInformation("Retrieved analysis options for repository: {Path}", repositoryPath);
            return (commits, branches, modifiedFiles, stagedFiles);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Repository not found at path: {Path}", repositoryPath);
            return (new List<object>(), new List<object>(), new List<string>(), new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis options for repository: {Path}", repositoryPath);
            return (new List<object>(), new List<object>(), new List<string>(), new List<string>());
        }
    }

    /// <summary>
    /// Get branch diff between two branches
    /// </summary>
    public (string diff, bool isError) GetBranchDiff(string repositoryPath, string sourceBranch, string targetBranch)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            
            // Find the source and target branches
            var sourceBranchObj = repo.Branches[sourceBranch];
            var targetBranchObj = repo.Branches[targetBranch];
            
            if (sourceBranchObj == null)
            {
                _logger.LogWarning("Source branch '{SourceBranch}' not found in repository: {Path}", sourceBranch, repositoryPath);
                return ($"Source branch '{sourceBranch}' not found.", true);
            }
            
            if (targetBranchObj == null)
            {
                _logger.LogWarning("Target branch '{TargetBranch}' not found in repository: {Path}", targetBranch, repositoryPath);
                return ($"Target branch '{targetBranch}' not found.", true);
            }
            
            // Handle case where branches might not have commits yet
            if (sourceBranchObj.Tip == null)
            {
                _logger.LogWarning("Source branch '{SourceBranch}' has no commits in repository: {Path}", sourceBranch, repositoryPath);
                return ($"Source branch '{sourceBranch}' has no commits.", true);
            }
            
            if (targetBranchObj.Tip == null)
            {
                _logger.LogWarning("Target branch '{TargetBranch}' has no commits in repository: {Path}", targetBranch, repositoryPath);
                return ($"Target branch '{targetBranch}' has no commits.", true);
            }
            
            // Compare the two branches
            var compareOptions = new CompareOptions
            {
                Similarity = SimilarityOptions.Renames,
                IncludeUnmodified = false
            };
            
            var branchDiff = repo.Diff.Compare<Patch>(targetBranchObj.Tip.Tree, sourceBranchObj.Tip.Tree, compareOptions);
            var diffContent = branchDiff.Content;
            
            // Size check - 100KB limit
            if (diffContent.Length > 102400)
            {
                _logger.LogWarning("Branch diff too large: {Size} bytes in repository: {Path}", diffContent.Length, repositoryPath);
                return ($"Branch diff too large ({diffContent.Length} bytes > 100KB).", true);
            }
            
            // Include branch information
            var branchInfo = $"Comparing {sourceBranch} → {targetBranch}\n" +
                            $"Source: {sourceBranchObj.Tip.Sha.Substring(0, 7)} - {sourceBranchObj.Tip.MessageShort}\n" +
                            $"Target: {targetBranchObj.Tip.Sha.Substring(0, 7)} - {targetBranchObj.Tip.MessageShort}\n\n";
            
            return string.IsNullOrEmpty(diffContent)
                ? (branchInfo + "No changes between branches.", false)
                : (branchInfo + diffContent, false);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Not a git repository at path: {Path}", repositoryPath);
            return ("Not a git repository.", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git error while getting branch diff between '{SourceBranch}' and '{TargetBranch}' from: {Path}", sourceBranch, targetBranch, repositoryPath);
            return ($"Git error: {ex.Message}", true);
        }
    }

    /// <summary>
    /// Preview changes for analysis configuration
    /// </summary>
    public (object changesSummary, bool isValid, string? error) PreviewChanges(string repositoryPath, string analysisType, string? targetCommit = null, string? sourceBranch = null, string? targetBranch = null)
    {
        try
        {
            using var repo = new Repository(repositoryPath);
            string diffContent;
            int totalAdditions = 0;
            int totalDeletions = 0;
            List<string> files = new List<string>();

            switch (analysisType.ToLower())
            {
                case "uncommitted":
                    var uncommittedDiff = repo.Diff.Compare<Patch>(
                        repo.Head.Tip?.Tree,
                        DiffTargets.Index | DiffTargets.WorkingDirectory);
                    diffContent = uncommittedDiff.Content;
                    ExtractFileStats(uncommittedDiff, ref totalAdditions, ref totalDeletions, files);
                    break;

                case "staged":
                    var stagedDiff = repo.Diff.Compare<Patch>(
                        repo.Head.Tip?.Tree,
                        DiffTargets.Index);
                    diffContent = stagedDiff.Content;
                    ExtractFileStats(stagedDiff, ref totalAdditions, ref totalDeletions, files);
                    break;

                case "commit":
                    if (string.IsNullOrEmpty(targetCommit))
                    {
                        return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Commit ID is required for commit analysis");
                    }

                    var commit = repo.Lookup<Commit>(targetCommit);
                    if (commit == null)
                    {
                        return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Commit not found");
                    }

                    var commitDiff = GetCommitDiff(repositoryPath, targetCommit);
                    diffContent = commitDiff.diff;
                    if (!commitDiff.isError)
                    {
                        // Parse commit diff for basic stats
                        var lines = diffContent.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("+") && !line.StartsWith("+++")) totalAdditions++;
                            else if (line.StartsWith("-") && !line.StartsWith("---")) totalDeletions++;
                        }
                    }
                    break;

                case "pullrequest":
                    if (string.IsNullOrEmpty(sourceBranch) || string.IsNullOrEmpty(targetBranch))
                    {
                        return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Both source and target branches are required for branch comparison");
                    }

                    if (sourceBranch == targetBranch)
                    {
                        return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Source and target branches cannot be the same");
                    }

                    var branchDiffResult = GetBranchDiff(repositoryPath, sourceBranch, targetBranch);
                    diffContent = branchDiffResult.diff;
                    if (branchDiffResult.isError)
                    {
                        return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, branchDiffResult.diff);
                    }

                    // Parse branch diff for basic stats
                    var branchLines = diffContent.Split('\n');
                    foreach (var line in branchLines)
                    {
                        if (line.StartsWith("+") && !line.StartsWith("+++")) totalAdditions++;
                        else if (line.StartsWith("-") && !line.StartsWith("---")) totalDeletions++;
                        else if (line.StartsWith("diff --git") || (line.StartsWith("---") && line.Contains("a/")))
                        {
                            // Extract file names from diff lines
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"diff --git a/(.*) b/(.*)");
                            if (match.Success)
                            {
                                files.Add(match.Groups[1].Value);
                            }
                        }
                    }
                    break;

                default:
                    return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Invalid analysis type");
            }

            if (string.IsNullOrEmpty(diffContent))
            {
                return (new
                {
                    filesModified = 0,
                    additions = 0,
                    deletions = 0,
                    fileList = new List<string>()
                }, true, null);
            }

            var changesSummary = new
            {
                filesModified = files.Count,
                additions = totalAdditions,
                deletions = totalDeletions,
                fileList = files
            };

            return (changesSummary, true, null);
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogWarning("Repository not found at path: {Path}", repositoryPath);
            return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, "Not a git repository");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing changes for repository: {Path}", repositoryPath);
            return (new { filesModified = 0, additions = 0, deletions = 0, fileList = new List<string>() }, false, $"Error: {ex.Message}");
        }
    }

    private void ExtractFileStats(Patch diff, ref int additions, ref int deletions, List<string> files)
    {
        foreach (var patchEntry in diff)
        {
            files.Add(patchEntry.Path);
            
            // Count lines manually from patch content
            var lines = patchEntry.Patch.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("+") && !line.StartsWith("+++"))
                    additions++;
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                    deletions++;
            }
        }
    }
}