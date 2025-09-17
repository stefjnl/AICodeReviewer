
// Add these global variables at top
let signalRConnection = null;
let currentAnalysisId = null;

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
});

// Initialize SignalR connection
function initializeSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/progress")
        .build();

    signalRConnection.on("UpdateProgress", function (data) {
        console.log('[SignalR] UpdateProgress received:', data);
        
        // Update progress message
        const progressMessage = document.getElementById('progressMessage');
        if (progressMessage) {
            let statusText = data.status;
            
            // Add model information if available
            if (data.modelUsed) {
                statusText += ` (Model: ${data.modelUsed})`;
            }
            if (data.fallbackModel) {
                statusText += ` [Fallback: ${data.fallbackModel}]`;
            }
            
            progressMessage.innerText = statusText;
            console.log('[SignalR] Updated progress message:', statusText);
        }
        
        // Handle result display
        if (data.result) {
            const resultElement = document.getElementById('result');
            if (resultElement) {
                resultElement.innerText = data.result;
            }
            
            // Show analysis result section if hidden
            const analysisResultElement = document.getElementById('analysisResult');
            if (analysisResultElement) {
                analysisResultElement.style.display = 'block';
            }
            
            console.log('[SignalR] Result displayed, length:', data.result.length);
        }
        
        // Handle error display
        if (data.error) {
            const resultElement = document.getElementById('result');
            if (resultElement) {
                resultElement.innerText = '❌ Error: ' + (data.error || 'Unknown error');
            }
            
            // Show analysis result section if hidden
            const analysisResultElement = document.getElementById('analysisResult');
            if (analysisResultElement) {
                analysisResultElement.style.display = 'block';
            }
            
            console.log('[SignalR] Error displayed:', data.error);
        }
        
        // Handle completion
        if (data.isComplete) {
            console.log('[SignalR] Analysis complete, hiding progress');
            
            try {
                hideProgress();
            } catch (error) {
                console.log('[SignalR] Error hiding progress:', error);
            }
            
            // Leave SignalR group
            if (currentAnalysisId && signalRConnection) {
                signalRConnection.invoke("LeaveAnalysisGroup", currentAnalysisId)
                    .catch(err => console.error("[SignalR] Failed to leave analysis group:", err));
            }
            
            // Store analysis ID in session for view switching
            if (currentAnalysisId) {
                console.log('[SignalR] Storing analysis ID in session:', currentAnalysisId);
                fetch('/Home/StoreAnalysisId', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ analysisId: currentAnalysisId })
                }).then(() => {
                    console.log('[SignalR] Analysis ID stored successfully');
                    
                    // Update the global analysis ID
                    window.currentAnalysisId = currentAnalysisId;
                    
                    // Show view switching buttons if they exist
                    if (typeof injectAnalysisButtons === 'function') {
                        injectAnalysisButtons();
                    }
                    
                    // Show the "New Analysis" button
                    const newAnalysisBtn = document.getElementById('newAnalysisBtn');
                    if (newAnalysisBtn) {
                        newAnalysisBtn.style.display = 'inline-block';
                    }
                    
                    // For workflow mode, show results in current page
                    if (document.querySelector('.workflow-horizontal-container')) {
                        console.log('[SignalR] Workflow mode detected, showing results in current page');
                        
                        // Update the result display in the bottom panel
                        const bottomPanelView = document.getElementById('bottomPanelView');
                        if (bottomPanelView && data.result) {
                            // Update or create result display
                            let resultCard = bottomPanelView.querySelector('.workflow-card');
                            if (!resultCard) {
                                resultCard = document.createElement('div');
                                resultCard.className = 'workflow-card';
                                resultCard.innerHTML = `
                                    <div class="workflow-card-header workflow-card-header-success">
                                        <h6 class="mb-0">Analysis Results</h6>
                                    </div>
                                    <div class="workflow-card-body">
                                        <pre class="analysis-text workflow-pre" style="white-space: pre-wrap; font-family: monospace;"></pre>
                                    </div>
                                `;
                                bottomPanelView.appendChild(resultCard);
                            }
                            
                            const preElement = resultCard.querySelector('pre');
                            if (preElement) {
                                preElement.textContent = data.result;
                            }
                            
                            bottomPanelView.style.display = 'block';
                        }
                    }
                }).catch(err => {
                    console.error("[SignalR] Failed to store analysis ID:", err);
                });
            }
            
            console.log('[SignalR] Analysis complete! Analysis ID:', currentAnalysisId);
        } else {
            // For intermediate progress updates, ensure workflow container stays visible
            if (document.querySelector('.workflow-horizontal-container')) {
                console.log('[SignalR] Intermediate progress update in workflow mode - keeping workflow visible');
                
                // Only update progress message, don't hide/show containers
                const analysisResultsSection = document.getElementById('analysisResultsSection');
                if (analysisResultsSection) {
                    analysisResultsSection.style.display = 'block';
                    analysisResultsSection.classList.remove('hidden-analysis');
                }
                
                // Make sure progress message is visible
                const progressMessage = document.getElementById('progressMessage');
                if (progressMessage) {
                    progressMessage.style.display = 'block';
                    progressMessage.textContent = data.status;
                }
                
                // Keep workflow container visible during progress
                const workflowContainer = document.querySelector('.workflow-horizontal-container');
                if (workflowContainer) {
                    workflowContainer.style.display = 'flex';
                }
            }
        }
    });

    signalRConnection.start().then(function () {
        console.log("SignalR connected successfully");
    }).catch(function (err) {
        console.error("SignalR connection failed:", err);
        // Fallback to polling if SignalR fails
        startPollingFallback();
    });
}

