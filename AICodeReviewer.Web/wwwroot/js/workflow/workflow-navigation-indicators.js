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
    
    // Update progress bar fill width
    const progressFill = document.querySelector('.progress-steps .absolute.h-1.bg-gradient-to-r');
    if (progressFill) {
        // Calculate progress percentage: 20% for step 1, 40% for step 2, etc.
        const progressPercentage = (currentStep - 1) * 20;
        progressFill.style.width = `${progressPercentage}%`;
    }
}

export { updateProgressIndicators };