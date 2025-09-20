// AI Code Reviewer - Repository Event Handlers

import { initializeRepositoryBrowser } from './repository-browser.js';
import { repositoryState } from './repository-state.js';
import { clearRepositoryValidation, updateRepositoryUI } from './repository-ui.js';
import { validateRepository } from './repository-validator.js';

// Event listener tracker for repository system
const repositoryEventListeners = [];

/**
 * Cleanup all repository-related event listeners
 */
export function cleanupRepositoryEventListeners() {
    repositoryEventListeners.forEach(({ element, event, handler }) => {
        if (element && element.removeEventListener) {
            element.removeEventListener(event, handler);
        }
    });
    repositoryEventListeners.length = 0;
    console.log('🧹 Repository event listeners cleaned up');
}

/**
 * Initialize repository event listeners with cleanup capability
 */
export function initializeRepositoryEventListeners() {
    // Clean up any existing listeners first
    cleanupRepositoryEventListeners();

    const pathInput = document.getElementById('repository-path');
    const validateBtn = document.getElementById('validate-repository-btn');
    const closeErrorBtn = document.getElementById('close-validation-error-btn');
    const resetBtn = document.getElementById('reset-repository-btn');

    if (pathInput) {
        const inputHandler = (e) => {
            const path = e.target.value.trim();
            if (validateBtn) validateBtn.disabled = !path;
            if (path !== repositoryState.path) {
                updateRepositoryUI('clear');
            }
        };
        pathInput.addEventListener('input', inputHandler);
        repositoryEventListeners.push({ element: pathInput, event: 'input', handler: inputHandler });

        const keypressHandler = (e) => {
            if (e.key === 'Enter' && e.target.value.trim()) {
                validateRepository();
            }
        };
        pathInput.addEventListener('keypress', keypressHandler);
        repositoryEventListeners.push({ element: pathInput, event: 'keypress', handler: keypressHandler });
    }

    if (validateBtn) {
        const handler = () => validateRepository();
        validateBtn.addEventListener('click', handler);
        repositoryEventListeners.push({ element: validateBtn, event: 'click', handler });
    }

    if (closeErrorBtn) {
        const handler = () => updateRepositoryUI('clear');
        closeErrorBtn.addEventListener('click', handler);
        repositoryEventListeners.push({ element: closeErrorBtn, event: 'click', handler });
    }

    if (resetBtn) {
        const handler = () => clearRepositoryValidation();
        resetBtn.addEventListener('click', handler);
        repositoryEventListeners.push({ element: resetBtn, event: 'click', handler });
    }

    console.log(`📁 Repository event listeners initialized: ${repositoryEventListeners.length} listeners`);
    
    // Debug: Verify button existence and listener attachment
    console.log('🔍 Repository system initialization complete');
    console.log('   browse-repository-btn:', document.getElementById('browse-repository-btn'));
    console.log('   validate-repository-btn:', document.getElementById('validate-repository-btn'));
    console.log('   repository-path:', document.getElementById('repository-path'));
}

/**
 * Reinitialize repository event listeners with cleanup
 */
export function reinitializeRepositoryEventListeners() {
    cleanupRepositoryEventListeners();
    initializeRepositoryEventListeners();
}

/**
 * Initialize repository validation (deprecated - use initializeRepositoryEventListeners)
 */
export function initializeRepositoryValidation() {
    console.warn('initializeRepositoryValidation() is deprecated. Use initializeRepositoryEventListeners() instead.');
    initializeRepositoryEventListeners();
}
