using AICodeReviewer.Web.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Parses diff content to extract statistics and file lists
/// </summary>
public class DiffStatisticsParser : IDiffStatisticsParser
{
    private static readonly Regex DiffGitRegex = new Regex(@"diff --git a/(.*) b/(.*)", RegexOptions.Compiled);

    /// <summary>
    /// Parses the diff content and returns statistics
    /// </summary>
    public (int additions, int deletions, List<string> files) Parse(string diffContent)
    {
        if (string.IsNullOrEmpty(diffContent))
        {
            return (0, 0, new List<string>());
        }

        var files = new List<string>();
        int additions = 0;
        int deletions = 0;

        var lines = diffContent.Split('\n');

        foreach (var line in lines)
        {
            // Count additions and deletions
            if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                additions++;
            }
            else if (line.StartsWith("-") && !line.StartsWith("---"))
            {
                deletions++;
            }
            // Extract file names from diff --git lines
            else if (line.StartsWith("diff --git"))
            {
                var match = DiffGitRegex.Match(line);
                if (match.Success)
                {
                    files.Add(match.Groups[1].Value);
                }
            }
        }

        return (additions, deletions, files);
    }
}
