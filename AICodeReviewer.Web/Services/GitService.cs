using LibGit2Sharp;
using System;

namespace AICodeReviewer.Web.Services
{
    public static class GitService
    {
        public static (string diff, bool isError) ExtractDiff(string repositoryPath)
        {
            try
            {
                using var repo = new Repository(repositoryPath);
                
                // Get both staged and unstaged changes
                var diff = repo.Diff.Compare<Patch>(
                    repo.Head.Tip?.Tree, 
                    DiffTargets.Index | DiffTargets.WorkingDirectory);
                
                var diffContent = diff.Content;
                
                // Simple size check - 100KB limit
                if (diffContent.Length > 102400) // 100KB
                    return ($"Diff too large ({diffContent.Length} bytes > 100KB). Commit some changes first.", true);
                
                // Include current branch information
                var branchInfo = $"Branch: {repo.Head.FriendlyName}\n\n";
                
                return string.IsNullOrEmpty(diffContent) 
                    ? (branchInfo + "No changes detected.", false)  // Empty repo graceful handling
                    : (branchInfo + diffContent, false);
            }
            catch (RepositoryNotFoundException)
            {
                return ("Not a git repository.", true);
            }
            catch (Exception ex)
            {
                return ($"Git error: {ex.Message}", true);
            }
        }
    }
}