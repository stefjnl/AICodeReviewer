using System.Collections.Generic;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces
{
    /// <summary>
    /// Interface for retrieving coding standards documents for analysis.
    /// </summary>
    public interface IDocumentRetrievalService
    {
        /// <summary>
        /// Loads selected coding standards documents asynchronously in parallel.
        /// </summary>
        /// <param name="selectedDocuments">List of document names to load.</param>
        /// <param name="documentsFolder">Path to the folder containing documents.</param>
        /// <returns>List of successfully loaded document contents.</returns>
        Task<List<string>> LoadDocumentsAsync(List<string> selectedDocuments, string documentsFolder);
    }
}