// Fallback polling function with delay to prevent hammering the server
function startPollingFallback() {
    console.log("Using polling fallback");
    if (currentAnalysisId) {
        // Add a small delay to prevent server hammering
        setTimeout(() => {
            pollStatus(currentAnalysisId);
        }, 1000); // 1 second delay
    } else {
        console.error("No analysisId available for fallback polling");
        try {
            hideProgress();
        } catch (error) {
            console.log('[Progress] Error hiding progress in fallback (may be in workflow mode):', error);
        }
    }
}

// US-006A: Async analysis with progress updates
function startAnalysis() {
    console.log('[startAnalysis] Starting analysis process');
    
    // Validate form data first
    const repositoryPath = document.querySelector('input[name="repositoryPath"]')?.value;
    const apiKey = document.querySelector('input[name="apiKey"]')?.value;
    
    if (!repositoryPath) {
        alert('Please provide repository path');
        return;
    }
    
    // Get analysis type and validate specific requirements
    const analysisType = document.querySelector('input[name="analysisType"]:checked')?.value || 'uncommitted';
    const commitId = document.getElementById('commitId')?.value?.trim();
    const filePath = document.getElementById('filePath')?.value?.trim();
    
    if (analysisType === 'commit' && !commitId) {
        alert('Please enter a commit ID for commit analysis');
        return;
    }
    
    if (analysisType === 'singlefile' && !filePath) {
        alert('Please enter a file path for single file analysis');
        return;
    }
    
    // Validate file path for single file analysis
    if (analysisType === 'singlefile' && filePath) {
        const allowedExtensions = ['.cs', '.js', '.py'];
        const extension = filePath.toLowerCase().substring(filePath.lastIndexOf('.'));
        if (!allowedExtensions.includes(extension)) {
            alert('Unsupported file type. Allowed extensions: ' + allowedExtensions.join(', '));
            return;
        }
    }
    
    // Show progress immediately
    showProgress();
    
    // Prepare JSON data - get selected documents from checkboxes
    const selectedDocuments = [];
    const checkboxes = document.querySelectorAll('input[name="selectedDocuments"]:checked');
    checkboxes.forEach(checkbox => {
        selectedDocuments.push(checkbox.value);
    });
    
    console.log('📋 Selected documents from checkboxes:', selectedDocuments);
    
    // For single file analysis, we need to provide the full path
    let fullFilePath = filePath;
    if (analysisType === 'singlefile') {
        console.log('[DEBUG] Initial filePath from input:', filePath);
        console.log('[DEBUG] window.selectedFile:', window.selectedFile);
        
        if (window.selectedFile) {
            // Use the file object properties
            fullFilePath = window.selectedFile.name;
            
            // Note: For security reasons, browsers don't provide full path access
            // The server will need to handle relative paths or the user needs to manually enter the full path
            console.log('[DEBUG] Selected file:', window.selectedFile.name, 'Size:', window.selectedFile.size, 'Type:', window.selectedFile.type);
            console.log('[DEBUG] Browser-only filename (no full path):', window.selectedFile.name);
        }
        
        console.log('[DEBUG] Final fullFilePath before validation:', fullFilePath);
        
        // Validate that we have a reasonable file path
        if (!fullFilePath || fullFilePath === '') {
            alert('Please select a file or enter a valid file path');
            try {
                hideProgress();
            } catch (error) {
                console.log('[Progress] Error hiding progress during file validation (may be in workflow mode):', error);
            }
            return;
        }
        
        // Validate file extension
        const allowedExtensions = ['.cs', '.js', '.py'];
        const extension = fullFilePath.toLowerCase().substring(fullFilePath.lastIndexOf('.'));
        if (!allowedExtensions.includes(extension)) {
            alert('Unsupported file type. Allowed extensions: ' + allowedExtensions.join(', '));
            try {
                hideProgress();
            } catch (error) {
                console.log('[Progress] Error hiding progress during file type validation (may be in workflow mode):', error);
            }
            return;
        }
        
        console.log('[DEBUG] File validation passed, extension:', extension);
    }

    const formData = {
        repositoryPath: repositoryPath,
        selectedDocuments: selectedDocuments,
        language: document.getElementById('language-dropdown')?.value || 'python',
        analysisType: analysisType,
        commitId: commitId,
        filePath: fullFilePath,
        model: document.getElementById('modelSelect')?.value || 'qwen/qwen3-coder'
        // documentsFolder is intentionally omitted to use session default
    };
    
    console.log('📤 Sending form data:', formData);
    
    // Start analysis with JSON
    fetch('/Home/RunAnalysis', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log('[startAnalysis] Response received:', data);
        
        if (data.success) {
            currentAnalysisId = data.analysisId;
            console.log('[startAnalysis] Analysis started with ID:', currentAnalysisId);
            
            if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Connected) {
                console.log('[startAnalysis] Joining SignalR group for analysis:', currentAnalysisId);
                signalRConnection.invoke("JoinAnalysisGroup", currentAnalysisId)
                    .catch(err => {
                        console.error("[startAnalysis] Failed to join SignalR group:", err);
                        startPollingFallback();
                    });
            } else {
                console.log('[startAnalysis] Using polling fallback');
                startPollingFallback();
            }
        } else {
            console.error('[startAnalysis] Analysis start failed:', data.error);
            try {
                hideProgress();
            } catch (error) {
                console.log('[Progress] Error hiding progress after analysis error (may be in workflow mode):', error);
            }
            alert('Error starting analysis: ' + (data.error || 'Unknown error'));
        }
    })
    .catch(error => {
        console.error('[startAnalysis] Network error:', error);
        try {
            hideProgress();
        } catch (error) {
            console.log('[Progress] Error hiding progress after network error (may be in workflow mode):', error);
        }
        alert('Network error: ' + error.message);
    });
}

