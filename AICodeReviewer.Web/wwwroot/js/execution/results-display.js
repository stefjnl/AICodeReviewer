// AI Code Reviewer - Enhanced Results Display Component
// Handles the display of analysis results with modern, interactive UI

export class ResultsDisplay {
    constructor() {
        this.resultsContainer = null;
        this.loadingContainer = null;
        this.errorContainer = null;
        this.currentResults = null;
        this.filterState = {
            severity: 'all',
            category: 'all',
            search: '',
            sortBy: 'severity',
            groupBy: 'severity',
            showOnlyFixable: false
        };
        this.expandedSections = {
            critical: true,
            warning: false,
            suggestion: false,
            style: false,
            info: false
        };
        
        // Bind event handlers for proper 'this' context
        this.handleDocumentClick = this.handleDocumentClick.bind(this);
        this.handleDocumentChange = this.handleDocumentChange.bind(this);
        this.handleDocumentInput = this.handleDocumentInput.bind(this);
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
        this.currentResults = results;
        
        if (this.resultsContainer) {
            this.resultsContainer.style.display = 'block';
            this.resultsContainer.innerHTML = this.buildResultsHTML(results);
            this.attachEventListeners();
            this.updateResultsCount();
        }
    }

    /**
     * Builds the HTML for displaying results
     * @param {Object|string} results Analysis results
     * @returns {string} HTML string
     */
    buildResultsHTML(results) {
        // Handle both string and object formats
        if (typeof results === 'string') {
            return this.buildStringResults(results);
        }
        
        return `
            <div class="bg-white shadow-xl rounded-xl overflow-hidden">
                <div class="px-6 py-5 bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-gray-200">
                    <div class="flex items-center justify-between">
                        <h2 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                            <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                            </svg>
                            Code Review Analysis
                        </h2>
                        <div class="flex items-center gap-2">
                            <span class="text-sm font-medium text-gray-600" id="results-count">Loading...</span>
                        </div>
                    </div>
                </div>
                
                <div class="p-6">
                    ${this.buildSummaryDashboard(results)}
                    ${this.buildResultsControls()}
                    ${this.buildIssuesSection(results)}
                    ${this.buildExportControls()}
                    ${this.buildRawResultsSection(results)}
                </div>
            </div>
        `;
    }

    /**
     * Builds results filter and sort controls
     * @returns {string} HTML string
     */
    buildResultsControls() {
        return `
            <div class="results-controls mb-6 p-4 bg-gray-50 rounded-lg border border-gray-200">
                <div class="flex flex-wrap items-center gap-4 mb-4">
                    <div class="search-box">
                        <svg class="w-5 h-5 search-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
                        </svg>
                        <input type="text" class="search-input" id="issue-search" placeholder="Search issues...">
                    </div>
                    
                    <div class="filter-group">
                        <label class="filter-label font-medium text-sm text-gray-700">Filter by Severity:</label>
                        <select class="filter-select" id="severity-filter">
                            <option value="all">All Severities</option>
                            <option value="Critical">Critical</option>
                            <option value="Warning">Warning</option>
                            <option value="Suggestion">Suggestion</option>
                            <option value="Style">Style</option>
                            <option value="Info">Info</option>
                        </select>
                    </div>
                    
                    <div class="filter-group">
                        <label class="filter-label font-medium text-sm text-gray-700">Filter by Category:</label>
                        <select class="filter-select" id="category-filter">
                            <option value="all">All Categories</option>
                            <option value="Security">Security</option>
                            <option value="Performance">Performance</option>
                            <option value="Style">Style</option>
                            <option value="ErrorHandling">Error Handling</option>
                            <option value="Maintainability">Maintainability</option>
                            <option value="Readability">Readability</option>
                        </select>
                    </div>
                </div>
                
                <div class="flex flex-wrap items-center gap-4">
                    <div class="filter-group">
                        <label class="filter-label font-medium text-sm text-gray-700">Group by:</label>
                        <select class="filter-select" id="group-by-filter">
                            <option value="severity">Severity</option>
                            <option value="file">File</option>
                            <option value="category">Category</option>
                            <option value="none">No Grouping</option>
                        </select>
                    </div>
                    
                    <div class="sort-group">
                        <label class="filter-label font-medium text-sm text-gray-700">Sort by:</label>
                        <div class="flex gap-1">
                            <button class="sort-button" data-sort="severity" title="Sort by severity">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                                </svg>
                                Severity
                            </button>
                            <button class="sort-button" data-sort="file" title="Sort by file">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                                </svg>
                                File
                            </button>
                            <button class="sort-button" data-sort="category" title="Sort by category">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path>
                                </svg>
                                Category
                            </button>
                        </div>
                    </div>
                    
                    <div class="flex items-center gap-2">
                        <input type="checkbox" id="show-only-fixable" class="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500">
                        <label for="show-only-fixable" class="text-sm font-medium text-gray-700">Show only fixable issues</label>
                    </div>
                </div>
                
                <div class="mt-3 flex items-center gap-2 text-sm text-gray-500">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                    <span>Use filters to focus on specific issue types</span>
                </div>
            </div>
        `;
    }

