
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
            progressMessage.innerText = data.status;
            console.log('[SignalR] Updated progress message:', data.status);
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
        language: document.getElementById('languageSelect')?.value || 'NET',
        analysisType: analysisType,
        commitId: commitId,
        filePath: fullFilePath
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
                updateProgress(data.status);
                
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
        // Workflow layout - hide workflow and show progress/results
        console.log('[Progress] Using workflow layout');
        
        // Hide the workflow container
        const workflowContainer = document.querySelector('.workflow-horizontal-container');
        if (workflowContainer) {
            workflowContainer.style.display = 'none';
            console.log('[Progress] Hidden workflow container');
        }
        
        // Show the analysis results section (which includes progress)
        const analysisResultsSection = document.getElementById('analysisResultsSection');
        if (analysisResultsSection) {
            analysisResultsSection.style.display = 'block';
            analysisResultsSection.classList.remove('hidden-analysis');
            console.log('[Progress] Shown analysis results section');
        }
        
        // Show progress within the results section
        const progressMessage = document.getElementById('progressMessage');
        if (progressMessage) {
            progressMessage.style.display = 'block';
            console.log('[Progress] Shown progress message');
        }
        
        // Hide any existing result content during progress
        const bottomPanelView = document.getElementById('bottomPanelView');
        if (bottomPanelView) {
            bottomPanelView.style.display = 'none';
            console.log('[Progress] Hidden bottom panel view during progress');
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
        // Workflow layout - show workflow and hide progress
        console.log('[Progress] Using workflow layout hide');
        
        // Show the workflow container again
        const workflowContainer = document.querySelector('.workflow-horizontal-container');
        if (workflowContainer) {
            workflowContainer.style.display = 'flex';
            console.log('[Progress] Shown workflow container');
        }
        
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

// Directory Browser Functions
let currentBrowsePath = '';

function openDirectoryBrowser() {
    const modal = new bootstrap.Modal(document.getElementById('directoryBrowserModal'));
    modal.show();
    
    // Start browsing from current repository path or default
    const currentPath = document.getElementById('repositoryPathInput').value || '';
    browseDirectory(currentPath);
}

function browseDirectory(path) {
    const loadingDiv = document.getElementById('directoryBrowserLoading');
    const contentDiv = document.getElementById('directoryBrowserContent');
    const errorDiv = document.getElementById('directoryBrowserError');
    
    // Show loading state
    loadingDiv.style.display = 'block';
    contentDiv.innerHTML = '';
    errorDiv.style.display = 'none';
    
    // Make API call to browse directory
    fetch('/Home/BrowseDirectory', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ currentPath: path })
    })
    .then(response => response.json())
    .then(data => {
        loadingDiv.style.display = 'none';
        
        if (data.error) {
            errorDiv.textContent = data.error;
            errorDiv.style.display = 'block';
            return;
        }
        
        // Update current path display
        currentBrowsePath = data.currentPath;
        document.getElementById('currentDirectoryPath').value = data.currentPath;
        
        // Render directory contents
        renderDirectoryContents(data);
    })
    .catch(error => {
        loadingDiv.style.display = 'none';
        errorDiv.textContent = 'Error browsing directory: ' + error.message;
        errorDiv.style.display = 'block';
    });
}

