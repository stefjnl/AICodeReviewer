// AI Code Reviewer - Analysis Execution Service
// Handles the complete analysis execution workflow

import { apiClient } from '../api/api-client.js';
import { apiEndpoints } from '../core/constants.js';
import { workflowState } from '../workflow/workflow-state.js';
import { repositoryState } from '../repository/repository-state.js';
import { languageState } from '../language/language-state.js';
import { modelState } from '../models/model-state.js';
import { documentManager } from '../documents/document-manager.js';
import { analysisState } from '../analysis/analysis-state.js';

export class ExecutionService {
    constructor() {
        this.isRunning = false;
        this.analysisId = null;
    }

    /**
     * Builds the complete analysis request from workflow state
     * @returns {Object} Complete StartAnalysisRequest payload
     */
    buildAnalysisRequest() {
        try {
            console.log('📋 Building analysis request from workflow state...');
            
            // Get repository path from repository state
            const repositoryPath = repositoryState.path;
            if (!repositoryPath) {
                throw new Error('Repository path is required but not selected');
            }

            // Get selected language
            const selectedLanguage = languageState.selectedLanguage;
            if (!selectedLanguage) {
                throw new Error('Programming language is required but not selected');
            }

            // Get selected model
            const selectedModel = modelState.selectedModel?.id || modelState.selectedModel;
            if (!selectedModel) {
                throw new Error('AI model is required but not selected');
            }

            // Get selected documents
            const selectedDocuments = documentManager.selectedDocuments || [];
            const documentsFolder = documentManager.selectedFolder || '';

            // Determine analysis type based on workflow configuration
            // Default to "uncommitted" if no analysis type is selected
            let analysisType = analysisState.analysisType || "uncommitted";
            let targetCommit = null;
            let targetFile = null;

            // Check what type of analysis was configured
            if (analysisType === 'single-file') {
                analysisType = "singlefile";
                targetFile = workflowState.targetFile;
            } else if (analysisType === 'commit') {
                targetCommit = workflowState.targetCommit;
            } else if (analysisType === 'document') {
                analysisType = "singlefile"; // Document analysis is treated as single file analysis
            } else if (analysisType === 'uncommitted') {
                analysisType = "uncommitted";
            } else if (analysisType === 'staged') {
                analysisType = "staged";
            }

            // Build request object with only the relevant fields
            const request = {
                repositoryPath,
                selectedDocuments,
                documentsFolder,
                language: selectedLanguage,
                analysisType: analysisType,
                model: selectedModel
            };

            // Only include commitId for commit analysis
            if (analysisType === "commit") {
                request.commitId = targetCommit;
            }

            // Only include filePath for single file analysis
            if (analysisType === "singlefile") {
                request.filePath = targetFile;
            }

            console.log('✅ Analysis request built successfully:', request);
            return request;

        } catch (error) {
            console.error('❌ Error building analysis request:', error);
            throw error;
        }
    }

    /**
     * Starts the analysis execution - matches exact pattern of ModelApiController
     * @returns {Promise<Object>} Analysis result with analysisId
     */
    async startAnalysis() {
        if (this.isRunning) {
            throw new Error('Analysis is already running');
        }

        try {
            this.isRunning = true;
            this.analysisId = null;

            console.log('🚀 Starting analysis execution...');

            // Build request payload
            const request = this.buildAnalysisRequest();

            // Show loading state
            this.showLoadingState();

            // Make API call - match exact pattern used by ModelApiController
            console.log(`📞 Calling API: POST ${apiEndpoints.executionStart}`, request);
            const response = await apiClient.post(apiEndpoints.executionStart, request);
            console.log('📤 API Response:', response);
            
            if (response.success) {
                this.analysisId = response.analysisId;
                console.log(`✅ Analysis started successfully with ID: ${this.analysisId}`);
                
                // Start monitoring progress
                this.startProgressMonitoring();
                
                return {
                    success: true,
                    analysisId: this.analysisId
                };
            } else {
                throw new Error(response.error || 'Failed to start analysis');
            }

        } catch (error) {
            console.error('❌ Error starting analysis:', error);
            this.showErrorState(error.message || 'Failed to start analysis');
            throw error;
        } finally {
            // Don't reset isRunning here - progress monitoring will handle it
        }
    }

