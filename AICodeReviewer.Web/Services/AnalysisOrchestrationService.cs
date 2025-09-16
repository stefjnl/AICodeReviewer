using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Services;

public class AnalysisOrchestrationService : IAnalysisOrchestrationService
{
    private readonly IMemoryCache _cache;
    private readonly ISignalRNotificationService _signalRService;
    private readonly ILogger<AnalysisOrchestrationService> _logger;

    public AnalysisOrchestrationService(IMemoryCache cache, ISignalRNotificationService signalRService, 
        ILogger<AnalysisOrchestrationService> logger)
    {
        _cache = cache;
        _signalRService = signalRService;
        _logger = logger;
    }

    public async Task<string> StartAnalysisAsync(string repositoryPath, List<string> selectedDocuments, 
        string documentsFolder, string apiKey, string model, string language, string analysisType, 
        string? commitId, string? filePath)
    {
        // Generate unique analysis ID
        var analysisId = Guid.NewGuid().ToString();

        // Create initial analysis result
        var analysisResult = new AnalysisResult
        {
            Status = "Starting",
            CreatedAt = DateTime.UtcNow
        };

        // Store in memory cache with expiration
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30)) // Reset on access
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)) // Max 1 hour
            .SetSize(1); // Size = 1 unit for size-limited cache

        _cache.Set($"analysis_{analysisId}", analysisResult, cacheOptions);
        
        _logger.LogInformation($"[AnalysisOrchestration] Started new analysis {analysisId}");

        // Start background analysis
        _ = Task.Run(async () =>
        {
            try
            {
                await RunBackgroundAnalysisAsync(analysisId, repositoryPath, selectedDocuments, 
                    documentsFolder, apiKey, model, language, analysisType, commitId, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background analysis failed for analysis {AnalysisId}", analysisId);
                await _signalRService.BroadcastError(analysisId, $"Background analysis error: {ex.Message}");
            }
        }).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                _logger.LogError(t.Exception, "Unobserved exception in background task for analysis {AnalysisId}", analysisId);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        return analysisId;
    }

    public async Task<AnalysisResult?> GetAnalysisStatusAsync(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            return null;
        }

        if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
        {
            _logger.LogInformation($"[AnalysisOrchestration] Retrieved status for analysis {analysisId}: {result.Status}");
            return result;
        }

        return null;
    }

    public async Task RunBackgroundAnalysisAsync(string analysisId, string repositoryPath, 
        List<string> selectedDocuments, string documentsFolder, string apiKey, string model, 
        string language, string analysisType, string? commitId, string? filePath)
    {
        _logger.LogInformation($"[AnalysisOrchestration] Starting background analysis {analysisId}");
        _logger.LogInformation($"[AnalysisOrchestration] Repository path: {repositoryPath}");
        _logger.LogInformation($"[AnalysisOrchestration] Selected documents: {string.Join(", ", selectedDocuments)}");
        _logger.LogInformation($"[AnalysisOrchestration] Documents folder: {documentsFolder}");
        _logger.LogInformation($"[AnalysisOrchestration] Language: {language}");
        _logger.LogInformation($"[AnalysisOrchestration] Analysis type: {analysisType}");

        try
        {
            // Get current result from cache
            _logger.LogInformation($"[AnalysisOrchestration] Checking cache for existing result");
            AnalysisResult? result;
            try
            {
                if (!_cache.TryGetValue($"analysis_{analysisId}", out result))
                {
                    _logger.LogError($"[AnalysisOrchestration] Analysis not found in cache - aborting");
                    return; // Analysis not found in cache
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AnalysisOrchestration] Error accessing cache - aborting");
                return;
            }
            _logger.LogInformation($"[AnalysisOrchestration] Found existing result in cache");

            // Update status via SignalR
            if (result != null)
            {
                result.Status = "Reading git changes...";
                await _signalRService.BroadcastProgress(analysisId, "Reading git changes...");
                _logger.LogInformation($"[AnalysisOrchestration] Updated status to 'Reading git changes...'");
            }

            // Get content based on analysis type
            _logger.LogInformation($"[AnalysisOrchestration] Analysis type: {analysisType}");
            string content;
            bool contentError;
            bool isFileContent = false;
            
            if (analysisType == "singlefile" && !string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation($"[AnalysisOrchestration] Reading single file: {filePath}");
                try
                {
                    content = await File.ReadAllTextAsync(filePath);
                    contentError = false;
                    isFileContent = true;
                    _logger.LogInformation($"[AnalysisOrchestration] File read successfully, length: {content.Length}");
                }
                catch (Exception ex)
                {
                    content = $"Error reading file: {ex.Message}";
                    contentError = true;
                    _logger.LogError(ex, $"[AnalysisOrchestration] Failed to read file: {filePath}");
                }
            }
            else if (analysisType == "commit" && !string.IsNullOrEmpty(commitId))
            {
                _logger.LogInformation($"[AnalysisOrchestration] Extracting commit diff for commit: {commitId}");
                (content, contentError) = GitService.GetCommitDiff(repositoryPath, commitId);
            }
            else
            {
                _logger.LogInformation($"[AnalysisOrchestration] Extracting uncommitted changes");
                (content, contentError) = GitService.ExtractDiff(repositoryPath);
            }
            
            _logger.LogInformation($"[AnalysisOrchestration] Content extraction complete - Error: {contentError}, Content length: {content?.Length ?? 0}");
            
            // Store content in cache for results display
            if (!contentError && !string.IsNullOrEmpty(content))
            {
                _cache.Set($"content_{analysisId}", content, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1));
                _logger.LogInformation($"[AnalysisOrchestration] Stored content in cache ({content.Length} bytes)");
            }
            
            if (contentError)
            {
                _logger.LogError($"[AnalysisOrchestration] Content extraction failed: {content}");
                if (result != null)
                {
                    result.Status = "Error";
                    result.Error = isFileContent ? $"File reading error: {content}" : $"Git diff error: {content}";
                    result.CompletedAt = DateTime.UtcNow;
                    
                    var cacheOptions2 = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                        .SetSize(1);
                    _cache.Set($"analysis_{analysisId}", result, cacheOptions2);
                    _logger.LogInformation($"[AnalysisOrchestration] Set error status and completed");
                }
                return;
            }
            _logger.LogInformation($"[AnalysisOrchestration] Content extracted successfully");

            // Update status via SignalR
            _logger.LogInformation($"[AnalysisOrchestration] Starting document loading phase");
            if (result != null)
            {
                result.Status = "Loading documents...";
                await _signalRService.BroadcastProgress(analysisId, "Loading documents...");
                _logger.LogInformation($"[AnalysisOrchestration] Updated status to 'Loading documents...'");
            }
            
            // Load selected documents asynchronously
            _logger.LogInformation($"[AnalysisOrchestration] Loading {selectedDocuments.Count} selected documents");
            _logger.LogInformation($"[AnalysisOrchestration] Documents folder path: {documentsFolder}");
            _logger.LogInformation($"[AnalysisOrchestration] Documents folder exists: {Directory.Exists(documentsFolder)}");
            
            // Create tasks for parallel document loading
            var documentTasks = selectedDocuments.Select(async docName =>
            {
                _logger.LogInformation($"[AnalysisOrchestration] Loading document: {docName} from folder: {documentsFolder}");
                var (docContent, docError) = await DocumentService.LoadDocumentAsync(docName, documentsFolder);
                _logger.LogInformation($"[AnalysisOrchestration] Document {docName} - Error: {docError}, Content length: {docContent?.Length ?? 0}");
                
                if (!docError)
                {
                    _logger.LogInformation($"[AnalysisOrchestration] Document {docName} loaded successfully");
                    return (docContent, docError, docName);
                }
                else
                {
                    _logger.LogWarning($"[AnalysisOrchestration] Failed to load document {docName}: {docContent}");
                    return (docContent, docError, docName);
                }
            }).ToList();
            
            // Wait for all documents to load in parallel
            var documentResults = await Task.WhenAll(documentTasks);
            
            // Collect successful documents
            var codingStandards = new List<string>();
            foreach (var (docContent, docError, docName) in documentResults)
            {
                if (!docError)
                {
                    codingStandards.Add(docContent);
                }
            }
            
            _logger.LogInformation($"[AnalysisOrchestration] Document loading complete - loaded {codingStandards.Count} documents");
            _logger.LogInformation($"[AnalysisOrchestration] Final codingStandards count: {codingStandards.Count}");

            // Update status via SignalR
            _logger.LogInformation($"[AnalysisOrchestration] Starting AI analysis phase");
            if (result != null)
            {
                result.Status = "AI analysis...";
                await _signalRService.BroadcastProgress(analysisId, "AI analysis...");
                _logger.LogInformation($"[AnalysisOrchestration] Updated status to 'AI analysis...'");
            }
            
            // Get requirements
            string requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation($"[AnalysisOrchestration] Requirements: {requirements}");

            // Call AI service with timeout protection
            _logger.LogInformation($"[AnalysisOrchestration] Calling AI service with:");
            _logger.LogInformation($"[AnalysisOrchestration] - Content length: {content?.Length ?? 0}");
            _logger.LogInformation($"[AnalysisOrchestration] - Coding standards count: {codingStandards.Count}");
            _logger.LogInformation($"[AnalysisOrchestration] - API key configured: {!string.IsNullOrEmpty(apiKey)}");
            _logger.LogInformation($"[AnalysisOrchestration] - Model: {model}");
            _logger.LogInformation($"[AnalysisOrchestration] - Analysis type: {(isFileContent ? "Single File" : "Git Diff")}");
            
            // Add timeout protection (60 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            
            string analysis;
            bool aiError;
            string? errorMessage;
            
            try
            {
                _logger.LogInformation($"[AnalysisOrchestration] Starting AI service call with 60-second timeout");
                
                // Call AI service with timeout
                var (analysisResult, isError, errorMsg) = await Task.Run(async () =>
                    await AIService.AnalyzeCodeAsync(content, codingStandards, requirements, apiKey, model, language, isFileContent),
                    cts.Token);
                
                analysis = analysisResult;
                aiError = isError;
                errorMessage = errorMsg;
                
                _logger.LogInformation($"[AnalysisOrchestration] AI service call complete - Error: {aiError}, Analysis length: {analysis?.Length ?? 0}");
                if (aiError)
                {
                    _logger.LogError($"[AnalysisOrchestration] AI analysis failed with detailed error: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError($"[AnalysisOrchestration] AI analysis timed out after 60 seconds");
                analysis = "";
                aiError = true;
                errorMessage = "AI analysis timed out after 60 seconds";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AnalysisOrchestration] Unexpected exception during AI service call");
                analysis = "";
                aiError = true;
                errorMessage = $"Unexpected error calling AI service: {ex.Message}";
            }

            // Always create a NEW instance to ensure we're writing fresh data
            var finalResult = new AnalysisResult
            {
                Status = aiError ? "Error" : "Complete",
                Result = aiError ? null : analysis,
                Error = aiError ? $"AI analysis failed: {errorMessage}" : null,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = result.CreatedAt, // preserve original creation time
                RequestId = result.RequestId  // preserve if you have it
            };

            // Broadcast completion via SignalR
            if (aiError)
            {
                await _signalRService.BroadcastError(analysisId, $"AI analysis failed: {errorMessage}");
            }
            else
            {
                await _signalRService.BroadcastComplete(analysisId, analysis);
            }

            // Still update cache for fallback
            _cache.Set($"analysis_{analysisId}", finalResult, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetSize(1));

            _logger.LogInformation($"[AnalysisOrchestration] Cache updated with final result: {finalResult.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[AnalysisOrchestration] Unhandled exception in background analysis");
            try
            {
                if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? errorResult))
                {
                    var finalErrorResult = new AnalysisResult {
                        Status = "Error",
                        Error = $"Analysis error: {ex.Message}",
                        CompletedAt = DateTime.UtcNow
                    };
                    _cache.Set($"analysis_{analysisId}", finalErrorResult, new MemoryCacheEntryOptions().SetSize(1));
                    await _signalRService.BroadcastError(analysisId, $"Analysis error: {ex.Message}");
                    _logger.LogInformation($"[AnalysisOrchestration] Set error status due to exception: {ex.Message}");
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogError(cacheEx, $"[AnalysisOrchestration] Failed to update cache with error status");
            }
        }
        finally
        {
            _logger.LogInformation($"[AnalysisOrchestration] Background analysis task completed for {analysisId}");
        }
    }
}