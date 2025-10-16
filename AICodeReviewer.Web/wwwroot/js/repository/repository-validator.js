// AI Code Reviewer - Repository Validation Functions

import { apiEndpoints } from '../core/constants.js';
import { canNavigateToStep, markStepCompleted } from '../workflow/workflow-navigation.js';
import { workflowState } from '../workflow/workflow-state.js';
import { repositoryState } from './repository-state.js';
import { showValidationError, updateRepositoryUI } from './repository-ui.js';

export async function validateRepository() {
    const path = document.getElementById('repository-path').value.trim();

    console.log('🔍 validateRepository called with path:', path);

    if (!path) {
        showValidationError('Please enter a repository path');
        return;
    }

    repositoryState.path = path;
    repositoryState.isValidating = true;
    repositoryState.error = null;

    try {
        updateRepositoryUI('validating');

        console.log('📡 Sending validation request to:', apiEndpoints.validateRepository);

        const response = await fetch(apiEndpoints.validateRepository, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ repositoryPath: path })
        });

        console.log('📥 Validation response status:', response.status);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        console.log('✅ Validation result:', result);

        if (result.success && result.isValidRepo) {
            // Additional validation: check if branch info indicates a real repository
            if (result.currentBranch && result.currentBranch !== "No git repository found") {
                repositoryState.isValid = true;
                repositoryState.info = result;
                updateRepositoryUI('success', result);

                console.log('✅ Repository is valid, calling onRepositoryValidationSuccess');

                // Trigger workflow progression and auto-detection
                onRepositoryValidationSuccess();
            } else {
                console.log('❌ Repository validation failed: invalid branch info');
                repositoryState.isValid = false;
                showValidationError('Not a valid git repository or access denied');
            }
        } else {
            console.log('❌ Repository validation failed:', result.error);
            repositoryState.isValid = false;
            showValidationError(result.error || 'Invalid repository');
        }
    } catch (error) {
        console.error('❌ Validation error:', error);
        showValidationError(error.message || 'Failed to validate repository');
    } finally {
        repositoryState.isValidating = false;
    }
}

function onRepositoryValidationSuccess() {
    console.log('✅ Repository validated successfully');
    console.log('📊 Repository state:', repositoryState);
    console.log('🎯 Calling markStepCompleted(2)...');

    const result = markStepCompleted(2);

    console.log('✅ markStepCompleted(2) returned:', result);

    // Auto-detect language from validated repository
    if (repositoryState.path) {
        // We'll implement this later
        // detectRepositoryLanguage(repositoryState.path);
    }

    // Debug: Check Step 3 availability after repository validation
    console.log('📊 Workflow state after completion:');
    console.log('   Step 2 completed:', workflowState.steps[2].completed);
    console.log('   Step 3 required:', workflowState.steps[3].required);
    console.log('   Can navigate to Step 3:', canNavigateToStep(3));
    console.log('   Current step:', workflowState.currentStep);
}