    /**
     * Builds HTML for string results (when AI returns plain text)
     * @param {string} resultText Analysis result text
     * @returns {string} HTML string
     */
    buildStringResults(resultText) {
        // Parse the text to extract structured data
        const parsedResults = this.parseTextResults(resultText);
        return this.buildResultsHTML(parsedResults);
    }

    /**
     * Parses text results into structured format
     * @param {string} resultText Analysis result text
     * @returns {Object} Structured results object
     */
    parseTextResults(resultText) {
        const lines = resultText.split('\n');
        const feedback = [];
        
        lines.forEach(line => {
            if (line.includes('**Critical**')) {
                feedback.push({
                    severity: 'Critical',
                    message: line.replace('**Critical**', '').trim(),
                    filePath: this.extractFileFromLine(line),
                    lineNumber: this.extractLineFromLine(line),
                    category: 'Security'
                });
            } else if (line.includes('**Warning**')) {
                feedback.push({
                    severity: 'Warning',
                    message: line.replace('**Warning**', '').trim(),
                    filePath: this.extractFileFromLine(line),
                    lineNumber: this.extractLineFromLine(line),
                    category: 'Performance'
                });
            } else if (line.includes('**Suggestion**')) {
                feedback.push({
                    severity: 'Suggestion',
                    message: line.replace('**Suggestion**', '').trim(),
                    category: 'General'
                });
            }
        });

        return {
            feedback,
            summary: {
                totalIssues: feedback.length,
                critical: feedback.filter(i => i.severity === 'Critical').length,
                warning: feedback.filter(i => i.severity === 'Warning').length,
                suggestion: feedback.filter(i => i.severity === 'Suggestion').length
            }
        };
    }

    /**
     * Extracts file name from a line of text
     * @param {string} line Text line
     * @returns {string} File name or empty string
     */
    extractFileFromLine(line) {
        const match = line.match(/\*\*(.*?)\.(cs|py|js|ts|java|rb|php|go|rs|cpp|c)\b/);
        return match ? match[1] + '.' + match[2] : '';
    }

    /**
     * Extracts line number from a line of text
     * @param {string} line Text line
     * @returns {number} Line number or null
     */
    extractLineFromLine(line) {
        const match = line.match(/Line (\d+)/);
        return match ? parseInt(match[1]) : null;
    }

