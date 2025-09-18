// AI Code Reviewer - Main Application JavaScript
// This file initializes Alpine.js components and sets up global functionality

console.log('🚀 AI Code Reviewer - App.js loaded successfully');

// Document ready check
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM fully loaded and parsed');
    
    // Initialize SignalR connection
    initializeSignalR();
});

// Global Alpine.js store for application state
document.addEventListener('alpine:init', () => {
    console.log('🎯 Alpine.js initialized');
    
    // Register global store
    Alpine.store('app', {
        version: '1.0.0',
        isLoading: false,
        currentStep: 1,
        
        // API endpoints
        endpoints: {
            progressHub: '/hubs/progress',
            repositoryBrowse: '/api/repository/browse',
            analysisStart: '/api/analysis/start',
            analysisResults: '/api/analysis/results',
            documentsScan: '/api/documentapi/scan',
            documentsContent: '/api/documentapi/content'
        },
        
        // Utility methods
        utils: {
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
        }
    });
});

// SignalR initialization
function initializeSignalR() {
    try {
        console.log('🔗 Initializing SignalR connection...');
        
        // Create SignalR connection
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/progress")
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

// Document API client functionality
window.documentApi = {
    async fetchDocuments() {
        try {
            console.log('📁 Fetching documents from API...');
            const response = await fetch(Alpine.store('app').endpoints.documentsScan);
            
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
            const response = await fetch(`${Alpine.store('app').endpoints.documentsContent}/${documentName}`);
            
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

// Utility functions for API calls
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

// Global error handler
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

// Global unhandled promise rejection handler
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Alpine.js document management component
document.addEventListener('alpine:init', () => {
    Alpine.data('documentManager', () => ({
        documents: [],
        loading: false,
        error: null,
        selectedDocument: null,
        documentContent: '',
        
        async loadDocuments() {
            try {
                this.loading = true;
                this.error = null;
                
                console.log('🔄 Loading documents...');
                const response = await window.documentApi.fetchDocuments();
                
                if (response.success) {
                    this.documents = response.documents;
                    console.log(`✅ Loaded ${this.documents.length} documents`);
                } else {
                    this.error = response.error || 'Failed to load documents';
                    console.error('❌ Error loading documents:', this.error);
                }
                
            } catch (error) {
                this.error = error.message || 'An unexpected error occurred';
                console.error('❌ API call failed:', error);
            } finally {
                this.loading = false;
            }
        },
        
        async loadDocumentContent(documentName) {
            try {
                this.loading = true;
                this.error = null;
                
                console.log(`🔄 Loading content for: ${documentName}`);
                const response = await window.documentApi.fetchDocumentContent(documentName);
                
                if (response.success) {
                    this.selectedDocument = documentName;
                    this.documentContent = response.content;
                    console.log(`✅ Loaded content for ${documentName}`);
                } else {
                    this.error = response.error || 'Failed to load document content';
                    console.error('❌ Error loading document content:', this.error);
                }
                
            } catch (error) {
                this.error = error.message || 'An unexpected error occurred';
                console.error('❌ API call failed:', error);
            } finally {
                this.loading = false;
            }
        },
        
        clearError() {
            this.error = null;
        },
        
        clearSelection() {
            this.selectedDocument = null;
            this.documentContent = '';
        }
    }));
});