function pollStatus(analysisId) {
    const interval = setInterval(() => {
        fetch(`/Home/GetAnalysisStatus?analysisId=${encodeURIComponent(analysisId)}`)
            .then(response => response.json())
            .then(data => {
                // Handle model information for polling fallback
                let statusText = data.status;
                if (data.modelUsed) {
                    statusText += ` (Model: ${data.modelUsed})`;
                }
                if (data.fallbackModel) {
                    statusText += ` [Fallback: ${data.fallbackModel}]`;
                }
                updateProgress(statusText);
                
                if (data.isComplete) {
                    clearInterval(interval);
                    hideProgress();
                    
                    // SHOW THE RESULT CARD
                    document.getElementById('analysisResult').style.display = 'block';

                    // DISPLAY THE RESULT TEXT
                    if (data.status === 'Complete') {
                        document.getElementById('result').innerText = data.result;
                    } else {
                        document.getElementById('result').innerText = '❌ Error: ' + (data.error || 'Unknown error');
                    }
                    return; // Stop polling
                }
            })
            .catch(error => {
                clearInterval(interval);
                try {
                    hideProgress();
                } catch (error) {
                    console.log('[Progress] Error hiding progress during polling error (may be in workflow mode):', error);
                }

                // Show error result card
                document.getElementById('analysisResult').style.display = 'block';
                document.getElementById('result').innerText = '❌ Error checking status: ' + error.message;
                
                // Also refresh page in case of error to show error message
                setTimeout(() => {
                    console.log('🔄 Refreshing page due to analysis error...');
                    window.location.reload();
                }, 300);
            });
    }, 1000); // Poll every second
}

