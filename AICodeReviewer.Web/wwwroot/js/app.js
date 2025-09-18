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

// Repository state management
const repositoryState = {
    path: '',
    isValid: false,
    isValidating: false,
    error: null,
    info: null
};

// API endpoints configuration
const apiEndpoints = {
    progressHub: '/hubs/progress',
    repositoryBrowse: '/api/repository/browse',
    analysisStart: '/api/analysis/start',
    analysisResults: '/api/analysis/results',
    documentsScan: '/api/documentapi/scan',
    documentsContent: '/api/documentapi/content',
    validateRepository: '/api/GitApi/validate'
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

// Repository validation functions
async function validateRepository() {
    const path = document.getElementById('repository-path').value.trim();
    
    if (!path) {
        showValidationError('Please enter a repository path');
        return;
    }

    repositoryState.path = path;
    repositoryState.isValidating = true;
    repositoryState.error = null;

    try {
        updateRepositoryUI('validating');
        
        const response = await fetch(apiEndpoints.validateRepository, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ repositoryPath: path })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        
        if (result.success && result.isValidRepo) {
            // Additional validation: check if branch info indicates a real repository
            if (result.currentBranch && result.currentBranch !== "No git repository found") {
                repositoryState.isValid = true;
                repositoryState.info = result;
                updateRepositoryUI('success', result);
                
                // Trigger workflow progression and auto-detection
                onRepositoryValidationSuccess();
            } else {
                repositoryState.isValid = false;
                showValidationError('Not a valid git repository or access denied');
            }
        } else {
            repositoryState.isValid = false;
            showValidationError(result.error || 'Invalid repository');
        }
    } catch (error) {
        showValidationError(error.message || 'Failed to validate repository');
    } finally {
        repositoryState.isValidating = false;
    }
}

function updateRepositoryUI(state, result = null) {
    const loadingEl = document.getElementById('validation-loading');
    const errorEl = document.getElementById('validation-error');
    const successEl = document.getElementById('validation-success');
    const infoEl = document.getElementById('repository-info');
    const validateBtn = document.getElementById('validate-repository-btn');
    const loadingSpinner = document.getElementById('validation-spinner');
    const loadingText = document.getElementById('validation-text');

    switch (state) {
        case 'validating':
            hideElement(errorEl);
            hideElement(successEl);
            hideElement(infoEl);
            showElement(loadingEl);
            if (validateBtn) validateBtn.disabled = true;
            if (loadingSpinner) loadingSpinner.classList.remove('hidden');
            if (loadingText) loadingText.textContent = 'Validating...';
            break;
        case 'success':
            hideElement(loadingEl);
            hideElement(errorEl);
            showElement(successEl);
            showElement(infoEl);
            displayRepositoryInfo(result);
            if (validateBtn) validateBtn.disabled = false;
            if (loadingSpinner) loadingSpinner.classList.add('hidden');
            if (loadingText) loadingText.textContent = 'Validate Repository';
            break;
        case 'error':
            hideElement(loadingEl);
            hideElement(successEl);
            hideElement(infoEl);
            showElement(errorEl);
            if (validateBtn) validateBtn.disabled = false;
            if (loadingSpinner) loadingSpinner.classList.add('hidden');
            if (loadingText) loadingText.textContent = 'Validate Repository';
            break;
        case 'clear':
            hideElement(loadingEl);
            hideElement(errorEl);
            hideElement(successEl);
            hideElement(infoEl);
            if (validateBtn) validateBtn.disabled = false;
            if (loadingSpinner) loadingSpinner.classList.add('hidden');
            if (loadingText) loadingText.textContent = 'Validate Repository';
            break;
    }
}

function showValidationError(message) {
    updateElementContent('validation-error-message', message);
    updateRepositoryUI('error');
}

