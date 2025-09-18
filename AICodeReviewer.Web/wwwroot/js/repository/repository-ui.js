// AI Code Reviewer - Repository UI Functions

import { showElement, hideElement, updateElementContent, updateElementHtml } from '../core/ui-helpers.js';
import { repositoryState } from './repository-state.js';

export function updateRepositoryUI(state, result = null) {
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

export function showValidationError(message) {
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

export function clearRepositoryValidation() {
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