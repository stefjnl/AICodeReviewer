
// Add these global variables at top
let signalRConnection = null;
let currentAnalysisId = null;

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
});

// Initialize SignalR connection
function initializeSignalR() {
    // Check if SignalR is available
    if (typeof signalR === 'undefined') {
        console.warn('SignalR library not loaded yet, will retry...');
        // Retry after a short delay
        setTimeout(initializeSignalR, 500);
        return;
    }
    
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/progress")
            .build();
    } catch (error) {
        console.error('Failed to initialize SignalR connection:', error);
        startPollingFallback();
        return;
    }

    signalRConnection.on("UpdateProgress", function (data) {
        document.getElementById('progressMessage').innerText = data.status;
        
        if (data.result) {
            document.getElementById('result').innerText = data.result;
            document.getElementById('analysisResult').style.display = 'block';
        }
        
        if (data.error) {
            document.getElementById('result').innerText = '❌ Error: ' + (data.error || 'Unknown error');
            document.getElementById('analysisResult').style.display = 'block';
        }
        
        if (data.isComplete) {
            hideProgress();
            if (currentAnalysisId && signalRConnection) {
                signalRConnection.invoke("LeaveAnalysisGroup", currentAnalysisId)
                    .catch(err => console.error("Failed to leave SignalR group:", err));
            }
            
            // Store analysis ID in session for view switching
            if (currentAnalysisId) {
                console.log('🔄 Storing analysis ID in session:', currentAnalysisId);
                fetch('/Home/StoreAnalysisId', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ analysisId: currentAnalysisId })
                }).then(() => {
                    console.log('✅ Analysis ID stored, refreshing page to show results section...');
                    
                    // ✅ REFRESH PAGE TO SHOW THE ANALYSIS RESULTS SECTION
                    setTimeout(() => {
                        window.location.reload();
                    }, 300); // Small delay to ensure session is persisted
                }).catch(err => {
                    console.error("Failed to store analysis ID:", err);
                    console.log('🔄 Falling back to page reload...');
                    setTimeout(() => {
                        window.location.reload();
                    }, 300);
                });
            }
            
            // Set the global analysis ID for view switching
            window.currentAnalysisId = currentAnalysisId;
            console.log('Analysis complete! Use the view switching buttons above to choose between Bottom Panel and Side Panel views.');
        }
    });

    signalRConnection.start().then(function () {
        console.log("SignalR connected successfully");
    }).catch(function (err) {
        console.error("SignalR connection failed:", err);
        // Fallback to polling if SignalR fails
        startPollingFallback();
    });
    
    // Handle SignalR connection errors during runtime
    signalRConnection.onclose(function (error) {
        console.warn("SignalR connection closed", error);
        if (currentAnalysisId) {
            console.log("Switching to polling fallback due to SignalR disconnection");
            startPollingFallback();
        }
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
        hideProgress();
    }
}

// US-006A: Async analysis with progress updates
function startAnalysis() {
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
            hideProgress();
            return;
        }
        
        // Validate file extension
        const allowedExtensions = ['.cs', '.js', '.py'];
        const extension = fullFilePath.toLowerCase().substring(fullFilePath.lastIndexOf('.'));
        if (!allowedExtensions.includes(extension)) {
            alert('Unsupported file type. Allowed extensions: ' + allowedExtensions.join(', '));
            hideProgress();
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
        if (data.success) {
            currentAnalysisId = data.analysisId;
            if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Connected) {
                signalRConnection.invoke("JoinAnalysisGroup", currentAnalysisId)
                    .catch(err => {
                        console.error("Failed to join SignalR group:", err);
                        startPollingFallback();
                    });
            } else {
                startPollingFallback();
            }
        } else {
            hideProgress();
            alert('Error starting analysis: ' + (data.error || 'Unknown error'));
        }
    })
    .catch(error => {
        hideProgress();
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
                hideProgress();

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
    document.getElementById('progressContainer').style.display = 'block';
    document.getElementById('analysisForm').style.display = 'none';
    document.getElementById('analysisResult').style.display = 'none'; // Hide result during progress
}

function hideProgress() {
    document.getElementById('progressContainer').style.display = 'none';
    document.getElementById('analysisForm').style.display = 'block';
}

function updateProgress(status) {
    document.getElementById('progressMessage').textContent = status || 'Processing...';
    document.getElementById('status').textContent = status || 'Processing...';
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
