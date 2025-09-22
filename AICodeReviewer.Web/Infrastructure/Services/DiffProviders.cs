using AICodeReviewer.Web.Domain;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;
/// <summary>
/// Interface for diff provider factories
/// </summary>
public interface IDiffProviderFactory
{
    IDiffProvider CreateProvider(string analysisType);
}

/// <summary>
/// Interface for diff statistics parsers
/// </summary>
public interface IDiffStatisticsParser
{
    (int additions, int deletions, List<string> files) Parse(string diffContent);
}

/// <summary>
/// Interface for diff providers that handle different types of git diff operations
/// </summary>
public interface IDiffProvider
{
    /// <summary>
    /// Validates the input parameters required for this diff type
    /// </summary>
    bool ValidateInputs(string? targetCommit, string? sourceBranch, string? targetBranch);

    /// <summary>
    /// Retrieves the diff content for the specified analysis type
    /// </summary>
    (string diffContent, bool isError, string? error) GetDiff(Repository repo, string? targetCommit, string? sourceBranch, string? targetBranch);
}

/// <summary>
/// Provides diff for uncommitted changes (staged + unstaged)
/// </summary>
public class UncommittedDiffProvider : IDiffProvider
{
    

    public bool ValidateInputs(string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        // No additional validation needed for uncommitted changes
        return true;
    }

    public (string diffContent, bool isError, string? error) GetDiff(Repository repo, string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        try
        {
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            var diffContent = diff.Content;

            // Size check - 200KB limit (same as original ExtractDiff)
            if (diffContent.Length > DiffConstants.MaxUncommittedDiffSize)
            {
                return (string.Empty, true, $"Diff too large ({diffContent.Length} bytes > 200KB). Commit some changes first.");
            }

            return (diffContent, false, null);
        }
        catch (Exception ex)
        {
            return (string.Empty, true, $"Error getting uncommitted diff: {ex.Message}");
        }
    }
}

/// <summary>
/// Provides diff for staged changes only
/// </summary>
public class StagedDiffProvider : IDiffProvider
{
    

    public bool ValidateInputs(string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        // No additional validation needed for staged changes
        return true;
    }

    public (string diffContent, bool isError, string? error) GetDiff(Repository repo, string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        try
        {
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index);

            var diffContent = diff.Content;

            // Size check - 100KB limit
            if (diffContent.Length > DiffConstants.MaxDiffSize)
            {
                return (string.Empty, true, $"Staged diff too large ({diffContent.Length} bytes > 100KB).");
            }

            return (diffContent, false, null);
        }
        catch (Exception ex)
        {
            return (string.Empty, true, $"Error getting staged diff: {ex.Message}");
        }
    }
}

/// <summary>
/// Provides diff for a specific commit
/// </summary>
public class CommitDiffProvider : IDiffProvider
{
    

    private readonly ILogger<CommitDiffProvider> _logger;

    public CommitDiffProvider(ILogger<CommitDiffProvider> logger)
    {
        _logger = logger;
    }

    public bool ValidateInputs(string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        return !string.IsNullOrEmpty(targetCommit);
    }

    public (string diffContent, bool isError, string? error) GetDiff(Repository repo, string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        try
        {
            if (string.IsNullOrEmpty(targetCommit))
            {
                return (string.Empty, true, "Commit ID is required");
            }

            var commit = repo.Lookup<Commit>(targetCommit);
            if (commit == null)
            {
                _logger.LogWarning("Commit '{CommitId}' not found", targetCommit);
                return (string.Empty, true, "Commit not found");
            }

            // Handle initial commit (no parent)
            if (!commit.Parents.Any())
            {
                var emptyTree = repo.Lookup<Tree>("4b825dc642cb6eb9a060e54bf8d69288fbee4904");
                var diff = repo.Diff.Compare<Patch>(emptyTree, commit.Tree);
                var initialCommitDiffContent = diff.Content;

                // Size check - 100KB limit
                if (initialCommitDiffContent.Length > DiffConstants.MaxDiffSize)
                {
                    return (string.Empty, true, $"Commit diff too large ({initialCommitDiffContent.Length} bytes > 100KB).");
                }

                return (initialCommitDiffContent, false, null);
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
            if (diffContent.Length > DiffConstants.MaxDiffSize)
            {
                return (string.Empty, true, $"Commit diff too large ({diffContent.Length} bytes > 100KB).");
            }

            return (diffContent, false, null);
        }
        catch (AmbiguousSpecificationException)
        {
            return (string.Empty, true, $"Ambiguous commit ID '{targetCommit}'. Please provide a more specific hash.");
        }
        catch (Exception ex)
        {
            return (string.Empty, true, $"Error getting commit diff: {ex.Message}");
        }
    }
}

/// <summary>
/// Provides diff for branch comparison (pull request style)
/// </summary>
public class PullRequestDiffProvider : IDiffProvider
{
    