    /**
     * Builds the summary section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildSummaryDashboard(results) {
        const feedback = results.feedback || [];
        const summary = results.summary || {
            totalIssues: feedback.length,
            critical: feedback.filter(i => i.severity === 'Critical').length,
            warning: feedback.filter(i => i.severity === 'Warning').length,
            suggestion: feedback.filter(i => i.severity === 'Suggestion').length,
            style: feedback.filter(i => i.severity === 'Style').length,
            info: feedback.filter(i => i.severity === 'Info').length
        };

        const riskLevel = this.calculateRiskLevel(summary);
        const fileImpact = this.calculateFileImpact(feedback);
        const estimatedTime = this.calculateEstimatedTime(summary);

        return `
            <div class="summary-dashboard mb-6 bg-gradient-to-br from-slate-50 to-blue-50 rounded-xl p-6 border border-gray-200 shadow-sm">
                <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <!-- Risk Assessment -->
                    <div class="risk-assessment">
                        <h3 class="text-lg font-semibold text-gray-900 mb-3 flex items-center gap-2">
                            <svg class="w-5 h-5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
                            </svg>
                            Risk Assessment
                        </h3>
                        <div class="risk-level ${riskLevel.toLowerCase()}">
                            <span class="risk-label">${riskLevel} Risk</span>
                            <div class="risk-bar">
                                <div class="risk-progress ${riskLevel.toLowerCase()}" style="width: ${riskLevel === 'High' ? '90%' : riskLevel === 'Medium' ? '60%' : '30%'}"></div>
                            </div>
                        </div>
                    </div>

                    <!-- Issue Statistics -->
                    <div class="issue-statistics">
                        <h3 class="text-lg font-semibold text-gray-900 mb-3 flex items-center gap-2">
                            <svg class="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"></path>
                            </svg>
                            Issue Breakdown
                        </h3>
                        <div class="stats-grid">
                            <div class="stat-item critical">
                                <span class="stat-number">${summary.critical || 0}</span>
                                <span class="stat-label">Critical</span>
                            </div>
                            <div class="stat-item warning">
                                <span class="stat-number">${summary.warning || 0}</span>
                                <span class="stat-label">Warnings</span>
                            </div>
                            <div class="stat-item suggestion">
                                <span class="stat-number">${summary.suggestion || 0}</span>
                                <span class="stat-label">Suggestions</span>
                            </div>
                            <div class="stat-item total">
                                <span class="stat-number">${summary.totalIssues || 0}</span>
                                <span class="stat-label">Total Issues</span>
                            </div>
                        </div>
                    </div>

                    <!-- Impact Analysis -->
                    <div class="impact-analysis">
                        <h3 class="text-lg font-semibold text-gray-900 mb-3 flex items-center gap-2">
                            <svg class="w-5 h-5 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                            </svg>
                            Impact Analysis
                        </h3>
                        <div class="impact-items">
                            <div class="impact-item">
                                <span class="impact-label">Estimated Fix Time:</span>
                                <span class="impact-value">${estimatedTime}</span>
                            </div>
                            ${fileImpact.length > 0 ? `
                            <div class="impact-item">
                                <span class="impact-label">Most Impacted File:</span>
                                <span class="impact-value file-impact" title="${fileImpact[0][0]}">
                                    ${this.getFileName(fileImpact[0][0])} (${fileImpact[0][1]} issues)
                                </span>
                            </div>
                            ` : ''}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Calculates risk level based on issue counts
     * @param {Object} summary Summary statistics
     * @returns {string} Risk level (High/Medium/Low)
     */
    calculateRiskLevel(summary) {
        if (summary.critical > 0) return 'High';
        if (summary.warning > 2 || summary.totalIssues > 5) return 'Medium';
        return 'Low';
    }

    calculateEstimatedTime(summary) {
        const criticalTime = (summary.critical || 0) * 30; // 30 min per critical
        const warningTime = (summary.warning || 0) * 15;  // 15 min per warning
        const suggestionTime = (summary.suggestion || 0) * 5; // 5 min per suggestion
        
        const totalMinutes = criticalTime + warningTime + suggestionTime;
        
        if (totalMinutes === 0) return 'No fixes needed';
        if (totalMinutes < 60) return `${totalMinutes} minutes`;
        
        const hours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        
        if (minutes === 0) return `${hours} hour${hours !== 1 ? 's' : ''}`;
        return `${hours} hour${hours !== 1 ? 's' : ''} ${minutes} minutes`;
    }

    /**
     * Calculates file impact statistics
     * @param {Array} feedback Feedback items
     * @returns {Array} Array of [file, count] pairs
     */
    calculateFileImpact(feedback) {
        const fileCounts = {};
        feedback.forEach(item => {
            if (item.filePath) {
                fileCounts[item.filePath] = (fileCounts[item.filePath] || 0) + 1;
            }
        });
        
        return Object.entries(fileCounts)
            .sort(([,a], [,b]) => b - a)
            .slice(0, 5);
    }