    /**
     * Shows loading state in the UI
     */
    showLoadingState() {
        const runBtn = document.getElementById('run-analysis-btn');
        const loadingState = document.getElementById('analysis-loading-state');
        const resultsContainer = document.getElementById('analysis-results-container');

        if (runBtn) {
            runBtn.disabled = true;
            runBtn.textContent = 'Starting Analysis...';
        }

        if (loadingState) {
            loadingState.style.display = 'block';
            loadingState.innerHTML = `
                <div class="flex items-center justify-center space-x-2">
                    <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-primary"></div>
                    <span>Preparing analysis...</span>
                </div>
            `;
        }

        if (resultsContainer) {
            resultsContainer.style.display = 'none';
        }
    }

    /**
     * Updates loading state with progress information
     * @param {Object} progress Progress information
     */
    updateLoadingState(progress) {
        const loadingState = document.getElementById('analysis-loading-state');
        if (loadingState) {
            loadingState.innerHTML = `
                <div class="flex items-center space-x-2">
                    <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-primary"></div>
                    <span>${progress.message || 'Processing...'} (${progress.percentage || 0}%)</span>
                </div>
            `;
        }
    }

    /**
     * Shows error state in the UI
     * @param {string} errorMessage Error message to display
     */
    showErrorState(errorMessage) {
        const runBtn = document.getElementById('run-analysis-btn');
        const loadingState = document.getElementById('analysis-loading-state');
        const errorContainer = document.getElementById('analysis-error-container');

        if (runBtn) {
            runBtn.disabled = false;
            runBtn.textContent = 'Run Analysis';
        }

        if (loadingState) {
            loadingState.style.display = 'none';
        }

        if (errorContainer) {
            errorContainer.style.display = 'block';
            errorContainer.innerHTML = `
                <div class="bg-red-50 border border-red-200 rounded-md p-4">
                    <div class="flex">
                        <div class="flex-shrink-0">
                            <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
                            </svg>
                        </div>
                        <div class="ml-3">
                            <h3 class="text-sm font-medium text-red-800">Analysis Failed</h3>
                            <div class="mt-2 text-sm text-red-700">
                                <p>${errorMessage}</p>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }

        this.isRunning = false;
    }

    /**
     * Shows success state with results
     * @param {Object} results Analysis results
     */
    showResults(results) {
        const runBtn = document.getElementById('run-analysis-btn');
        const loadingState = document.getElementById('analysis-loading-state');
        const resultsContainer = document.getElementById('analysis-results-container');

        if (runBtn) {
            runBtn.disabled = false;
            runBtn.textContent = 'Run New Analysis';
        }

        if (loadingState) {
            loadingState.style.display = 'none';
        }

        if (resultsContainer) {
            resultsContainer.style.display = 'block';
            this.renderResults(results);
        }

        this.isRunning = false;
    }

    /**
     * Renders the analysis results in the UI
     * @param {Object} results Analysis results
     */
    renderResults(results) {
        const resultsContainer = document.getElementById('analysis-results-container');
        if (!resultsContainer) return;

        resultsContainer.innerHTML = `
            <div class="bg-white shadow rounded-lg">
                <div class="px-4 py-5 sm:p-6">
                    <h3 class="text-lg leading-6 font-medium text-gray-900 mb-4">
                        Analysis Results
                    </h3>
                    
                    ${this.renderSummary(results.summary)}
                    ${this.renderFeedback(results.feedback)}
                    ${this.renderDetailedResults(results.detailedResults)}
                </div>
            </div>
        `;
    }

    /**
     * Renders analysis summary
     * @param {Object} summary Analysis summary
     */
    renderSummary(summary) {
        return `
            <div class="mb-6">
                <h4 class="text-sm font-medium text-gray-900 mb-2">Summary</h4>
                <div class="bg-gray-50 rounded-md p-4">
                    <dl class="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Total Issues</dt>
                            <dd class="mt-1 text-sm text-gray-900">${summary.totalIssues || 0}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Critical</dt>
                            <dd class="mt-1 text-sm text-red-600">${summary.critical || 0}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Warnings</dt>
                            <dd class="mt-1 text-sm text-yellow-600">${summary.warnings || 0}</dd>
                        </div>
                    </dl>
                </div>
            </div>
        `;
    }

    /**
     * Renders feedback items
     * @param {Array} feedback Array of feedback items
     */
    renderFeedback(feedback) {
        if (!feedback || feedback.length === 0) {
            return '<p class="text-sm text-gray-500">No feedback available.</p>';
        }

        return `
            <div class="mb-6">
                <h4 class="text-sm font-medium text-gray-900 mb-2">Feedback</h4>
                <div class="space-y-4">
                    ${feedback.map(item => `
                        <div class="border-l-4 ${this.getSeverityBorderColor(item.severity)} pl-4">
                            <div class="flex items-center justify-between">
                                <h5 class="text-sm font-medium text-gray-900">${item.title}</h5>
                                <span class="text-xs ${this.getSeverityTextColor(item.severity)} font-medium">
                                    ${item.severity}
                                </span>
                            </div>
                            <p class="mt-1 text-sm text-gray-600">${item.description}</p>
                            ${item.suggestions ? `
                                <div class="mt-2 text-sm">
                                    <span class="font-medium text-gray-700">Suggestions:</span>
                                    <ul class="list-disc list-inside mt-1 text-gray-600">
                                        ${item.suggestions.map(s => `<li>${s}</li>`).join('')}
                                    </ul>
                                </div>
                            ` : ''}
                        </div>
                    `).join('')}
                </div>
            </div>
        `;
    }

    /**
     * Renders detailed results
     * @param {Object} detailedResults Detailed analysis results
     */
    renderDetailedResults(detailedResults) {
        return `
            <div>
                <h4 class="text-sm font-medium text-gray-900 mb-2">Detailed Analysis</h4>
                <details class="bg-gray-50 rounded-md">
                    <summary class="px-4 py-3 cursor-pointer text-sm font-medium text-gray-700 hover:bg-gray-100">
                        View Detailed Results
                    </summary>
                    <div class="px-4 py-3">
                        <pre class="text-xs text-gray-600 overflow-auto">${JSON.stringify(detailedResults, null, 2)}</pre>
                    </div>
                </details>
            </div>
        `;
    }

    /**
     * Gets border color based on severity
     * @param {string} severity Issue severity
     */
    getSeverityBorderColor(severity) {
        switch (severity?.toLowerCase()) {
            case 'critical': return 'border-red-400';
            case 'high': return 'border-orange-400';
            case 'medium': return 'border-yellow-400';
            case 'low': return 'border-blue-400';
            default: return 'border-gray-400';
        }
    }

    /**
     * Gets text color based on severity
     * @param {string} severity Issue severity
     */
    getSeverityTextColor(severity) {
        switch (severity?.toLowerCase()) {
            case 'critical': return 'text-red-600';
            case 'high': return 'text-orange-600';
            case 'medium': return 'text-yellow-600';
            case 'low': return 'text-blue-600';
            default: return 'text-gray-600';
        }
    }

    /**
     * Starts progress monitoring via SignalR
     */
    startProgressMonitoring() {
        // This will be handled by SignalR connection
        console.log('📊 Starting progress monitoring for analysis ID:', this.analysisId);
        
        // Listen for progress updates
        document.addEventListener('progress-update', (event) => {
            this.updateLoadingState(event.detail);
        });

        // Listen for analysis completion
        document.addEventListener('analysis-complete', (event) => {
            this.showResults(event.detail);
        });

        // Listen for errors
        document.addEventListener('signalr-error', (event) => {
            this.showErrorState(event.detail.message || 'Analysis failed');
        });
    }
}

// Create singleton instance
export const executionService = new ExecutionService();