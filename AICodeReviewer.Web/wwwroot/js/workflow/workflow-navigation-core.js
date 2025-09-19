// AI Code Reviewer - Workflow Navigation Core Functions

import { workflowState } from './workflow-state.js';
import { updateProgressIndicators } from './workflow-navigation-indicators.js';
import { updateNavigationButtons } from './workflow-navigation-buttons.js';

export function canNavigateToStep(targetStep) {
    if (targetStep < 1 || targetStep > 5) return false;
    
    // Always allow navigation to completed or current steps
    if (targetStep <= workflowState.currentStep) return true;
    
    // Check if all required previous steps are completed
    for (let step = 1; step < targetStep; step++) {
        if (workflowState.steps[step].required && !workflowState.steps[step].completed) {
            return false;
        }
    }
    
    return true;
}

export function showStep(stepNumber) {
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