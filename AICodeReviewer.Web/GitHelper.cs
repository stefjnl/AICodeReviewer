using LibGit2Sharp;

namespace AICodeReviewer.Web;

public static class GitHelper
{
    public static (string branchInfo, bool isError) DetectRepository(string startPath)
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
            return ("Repository access denied", true);
        }
        catch (LibGit2SharpException)
        {
            return ("Git detection unavailable", true);
        }
        catch (Exception)
        {
            return ("Git detection error", true);
        }
    }
}