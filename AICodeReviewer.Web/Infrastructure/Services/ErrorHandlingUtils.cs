using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Utility class for consistent error handling and sanitization across all controllers
/// </summary>
public static class ErrorHandlingUtils
{
    /// <summary>
    /// Creates a sanitized error response that doesn't expose internal implementation details
    /// </summary>
    /// <param name="context">The context description for logging</param>
    /// <param name="userMessage">User-friendly error message</param>
    /// <returns>ObjectResult with sanitized error response</returns>
    public static ObjectResult CreateSanitizedError(string context, string userMessage)
    {
        return new ObjectResult(new { error = userMessage })
        {
            StatusCode = 500
        };
    }

    /// <summary>
    /// Creates a sanitized error response with success flag for API consistency
    /// </summary>
    /// <param name="context">The context description for logging</param>
    /// <param name="userMessage">User-friendly error message</param>
    /// <param name="includeSuccessFlag">Whether to include success=false in response</param>
    /// <returns>ObjectResult with sanitized error response</returns>
    public static ObjectResult CreateSanitizedError(string context, string userMessage, bool includeSuccessFlag)
    {
        object errorResponse;

        if (includeSuccessFlag)
        {
            errorResponse = new { success = false, error = userMessage };
        }
        else
        {
            errorResponse = new { error = userMessage };
        }

        return new ObjectResult(errorResponse)
        {
            StatusCode = 500
        };
    }

    /// <summary>
    /// Common error messages for consistent user experience
    /// </summary>
    public static class ErrorMessages
    {
        public const string GenericError = "An unexpected error occurred. Please try again.";
        public const string RepositoryAccessError = "An error occurred while accessing the repository. Please check the path and try again.";
        public const string FileAccessError = "An error occurred while accessing the file. Please try again.";
        public const string AnalysisError = "An error occurred while processing the analysis. Please try again.";
        public const string ModelLoadingError = "An error occurred while loading available models. Please try again later.";
        public const string DocumentScanningError = "An error occurred while scanning documents. Please try again.";
        public const string DirectoryBrowsingError = "An error occurred while browsing the directory. Please try again.";
        public const string LanguageDetectionError = "An error occurred while detecting the programming language. Please try again.";
        public const string ValidationError = "An error occurred while validating the request. Please check your input and try again.";
    }
}