function showProgress() {
    console.log('[Progress] showProgress called');
    
    // Check if workflow layout elements exist
    const progressContainer = document.getElementById('progressContainer');
    const analysisForm = document.getElementById('analysisForm');
    const analysisResult = document.getElementById('analysisResult');
    
    if (progressContainer && analysisForm && analysisResult) {
        // Original layout - use existing functionality
        console.log('[Progress] Using original layout');
        progressContainer.style.display = 'block';
        analysisForm.style.display = 'none';
        analysisResult.style.display = 'none';
    } else {
        // Workflow layout - show progress without hiding workflow
        console.log('[Progress] Using workflow layout');
        
        // CRITICAL: First, ensure the analysis results section is visible
        // This is the main issue - the section might be hidden with hidden-analysis class
        const analysisResultsSection = document.getElementById('analysisResultsSection');
        if (analysisResultsSection) {
            // Force display and remove hidden class with high specificity
            analysisResultsSection.style.display = 'block';
            analysisResultsSection.classList.remove('hidden-analysis');
            analysisResultsSection.style.visibility = 'visible';
            analysisResultsSection.style.opacity = '1';
            console.log('[Progress] Analysis results section made visible');
        }
        
        // Show progress within the results section
        const progressMessage = document.getElementById('progressMessage');
        if (progressMessage) {
            progressMessage.style.display = 'block';
            progressMessage.style.visibility = 'visible';
            progressMessage.style.opacity = '1';
            console.log('[Progress] Progress message made visible');
        }
        
        // Hide any existing result content during progress
        const bottomPanelView = document.getElementById('bottomPanelView');
        if (bottomPanelView) {
            bottomPanelView.style.display = 'none';
            console.log('[Progress] Hidden bottom panel view during progress');
        }
        
        // IMPORTANT: Do NOT hide the workflow container during progress updates
        // The workflow should remain visible while analysis is running
        const workflowContainer = document.querySelector('.workflow-horizontal-container');
        if (workflowContainer) {
            console.log('[Progress] Keeping workflow container visible during progress');
        }
    }
}

function hideProgress() {
    console.log('[Progress] hideProgress called');
    
    // Check if workflow layout elements exist
    const progressContainer = document.getElementById('progressContainer');
    const analysisForm = document.getElementById('analysisForm');
    
    if (progressContainer && analysisForm) {
        // Original layout - use existing functionality
        console.log('[Progress] Using original layout hide');
        progressContainer.style.display = 'none';
        analysisForm.style.display = 'block';
    } else {
        // Workflow layout - hide progress but keep analysis results visible
        console.log('[Progress] Using workflow layout hide');
        
        // Hide progress within the results section
        const progressMessage = document.getElementById('progressMessage');
        if (progressMessage) {
            progressMessage.style.display = 'none';
            console.log('[Progress] Hidden progress message');
        }
        
        // Show the result content
        const bottomPanelView = document.getElementById('bottomPanelView');
        if (bottomPanelView) {
            bottomPanelView.style.display = 'block';
            console.log('[Progress] Shown bottom panel view');
        }
        
        // IMPORTANT: Do NOT show the workflow container again - it should stay hidden
        // The analysis results should remain visible in their dedicated section
        // Only show workflow if we're going back to start a new analysis
        console.log('[Progress] Keeping workflow container hidden - analysis results are displayed');
    }
}

