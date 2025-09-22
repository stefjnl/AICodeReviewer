namespace AICodeReviewer.Web.Domain;

/// <summary>
/// Constants related to diff operations and size limits
/// </summary>
public static class DiffConstants
{
    /// <summary>
    /// Maximum size for uncommitted diff (200KB)
    /// </summary>
    public const int MaxUncommittedDiffSize = 204800;

    /// <summary>
    /// Maximum size for staged, commit, and branch diffs (100KB)
    /// </summary>
    public const int MaxDiffSize = 102400;

    /// <summary>
    /// Git empty tree SHA used for initial commits
    /// </summary>
    public const string GitEmptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";
}