function renderDirectoryContents(data) {
    const contentDiv = document.getElementById('directoryBrowserContent');
    let html = '';
    
    // Add parent directory navigation (if not at root)
    if (data.parentPath !== null) {
        html += `
            <div class="directory-item" onclick="browseDirectory('${escapeHtml(data.parentPath)}')">
                <div class="d-flex align-items-center p-2 hover-bg-light cursor-pointer">
                    <span class="me-2">⬆️</span>
                    <span><em>.. (Parent Directory)</em></span>
                </div>
            </div>
        `;
    }
    
    // Add directories
    if (data.directories && data.directories.length > 0) {
        html += '<div class="directories-section">';
        data.directories.forEach(dir => {
            const icon = dir.isGitRepository ? '📁🌿' : '📁';
            const gitBadge = dir.isGitRepository ? '<span class="badge bg-success ms-2">Git</span>' : '';
            
            html += `
                <div class="directory-item" onclick="browseDirectory('${escapeHtml(dir.fullPath)}')">
                    <div class="d-flex align-items-center p-2 hover-bg-light cursor-pointer">
                        <span class="me-2">${icon}</span>
                        <span>${escapeHtml(dir.name)}${gitBadge}</span>
                        <small class="text-muted ms-auto">${formatDate(dir.lastModified)}</small>
                    </div>
                </div>
            `;
        });
        html += '</div>';
    }
    
    // Add files
    if (data.files && data.files.length > 0) {
        html += '<div class="files-section mt-3">';
        html += '<h6 class="text-muted mb-2">Files</h6>';
        data.files.forEach(file => {
            const icon = getFileIcon(file.name);
            const sizeText = formatFileSize(file.size);
            
            html += `
                <div class="file-item">
                    <div class="d-flex align-items-center p-2 text-muted">
                        <span class="me-2">${icon}</span>
                        <span>${escapeHtml(file.name)}</span>
                        <small class="text-muted ms-auto">${sizeText} • ${formatDate(file.lastModified)}</small>
                    </div>
                </div>
            `;
        });
        html += '</div>';
    }
    
    // Add empty state
    if ((!data.directories || data.directories.length === 0) &&
        (!data.files || data.files.length === 0)) {
        html += `
            <div class="text-center text-muted p-4">
                <p>This directory is empty</p>
            </div>
        `;
    }
    
    // Highlight if current directory is a git repository
    if (data.isGitRepository) {
        html = `
            <div class="alert alert-success mb-3">
                <strong>🌿 Git Repository Detected!</strong> This directory contains a .git folder.
            </div>
        ` + html;
    }
    
    contentDiv.innerHTML = html;
    
    // Add some CSS for hover effects
    const style = document.createElement('style');
    style.textContent = `
        .hover-bg-light:hover {
            background-color: #f8f9fa;
        }
        .cursor-pointer {
            cursor: pointer;
        }
        .directory-item, .file-item {
            border-bottom: 1px solid #e9ecef;
        }
        .directory-item:last-child, .file-item:last-child {
            border-bottom: none;
        }
    `;
    if (!document.getElementById('directory-browser-styles')) {
        style.id = 'directory-browser-styles';
        document.head.appendChild(style);
    }
}

function navigateToParent() {
    if (currentBrowsePath) {
        browseDirectory(currentBrowsePath); // This will handle parent navigation
    }
}

function selectCurrentDirectory() {
    if (currentBrowsePath) {
        document.getElementById('repositoryPathInput').value = currentBrowsePath;
        
        // Close the modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('directoryBrowserModal'));
        modal.hide();
        
        console.log('Selected repository path:', currentBrowsePath);
    }
}

function getFileIcon(filename) {
    const ext = filename.toLowerCase().split('.').pop();
    const iconMap = {
        'cs': '📝',
        'js': '📜',
        'py': '🐍',
        'json': '📋',
        'xml': '📄',
        'config': '⚙️',
        'md': '📖',
        'txt': '📃',
        'yml': '🔧',
        'yaml': '🔧'
    };
    return iconMap[ext] || '📄';
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

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

function escapeHtml(text) {
    const map = {
        '&': '&',
        '<': '<',
        '>': '>',
        '"': '"',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// Initialize directory browser on modal show
document.addEventListener('DOMContentLoaded', function() {
    const modal = document.getElementById('directoryBrowserModal');
    if (modal) {
        modal.addEventListener('shown.bs.modal', function() {
            // Focus on the current path input
            document.getElementById('currentDirectoryPath').focus();
        });
        
        modal.addEventListener('hidden.bs.modal', function() {
            // Clean up any temporary styles
            const style = document.getElementById('directory-browser-styles');
            if (style) {
                style.remove();
            }
        });
    }
});
