// AI Code Reviewer - Repository Event Handlers

import { cloneRepository } from '../api/git-clone-api.js';
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
    const cloneBtn = document.getElementById('clone-repository-btn');
    const closeCloneStatusBtn = document.getElementById('close-clone-status-btn');
    const gitUrlInput = document.getElementById('git-url-input');

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

    // Git Clone button handler
    if (cloneBtn) {
        const handler = async () => {
            await handleCloneRepository();
        };
        cloneBtn.addEventListener('click', handler);
        repositoryEventListeners.push({ element: cloneBtn, event: 'click', handler });
    }

    // Close clone status message handler
    if (closeCloneStatusBtn) {
        const handler = () => {
            const cloneStatus = document.getElementById('clone-status');
            if (cloneStatus) {
                cloneStatus.classList.add('hidden');
            }
        };
        closeCloneStatusBtn.addEventListener('click', handler);
        repositoryEventListeners.push({ element: closeCloneStatusBtn, event: 'click', handler });
    }

    // Enable clone button when URL is entered
    if (gitUrlInput && cloneBtn) {
        const inputHandler = () => {
            const url = gitUrlInput.value.trim();
            cloneBtn.disabled = !url;
        };
        gitUrlInput.addEventListener('input', inputHandler);
        repositoryEventListeners.push({ element: gitUrlInput, event: 'input', handler: inputHandler });
    }

    console.log(`📁 Repository event listeners initialized: ${repositoryEventListeners.length} listeners`);

    // Debug: Verify button existence and listener attachment
    console.log('🔍 Repository system initialization complete');
    console.log('   browse-repository-btn:', document.getElementById('browse-repository-btn'));
    console.log('   validate-repository-btn:', document.getElementById('validate-repository-btn'));
    console.log('   clone-repository-btn:', document.getElementById('clone-repository-btn'));
    console.log('   repository-path:', document.getElementById('repository-path'));
    console.log('   git-url-input:', document.getElementById('git-url-input'));
}

/**
 * Handle Git repository cloning
 */
async function handleCloneRepository() {
    const gitUrlInput = document.getElementById('git-url-input');
    const gitTokenInput = document.getElementById('git-token-input');
    const cloneBtn = document.getElementById('clone-repository-btn');
    const cloneSpinner = document.getElementById('clone-spinner');
    const cloneButtonText = document.getElementById('clone-button-text');
    const cloneStatus = document.getElementById('clone-status');
    const cloneStatusIcon = document.getElementById('clone-status-icon');
    const cloneStatusTitle = document.getElementById('clone-status-title');
    const cloneStatusMessage = document.getElementById('clone-status-message');
    const pathInput = document.getElementById('repository-path');

    if (!gitUrlInput) return;

    const gitUrl = gitUrlInput.value.trim();
    const accessToken = gitTokenInput ? gitTokenInput.value.trim() || null : null;

    if (!gitUrl) {
        showCloneStatus('error', 'Invalid URL', 'Please enter a Git repository URL');
        return;
    }

    // Show loading state
    if (cloneBtn) cloneBtn.disabled = true;
    if (cloneSpinner) cloneSpinner.classList.remove('hidden');
    if (cloneButtonText) cloneButtonText.textContent = 'Cloning...';
    if (cloneStatus) cloneStatus.classList.add('hidden');

    try {
        console.log('🔄 Cloning repository from:', gitUrl);

        // Call clone API
        const result = await cloneRepository(gitUrl, accessToken);

        if (result.success && result.repositoryPath) {
            console.log('✅ Repository cloned successfully to:', result.repositoryPath);

            // Update the repository path input with cloned path
            if (pathInput) {
                pathInput.value = result.repositoryPath;
            }

            // Store the cloned path in repository state
            repositoryState.path = result.repositoryPath;
            repositoryState.isCloned = true;

            // Show success message
            showCloneStatus('success', 'Clone Successful', `Repository cloned to: ${result.repositoryPath}`);

            // Automatically validate the cloned repository
            setTimeout(async () => {
                console.log('🔍 Auto-validating cloned repository...');
                console.log('📍 Repository path:', result.repositoryPath);
                console.log('📊 Repository state before validation:', JSON.stringify(repositoryState));
                await validateRepository();
                console.log('📊 Repository state after validation:', JSON.stringify(repositoryState));
            }, 1000);

        } else {
            console.error('❌ Clone failed:', result.error);
            showCloneStatus('error', 'Clone Failed', result.error || 'Unknown error occurred');
        }

    } catch (error) {
        console.error('❌ Error cloning repository:', error);
        showCloneStatus('error', 'Clone Error', error.message || 'An unexpected error occurred');
    } finally {
        // Reset button state
        if (cloneBtn) cloneBtn.disabled = false;
        if (cloneSpinner) cloneSpinner.classList.add('hidden');
        if (cloneButtonText) cloneButtonText.textContent = 'Clone Repository';
    }
}

/**
 * Show clone status message
 */
function showCloneStatus(type, title, message) {
    const cloneStatus = document.getElementById('clone-status');
    const cloneStatusIcon = document.getElementById('clone-status-icon');
    const cloneStatusTitle = document.getElementById('clone-status-title');
    const cloneStatusMessage = document.getElementById('clone-status-message');

    if (!cloneStatus) return;

    // Remove all type classes
    cloneStatus.classList.remove('bg-green-50', 'border-green-200', 'bg-red-50', 'border-red-200');

    if (cloneStatusTitle) {
        cloneStatusTitle.classList.remove('text-green-800', 'text-red-800');
    }

    if (cloneStatusMessage) {
        cloneStatusMessage.classList.remove('text-green-700', 'text-red-700');
    }

    if (cloneStatusIcon) {
        cloneStatusIcon.classList.remove('text-green-400', 'text-red-400');
    }

    // Apply type-specific styles
    if (type === 'success') {
        cloneStatus.classList.add('bg-green-50', 'border-green-200');
        if (cloneStatusTitle) cloneStatusTitle.classList.add('text-green-800');
        if (cloneStatusMessage) cloneStatusMessage.classList.add('text-green-700');
        if (cloneStatusIcon) {
            cloneStatusIcon.classList.add('text-green-400');
            cloneStatusIcon.innerHTML = '<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />';
        }
    } else {
        cloneStatus.classList.add('bg-red-50', 'border-red-200');
        if (cloneStatusTitle) cloneStatusTitle.classList.add('text-red-800');
        if (cloneStatusMessage) cloneStatusMessage.classList.add('text-red-700');
        if (cloneStatusIcon) {
            cloneStatusIcon.classList.add('text-red-400');
            cloneStatusIcon.innerHTML = '<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />';
        }
    }

    // Set content
    if (cloneStatusTitle) cloneStatusTitle.textContent = title;
    if (cloneStatusMessage) cloneStatusMessage.textContent = message;

    // Show status
    cloneStatus.classList.remove('hidden');
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