function displayRepositoryInfo(info) {
    updateElementContent('repository-info-title', `Repository: ${info.repositoryPath}`);
    updateElementContent('validation-success-message', `Repository validated successfully at ${info.repositoryPath}`);
    
    const detailsHtml = `
        <div class="grid grid-cols-2 gap-2">
            <div>
                <p class="text-gray-600"><strong>Current Branch:</strong></p>
                <p class="font-mono bg-gray-100 px-2 py-1 rounded text-sm">${info.currentBranch}</p>
            </div>
            <div>
                <p class="text-gray-600"><strong>Last Commit:</strong></p>
                <p class="text-sm">${info.lastCommit}</p>
            </div>
            <div>
                <p class="text-gray-600"><strong>Status:</strong></p>
                <p class="text-sm ${info.hasChanges ? 'text-orange-600' : 'text-green-600'}">
                    ${info.hasChanges ? 'Has changes' : 'Clean'}
                </p>
            </div>
            <div>
                <p class="text-gray-600"><strong>Files:</strong></p>
                <p class="text-sm">${info.stagedFiles} staged, ${info.unstagedFiles} unstaged</p>
            </div>
            ${info.aheadBy > 0 ? `<div><p class="text-gray-600">Ahead:</p><p class="text-sm text-blue-600">${info.aheadBy} commits</p></div>` : ''}
            ${info.behindBy > 0 ? `<div><p class="text-gray-600">Behind:</p><p class="text-sm text-yellow-600">${info.behindBy} commits</p></div>` : ''}
        </div>
    `;
    
    updateElementHtml('repository-info-details', detailsHtml);
    
    // Show visual indicators
    const pathInput = document.getElementById('repository-path');
    const validIcon = document.getElementById('path-valid-icon');
    const invalidIcon = document.getElementById('path-invalid-icon');
    
    if (pathInput) pathInput.classList.add('border-green-500', 'focus:border-green-500');
    if (validIcon) validIcon.classList.remove('hidden');
    if (invalidIcon) invalidIcon.classList.add('hidden');
}

function clearRepositoryValidation() {
    repositoryState.path = '';
    repositoryState.isValid = false;
    repositoryState.error = null;
    repositoryState.info = null;
    
    const pathInput = document.getElementById('repository-path');
    const validIcon = document.getElementById('path-valid-icon');
    const invalidIcon = document.getElementById('path-invalid-icon');
    
    if (pathInput) {
        pathInput.value = '';
        pathInput.classList.remove('border-green-500', 'focus:border-green-500', 'border-red-500', 'focus:border-red-500');
    }
    if (validIcon) validIcon.classList.add('hidden');
    if (invalidIcon) invalidIcon.classList.add('hidden');
    
    updateRepositoryUI('clear');
}

function showValidationError(message) {
    repositoryState.error = message;
    updateElementContent('validation-error-message', message);
    updateRepositoryUI('error');
    
    // Show error indicators
    const pathInput = document.getElementById('repository-path');
    const validIcon = document.getElementById('path-valid-icon');
    const invalidIcon = document.getElementById('path-invalid-icon');
    
    if (pathInput) {
        pathInput.classList.remove('border-green-500', 'focus:border-green-500');
        pathInput.classList.add('border-red-500', 'focus:border-red-500');
    }
    if (validIcon) validIcon.classList.add('hidden');
    if (invalidIcon) invalidIcon.classList.remove('hidden');
}

// Repository validation event handlers
function initializeRepositoryValidation() {
    const pathInput = document.getElementById('repository-path');
    const validateBtn = document.getElementById('validate-repository-btn');
    const closeErrorBtn = document.getElementById('close-validation-error-btn');

    if (pathInput) {
        pathInput.addEventListener('input', (e) => {
            const path = e.target.value.trim();
            if (validateBtn) validateBtn.disabled = !path;
            
            // Clear validation states on new input
            if (path !== repositoryState.path) {
                updateRepositoryUI('clear');
            }
        });
        
        pathInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && e.target.value.trim()) {
                validateRepository();
            }
        });
    }
    
    if (validateBtn) {
        validateBtn.addEventListener('click', validateRepository);
    }
    
    if (closeErrorBtn) {
        closeErrorBtn.addEventListener('click', () => {
            updateRepositoryUI('clear');
        });
    }
}

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
    
    // Initialize repository validation
    initializeRepositoryValidation();
}