    private readonly ILogger<PullRequestDiffProvider> _logger;

    public PullRequestDiffProvider(ILogger<PullRequestDiffProvider> logger)
    {
        _logger = logger;
    }

    public bool ValidateInputs(string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        if (string.IsNullOrEmpty(sourceBranch) || string.IsNullOrEmpty(targetBranch))
        {
            return false;
        }

        if (sourceBranch == targetBranch)
        {
            return false;
        }

        return true;
    }

    public (string diffContent, bool isError, string? error) GetDiff(Repository repo, string? targetCommit, string? sourceBranch, string? targetBranch)
    {
        try
        {
            if (string.IsNullOrEmpty(sourceBranch) || string.IsNullOrEmpty(targetBranch))
            {
                return (string.Empty, true, "Both source and target branches are required");
            }

            if (sourceBranch == targetBranch)
            {
                return (string.Empty, true, "Source and target branches cannot be the same");
            }

            var sourceBranchObj = repo.Branches[sourceBranch];
            var targetBranchObj = repo.Branches[targetBranch];

            if (sourceBranchObj == null)
            {
                _logger.LogWarning("Source branch '{SourceBranch}' not found", sourceBranch);
                return (string.Empty, true, $"Source branch '{sourceBranch}' not found.");
            }

            if (targetBranchObj == null)
            {
                _logger.LogWarning("Target branch '{TargetBranch}' not found", targetBranch);
                return (string.Empty, true, $"Target branch '{targetBranch}' not found.");
            }

            if (sourceBranchObj.Tip == null)
            {
                return (string.Empty, true, $"Source branch '{sourceBranch}' has no commits.");
            }

            if (targetBranchObj.Tip == null)
            {
                return (string.Empty, true, $"Target branch '{targetBranch}' has no commits.");
            }

            var compareOptions = new CompareOptions
            {
                Similarity = SimilarityOptions.Renames,
                IncludeUnmodified = false
            };

            var branchDiff = repo.Diff.Compare<Patch>(targetBranchObj.Tip.Tree, sourceBranchObj.Tip.Tree, compareOptions);
            var diffContent = branchDiff.Content;

            // Size check - 100KB limit
            if (diffContent.Length > DiffConstants.MaxDiffSize)
            {
                return (string.Empty, true, $"Branch diff too large ({diffContent.Length} bytes > 100KB).");
            }

            return (diffContent, false, null);
        }
        catch (Exception ex)
        {
            return (string.Empty, true, $"Error getting branch diff: {ex.Message}");
        }
    }
}

/// <summary>
/// Factory for creating diff providers based on analysis type
/// </summary>
public class DiffProviderFactory : IDiffProviderFactory
{
    private readonly ILogger<CommitDiffProvider> _commitLogger;
    private readonly ILogger<PullRequestDiffProvider> _prLogger;

    public DiffProviderFactory(
        ILogger<CommitDiffProvider> commitLogger,
        ILogger<PullRequestDiffProvider> prLogger)
    {
        _commitLogger = commitLogger;
        _prLogger = prLogger;
    }

    public IDiffProvider CreateProvider(string analysisType)
    {
        return analysisType.ToLower() switch
        {
            "uncommitted" => new UncommittedDiffProvider(),
            "staged" => new StagedDiffProvider(),
            "commit" => new CommitDiffProvider(_commitLogger),
            "pullrequest" => new PullRequestDiffProvider(_prLogger),
            _ => throw new ArgumentException($"Invalid analysis type: {analysisType}")
        };
    }
}