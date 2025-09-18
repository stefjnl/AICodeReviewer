// AI Code Reviewer - Main Application JavaScript
// Vanilla JavaScript implementation - Alpine.js removed

console.log('🚀 AI Code Reviewer - App.js loaded successfully');

// Document manager state
const documentManager = {
    documents: [],
    loading: false,
    error: null,
    selectedDocument: null,
    documentContent: ''
};

// API endpoints configuration
const apiEndpoints = {
    progressHub: '/hubs/progress',
    repositoryBrowse: '/api/repository/browse',
    analysisStart: '/api/analysis/start',
    analysisResults: '/api/analysis/results',
    documentsScan: '/api/documentapi/scan',
    documentsContent: '/api/documentapi/content'
};

// Utility methods
const utils = {
    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },
    
    formatDuration(ms) {
        if (ms < 1000) return ms + 'ms';
        if (ms < 60000) return (ms / 1000).toFixed(1) + 's';
        return Math.floor(ms / 60000) + 'm ' + Math.floor((ms % 60000) / 1000) + 's';
    }
};

// UI State Management Functions
function showElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.classList.remove('hidden');
    }
}

function hideElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.classList.add('hidden');
    }
}

function updateElementContent(elementId, content) {
    const element = document.getElementById(elementId);
    if (element) {
        element.textContent = content;
    }
}

function updateElementHtml(elementId, html) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = html;
    }
}

function setButtonState(buttonId, loading) {
    const button = document.getElementById(buttonId);
    if (button) {
        button.disabled = loading;
        if (loading) {
            button.classList.add('opacity-50', 'cursor-not-allowed');
        } else {
            button.classList.remove('opacity-50', 'cursor-not-allowed');
        }
    }
}

// Document Management Functions
async function loadDocuments() {
    try {
        documentManager.loading = true;
        documentManager.error = null;
        
        console.log('🔄 Loading documents...');
        
        // Update UI state
        showElement('loading-spinner');
        hideElement('error-container');
        hideElement('document-list-container');
        hideElement('empty-state');
        updateElementContent('loading-text', 'Loading...');
        setButtonState('load-documents-btn', true);
        
        const response = await fetch(apiEndpoints.documentsScan);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log('✅ Documents fetched successfully:', data);
        
        if (data.success) {
            documentManager.documents = data.documents;
            console.log(`✅ Loaded ${documentManager.documents.length} documents`);
            
            // Update UI
            updateDocumentList(documentManager.documents);
            
            if (documentManager.documents.length > 0) {
                showElement('document-list-container');
            } else {
                showElement('empty-state');
            }
        } else {
            documentManager.error = data.error || 'Failed to load documents';
            console.error('❌ Error loading documents:', documentManager.error);
            showError(documentManager.error);
        }
        
    } catch (error) {
        documentManager.error = error.message || 'An unexpected error occurred';
        console.error('❌ API call failed:', error);
        showError(documentManager.error);
    } finally {
        documentManager.loading = false;
        hideElement('loading-spinner');
        updateElementContent('loading-text', 'Load Documents');
        setButtonState('load-documents-btn', false);
    }
}

