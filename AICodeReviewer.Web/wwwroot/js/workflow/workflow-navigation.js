// AI Code Reviewer - Workflow Navigation Export Module

// Import all the functionality from the new modular files
import { canNavigateToStep } from './workflow-navigation-core.js';
import { showStep } from './workflow-navigation-core.js';
import { updateProgressIndicators } from './workflow-navigation-indicators.js';
import { markStepCompleted } from './workflow-navigation-completion.js';
import { updateNavigationButtons } from './workflow-navigation-buttons.js';
import { initializeWorkflowNavigation } from './workflow-navigation-initializer.js';
import { cleanupWorkflowNavigation } from './workflow-navigation-initializer.js';
import { reinitializeWorkflowNavigation } from './workflow-navigation-initializer.js';

// Re-export all public functions
export {
    canNavigateToStep,
    showStep,
    markStepCompleted,
    initializeWorkflowNavigation,
    cleanupWorkflowNavigation,
    reinitializeWorkflowNavigation
};

// Keep the getPrerequisitesError function as a global function for backward compatibility
window.getPrerequisitesError = function() {
    const missing = [];
    
    if (!workflowState.steps[1].completed) missing.push('Step 1: Repository validation');
    if (!workflowState.steps[2].completed) missing.push('Step 2: Requirements documents');
    if (!workflowState.steps[3].completed) missing.push('Step 3: Language selection');
    if (!workflowState.steps[4].completed) missing.push('Step 4: Analysis configuration');
    if (!workflowState.steps[5].completed) missing.push('Step 5: AI model selection');
    
    return `Please complete the following steps before running analysis: ${missing.join(', ')}`;
};

// Export the private functions that are used internally by the new files
export {
    updateProgressIndicators,
    updateNavigationButtons
};