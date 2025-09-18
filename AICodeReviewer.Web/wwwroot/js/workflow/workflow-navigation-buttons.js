// AI Code Reviewer - Workflow Navigation Button Management

import { workflowState } from './workflow-state.js';
import { canNavigateToStep } from './workflow-navigation-core.js';
import { modelState } from '../models/model-state.js';

function updateNavigationButtons() {
    const currentStep = workflowState.currentStep;
    
    // Update Previous button
    const prevBtn = document.getElementById(`previous-step-${currentStep}-btn`);
    if (prevBtn) {
        prevBtn.disabled = currentStep === 1;
    }
    
    // Update Next button
    const nextBtn = document.getElementById(`next-step-${currentStep}-btn`);
    if (nextBtn) {
        if (currentStep === 5) {
            // Last step - show "Run Analysis" button
            nextBtn.style.display = 'none';
            const runBtn = document.getElementById('run-analysis-btn');
            if (runBtn) {
                runBtn.style.display = 'inline-flex';
                const canNavigate = canNavigateToStep(5);
                const step5Completed = workflowState.steps[5].completed;
                const modelSelected = modelState.selectedModel !== null;
                console.log(`Run button state - canNavigateToStep(5): ${canNavigate}, step5 completed: ${step5Completed}, model selected: ${modelSelected}`);
                runBtn.disabled = false; // Enable when all steps are completed and model is selected
                console.log('Run button disabled state:', runBtn.disabled);
            }
        } else {
            nextBtn.disabled = !canNavigateToStep(currentStep + 1);
        }
    }
    
    // Update progress indicator clickability
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        indicator.classList.remove('clickable');
        
        if (stepNum <= currentStep || canNavigateToStep(stepNum)) {
            indicator.classList.add('clickable');
        }
    });
}

export { updateNavigationButtons };