// Global error handlers
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});

// Language state management
const languageState = {
    supportedLanguages: [],
    selectedLanguage: null,
    detectedLanguages: [],
    fileCounts: {},
    loading: false,
    error: null
};

// Workflow state management
const workflowState = {
    currentStep: 1,
    completedSteps: [],
    steps: {
        1: { name: 'documents', completed: false, required: false },
        2: { name: 'repository', completed: false, required: true },
        3: { name: 'language', completed: false, required: true },
        4: { name: 'analysis', completed: false, required: true },
        5: { name: 'results', completed: false, required: false }
    }
};

// Step completion criteria
const stepCompletionCriteria = {
    1: function() {
        // Step 1: Documents - completed when documents are loaded
        return documentManager.documents.length > 0;
    },
    2: function() {
        // Step 2: Repository - completed when repository is validated
        return repositoryState.isValid === true;
    },
    3: function() {
        // Step 3: Language - completed when language is selected
        return languageState.selectedLanguage !== null;
    },
    4: function() {
        // Step 4: Analysis - placeholder, always completed for now
        return true;
    },
    5: function() {
        // Step 5: Results - placeholder, always completed for now
        return true;
    }
};

// Workflow navigation functions
function showStep(stepNumber) {
    // Validate step number
    if (stepNumber < 1 || stepNumber > 5) return;
    
    // Check if trying to navigate to a step that's not accessible
    if (stepNumber > workflowState.currentStep && !canNavigateToStep(stepNumber)) {
        console.log(`Cannot navigate to step ${stepNumber} - previous steps not completed`);
        return;
    }
    
    // Hide all step contents
    document.querySelectorAll('.step-content').forEach(content => {
        content.classList.remove('active');
    });
    
    // Update progress indicators
    updateProgressIndicators(stepNumber);
    
    // Show current step
    const currentContent = document.getElementById(`step-${stepNumber}-content`);
    if (currentContent) {
        currentContent.classList.add('active');
    }
    
    workflowState.currentStep = stepNumber;
    updateNavigationButtons();
    console.log(`Switched to step ${stepNumber}`);
}

function updateProgressIndicators(currentStep) {
    // Update step indicators
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        
        // Remove all state classes
        indicator.classList.remove('active', 'completed');
        
        if (stepNum < currentStep) {
            // Completed steps
            indicator.classList.add('completed');
        } else if (stepNum === currentStep) {
            // Current step
            indicator.classList.add('active');
        }
    });
    
    // Update step connections
    document.querySelectorAll('.step-connection').forEach((connection, index) => {
        const stepNum = index + 1;
        
        // Remove all state classes
        connection.classList.remove('active', 'completed');
        
        if (stepNum < currentStep) {
            // Completed connections
            connection.classList.add('active', 'completed');
        } else if (stepNum < currentStep) {
            // Active connection leading to current step
            connection.classList.add('active');
        }
    });
}

function canNavigateToStep(stepNumber) {
    // Can navigate to any step that's already completed or the current step
    console.log(`Checking navigation to step ${stepNumber}:`);
    for (let i = 1; i < stepNumber; i++) {
        console.log(`  Step ${i}: completed=${workflowState.steps[i].completed}, required=${workflowState.steps[i].required}`);
        if (!workflowState.steps[i].completed && workflowState.steps[i].required) {
            console.log(`  Cannot navigate - step ${i} is required but not completed`);
            return false;
        }
    }
    console.log(`  Navigation to step ${stepNumber} is allowed`);
    return true;
}

