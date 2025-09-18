// AI Code Reviewer - Workflow Step Completion Logic

import { workflowState, stepCompletionCriteria } from './workflow-state.js';
import { updateNavigationButtons } from './workflow-navigation-buttons.js';

export function markStepCompleted(stepNumber) {
    if (stepNumber < 1 || stepNumber > 5) return false;
    
    // Check completion criteria
    if (!stepCompletionCriteria[stepNumber] || !stepCompletionCriteria[stepNumber]()) {
        return false;
    }
    
    const wasAlreadyCompleted = workflowState.steps[stepNumber].completed;
    
    workflowState.steps[stepNumber].completed = true;
    
    // Only add to completedSteps if not already there
    if (!wasAlreadyCompleted) {
        workflowState.completedSteps.push(stepNumber);
    }
    
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