function updateProgress(status) {
    const progressMessage = document.getElementById('progressMessage');
    const statusElement = document.getElementById('status');
    
    if (progressMessage) {
        progressMessage.textContent = status || 'Processing...';
    }
    if (statusElement) {
        statusElement.textContent = status || 'Processing...';
    }
}
// ==================== DIRECTORY BROWSER FUNCTIONS ====================

// Global variable to track current browse path
let currentBrowsePath = '';

// Open directory browser modal
function openDirectoryBrowser() {
    console.log('[Directory Browser] Opening directory browser...');
    
    // Ensure Bootstrap is loaded
    if (typeof bootstrap === 'undefined') {
        console.error('[Directory Browser] Bootstrap is not loaded');
        alert('UI framework not loaded. Please refresh the page.');
        return;
    }
    
    try {
        // Get modal element
        const modalElement = document.getElementById('directoryBrowserModal');
        if (!modalElement) {
            console.error('[Directory Browser] Modal element not found');
            alert('Directory browser not available. Please refresh the page.');
            return;
        }
        
        // Create and show modal
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
        
        // Get current repository path from input
        const currentPath = document.getElementById('repositoryPathInput')?.value || '';
        console.log('[Directory Browser] Starting browse from path:', currentPath);
        
        // Small delay to ensure modal is fully rendered before loading content
        setTimeout(() => {
            browseDirectory(currentPath);
        }, 100);
        
    } catch (error) {
        console.error('[Directory Browser] Error opening modal:', error);
        alert('Error opening directory browser: ' + error.message);
    }
}