    /**
     * Builds the issues section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildIssuesSection(results) {
        const feedback = results.feedback || [];
        
        if (feedback.length === 0) {
            return `
                <div class="mb-6">
                    <div class="bg-green-50 border border-green-200 rounded-xl p-6 text-center">
                        <div class="flex flex-col items-center">
                            <svg class="h-12 w-12 text-green-500 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                            </svg>
                            <h3 class="text-lg font-semibold text-green-800 mb-1">Excellent Work!</h3>
                            <p class="text-green-600">No issues found. Your code meets quality standards.</p>
                        </div>
                    </div>
                </div>
            `;
        }

        const filteredFeedback = this.filterAndSortFeedback(feedback);
        const groupedIssues = this.groupIssues(filteredFeedback);
        
        return `
            <div class="mb-6">
                <div class="flex items-center justify-between mb-4">
                    <h3 class="text-xl font-bold text-gray-900">Issues Found</h3>
                    <div class="flex items-center gap-2 text-sm text-gray-500">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                        </svg>
                        <span>Click to expand/collapse sections</span>
                    </div>
                </div>
                
                <div id="issues-container">
                    ${this.buildGroupedIssues(groupedIssues)}
                </div>
            </div>
        `;
    }

    groupIssues(issues) {
        const groupBy = this.filterState.groupBy;
        
        if (groupBy === 'none') {
            return { 'All Issues': issues };
        }
        
        const groups = {};
        
        issues.forEach(issue => {
            const groupKey = issue[groupBy] || 'Unknown';
            if (!groups[groupKey]) {
                groups[groupKey] = [];
            }
            groups[groupKey].push(issue);
        });
        
        return groups;
    }

    buildGroupedIssues(groupedIssues) {
        let html = '';
        
        Object.entries(groupedIssues).forEach(([groupName, issues]) => {
            const severityCounts = this.getSeverityCounts(issues);
            const isExpanded = this.expandedSections[groupName.toLowerCase()] !== false;
            
            html += `
                <div class="issue-group mb-4 border border-gray-200 rounded-lg overflow-hidden">
                    <div class="group-header bg-gray-50 px-4 py-3 cursor-pointer border-b border-gray-200"
                         data-group="${groupName}">
                        <div class="flex items-center justify-between">
                            <div class="flex items-center gap-3">
                                <h4 class="text-lg font-semibold text-gray-900">${groupName}</h4>
                                ${this.buildGroupBadges(severityCounts)}
                            </div>
                            <svg class="w-5 h-5 text-gray-400 transform ${isExpanded ? 'rotate-180' : ''}"
                                 fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                            </svg>
                        </div>
                    </div>
                    <div class="group-content ${isExpanded ? '' : 'hidden'}">
                        ${issues.map(issue => this.buildIssueCard(issue)).join('')}
                    </div>
                </div>
            `;
        });
        
        return html;
    }

    getSeverityCounts(issues) {
        return {
            critical: issues.filter(i => i.severity === 'Critical').length,
            warning: issues.filter(i => i.severity === 'Warning').length,
            suggestion: issues.filter(i => i.severity === 'Suggestion').length,
            style: issues.filter(i => i.severity === 'Style').length,
            info: issues.filter(i => i.severity === 'Info').length
        };
    }

    buildGroupBadges(counts) {
        return `
            <div class="flex items-center gap-2">
                ${counts.critical > 0 ? `
                    <span class="badge critical">
                        ${counts.critical} Critical
                    </span>
                ` : ''}
                ${counts.warning > 0 ? `
                    <span class="badge warning">
                        ${counts.warning} Warning${counts.warning !== 1 ? 's' : ''}
                    </span>
                ` : ''}
                ${counts.suggestion > 0 ? `
                    <span class="badge suggestion">
                        ${counts.suggestion} Suggestion${counts.suggestion !== 1 ? 's' : ''}
                    </span>
                ` : ''}
            </div>
        `;
    }

    /**
     * Filters and sorts feedback based on current filter state
     * @param {Array} feedback Feedback items
     * @returns {Array} Filtered and sorted feedback
     */
    filterAndSortFeedback(feedback) {
        let filtered = feedback.filter(item => {
            const severityMatch = this.filterState.severity === 'all' ||
                                item.severity === this.filterState.severity;
            const categoryMatch = this.filterState.category === 'all' ||
                                 (item.category && item.category === this.filterState.category);
            
            // Search filter
            const searchTerm = this.filterState.search.toLowerCase();
            const searchMatch = !searchTerm ||
                               (item.message && item.message.toLowerCase().includes(searchTerm)) ||
                               (item.filePath && item.filePath.toLowerCase().includes(searchTerm)) ||
                               (item.category && item.category.toLowerCase().includes(searchTerm));
            
            return severityMatch && categoryMatch && searchMatch;
        });

        // Sort the filtered results
        filtered.sort((a, b) => {
            switch (this.filterState.sortBy) {
                case 'file':
                    return (a.filePath || '').localeCompare(b.filePath || '');
                case 'category':
                    return (a.category || '').localeCompare(b.category || '');
                case 'severity':
                default:
                    const severityOrder = { Critical: 0, Warning: 1, Suggestion: 2, Style: 3, Info: 4 };
                    return severityOrder[a.severity] - severityOrder[b.severity];
            }
        });

        return filtered;
    }

