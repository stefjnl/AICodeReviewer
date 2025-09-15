namespace AICodeReviewer.Web.Models;

public class ValidateCommitRequest
{
    public required string CommitId { get; set; }
    public string? RepositoryPath { get; set; }
}