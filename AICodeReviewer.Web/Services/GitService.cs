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

                // Get unstaged changes (working directory vs Index)
                var statusOptions = new StatusOptions
                {
                    IncludeUnaltered = false,
                    RecurseUntrackedDirs = true,
                    Show = StatusShowOption.IndexAndWorkDir
                };

                var status = repo.RetrieveStatus(statusOptions);
                
                // Check if there are any changes
                if (!status.IsDirty)
                {
                    return ($"Branch: {repo.Head.FriendlyName}\n\nNo uncommitted changes detected.", false);
                }

                // Compare working directory against index (unstaged changes)
                var diff = repo.Diff.Compare<Patch>(
                    repo.Head.Tip?.Tree,
                    DiffTargets.WorkingDirectory);

                var diffContent = diff.Content;

                // Size check - 100KB limit
                if (diffContent.Length > 102400)
                    return ($"Diff too large ({diffContent.Length} bytes > 100KB). Commit some changes first.", true);

                var branchInfo = $"Branch: {repo.Head.FriendlyName}\n\n";

                return string.IsNullOrEmpty(diffContent)
                    ? (branchInfo + "No uncommitted changes detected.", false)
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

        public static (string diff, bool isError) GetCommitDiff(string repositoryPath, string commitId)
        {
            try
            {
                using var repo = new Repository(repositoryPath);
                
                // Lookup commit by ID (full or partial hash)
                var commit = repo.Lookup<Commit>(commitId);
                if (commit == null)
                {
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
                    return ($"Commit diff too large ({diffContent.Length} bytes > 100KB).", true);
                
                // Include commit information
                var commitInfo = $"Branch: {repo.Head.FriendlyName}\nCommit: {commit.Sha.Substring(0, 7)} - {commit.MessageShort}\n\n";
                
                return string.IsNullOrEmpty(diffContent)
                    ? (commitInfo + "No changes in this commit.", false)
                    : (commitInfo + diffContent, false);
            }
            catch (RepositoryNotFoundException)
            {
                return ("Not a git repository.", true);
            }
            catch (AmbiguousSpecificationException)
            {
                return ($"Ambiguous commit ID '{commitId}'. Please provide a more specific hash.", true);
            }
            catch (Exception ex)
            {
                return ($"Git error: {ex.Message}", true);
            }
        }
    }
}