using LibGit2Sharp;
using AICodeReviewer.Web.Models;
using System.Threading.Tasks;
using FileSystemItem = AICodeReviewer.Web.Models.FileSystemItem;

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
    
    /// <summary>
    /// Extract git diff for staged changes only
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <returns>Staged diff content and error status</returns>
    (string diff, bool isError) ExtractStagedDiff(string repositoryPath);
    
    /// <summary>
    /// Check if repository has staged changes
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <returns>Validation result and error message</returns>
    (bool hasStaged, string? error) HasStagedChanges(string repositoryPath);

    /// <summary>
    /// Get analysis options for a repository
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <returns>Available analysis options</returns>
    (List<object> commits, List<object> branches, List<string> modifiedFiles, List<string> stagedFiles) GetAnalysisOptions(string repositoryPath);

    /// <summary>
    /// Preview changes for analysis configuration
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="analysisType">Type of analysis to perform</param>
    /// <param name="targetCommit">Specific commit for commit analysis</param>
    /// <param name="sourceBranch">Source branch for pull request analysis</param>
    /// <param name="targetBranch">Target branch for pull request analysis</param>
    /// <returns>Changes summary and validation result</returns>
    (object changesSummary, bool isValid, string? error) PreviewChanges(string repositoryPath, AnalysisType analysisType, string? targetCommit = null, string? sourceBranch = null, string? targetBranch = null);

    /// <summary>
    /// Get file content for single file analysis
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="filePath">Path to the file to read</param>
    /// <returns>File content and error status</returns>
    Task<(string content, bool isError)> GetFileContentAsync(string repositoryPath, string filePath);

    /// <summary>
    /// Get files in a directory within the repository for single file analysis
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="relativePath">Relative path within the repository</param>
    /// <returns>List of files and directories with error status</returns>
    Task<(List<FileSystemItem> files, bool isError)> GetFilesInDirectoryAsync(string repositoryPath, string relativePath = "");
}