async function loadDocumentContent(documentName) {
    try {
        documentManager.loading = true;
        documentManager.error = null;
        
        console.log(`🔄 Loading content for: ${documentName}`);
        
        // Update UI state
        showElement('loading-state');
        setButtonState('load-documents-btn', true);
        
        const response = await fetch(`${apiEndpoints.documentsContent}/${documentName}`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log(`✅ Loaded content for ${documentName}`);
        
        if (data.success) {
            documentManager.selectedDocument = documentName;
            documentManager.documentContent = data.content;
            
            // Update UI
            showDocumentContent(data.content, documentName);
        } else {
            documentManager.error = data.error || 'Failed to load document content';
            console.error('❌ Error loading document content:', documentManager.error);
            showError(documentManager.error);
        }
        
    } catch (error) {
        documentManager.error = error.message || 'An unexpected error occurred';
        console.error('❌ API call failed:', error);
        showError(documentManager.error);
    } finally {
        documentManager.loading = false;
        hideElement('loading-state');
        setButtonState('load-documents-btn', false);
    }
}

function clearError() {
    documentManager.error = null;
    hideElement('error-container');
}

function clearSelection() {
    documentManager.selectedDocument = null;
    documentManager.documentContent = '';
    hideElement('document-viewer');
}

function showDocumentContent(content, title) {
    updateElementContent('selected-document-name', title);
    updateElementContent('document-content', content);
    showElement('document-viewer');
}

function updateDocumentList(documents) {
    const documentGrid = document.getElementById('document-grid');
    const documentCount = document.getElementById('document-count');
    
    if (documentGrid && documentCount) {
        documentCount.textContent = documents.length;
        
        documentGrid.innerHTML = '';
        
        documents.forEach(doc => {
            const documentDiv = window.document.createElement('div');
            documentDiv.className = 'bg-gray-50 rounded-md p-3 hover:bg-gray-100 transition-colors';
            documentDiv.innerHTML = `
                <div class="flex items-center justify-between">
                    <div>
                        <h5 class="text-sm font-medium text-gray-900">${doc}</h5>
                        <p class="text-xs text-gray-500">Markdown document</p>
                    </div>
                    <button
                        class="text-xs text-primary hover:text-primary/80 font-medium view-document-btn"
                        data-document="${doc}"
                    >
                        View
                    </button>
                </div>
            `;
            documentGrid.appendChild(documentDiv);
        });
        
        // Add event listeners to new view buttons
        document.querySelectorAll('.view-document-btn').forEach(button => {
            button.addEventListener('click', (e) => {
                const documentName = e.target.dataset.document;
                loadDocumentContent(documentName);
            });
        });
    }
}

function showError(message) {
    updateElementContent('error-message', message);
    showElement('error-container');
}

// SignalR initialization
function initializeSignalR() {
    try {
        console.log('🔗 Initializing SignalR connection...');
        
        // Create SignalR connection
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(apiEndpoints.progressHub)
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        // Connection event handlers
        connection.start()
            .then(() => {
                console.log('✅ SignalR connected successfully');
                updateSignalRStatus('Connected', true);
                
                // Register for progress updates
                connection.on("ReceiveProgress", (progress) => {
                    handleProgressUpdate(progress);
                });
                
                // Register for analysis completion
                connection.on("AnalysisComplete", (results) => {
                    handleAnalysisComplete(results);
                });
                
                // Register for errors
                connection.on("Error", (error) => {
                    handleError(error);
                });
            })
            .catch(err => {
                console.error('❌ SignalR connection failed:', err);
                updateSignalRStatus('Connection failed', false);
            });
        
        // Reconnection event handlers
        connection.onreconnecting(() => {
            console.warn('🔄 SignalR reconnecting...');
            updateSignalRStatus('Reconnecting...', false);
        });
        
        connection.onreconnected(() => {
            console.log('✅ SignalR reconnected');
            updateSignalRStatus('Connected', true);
        });
        
        // Global access to connection
        window.signalRConnection = connection;
        
    } catch (error) {
        console.error('❌ SignalR initialization error:', error);
        updateSignalRStatus('Initialization error', false);
    }
}

// Update SignalR status display
function updateSignalRStatus(status, isConnected) {
    const statusElement = document.getElementById('signalr-status');
    if (statusElement) {
        statusElement.textContent = status;
        statusElement.className = `text-xs ${isConnected ? 'text-green-600' : 'text-red-600'}`;
    }
}

// Handle progress updates from SignalR
function handleProgressUpdate(progress) {
    console.log('📊 Progress update:', progress);
    
    // Dispatch custom event for components to listen to
    const event = new CustomEvent('progress-update', { detail: progress });
    document.dispatchEvent(event);
}

// Handle analysis completion
function handleAnalysisComplete(results) {
    console.log('✅ Analysis complete:', results);
    
    // Dispatch custom event for components to listen to
    const event = new CustomEvent('analysis-complete', { detail: results });
    document.dispatchEvent(event);
}

// Handle errors from SignalR
function handleError(error) {
    console.error('❌ SignalR error:', error);
    
    // Dispatch custom event for components to listen to
    const event = new CustomEvent('signalr-error', { detail: error });
    document.dispatchEvent(event);
}

// Document API client functionality - updated without Alpine.js dependencies
window.documentApi = {
    async fetchDocuments() {
        try {
            console.log('📁 Fetching documents from API...');
            const response = await fetch(apiEndpoints.documentsScan);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            console.log('✅ Documents fetched successfully:', data);
            return data;
            
        } catch (error) {
            console.error('❌ Error fetching documents:', error);
            throw error;
        }
    },
    
    async fetchDocumentContent(documentName) {
        try {
            console.log(`📄 Fetching document content: ${documentName}`);
            const response = await fetch(`${apiEndpoints.documentsContent}/${documentName}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            console.log('✅ Document content fetched successfully:', data);
            return data;
            
        } catch (error) {
            console.error('❌ Error fetching document content:', error);
            throw error;
        }
    }
};

// Utility functions for API calls - updated without Alpine.js dependencies
window.api = {
    async get(endpoint, options = {}) {
        try {
            const response = await fetch(endpoint, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error(`API GET error for ${endpoint}:`, error);
            throw error;
        }
    },
    
    async post(endpoint, data, options = {}) {
        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                body: JSON.stringify(data),
                ...options
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error(`API POST error for ${endpoint}:`, error);
            throw error;
        }
    }
};

// Event listeners initialization
function initializeEventListeners() {
    // Load documents button
    const loadButton = document.getElementById('load-documents-btn');
    if (loadButton) {
        loadButton.addEventListener('click', loadDocuments);
    }
    
    // Close error button
    const closeErrorBtn = document.getElementById('close-error-btn');
    if (closeErrorBtn) {
        closeErrorBtn.addEventListener('click', clearError);
    }
    
    // Close document button
    const closeDocumentBtn = document.getElementById('close-document-btn');
    if (closeDocumentBtn) {
        closeDocumentBtn.addEventListener('click', clearSelection);
    }
}

// Global error handlers
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Document ready check
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM fully loaded and parsed');
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Initialize event listeners
    initializeEventListeners();
    
    console.log('✅ Application initialized successfully');
});