function markStepCompleted(stepNumber) {
    if (stepNumber < 1 || stepNumber > 5) return;
    
    // Check completion criteria
    if (!stepCompletionCriteria[stepNumber] || !stepCompletionCriteria[stepNumber]()) {
        return false;
    }
    
    workflowState.steps[stepNumber].completed = true;
    workflowState.completedSteps.push(stepNumber);
    
    // Update visual indicator
    const indicator = document.querySelector(`[data-step="${stepNumber}"]`);
    if (indicator) {
        indicator.classList.add('completed');
        indicator.classList.remove('active');
    }
    
    // Update connection
    const connection = document.querySelector(`.step-connection:nth-child(${stepNumber * 2})`);
    if (connection) {
        connection.classList.add('completed');
    }
    
    console.log(`Step ${stepNumber} marked as completed`);
    updateNavigationButtons();
    return true;
}

function updateNavigationButtons() {
    const currentStep = workflowState.currentStep;
    
    // Update Previous button
    const prevBtn = document.getElementById(`previous-step-${currentStep}-btn`);
    if (prevBtn) {
        prevBtn.disabled = currentStep === 1;
    }
    
    // Update Next button
    const nextBtn = document.getElementById(`next-step-${currentStep}-btn`);
    if (nextBtn) {
        if (currentStep === 5) {
            // Last step - show "Run Analysis" button
            nextBtn.style.display = 'none';
            const runBtn = document.getElementById('run-analysis-btn');
            if (runBtn) {
                runBtn.style.display = 'inline-flex';
                runBtn.disabled = !canNavigateToStep(5);
            }
        } else {
            nextBtn.disabled = !canNavigateToStep(currentStep + 1);
        }
    }
    
    // Update progress indicator clickability
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        indicator.classList.remove('clickable');
        
        if (stepNum <= currentStep || canNavigateToStep(stepNum)) {
            indicator.classList.add('clickable');
        }
    });
}

function initializeWorkflowNavigation() {
    // Add click handlers to step indicators
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        indicator.addEventListener('click', () => {
            console.log(`Step indicator ${stepNum} clicked`);
            console.log(`  Current step: ${workflowState.currentStep}`);
            console.log(`  Can navigate to ${stepNum}: ${canNavigateToStep(stepNum)}`);
            if (stepNum <= workflowState.currentStep || canNavigateToStep(stepNum)) {
                console.log(`  Navigating to step ${stepNum}`);
                showStep(stepNum);
            } else {
                console.log(`  Cannot navigate to step ${stepNum}`);
            }
        });
    });
    
    // Add click handlers to navigation buttons
    for (let i = 1; i <= 5; i++) {
        const prevBtn = document.getElementById(`previous-step-${i}-btn`);
        const nextBtn = document.getElementById(`next-step-${i}-btn`);
        
        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                if (i > 1) {
                    showStep(i - 1);
                }
            });
        }
        
        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                if (i < 5 && markStepCompleted(i)) {
                    showStep(i + 1);
                }
            });
        }
    }
    
    // Run Analysis button
    const runBtn = document.getElementById('run-analysis-btn');
    if (runBtn) {
        runBtn.addEventListener('click', () => {
            if (canNavigateToStep(5)) {
                console.log('Running analysis...');
                // Add analysis logic here
            }
        });
    }
}

// Enhanced completion tracking for existing functionality
function onDocumentLoadSuccess() {
    console.log('Documents loaded successfully');
    markStepCompleted(1);
}

function onDocumentLoadError() {
    console.log('Document loading failed');
    // Don't mark as completed on error
}

function onRepositoryValidationSuccess() {
    console.log('Repository validated successfully');
    markStepCompleted(2);
    
    // Auto-detect language from validated repository
    if (repositoryState.path) {
        detectRepositoryLanguage(repositoryState.path);
    }
    
    // Debug: Check Step 3 availability after repository validation
    console.log('Step 2 completed:', workflowState.steps[2].completed);
    console.log('Step 3 required:', workflowState.steps[3].required);
    console.log('Can navigate to Step 3:', canNavigateToStep(3));
    console.log('Current step:', workflowState.currentStep);
}

