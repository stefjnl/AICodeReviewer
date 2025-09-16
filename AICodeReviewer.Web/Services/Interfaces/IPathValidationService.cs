namespace AICodeReviewer.Web.Services;

public interface IPathValidationService
{
    (string resolvedPath, string? error) ResolveAndValidateFilePath(string filePath, string repositoryPath, string contentRootPath);
    bool IsValidFileExtension(string filePath, string[] allowedExtensions);
}