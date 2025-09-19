// AI Code Reviewer - Main Application Entry Point
// Vanilla JavaScript implementation - ES6 Modules

import { initializeSignalR } from './signalr/signalr-client.js';
import { loadDocuments } from './documents/document-api.js';
import { clearError, clearSelection } from './documents/document-ui.js';
import { initializeRepositoryValidation } from './repository/repository-event-handlers.js';
import { initializeWorkflowNavigation, showStep } from './workflow/workflow-navigation.js';
import { loadSupportedLanguages } from './language/language-detector.js';
import { initializeAnalysisEventListeners } from './analysis/analysis-event-handlers.js';
import { initializeModelEventListeners } from './models/model-event-handlers.js';
import { updateRepositoryUI } from './repository/repository-ui.js';
import { initializeAnalysisState } from './analysis/analysis-state.js';
import { modelState } from './models/model-state.js';
import { documentManager } from './documents/document-manager.js';
import ThemeManager from './core/theme-manager.js';

console.log('🚀 AI Code Reviewer - Main.js loaded successfully');

// Document ready check
document.addEventListener('DOMContentLoaded', function() {
    console.log('📄 DOM fully loaded and parsed');
    
    // Initialize theme manager
    const themeManager = new ThemeManager();
    
    // Initialize SignalR connection
    initializeSignalR();
    
    // Initialize event listeners
    initializeEventListeners();
    
    // Initialize workflow navigation
    initializeWorkflowNavigation();
    
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
    
    // Initialize analysis state
    initializeAnalysisState();
    
    // Initialize model state
    modelState.availableModels = [];
    modelState.selectedModel = null;
    modelState.loading = false;
    modelState.error = null;
});

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