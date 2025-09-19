// AI Code Reviewer - Repository Event Handlers

import { initializeRepositoryBrowser } from './repository-browser.js';
import { repositoryState } from './repository-state.js';
import { clearRepositoryValidation, updateRepositoryUI } from './repository-ui.js';
import { validateRepository } from './repository-validator.js';

export function initializeRepositoryValidation() {
    // Initialize repository browser functionality
    initializeRepositoryBrowser();

    const pathInput = document.getElementById('repository-path');
    const validateBtn = document.getElementById('validate-repository-btn');
    const closeErrorBtn = document.getElementById('close-validation-error-btn');
    const resetBtn = document.getElementById('reset-repository-btn');

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

    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            clearRepositoryValidation();
        });
    }
}