function onRepositoryValidationError() {
    console.log('Repository validation failed');
    // Don't mark as completed on error
}

// Language management functions
async function loadSupportedLanguages() {
    try {
        const response = await fetch('/api/languageapi/supported');
        if (!response.ok) throw new Error('Failed to load languages');
        
        const data = await response.json();
        languageState.supportedLanguages = data.languages || [];
        populateLanguageDropdown();
        return true;
    } catch (error) {
        console.error('Error loading languages:', error);
        languageState.error = error.message;
        return false;
    }
}

async function detectRepositoryLanguage(repositoryPath) {
    if (!repositoryPath) return;
    
    languageState.loading = true;
    languageState.error = null;
    updateLanguageUI('loading');
    
    try {
        const response = await fetch('/api/languageapi/detect', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ repositoryPath })
        });
        
        if (!response.ok) throw new Error('Language detection failed');
        
        const data = await response.json();
        languageState.detectedLanguages = data.detectedLanguages || [];
        languageState.fileCounts = data.fileCounts || {};
        languageState.selectedLanguage = data.primaryLanguage || 'multi';
        
        updateLanguageUI('loaded');
        
        // Auto-select detected language
        if (languageState.selectedLanguage) {
            selectLanguage(languageState.selectedLanguage);
        }
        
        return true;
    } catch (error) {
        console.error('Error detecting language:', error);
        languageState.error = error.message;
        languageState.selectedLanguage = 'multi';
        updateLanguageUI('error');
        return false;
    } finally {
        languageState.loading = false;
    }
}

function selectLanguage(languageId) {
    const language = languageState.supportedLanguages.find(l => l.id === languageId);
    if (!language) return;
    
    languageState.selectedLanguage = languageId;
    updateLanguageUI('selected');
    
    // Mark Step 3 as completed and enable Step 4
    markStepCompleted(3);
    
    console.log(`Language selected: ${language.name}`);
}

function populateLanguageDropdown() {
    const dropdown = document.getElementById('language-select');
    if (!dropdown) return;
    
    dropdown.innerHTML = '<option value="">Select a language...</option>';
    
    languageState.supportedLanguages.forEach(language => {
        const option = document.createElement('option');
        option.value = language.id;
        option.textContent = `${language.icon} ${language.name}`;
        option.selected = language.id === languageState.selectedLanguage;
        dropdown.appendChild(option);
    });
    
    // Add change event listener
    dropdown.addEventListener('change', (e) => {
        if (e.target.value) {
            selectLanguage(e.target.value);
        }
    });
}

