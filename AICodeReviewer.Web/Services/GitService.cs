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
                
                // Simple size check - 50KB limit
                if (diffContent.Length > 51200) // 50KB
                    return ($"Diff too large ({diffContent.Length} bytes > 50KB). Commit some changes first.", true);
                
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