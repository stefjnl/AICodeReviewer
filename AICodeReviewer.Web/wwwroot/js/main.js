// AI Code Reviewer - Main Application Entry Point
// Vanilla JavaScript implementation - ES6 Modules

import { initializeSignalR } from './signalr/signalr-client.js';
import { loadDocuments } from './documents/document-api.js';
import { clearError, clearSelection } from './documents/document-ui.js';
import { initializeRepositoryEventListeners, reinitializeRepositoryEventListeners } from './repository/repository-event-handlers.js';
import { initializeWorkflowNavigation, showStep, reinitializeWorkflowNavigation } from './workflow/workflow-navigation.js';
import { loadSupportedLanguages } from './language/language-detector.js';
import { initializeAnalysisEventListeners } from './analysis/analysis-event-handlers.js';
import { initializeModelEventListeners } from './models/model-event-handlers.js';
import { updateRepositoryUI } from './repository/repository-ui.js';
import { initializeAnalysisState } from './analysis/analysis-state.js';
import { modelState } from './models/model-state.js';
import { documentManager } from './documents/document-manager.js';
import ThemeManager from './core/theme-manager.js';

console.log('🚀 AI Code Reviewer - Main.js loaded successfully');

// Event listener tracker for document system
const documentEventListeners = [];

/**
 * Cleanup all document-related event listeners
 */
export function cleanupDocumentEventListeners() {
    documentEventListeners.forEach(({ element, event, handler }) => {
        if (element && element.removeEventListener) {
            element.removeEventListener(event, handler);
        }
    });
    documentEventListeners.length = 0; // Clear the array
    console.log('🧹 Document event listeners cleaned up');
}

/**
 * Initialize document event listeners with cleanup capability
 */
export function initializeDocumentEventListeners() {
// Clean up any existing listeners first
cleanupDocumentEventListeners();

console.log('🔍 Initializing document event listeners...');

// Load documents button
const loadButton = document.getElementById('load-documents-btn');
if (loadButton) {
    const handler = () => {
        console.log('📄 load-documents-btn clicked');
        loadDocuments();
    };
    loadButton.addEventListener('click', handler);
    documentEventListeners.push({ element: loadButton, event: 'click', handler });
    console.log('✅ load-documents-btn listener attached');
} else {
    console.warn('⚠️ load-documents-btn not found');
}

// Close error button
const closeErrorBtn = document.getElementById('close-error-btn');
if (closeErrorBtn) {
    const handler = () => {
        console.log('❌ close-error-btn clicked');
        clearError();
    };
    closeErrorBtn.addEventListener('click', handler);
    documentEventListeners.push({ element: closeErrorBtn, event: 'click', handler });
    console.log('✅ close-error-btn listener attached');
} else {
    console.warn('⚠️ close-error-btn not found');
}

// Close document button
const closeDocumentBtn = document.getElementById('close-document-btn');
if (closeDocumentBtn) {
    const handler = () => {
        console.log('🗑️ close-document-btn clicked');
        clearSelection();
    };
    closeDocumentBtn.addEventListener('click', handler);
    documentEventListeners.push({ element: closeDocumentBtn, event: 'click', handler });
    console.log('✅ close-document-btn listener attached');
} else {
    console.warn('⚠️ close-document-btn not found');
}

console.log(`📋 Document event listeners initialized: ${documentEventListeners.length} listeners`);
}

/**
 * Reinitialize document event listeners with cleanup
 */
export function reinitializeDocumentEventListeners() {
    cleanupDocumentEventListeners();
    initializeDocumentEventListeners();
}

// Document ready check
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM fully loaded and parsed');
    
    // Initialize theme manager
    const themeManager = new ThemeManager();
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Initialize document event listeners
    initializeDocumentEventListeners();
    
    // Initialize workflow navigation
    initializeWorkflowNavigation();
    
    // Create reinitialization functions for future use
    window.reinitializeWorkflowNavigation = reinitializeWorkflowNavigation;
    window.reinitializeDocumentEventListeners = reinitializeDocumentEventListeners;
    
    // Initialize analysis event listeners
    initializeAnalysisEventListeners();
    
    // Initialize model event listeners
    initializeModelEventListeners();
    
    // Load supported languages
    loadSupportedLanguages();
    
    // Initialize workflow UI
    showStep(1);
    
    console.log('✅ Application initialized successfully');
    console.log(`🎨 Current theme: ${themeManager.getCurrentTheme()}`);
    
    // Initialize repository validation UI state
    updateRepositoryUI('clear');
    
    // Initialize repository event listeners
    console.log('🔍 DEBUG: About to call initializeRepositoryEventListeners()');
    initializeRepositoryEventListeners();
    
    // Also initialize the repository browser separately
    console.log('🔍 DEBUG: About to call initializeRepositoryBrowser()');
    import('./repository/repository-browser.js').then(module => {
        module.initializeRepositoryBrowser();
        console.log('🔍 DEBUG: Repository browser initialized');
    }).catch(error => {
        console.error('❌ ERROR: Failed to initialize repository browser:', error);
    });

    // Initialize file browser for single file analysis
    console.log('🔍 DEBUG: About to call initializeFileBrowser()');
    import('./analysis/file-browser.js').then(module => {
        module.initializeFileBrowser();
        console.log('🔍 DEBUG: File browser initialized');
    }).catch(error => {
        console.error('❌ ERROR: Failed to initialize file browser:', error);
    });
    
    // Initialize analysis state
    initializeAnalysisState();
    
    // Initialize model state
    modelState.availableModels = [];
    modelState.selectedModel = null;
    modelState.loading = false;
    modelState.error = null;
});

// Event listeners initialization (removed - use initializeDocumentEventListeners instead)
// function initializeEventListeners() {}

// Global error handlers
window.addEventListener('error', (event) => {
    console.error('Global error:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
});