    /**
     * Builds individual issue card
     * @param {Object} issue Issue object
     * @returns {string} HTML string
     */
    buildIssueCard(issue) {
        const severity = issue.severity || 'Suggestion';
        const icon = this.getSeverityIcon(severity);
        const actionType = this.getActionType(severity);
        
        return `
            <div class="issue-card ${severity.toLowerCase()}" data-severity="${severity}" data-category="${issue.category || 'General'}">
                <div class="issue-header">
                    <div class="issue-header-content">
                        <div class="flex items-start justify-between">
                            <div class="flex-1 min-w-0">
                                <h4 class="issue-title">${this.escapeHtml(issue.message || 'No message')}</h4>
                                <div class="issue-meta">
                                    <span class="issue-severity ${severity.toLowerCase()}">
                                        ${icon} ${severity}
                                    </span>
                                    ${issue.filePath ? `
                                    <div class="issue-location" data-file-path="${issue.filePath}${issue.lineNumber ? `:${issue.lineNumber}` : ''}">
                                        <h5 class="text-sm font-medium text-gray-900">${issue.filePath}${issue.lineNumber ? `:${issue.lineNumber}` : ''}</h5>
                                        <button class="copy-path-btn ml-1" title="Copy file path">
                                            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                                            </svg>
                                        </button>
                                    </div>
                                    ` : ''}
                                    ${issue.category ? `
                                    <span class="issue-category">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path>
                                        </svg>
                                        ${issue.category}
                                    </span>
                                    ` : ''}
                                </div>
                            </div>
                            <div class="action-indicator ${actionType}">
                                ${this.getActionIcon(actionType)} ${this.getActionText(actionType)}
                            </div>
                        </div>
                    </div>
                    <button class="issue-expand-toggle" aria-label="Toggle details">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                        </svg>
                    </button>
                </div>
                
                <div class="issue-content hidden">
                    <div class="issue-description">
                        <div class="description-content">
                            ${this.formatIssueDescription(issue.message)}
                        </div>
                    </div>
                    
                    ${issue.suggestion ? `
                    <div class="issue-suggestion">
                        <div class="suggestion-header">
                            <svg class="w-5 h-5 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"></path>
                            </svg>
                            <span>Recommended Solution</span>
                        </div>
                        <div class="suggestion-content">
                            <p>${this.escapeHtml(issue.suggestion)}</p>
                        </div>
                    </div>
                    ` : ''}
                    
                    ${issue.codeSnippet ? `
                    <div class="issue-code-snippet">
                        <div class="code-header">
                            <span>Code Reference</span>
                            <button class="copy-code-btn" data-code="${this.escapeHtml(issue.codeSnippet)}">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                                </svg>
                                Copy Code
                            </button>
                        </div>
                        <div class="code-content">
                            <pre><code>${this.escapeHtml(issue.codeSnippet)}</code></pre>
                        </div>
                    </div>
                    ` : ''}
                </div>
            </div>
        `;
    }

    getActionType(severity) {
        const actions = {
            Critical: 'fix',
            Warning: 'fix',
            Suggestion: 'review',
            Style: 'consider',
            Info: 'consider'
        };
        return actions[severity] || 'review';
    }

    /**
     * Gets action text based on action type
     * @param {string} actionType Action type
     * @returns {string} Action text
     */
    getActionText(actionType) {
        const actionTexts = {
            fix: 'Fix',
            review: 'Review',
            consider: 'Consider'
        };
        return actionTexts[actionType] || 'Review';
    }

