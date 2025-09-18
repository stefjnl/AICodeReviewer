// AI Code Reviewer - Results Display Component
// Handles the display of analysis results in the UI

export class ResultsDisplay {
    constructor() {
        this.resultsContainer = null;
        this.loadingContainer = null;
        this.errorContainer = null;
    }

    /**
     * Initializes the results display containers
     */
    initialize() {
        this.resultsContainer = document.getElementById('analysis-results-container');
        this.loadingContainer = document.getElementById('analysis-loading-state');
        this.errorContainer = document.getElementById('analysis-error-container');
        
        this.createResultsStructure();
    }

    /**
     * Creates the HTML structure for results display
     */
    createResultsStructure() {
        const step5Content = document.getElementById('step-5-content');
        if (!step5Content) return;

        // Check if containers already exist
        if (!document.getElementById('analysis-loading-state')) {
            const loadingDiv = document.createElement('div');
            loadingDiv.id = 'analysis-loading-state';
            loadingDiv.className = 'mb-6';
            loadingDiv.style.display = 'none';
            step5Content.appendChild(loadingDiv);
        }

        if (!document.getElementById('analysis-error-container')) {
            const errorDiv = document.createElement('div');
            errorDiv.id = 'analysis-error-container';
            errorDiv.className = 'mb-6';
            errorDiv.style.display = 'none';
            step5Content.appendChild(errorDiv);
        }

        if (!document.getElementById('analysis-results-container')) {
            const resultsDiv = document.createElement('div');
            resultsDiv.id = 'analysis-results-container';
            resultsDiv.className = 'mb-6';
            resultsDiv.style.display = 'none';
            step5Content.appendChild(resultsDiv);
        }
    }

