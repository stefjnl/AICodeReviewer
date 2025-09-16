using LibGit2Sharp;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for managing git repositories and operations
/// </summary>
public interface IRepositoryManagementService
{
    /// <summary>
    /// Detect and validate git repository at the specified path
    /// </summary>
    /// <param name="startPath">Starting path for repository detection</param>
    /// <returns>Branch information and error status</returns>
    (string branchInfo, bool isError) DetectRepository(string startPath);

    /// <summary>
    /// Extract git diff for uncommitted changes
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <returns>Diff content and error status</returns>
    (string diff, bool isError) ExtractDiff(string repositoryPath);

    /// <summary>
    /// Extract git diff for a specific commit
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="commitId">Commit hash or identifier</param>
    /// <returns>Diff content and error status</returns>
    (string diff, bool isError) GetCommitDiff(string repositoryPath, string commitId);

    /// <summary>
    /// Validate a git commit exists
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="commitId">Commit hash or identifier</param>
    /// <returns>Validation result and commit message</returns>
    (bool isValid, string? message, string? error) ValidateCommit(string repositoryPath, string commitId);

    /// <summary>
    /// Validate repository path for analysis operations
    /// </summary>
    /// <param name="repositoryPath">Repository path to validate</param>
    /// <returns>Validation result and error message</returns>
    (bool isValid, string? error) ValidateRepositoryForAnalysis(string repositoryPath);
}