    /**
     * Gets action icon based on action type
     * @param {string} actionType Action type
     * @returns {string} Icon SVG
     */
    getActionIcon(actionType) {
        const icons = {
            fix: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
            </svg>`,
            review: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
            </svg>`,
            consider: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"></path>
            </svg>`
        };
        return icons[actionType] || icons.review;
    }

    /**
     * Gets CSS classes for severity levels
     * @param {string} severity Severity level
     * @returns {Object} CSS classes for different elements
     */
    getSeverityClasses(severity) {
        const classes = {
            Critical: {
                card: 'border-l-4 border-red-500 bg-white',
                badge: 'bg-red-100 text-red-800',
                solutionBg: 'bg-red-50',
                solutionText: 'text-red-700'
            },
            Warning: {
                card: 'border-l-4 border-orange-500 bg-white',
                badge: 'bg-orange-100 text-orange-800',
                solutionBg: 'bg-orange-50',
                solutionText: 'text-orange-700'
            },
            Suggestion: {
                card: 'border-l-4 border-blue-500 bg-white',
                badge: 'bg-blue-100 text-blue-800',
                solutionBg: 'bg-blue-50',
                solutionText: 'text-blue-700'
            },
            Style: {
                card: 'border-l-4 border-purple-500 bg-white',
                badge: 'bg-purple-100 text-purple-800',
                solutionBg: 'bg-purple-50',
                solutionText: 'text-purple-700'
            },
            Info: {
                card: 'border-l-4 border-gray-500 bg-white',
                badge: 'bg-gray-100 text-gray-800',
                solutionBg: 'bg-gray-50',
                solutionText: 'text-gray-700'
            }
        };
        return classes[severity] || classes.Suggestion;
    }

    /**
     * Gets severity icon based on severity level
     * @param {string} severity Severity level
     * @returns {string} Icon emoji
     */
    getSeverityIcon(severity) {
        const icons = {
            Critical: `<svg class="w-5 h-5 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
            </svg>`,
            Warning: `<svg class="w-5 h-5 text-orange-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path>
            </svg>`,
            Suggestion: `<svg class="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"></path>
            </svg>`,
            Style: `<svg class="w-5 h-5 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zM21 5a2 2 0 00-2-2h-4a2 2 0 00-2 2v12a4 4 0 004 4h4a2 2 0 002-2V5z"></path>
            </svg>`,
            Info: `<svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>`
        };
        return icons[severity] || icons.Suggestion;
    }

    /**
     * Extracts file name from full path
     * @param {string} filePath Full file path
     * @returns {string} File name
     */
    getFileName(filePath) {
        return filePath.split(/[\\/]/).pop() || filePath;
    }

    /**
     * Formats issue description with proper line breaks
     * @param {string} description Issue description
     * @returns {string} Formatted HTML
     */
    formatIssueDescription(description) {
        if (!description) return '';
        return this.escapeHtml(description).replace(/\n/g, '<br>');
    }

    /**
     * Escapes HTML special characters
     * @param {string} text Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Builds the raw results section
     * @param {Object} results Analysis results
     * @returns {string} HTML string
     */
    buildRawResultsSection(results) {
        return `
            <div class="mt-6">
                <div class="admin-toggle hidden">
                    <button class="text-sm text-gray-500 hover:text-gray-700 underline" id="toggle-raw-data">
                        Show Raw Analysis Data
                    </button>
                </div>
                <div id="raw-data-section" class="hidden mt-2">
                    <pre class="text-xs text-gray-600 bg-gray-100 p-4 rounded-md overflow-auto max-h-64">${JSON.stringify(results, null, 2)}</pre>
                </div>
            </div>
        `;
    }

    /**
     * Builds export controls
     * @returns {string} HTML string
     */
    buildExportControls() {
        return `
            <div class="export-controls">
                <button class="export-button" id="export-json-btn">
                    📋 Export as JSON
                </button>
                <button class="export-button" id="export-markdown-btn">
                    📝 Export as Markdown
                </button>
            </div>
        `;
    }

    /**
     * Initializes event listeners for interactive elements
     */
    /**
     * Attaches document-level event listeners for event delegation
     */
    attachEventListeners() {
        document.addEventListener('click', this.handleDocumentClick.bind(this));
        document.addEventListener('change', this.handleDocumentChange.bind(this));
        document.addEventListener('input', this.handleDocumentInput.bind(this));
    }

    /**
     * Removes document-level event listeners during cleanup
     */
    removeEventListeners() {
        document.removeEventListener('click', this.handleDocumentClick);
        document.removeEventListener('change', this.handleDocumentChange);
        document.removeEventListener('input', this.handleDocumentInput);
    }

    /**
     * Handles document-level click events using event delegation
     * @param {Event} event Click event
     */
    handleDocumentClick(event) {
        // Handle group header toggles
        const groupHeader = event.target.closest('.group-header');
        if (groupHeader) {
            const groupName = groupHeader.dataset.group;
            const content = groupHeader.nextElementSibling;
            const icon = groupHeader.querySelector('svg');
            
            content.classList.toggle('hidden');
            icon.classList.toggle('rotate-180');
            this.expandedSections[groupName.toLowerCase()] = !content.classList.contains('hidden');
            return;
        }

        // Handle issue expand toggles
        const expandToggle = event.target.closest('.issue-expand-toggle');
        if (expandToggle) {
            event.stopPropagation();
            const content = expandToggle.closest('.issue-header').nextElementSibling;
            content.classList.toggle('hidden');
            
            const icon = expandToggle.querySelector('svg');
            if (icon) {
                icon.classList.toggle('rotate-180');
            }
            return;
        }

        // Handle copy code buttons
        const copyCodeBtn = event.target.closest('.copy-code-btn');
        if (copyCodeBtn) {
            event.stopPropagation();
            const code = copyCodeBtn.dataset.code;
            navigator.clipboard.writeText(code).then(() => {
                const originalHtml = copyCodeBtn.innerHTML;
                copyCodeBtn.innerHTML = `
                    <svg class="w-4 h-4 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                    </svg>
                    Copied!
                `;
                setTimeout(() => {
                    copyCodeBtn.innerHTML = originalHtml;
                }, 2000);
            });
            return;
        }

        // Handle copy path buttons
        const copyPathBtn = event.target.closest('.copy-path-btn');
        if (copyPathBtn) {
            event.stopPropagation();
            const locationSpan = copyPathBtn.closest('.issue-location');
            const filePath = locationSpan.dataset.filePath;
            
            navigator.clipboard.writeText(filePath).then(() => {
                const originalHtml = copyPathBtn.innerHTML;
                copyPathBtn.innerHTML = `
                    <svg class="w-3 h-3 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                    </svg>
                `;
                setTimeout(() => {
                    copyPathBtn.innerHTML = originalHtml;
                }, 2000);
            });
            return;
        }

        // Handle sort buttons
        const sortButton = event.target.closest('.sort-button');
        if (sortButton) {
            const sortBy = sortButton.dataset.sort;
            this.filterState.sortBy = sortBy;
            
            // Update active state
            document.querySelectorAll('.sort-button').forEach(btn => btn.classList.remove('active'));
            sortButton.classList.add('active');
            
            this.updateResultsDisplay();
            return;
        }

        // Handle export buttons
        const exportButton = event.target.closest('.export-button');
        if (exportButton) {
            const format = exportButton.id === 'export-json-btn' ? 'json' : 'markdown';
            this.exportResults(format);
            return;
        }

        // Handle raw data toggle
        if (event.target.matches('#toggle-raw-data')) {
            const rawDataSection = document.getElementById('raw-data-section');
            if (rawDataSection) {
                const isHidden = rawDataSection.classList.contains('hidden');
                rawDataSection.classList.toggle('hidden');
                event.target.textContent = isHidden ? 'Hide Raw Analysis Data' : 'Show Raw Analysis Data';
            }
            return;
        }
    }

    /**
     * Handles document-level change events for filter controls
     * @param {Event} event Change event
     */
    handleDocumentChange(event) {
        const target = event.target;
        
        if (target.matches('#severity-filter')) {
            this.filterState.severity = target.value;
            this.updateResultsDisplay();
        }
        else if (target.matches('#category-filter')) {
            this.filterState.category = target.value;
            this.updateResultsDisplay();
        }
        else if (target.matches('#group-by-filter')) {
            this.filterState.groupBy = target.value;
            this.updateResultsDisplay();
        }
        else if (target.matches('#show-only-fixable')) {
            this.filterState.showOnlyFixable = target.checked;
            this.updateResultsDisplay();
        }
    }

    /**
     * Handles document-level input events for search field
     * @param {Event} event Input event
     */
    handleDocumentInput(event) {
        if (event.target.matches('#issue-search')) {
            this.filterState.search = event.target.value;
            this.updateResultsDisplay();
        }
    }

    /**
     * Updates the results display based on current filters
     */
    updateResultsDisplay() {
        if (!this.currentResults || !this.resultsContainer) return;
        
        const feedback = this.currentResults.feedback || [];
        const filteredFeedback = this.filterAndSortFeedback(feedback);
        const groupedIssues = this.groupIssues(filteredFeedback);
        
        const issuesContainer = document.getElementById('issues-container');
        if (issuesContainer) {
            issuesContainer.innerHTML = this.buildGroupedIssues(groupedIssues);
        }
        
        this.updateResultsCount();
    }

    updateResultsCount() {
        const feedback = this.currentResults?.feedback || [];
        const filteredFeedback = this.filterAndSortFeedback(feedback);
        const resultsCount = document.getElementById('results-count');
        
        if (resultsCount) {
            resultsCount.textContent = `${filteredFeedback.length} issue${filteredFeedback.length !== 1 ? 's' : ''} found`;
        }
    }

    // Removed: Replaced by event delegation

    /**
     * Exports results in specified format
     * @param {string} format Export format (json/markdown)
     */
    exportResults(format) {
        if (!this.currentResults) return;
        
        let content, filename, mimeType;
        
        if (format === 'json') {
            content = JSON.stringify(this.currentResults, null, 2);
            filename = 'code-review-results.json';
            mimeType = 'application/json';
        } else if (format === 'markdown') {
            content = this.convertToMarkdown(this.currentResults);
            filename = 'code-review-results.md';
            mimeType = 'text/markdown';
        }
        
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }

    /**
     * Converts results to markdown format
     * @param {Object} results Analysis results
     * @returns {string} Markdown content
     */
    convertToMarkdown(results) {
        const feedback = results.feedback || [];
        let markdown = '# Code Review Results\n\n';
        
        // Summary section
        const summary = results.summary || { totalIssues: feedback.length };
        markdown += `## Summary\n`;
        markdown += `- **Total Issues**: ${summary.totalIssues || 0}\n`;
        markdown += `- **Critical**: ${summary.critical || 0}\n`;
        markdown += `- **Warnings**: ${summary.warning || 0}\n`;
        markdown += `- **Suggestions**: ${summary.suggestion || 0}\n\n`;
        
        // Issues section
        if (feedback.length > 0) {
            markdown += `## Issues Found\n\n`;
            feedback.forEach((issue, index) => {
                markdown += `### ${index + 1}. ${issue.severity}: ${issue.message}\n`;
                if (issue.filePath) {
                    markdown += `- **File**: ${issue.filePath}`;
                    if (issue.lineNumber) markdown += `:${issue.lineNumber}`;
                    markdown += `\n`;
                }
                if (issue.category) {
                    markdown += `- **Category**: ${issue.category}\n`;
                }
                if (issue.suggestion) {
                    markdown += `- **Suggestion**: ${issue.suggestion}\n`;
                }
                if (issue.codeSnippet) {
                    markdown += `\n\`\`\`${this.getLanguageFromFile(issue.filePath)}\n${issue.codeSnippet}\n\`\`\`\n`;
                }
                markdown += `\n`;
            });
        } else {
            markdown += `## No Issues Found\n\nYour code looks great! ✅\n`;
        }
        
        return markdown;
    }

    /**
     * Gets programming language from file extension
     * @param {string} filePath File path
     * @returns {string} Language identifier
     */
    getLanguageFromFile(filePath) {
        if (!filePath) return '';
        const ext = filePath.split('.').pop().toLowerCase();
        const langMap = {
            'cs': 'csharp',
            'py': 'python',
            'js': 'javascript',
            'ts': 'typescript',
            'java': 'java',
            'rb': 'ruby',
            'php': 'php',
            'go': 'go',
            'rs': 'rust',
            'cpp': 'cpp',
            'c': 'c',
            'html': 'html',
            'css': 'css',
            'json': 'json',
            'xml': 'xml'
        };
        return langMap[ext] || '';
    }

    /**
     * Hides all display states
     */
    hideAllStates() {
        if (this.loadingContainer) this.loadingContainer.style.display = 'none';
        if (this.errorContainer) this.errorContainer.style.display = 'none';
        if (this.resultsContainer) this.resultsContainer.style.display = 'none';
        this.removeEventListeners();
    }
}

// Create singleton instance
export const resultsDisplay = new ResultsDisplay();