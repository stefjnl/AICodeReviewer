using AICodeReviewer.Web.Domain;
using LibGit2Sharp;

namespace AICodeReviewer.Web.Domain.Interfaces;

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