/**
 * US-007 Display Results - Interactive functionality for split-pane code review interface
 */

class ResultsApp {
    constructor() {
        this.analysisId = window.analysisId || '';
        this.analysisResults = null;
        this.diffContent = '';
        this.currentFile = '';
        this.files = {};
        
        // Initialize components
        this.feedbackProcessor = new FeedbackProcessor();
        this.diffRenderer = new DiffRenderer();
        this.interactiveLinker = new InteractiveLinker(this.feedbackProcessor, this.diffRenderer);
        this.responsiveHandler = new ResponsiveHandler();
        
        this.init();
    }

    async init() {
        try {
            this.setupEventListeners();
            this.showLoadingState();
            
            if (!this.analysisId) {
                this.showError('No analysis ID provided');
                return;
            }

            // Load and process data
            await this.loadAnalysisData();
            
            // Render UI
            this.renderUI();
            
            // Setup interactions
            this.setupInteractivity();
            
            this.hideLoadingState();
            
        } catch (error) {
            this.showError(`Failed to load analysis results: ${error.message}`);
        }
    }

    setupEventListeners() {
        // Navigation buttons
        document.getElementById('newAnalysisBtn').addEventListener('click', () => {
            window.location.href = '/';
        });

        document.getElementById('exportBtn').addEventListener('click', () => {
            this.exportResults();
        });

        // Mobile pane toggles
        document.querySelectorAll('.mobile-toggle').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const pane = e.target.dataset.pane;
                this.responsiveHandler.togglePane(pane);
            });
        });

        // Pane expansion
        document.getElementById('expandLeftPane').addEventListener('click', () => {
            this.togglePaneExpansion('left');
        });

        document.getElementById('expandRightPane').addEventListener('click', () => {
            this.togglePaneExpansion('right');
        });

        // Filter controls
        document.querySelectorAll('.filter-checkbox input').forEach(checkbox => {
            checkbox.addEventListener('change', () => {
                this.applyFilters();
            });
        });

        // File selector
        document.getElementById('fileSelector').addEventListener('change', (e) => {
            this.selectFile(e.target.value);
        });

        // Error modal
        document.getElementById('closeErrorModal').addEventListener('click', () => {
            this.hideErrorModal();
        });

        document.getElementById('retryBtn').addEventListener('click', () => {
            this.hideErrorModal();
            this.init();
        });

        document.getElementById('returnHomeBtn').addEventListener('click', () => {
            window.location.href = '/';
        });
    }

    async loadAnalysisData() {
        try {
            console.log('Loading analysis data for ID:', this.analysisId);
            
            // Load analysis results and diff in parallel
            const [resultsResponse, diffResponse] = await Promise.all([
                fetch(`/api/results/${this.analysisId}`),
                fetch(`/api/diff/${this.analysisId}`)
            ]);

            console.log('Results response status:', resultsResponse.status);
            console.log('Diff response status:', diffResponse.status);

            if (!resultsResponse.ok) {
                throw new Error(`Results API error: ${resultsResponse.status}`);
            }

            this.analysisResults = await resultsResponse.json();
            this.diffContent = await diffResponse.text();

            console.log('Analysis results loaded:', this.analysisResults);
            console.log('Diff content length:', this.diffContent.length);

            // Process diff content
            this.files = this.diffRenderer.renderDiff(this.diffContent);

        } catch (error) {
            console.error('Error loading analysis data:', error);
            throw new Error(`Failed to load analysis data: ${error.message}`);
        }
    }

    renderUI() {
        this.renderSummary();
        this.renderDiff();
        this.renderFeedback();
    }

    renderSummary() {
        const summarySection = document.getElementById('summarySection');
        const feedback = this.analysisResults?.feedback || [];
        
        // Count by severity
        const counts = {
            total: feedback.length,
            critical: feedback.filter(f => f.severity === 'Critical').length,
            warning: feedback.filter(f => f.severity === 'Warning').length,
            suggestion: feedback.filter(f => f.severity === 'Suggestion').length,
            style: feedback.filter(f => f.severity === 'Style').length
        };

        // Update counts
        document.getElementById('totalIssues').textContent = counts.total;
        document.getElementById('criticalCount').textContent = counts.critical;
        document.getElementById('warningCount').textContent = counts.warning;
        document.getElementById('suggestionCount').textContent = counts.suggestion;
        document.getElementById('styleCount').textContent = counts.style;

        // Update status
        const statusText = document.querySelector('.status-text');
        const statusIndicator = document.querySelector('.status-indicator');
        
        if (counts.critical > 0) {
            statusText.textContent = `${counts.critical} critical issues found`;
            statusIndicator.style.backgroundColor = 'var(--critical-color)';
        } else if (counts.warning > 0) {
            statusText.textContent = `${counts.warning} warnings found`;
            statusIndicator.style.backgroundColor = 'var(--warning-color)';
        } else if (counts.total > 0) {
            statusText.textContent = 'Analysis complete - review suggestions';
            statusIndicator.style.backgroundColor = 'var(--suggestion-color)';
        } else {
            statusText.textContent = 'Analysis complete - no issues found';
            statusIndicator.style.backgroundColor = 'var(--style-color)';
        }

        summarySection.style.display = 'block';
    }

    renderDiff() {
        const container = document.getElementById('diffContainer');
        const fileSelector = document.getElementById('fileSelector');
        const noDiffMessage = document.getElementById('noDiffMessage');
        
        // Clear existing content
        container.innerHTML = '';
        fileSelector.innerHTML = '<option value="">Select file...</option>';

        const files = Object.keys(this.files);
        
        if (files.length === 0) {
            container.style.display = 'none';
            noDiffMessage.style.display = 'flex';
            return;
        }

        // Populate file selector
        files.forEach(file => {
            const option = document.createElement('option');
            option.value = file;
            option.textContent = file;
            fileSelector.appendChild(option);
        });

        // Select first file by default
        if (files.length > 0) {
            this.selectFile(files[0]);
        }

        container.style.display = 'block';
        noDiffMessage.style.display = 'none';
    }

    renderFeedback() {
        const container = document.getElementById('feedbackContainer');
        const noFeedbackMessage = document.getElementById('noFeedbackMessage');
        const feedback = this.analysisResults?.feedback || [];

        console.log('Rendering feedback:', feedback.length, 'items');
        console.log('Analysis results:', this.analysisResults);

        container.innerHTML = '';

        if (feedback.length === 0) {
            console.log('No feedback items found');
            container.style.display = 'none';
            noFeedbackMessage.style.display = 'flex';
            return;
        }

        // Create feedback list
        const feedbackList = document.createElement('div');
        feedbackList.className = 'feedback-list';

        feedback.forEach((item, index) => {
            console.log('Creating feedback element', index, item);
            const feedbackElement = this.createFeedbackElement(item);
            feedbackList.appendChild(feedbackElement);
        });

        container.appendChild(feedbackList);
        container.style.display = 'block';
        noFeedbackMessage.style.display = 'none';
        
        console.log('Feedback rendering complete');
    }

    createFeedbackElement(item) {
        const element = document.createElement('div');
        element.className = 'feedback-item';
        element.dataset.severity = item.severityString;
        element.dataset.filePath = item.filePath || '';
        element.dataset.lineNumber = item.lineNumber || '';
        
        // Enhanced HTML structure for better display
        element.innerHTML = `
            <div class="feedback-header">
                <span class="feedback-severity severity-${item.severityString.toLowerCase()}">
                    ${item.severityString}
                </span>
                <div class="feedback-meta">
                    ${item.filePath ? `<span class="feedback-file">${this.truncatePath(item.filePath)}</span>` : ''}
                    ${item.lineNumber ? `<span class="feedback-line">Line ${item.lineNumber}</span>` : ''}
                    ${item.categoryString ? `<span class="feedback-category">${item.categoryString}</span>` : ''}
                </div>
            </div>
            <div class="feedback-content">
                <div class="feedback-message">${this.escapeHtml(item.message)}</div>
                ${item.suggestion ? `
                    <div class="feedback-suggestion">
                        <div class="suggestion-header">
                            <i class="fas fa-lightbulb"></i>
                            <strong>Suggestion</strong>
                        </div>
                        <div class="suggestion-content">${this.escapeHtml(item.suggestion)}</div>
                    </div>
                ` : ''}
            </div>
        `;

        // Add click handler for line highlighting
        if (item.lineNumber) {
            element.addEventListener('click', () => {
                this.interactiveLinker.highlightCodeLine(item.lineNumber);
                this.interactiveLinker.scrollToLine(item.lineNumber);
            });
        }

        return element;
    }

    selectFile(filePath) {
        if (!filePath || !this.files[filePath]) {
            return;
        }

        this.currentFile = filePath;
        const container = document.getElementById('diffContainer');
        
        // Render the selected file
        const fileContent = this.files[filePath];
        container.innerHTML = '';

        fileContent.forEach(line => {
            const lineElement = document.createElement('div');
            lineElement.className = `diff-line ${line.type}`;
            lineElement.dataset.line = line.lineNumber || '';
            
            if (line.lineNumber) {
                lineElement.innerHTML = `
                    <span class="line-number">${line.lineNumber}</span>
                    <span class="line-content">${this.escapeHtml(line.content)}</span>
                `;
            } else {
                lineElement.innerHTML = `<span class="line-content">${this.escapeHtml(line.content)}</span>`;
            }
            
            container.appendChild(lineElement);
        });

        // Apply syntax highlighting
        this.applySyntaxHighlighting();
    }

    applySyntaxHighlighting() {
        const codeElements = document.querySelectorAll('.line-content');
        codeElements.forEach(element => {
            const highlighted = hljs.highlightAuto(element.textContent);
            element.innerHTML = highlighted.value;
        });
    }

    applyFilters() {
        console.log('Applying filters...');
        const checkboxes = document.querySelectorAll('.filter-checkbox input');
        console.log('Found checkboxes:', checkboxes.length);
        
        const checkedSeverities = Array.from(checkboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.dataset.severity);
        
        console.log('Checked severities:', checkedSeverities);

        const feedbackItems = document.querySelectorAll('.feedback-item');
        console.log('Found feedback items:', feedbackItems.length);
        
        feedbackItems.forEach(item => {
            const severity = item.dataset.severity;
            console.log('Processing item:', severity, 'checked:', checkedSeverities.includes(severity));
            
            if (checkedSeverities.includes(severity)) {
                item.classList.remove('hidden');
            } else {
                item.classList.add('hidden');
            }
        });
        
        console.log('Filter application complete');
    }

    exportResults() {
        if (!this.analysisResults) {
            return;
        }

        const exportData = {
            analysisId: this.analysisResults.analysisId,
            timestamp: new Date().toISOString(),
            summary: {
                total: this.analysisResults.feedback.length,
                critical: this.analysisResults.feedback.filter(f => f.severityString === 'Critical').length,
                warning: this.analysisResults.feedback.filter(f => f.severityString === 'Warning').length,
                suggestion: this.analysisResults.feedback.filter(f => f.severityString === 'Suggestion').length,
                style: this.analysisResults.feedback.filter(f => f.severityString === 'Style').length
            },
            feedback: this.analysisResults.feedback
        };

        const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `code-review-${this.analysisResults.analysisId}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }

    setupInteractivity() {
        this.interactiveLinker.setupFeedbackLinks();
        this.setupKeyboardShortcuts();
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey || e.metaKey) {
                switch (e.key) {
                    case 'f':
                        e.preventDefault();
                        this.focusFileSelector();
                        break;
                    case 'r':
                        e.preventDefault();
                        this.init();
                        break;
                    case 'e':
                        e.preventDefault();
                        this.exportResults();
                        break;
                }
            }
        });
    }

    focusFileSelector() {
        document.getElementById('fileSelector').focus();
    }

    togglePaneExpansion(pane) {
        const leftPane = document.getElementById('leftPane');
        const rightPane = document.getElementById('rightPane');
        
        if (pane === 'left') {
            leftPane.style.flex = leftPane.style.flex === '2' ? '1' : '2';
            rightPane.style.flex = '1';
        } else {
            rightPane.style.flex = rightPane.style.flex === '2' ? '1' : '2';
            leftPane.style.flex = '1';
        }
    }

    // Utility methods
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    truncatePath(path, maxLength = 50) {
        if (path.length <= maxLength) return path;
        
        const parts = path.split(/[\\/]/);
        if (parts.length <= 2) return path;
        
        const filename = parts[parts.length - 1];
        const parent = parts[parts.length - 2];
        return `.../${parent}/${filename}`;
    }

    showLoadingState() {
        document.querySelectorAll('.loading-message').forEach(el => {
            el.style.display = 'flex';
        });
        document.querySelectorAll('.no-content-message').forEach(el => {
            el.style.display = 'none';
        });
    }

    hideLoadingState() {
        document.querySelectorAll('.loading-message').forEach(el => {
            el.style.display = 'none';
        });
    }

    showError(message) {
        document.getElementById('errorMessage').textContent = message;
        document.getElementById('errorModal').style.display = 'flex';
    }

    hideErrorModal() {
        document.getElementById('errorModal').style.display = 'none';
    }
}

// Feedback Processor
class FeedbackProcessor {
    parseAIResponse(rawResponse) {
        const feedback = [];
        
        if (!rawResponse) return feedback;

        // Define patterns for different severity levels
        const patterns = {
            Critical: /(?:critical|error|must fix|security issue):\s*(.+?)(?:\n|$)/gi,
            Warning: /(?:warning|should|performance issue|potential problem):\s*(.+?)(?:\n|$)/gi,
            Suggestion: /(?:suggestion|consider|recommend|improvement):\s*(.+?)(?:\n|$)/gi,
            Style: /(?:style|formatting|naming|convention):\s*(.+?)(?:\n|$)/gi
        };

        // Extract feedback items using patterns
        Object.entries(patterns).forEach(([severity, pattern]) => {
            let match;
            while ((match = pattern.exec(rawResponse)) !== null) {
                if (match[1]) {
                    feedback.push({
                        severity: severity,
                        message: match[1].trim(),
                        filePath: this.extractFilePath(match[1]),
                        lineNumber: this.extractLineNumber(match[1]),
                        category: this.determineCategory(match[1])
                    });
                }
            }
        });

        return feedback;
    }

    extractFilePath(text) {
        const patterns = [
            /([A-Za-z]:\\[^:\n]+)/, // Windows paths
            /([A-Za-z0-9_/\\]+\.(cs|js|ts|html|css|json|xml|config))/, // Relative paths
            /(\.\/[^:\n]+)/, // Unix-style paths
            /([A-Za-z0-9_\-]+\.(cs|js|ts|html|css|json|xml|config))/ // Just filename
        ];

        for (const pattern of patterns) {
            const match = text.match(pattern);
            if (match) return match[1];
        }
        return '';
    }

    extractLineNumber(text) {
        const patterns = [
            /line (\d+)/i,
            /line:(\d+)/i,
            /L(\d+)/i,
            /:(\d+):/,
            /at line (\d+)/i
        ];

        for (const pattern of patterns) {
            const match = text.match(pattern);
            if (match && match[1]) {
                const lineNum = parseInt(match[1]);
                if (!isNaN(lineNum)) return lineNum;
            }
        }
        return null;
    }

    determineCategory(text) {
        if (/security|vulnerability|injection/i.test(text)) return 'Security';
        if (/performance|slow|optimization/i.test(text)) return 'Performance';
        if (/naming|style|formatting|convention/i.test(text)) return 'Style';
        if (/error handling|exception|validation/i.test(text)) return 'Error Handling';
        return 'General';
    }
}

// Diff Renderer
class DiffRenderer {
    renderDiff(diffText) {
        const files = {};
        const lines = diffText.split('\n');
        let currentFile = null;
        let fileContent = [];

        lines.forEach(line => {
            if (line.startsWith('diff --git')) {
                // Save previous file if exists
                if (currentFile && fileContent.length > 0) {
                    files[currentFile] = fileContent;
                }
                
                // Extract new filename
                const match = line.match(/diff --git a\/(.+) b\/(.+)/);
                currentFile = match ? match[2] : 'unknown';
                fileContent = [];
            } else if (currentFile) {
                const processedLine = this.processDiffLine(line);
                if (processedLine) {
                    fileContent.push(processedLine);
                }
            }
        });

        // Save last file
        if (currentFile && fileContent.length > 0) {
            files[currentFile] = fileContent;
        }

        return files;
    }

    processDiffLine(line) {
        if (line.startsWith('---') || line.startsWith('+++')) {
            return { type: 'header', content: line, lineNumber: null };
        } else if (line.startsWith('@@')) {
            // Extract line numbers from hunk header
            const match = line.match(/@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@/);
            if (match) {
                const startLine = parseInt(match[3]);
                return { type: 'hunk', content: line, lineNumber: startLine };
            }
            return { type: 'hunk', content: line, lineNumber: null };
        } else if (line.startsWith('+')) {
            return { type: 'added', content: line.substring(1), lineNumber: this.currentLineNumber++ };
        } else if (line.startsWith('-')) {
            return { type: 'removed', content: line.substring(1), lineNumber: this.currentLineNumber };
        } else if (line.startsWith(' ')) {
            return { type: 'unchanged', content: line.substring(1), lineNumber: this.currentLineNumber++ };
        }
        return null;
    }
}

// Interactive Linker
class InteractiveLinker {
    constructor(feedbackProcessor, diffRenderer) {
        this.feedbackProcessor = feedbackProcessor;
        this.diffRenderer = diffRenderer;
        this.highlightedLines = new Set();
    }

    setupFeedbackLinks() {
        document.addEventListener('click', (e) => {
            if (e.target.closest('.feedback-item')) {
                const item = e.target.closest('.feedback-item');
                const lineNumber = item.dataset.lineNumber;
                
                if (lineNumber) {
                    this.highlightCodeLine(parseInt(lineNumber));
                    this.scrollToLine(parseInt(lineNumber));
                }
            }
        });
    }

    highlightCodeLine(lineNumber) {
        // Clear previous highlights
        document.querySelectorAll('.diff-line.highlighted').forEach(el => {
            el.classList.remove('highlighted');
        });

        // Add new highlight
        const line = document.querySelector(`[data-line="${lineNumber}"]`);
        if (line) {
            line.classList.add('highlighted');
            setTimeout(() => {
                line.classList.remove('highlighted');
            }, 3000);
        }
    }

    scrollToLine(lineNumber) {
        const line = document.querySelector(`[data-line="${lineNumber}"]`);
        if (line) {
            line.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
}

// Responsive Handler
class ResponsiveHandler {
    constructor() {
        this.currentPane = 'left';
        this.setupResponsiveBehavior();
    }

    setupResponsiveBehavior() {
        // Handle window resize
        window.addEventListener('resize', () => {
            this.handleResize();
        });
        
        // Initial setup
        this.handleResize();
    }

    handleResize() {
        const isMobile = window.innerWidth <= 768;
        const mobileControls = document.getElementById('mobileControls');
        const leftPane = document.getElementById('leftPane');
        const rightPane = document.getElementById('rightPane');
        
        if (isMobile) {
            mobileControls.style.display = 'flex';
            this.showPane(this.currentPane);
        } else {
            mobileControls.style.display = 'none';
            leftPane.classList.add('active');
            rightPane.classList.add('active');
        }
    }

    togglePane(targetPane) {
        // Update button states
        document.querySelectorAll('.mobile-toggle').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-pane="${targetPane}"]`).classList.add('active');
        
        // Show target pane, hide others
        this.showPane(targetPane);
        this.currentPane = targetPane;
    }

    showPane(targetPane) {
        const leftPane = document.getElementById('leftPane');
        const rightPane = document.getElementById('rightPane');
        
        if (targetPane === 'left') {
            leftPane.classList.add('active');
            rightPane.classList.remove('active');
        } else {
            rightPane.classList.add('active');
            leftPane.classList.remove('active');
        }
    }
}

// Initialize the application when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new ResultsApp();
});