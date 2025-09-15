namespace AICodeReviewer.Web.Models;

public record ProgressDto(string Status, string? Result, string? Error, bool IsComplete);