    /**
     * Shows loading state
     * @param {string} message Loading message
     */
    showLoading(message = 'Loading...') {
        this.hideAllStates();
        
        if (this.loadingContainer) {
            this.loadingContainer.style.display = 'block';
            this.loadingContainer.innerHTML = `
                <div class="bg-blue-50 border border-blue-200 rounded-md p-4">
                    <div class="flex items-center">
                        <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600 mr-3"></div>
                        <div>
                            <h3 class="text-sm font-medium text-blue-800">Processing Analysis</h3>
                            <p class="text-sm text-blue-600 mt-1">${message}</p>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    /**
     * Updates loading progress
     * @param {Object} progress Progress information
     */
    updateProgress(progress) {
        if (this.loadingContainer && this.loadingContainer.style.display !== 'none') {
            this.loadingContainer.innerHTML = `
                <div class="bg-blue-50 border border-blue-200 rounded-md p-4">
                    <div class="flex items-center">
                        <div class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600 mr-3"></div>
                        <div>
                            <h3 class="text-sm font-medium text-blue-800">Processing Analysis</h3>
                            <p class="text-sm text-blue-600 mt-1">${progress.message || 'Processing...'}</p>
                            <div class="mt-2">
                                <div class="bg-blue-200 rounded-full h-2">
                                    <div class="bg-blue-600 h-2 rounded-full transition-all duration-300" style="width: ${progress.percentage || 0}%"></div>
                                </div>
                                <p class="text-xs text-blue-600 mt-1">${progress.percentage || 0}% Complete</p>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    /**
     * Shows error state
     * @param {string} errorMessage Error message to display
     */
    showError(errorMessage) {
        this.hideAllStates();
        
        if (this.errorContainer) {
            this.errorContainer.style.display = 'block';
            this.errorContainer.innerHTML = `
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
                            <div class="mt-4">
                                <button type="button" class="retry-analysis-btn text-sm text-red-600 hover:text-red-500 font-medium">
                                    Try Again
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    /**
     * Shows analysis results
     * @param {Object} results Analysis results
     */
    showResults(results) {
        this.hideAllStates();
        
        if (this.resultsContainer) {
            this.resultsContainer.style.display = 'block';
            this.resultsContainer.innerHTML = this.buildResultsHTML(results);
        }
    }

    /**
     * Builds the HTML for displaying results
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildResultsHTML(results) {
        return `
            <div class="bg-white shadow rounded-lg">
                <div class="px-4 py-5 sm:p-6">
                    <div class="flex items-center justify-between mb-4">
                        <h3 class="text-lg leading-6 font-medium text-gray-900">
                            Analysis Results
                        </h3>
                        <button type="button" class="text-sm text-primary hover:text-primary/80">
                            Export Results
                        </button>
                    </div>
                    
                    ${this.buildSummarySection(results)}
                    ${this.buildIssuesSection(results)}
                    ${this.buildRecommendationsSection(results)}
                    ${this.buildRawResultsSection(results)}
                </div>
            </div>
        `;
    }

    /**
     * Builds the summary section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildSummarySection(results) {
        const summary = results.summary || {};
        
        return `
            <div class="mb-6">
                <h4 class="text-md font-medium text-gray-900 mb-3">Summary</h4>
                <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
                    <div class="bg-gray-50 rounded-md p-4">
                        <p class="text-sm font-medium text-gray-500">Total Issues</p>
                        <p class="mt-1 text-2xl font-semibold text-gray-900">${summary.totalIssues || 0}</p>
                    </div>
                    <div class="bg-red-50 rounded-md p-4">
                        <p class="text-sm font-medium text-red-600">Critical</p>
                        <p class="mt-1 text-2xl font-semibold text-red-600">${summary.critical || 0}</p>
                    </div>
                    <div class="bg-orange-50 rounded-md p-4">
                        <p class="text-sm font-medium text-orange-600">High</p>
                        <p class="mt-1 text-2xl font-semibold text-orange-600">${summary.high || 0}</p>
                    </div>
                    <div class="bg-yellow-50 rounded-md p-4">
                        <p class="text-sm font-medium text-yellow-600">Medium</p>
                        <p class="mt-1 text-2xl font-semibold text-yellow-600">${summary.medium || 0}</p>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Builds the issues section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildIssuesSection(results) {
        const issues = results.issues || [];
        
        if (issues.length === 0) {
            return `
                <div class="mb-6">
                    <h4 class="text-md font-medium text-gray-900 mb-3">Issues Found</h4>
                    <div class="bg-green-50 border border-green-200 rounded-md p-4">
                        <div class="flex">
                            <div class="flex-shrink-0">
                                <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                                </svg>
                            </div>
                            <div class="ml-3">
                                <p class="text-sm text-green-800">No issues found! Your code looks great.</p>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }

        return `
            <div class="mb-6">
                <h4 class="text-md font-medium text-gray-900 mb-3">Issues Found</h4>
                <div class="space-y-4">
                    ${issues.map(issue => this.buildIssueItem(issue)).join('')}
                </div>
            </div>
        `;
    }

    /**
     * Builds individual issue item
     * @param {Object} issue Issue object
     * @returns {string} HTML string
     */
    buildIssueItem(issue) {
        const severityClass = this.getSeverityClass(issue.severity);
        
        return `
            <div class="border-l-4 ${severityClass.border} pl-4">
                <div class="flex items-center justify-between">
                    <h5 class="text-sm font-medium text-gray-900">${issue.title || 'Untitled Issue'}</h5>
                    <span class="text-xs ${severityClass.text} font-medium">
                        ${issue.severity || 'Unknown'}
                    </span>
                </div>
                <p class="mt-1 text-sm text-gray-600">${issue.description || ''}</p>
                ${issue.file ? `<p class="text-xs text-gray-500 mt-1">File: ${issue.file}</p>` : ''}
                ${issue.line ? `<p class="text-xs text-gray-500">Line: ${issue.line}</p>` : ''}
                ${issue.suggestion ? `
                    <div class="mt-2">
                        <p class="text-sm font-medium text-gray-700">Suggestion:</p>
                        <p class="text-sm text-gray-600">${issue.suggestion}</p>
                    </div>
                ` : ''}
            </div>
        `;
    }

    /**
     * Builds the recommendations section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildRecommendationsSection(results) {
        const recommendations = results.recommendations || [];
        
        if (recommendations.length === 0) {
            return '';
        }

        return `
            <div class="mb-6">
                <h4 class="text-md font-medium text-gray-900 mb-3">Recommendations</h4>
                <div class="bg-blue-50 rounded-md p-4">
                    <div class="space-y-2">
                        ${recommendations.map(rec => `
                            <div class="flex items-start">
                                <svg class="h-5 w-5 text-blue-400 mr-2 mt-0.5" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                                </svg>
                                <p class="text-sm text-gray-700">${rec}</p>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Builds the raw results section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildRawResultsSection(results) {
        return `
            <div>
                <h4 class="text-md font-medium text-gray-900 mb-3">Raw Analysis Data</h4>
                <details class="border rounded-md">
                    <summary class="px-4 py-3 cursor-pointer text-sm font-medium text-gray-700 hover:bg-gray-50">
                        View Raw Results
                    </summary>
                    <div class="px-4 py-3 border-t">
                        <pre class="text-xs text-gray-600 overflow-auto max-h-64">${JSON.stringify(results, null, 2)}</pre>
                    </div>
                </details>
            </div>
        `;
    }

    /**
     * Gets severity CSS classes
     * @param {string} severity Issue severity
     * @returns {Object} CSS classes object
     */
    getSeverityClass(severity) {
        switch (severity?.toLowerCase()) {
            case 'critical':
                return { border: 'border-red-400', text: 'text-red-600' };
            case 'high':
                return { border: 'border-orange-400', text: 'text-orange-600' };
            case 'medium':
                return { border: 'border-yellow-400', text: 'text-yellow-600' };
            case 'low':
                return { border: 'border-blue-400', text: 'text-blue-600' };
            default:
                return { border: 'border-gray-400', text: 'text-gray-600' };
        }
    }

    /**
     * Hides all display states
     */
    hideAllStates() {
        if (this.loadingContainer) this.loadingContainer.style.display = 'none';
        if (this.errorContainer) this.errorContainer.style.display = 'none';
        if (this.resultsContainer) this.resultsContainer.style.display = 'none';
    }
}

// Create singleton instance
export const resultsDisplay = new ResultsDisplay();