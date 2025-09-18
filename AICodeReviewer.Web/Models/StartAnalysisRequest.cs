namespace AICodeReviewer.Web.Models
{
    /// <summary>
    /// Request model for starting AI code analysis with all workflow selections
    /// </summary>
    public class StartAnalysisRequest
    {
        /// <summary>
        /// Path to the validated repository
        /// </summary>
        public string RepositoryPath { get; set; } = string.Empty;

        /// <summary>
        /// Selected programming language
        /// </summary>
        public string SelectedLanguage { get; set; } = string.Empty;

        /// <summary>
        /// Type of analysis to perform (CommitAnalysis, SingleFile, DocumentAnalysis)
        /// </summary>
        public string AnalysisType { get; set; } = string.Empty;

        /// <summary>
        /// Selected AI model for analysis
        /// </summary>
        public string SelectedModel { get; set; } = string.Empty;

        /// <summary>
        /// Target commit hash (for commit analysis)
        /// </summary>
        public string? TargetCommit { get; set; }

        /// <summary>
        /// Target file path (for single file analysis)
        /// </summary>
        public string? TargetFile { get; set; }

        /// <summary>
        /// List of selected documents for analysis
        /// </summary>
        public List<string> SelectedDocuments { get; set; } = new List<string>();

        /// <summary>
        /// Selected documents folder (for document analysis)
        /// </summary>
        public string? DocumentsFolder { get; set; }
    }
}