// Browse directory contents
function browseDirectory(path) {
    console.log('[Directory Browser] Browsing directory:', path);
    
    // Get UI elements
    const loadingDiv = document.getElementById('directoryBrowserLoading');
    const contentDiv = document.getElementById('directoryBrowserContent');
    const errorDiv = document.getElementById('directoryBrowserError');
    const pathInput = document.getElementById('currentDirectoryPath');
    
    // Validate elements exist
    if (!loadingDiv || !contentDiv || !errorDiv || !pathInput) {
        console.error('[Directory Browser] Required DOM elements not found');
        return;
    }
    
    // Show loading state
    loadingDiv.style.display = 'block';
    contentDiv.style.display = 'none';
    errorDiv.style.display = 'none';
    contentDiv.innerHTML = '';
    
    // Update path input
    pathInput.value = path;
    currentBrowsePath = path;
    
    // Make API call to browse directory
    fetch('/Home/BrowseDirectory', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ currentPath: path })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log('[Directory Browser] Directory data received:', data);
        
        // Hide loading
        loadingDiv.style.display = 'none';
        
        // Handle errors from server
        if (data.error) {
            console.warn('[Directory Browser] Server returned error:', data.error);
            errorDiv.innerHTML = `
                <div class="alert alert-warning" role="alert">
                    <i class="fas fa-exclamation-triangle"></i> ${data.error}
                </div>
            `;
            errorDiv.style.display = 'block';
            return;
        }
        
        // Update current path
        currentBrowsePath = data.currentPath;
        pathInput.value = data.currentPath;
        
        // Render directory contents
        renderDirectoryContents(data);
        
        // Show content
        contentDiv.style.display = 'block';
    })
    .catch(error => {
        console.error('[Directory Browser] Error browsing directory:', error);
        
        // Hide loading
        loadingDiv.style.display = 'none';
        
        // Show error
        errorDiv.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="fas fa-exclamation-circle"></i> Error browsing directory: ${error.message}
            </div>
        `;
        errorDiv.style.display = 'block';
    });
}

// Render directory contents in the modal
function renderDirectoryContents(data) {
    const contentDiv = document.getElementById('directoryBrowserContent');
    if (!contentDiv) {
        console.error('[Directory Browser] Content div not found');
        return;
    }
    
    let html = '';
    
    // Add parent directory navigation (if not at root)
    if (data.parentPath !== null) {
        html += `
            <div class="directory-item" onclick="browseDirectory('${escapeHtml(data.parentPath)}')">
                <div class="d-flex align-items-center p-2 hover-bg-light cursor-pointer">
                    <i class="fas fa-level-up-alt me-2"></i>
                    <span class="text-muted">.. (Parent Directory)</span>
                </div>
            </div>
        `;
    }
    
    // Add directories
    if (data.directories && data.directories.length > 0) {
        data.directories.forEach(dir => {
            const icon = dir.isGitRepository ? 'fab fa-git-alt text-success' : 'fas fa-folder text-warning';
            const gitBadge = dir.isGitRepository ? '<span class="badge bg-success ms-2">Git</span>' : '';
            
            html += `
                <div class="directory-item" onclick="browseDirectory('${escapeHtml(dir.fullPath)}')">
                    <div class="d-flex align-items-center p-2 hover-bg-light cursor-pointer">
                        <i class="${icon} me-2"></i>
                        <span>${escapeHtml(dir.name)}${gitBadge}</span>
                        <small class="text-muted ms-auto">${formatDate(dir.lastModified)}</small>
                    </div>
                </div>
            `;
        });
    }
    
    // Add files (for display only, not clickable)
    if (data.files && data.files.length > 0) {
        html += '<div class="mt-3"><h6 class="text-muted border-bottom pb-1">Files</h6></div>';
        data.files.forEach(file => {
            const icon = getFileIcon(file.name);
            
            html += `
                <div class="file-item">
                    <div class="d-flex align-items-center p-2 text-muted">
                        <i class="${icon} me-2"></i>
                        <span>${escapeHtml(file.name)}</span>
                        <small class="text-muted ms-auto">${formatFileSize(file.size)} • ${formatDate(file.lastModified)}</small>
                    </div>
                </div>
            `;
        });
    }
    
    // Add empty state
    if ((!data.directories || data.directories.length === 0) && 
        (!data.files || data.files.length === 0)) {
        html += `
            <div class="text-center text-muted p-4">
                <i class="fas fa-inbox fa-2x mb-2"></i>
                <p>This directory is empty</p>
            </div>
        `;
    }
    
    // Highlight if current directory is a git repository
    if (data.isGitRepository) {
        html = `
            <div class="alert alert-success mb-3">
                <i class="fab fa-git-alt"></i> <strong>Git Repository Detected!</strong> This directory contains a .git folder.
            </div>
        ` + html;
    }
    
    contentDiv.innerHTML = html;
}

// Navigate to path entered in the input field
function navigateToPath() {
    const pathInput = document.getElementById('currentDirectoryPath');
    if (pathInput && pathInput.value.trim() !== '') {
        const newPath = pathInput.value.trim();
        console.log('[Directory Browser] Navigating to path:', newPath);
        browseDirectory(newPath);
    }
}

// Select current directory and close modal
function selectCurrentDirectory() {
    if (currentBrowsePath) {
        // Update repository path input
        const repoPathInput = document.getElementById('repositoryPathInput');
        if (repoPathInput) {
            repoPathInput.value = currentBrowsePath;
        }
        
        // Close the modal
        const modalElement = document.getElementById('directoryBrowserModal');
        if (modalElement) {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
        
        console.log('[Directory Browser] Selected repository path:', currentBrowsePath);
    }
}

// Get appropriate icon for file based on extension
function getFileIcon(filename) {
    const ext = filename.toLowerCase().split('.').pop();
    const iconMap = {
        'cs': 'fas fa-file-code',
        'js': 'fab fa-js',
        'py': 'fab fa-python',
        'json': 'fas fa-file-code',
        'xml': 'fas fa-file-code',
        'config': 'fas fa-cog',
        'md': 'fab fa-markdown',
        'txt': 'fas fa-file-alt',
        'yml': 'fas fa-file-code',
        'yaml': 'fas fa-file-code'
    };
    return iconMap[ext] || 'fas fa-file';
}

// Format file size for display
function formatFileSize(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

// Format date for display
function formatDate(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now - date);
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) {
        return 'Today';
    } else if (diffDays === 1) {
        return 'Yesterday';
    } else if (diffDays < 7) {
        return `${diffDays} days ago`;
    } else {
        return date.toLocaleDateString();
    }
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// ==================== END DIRECTORY BROWSER FUNCTIONS ====================

