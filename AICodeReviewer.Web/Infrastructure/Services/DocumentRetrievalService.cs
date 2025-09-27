using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Service for loading coding standards documents in parallel.
    /// </summary>
    public class DocumentRetrievalService : IDocumentRetrievalService
    {
        private readonly ILogger<DocumentRetrievalService> _logger;
        private readonly IDocumentManagementService _documentService;

        public DocumentRetrievalService(
            ILogger<DocumentRetrievalService> logger,
            IDocumentManagementService documentService)
        {
            _logger = logger;
            _documentService = documentService;
        }

        public async Task<List<string>> LoadDocumentsAsync(List<string> selectedDocuments, string documentsFolder)
        {
            _logger.LogInformation("[DocumentRetrieval] Loading {Count} selected documents from folder: {Folder}", selectedDocuments.Count, documentsFolder);
            _logger.LogInformation("[DocumentRetrieval] Documents folder exists: {Exists}", Directory.Exists(documentsFolder));

            if (selectedDocuments.Count == 0)
            {
                _logger.LogWarning("[DocumentRetrieval] No documents to load");
                return new List<string>();
            }

            // Create tasks for parallel document loading
            var documentTasks = selectedDocuments.Select(async docName =>
            {
                _logger.LogInformation("[DocumentRetrieval] Loading document: {DocName} from folder: {Folder}", docName, documentsFolder);
                var (docContent, docError) = await _documentService.LoadDocumentAsync(docName, documentsFolder);
                _logger.LogInformation("[DocumentRetrieval] Document {DocName} - Error: {Error}, Content length: {Length}", docName, docError, docContent?.Length ?? 0);

                if (!docError)
                {
                    _logger.LogInformation("[DocumentRetrieval] Document {DocName} loaded successfully", docName);
                    return docContent;
                }
                else
                {
                    _logger.LogWarning("[DocumentRetrieval] Failed to load document {DocName}: {Error}", docName, docContent ?? "Unknown error");
                    return (string?)null;
                }
            }).ToArray();

            // Wait for all documents to load in parallel
            var documentResults = await Task.WhenAll(documentTasks);

            // Collect successful documents
            var codingStandards = documentResults.Where(content => !string.IsNullOrEmpty(content)).ToList() as List<string> ?? new List<string>();

            _logger.LogInformation("[DocumentRetrieval] Document loading complete - loaded {LoadedCount} out of {TotalCount} documents", codingStandards.Count, selectedDocuments.Count);

            return codingStandards;
        }
    }
}