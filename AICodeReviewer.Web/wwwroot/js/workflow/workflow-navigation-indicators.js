// AI Code Reviewer - Workflow Progress Indicators

import { workflowState } from './workflow-state.js';

function updateProgressIndicators(currentStep) {
    // Update step indicators
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        
        // Remove all state classes
        indicator.classList.remove('active', 'completed');
        
        if (stepNum < currentStep) {
            // Completed steps
            indicator.classList.add('completed');
        } else if (stepNum === currentStep) {
            // Current step
            indicator.classList.add('active');
        }
    });
    
    // Update step connections
    document.querySelectorAll('.step-connection').forEach((connection, index) => {
        const stepNum = index + 1;
        
        // Remove all state classes
        connection.classList.remove('active', 'completed');
        
        if (stepNum < currentStep) {
            // Completed connections
            connection.classList.add('active', 'completed');
        }
    });
}

export { updateProgressIndicators };