function updateLanguageUI(state) {
    const loadingEl = document.getElementById('language-loading');
    const errorEl = document.getElementById('language-error');
    const contentEl = document.getElementById('language-content');
    const selectedEl = document.getElementById('language-selected');
    const detectionResultsEl = document.getElementById('detection-results');
    const detectedLanguagesEl = document.getElementById('detected-languages');
    const fileCountSummaryEl = document.getElementById('file-count-summary');
    const fileCountsEl = document.getElementById('file-counts');
    const selectedLanguageNameEl = document.getElementById('selected-language-name');
    
    if (!loadingEl || !errorEl || !contentEl || !selectedEl) return;
    
    // Reset all states
    loadingEl.classList.add('hidden');
    errorEl.classList.add('hidden');
    contentEl.classList.add('hidden');
    selectedEl.classList.add('hidden');
    if (detectionResultsEl) detectionResultsEl.classList.add('hidden');
    if (fileCountSummaryEl) fileCountSummaryEl.classList.add('hidden');
    
    switch (state) {
        case 'loading':
            loadingEl.classList.remove('hidden');
            break;
        case 'error':
            errorEl.classList.remove('hidden');
            const errorMessageEl = document.getElementById('language-error-message');
            if (errorMessageEl) errorMessageEl.textContent = languageState.error || 'An error occurred';
            contentEl.classList.remove('hidden');
            break;
        case 'selected':
            selectedEl.classList.remove('hidden');
            const selectedLanguage = languageState.supportedLanguages.find(l => l.id === languageState.selectedLanguage);
            if (selectedLanguage && selectedLanguageNameEl) {
                selectedLanguageNameEl.textContent = `${selectedLanguage.icon} ${selectedLanguage.name}`;
            }
            contentEl.classList.remove('hidden');
            break;
        default:
            contentEl.classList.remove('hidden');
            // Show detection results and file counts
            if (languageState.detectedLanguages.length > 0 && detectionResultsEl) {
                detectionResultsEl.classList.remove('hidden');
                if (detectedLanguagesEl) {
                    detectedLanguagesEl.innerHTML = '';
                    languageState.detectedLanguages.forEach(langId => {
                        const lang = languageState.supportedLanguages.find(l => l.id === langId);
                        if (lang) {
                            const badge = document.createElement('span');
                            badge.className = 'inline-flex items-center px-3 py-1 rounded-full text-sm bg-blue-100 text-blue-800';
                            badge.textContent = `${lang.icon} ${lang.name}`;
                            detectedLanguagesEl.appendChild(badge);
                        }
                    });
                }
            }
            
            if (Object.keys(languageState.fileCounts).length > 0 && fileCountSummaryEl) {
                fileCountSummaryEl.classList.remove('hidden');
                if (fileCountsEl) {
                    fileCountsEl.innerHTML = '';
                    Object.entries(languageState.fileCounts).forEach(([langId, count]) => {
                        const lang = languageState.supportedLanguages.find(l => l.id === langId);
                        if (lang) {
                            const div = document.createElement('div');
                            div.className = 'flex justify-between';
                            div.innerHTML = `<span>${lang.icon} ${lang.name}</span><span class="font-medium">${count} files</span>`;
                            fileCountsEl.appendChild(div);
                        }
                    });
                }
            }
            break;
    }
    
    // Update dropdown selection
    const dropdown = document.getElementById('language-select');
    if (dropdown) {
        dropdown.value = languageState.selectedLanguage || '';
    }
}

function updateLanguageUI(state) {
    const loadingEl = document.getElementById('language-loading');
    const errorEl = document.getElementById('language-error');
    const contentEl = document.getElementById('language-content');
    const selectedEl = document.getElementById('language-selected');
    
    if (!loadingEl || !errorEl || !contentEl || !selectedEl) return;
    
    // Reset all states
    loadingEl.classList.add('hidden');
    errorEl.classList.add('hidden');
    contentEl.classList.add('hidden');
    selectedEl.classList.add('hidden');
    
    switch (state) {
        case 'loading':
            loadingEl.classList.remove('hidden');
            break;
        case 'error':
            errorEl.classList.remove('hidden');
            errorEl.textContent = languageState.error || 'An error occurred';
            break;
        case 'selected':
            selectedEl.classList.remove('hidden');
            const selectedLanguage = languageState.supportedLanguages.find(l => l.id === languageState.selectedLanguage);
            if (selectedLanguage) {
                selectedEl.textContent = `Selected: ${selectedLanguage.icon} ${selectedLanguage.name}`;
            }
            // Fall through to show content too
            contentEl.classList.remove('hidden');
            break;
        default:
            contentEl.classList.remove('hidden');
            break;
    }
    
    // Update dropdown selection
    const dropdown = document.getElementById('language-select');
    if (dropdown) {
        dropdown.value = languageState.selectedLanguage || '';
    }
}

// Document ready check
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM fully loaded and parsed');
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Initialize event listeners
    initializeEventListeners();
    
    // Initialize workflow navigation
    initializeWorkflowNavigation();
    
    // Load supported languages
    loadSupportedLanguages();
    
    // Initialize workflow UI
    showStep(1);
    
    console.log('✅ Application initialized successfully');
    
    // Initialize repository validation UI state
    updateRepositoryUI('clear');
});