// AI Code Reviewer - Workflow Navigation Functions

import { workflowState, stepCompletionCriteria } from './workflow-state.js';
import { showElement, hideElement } from '../core/ui-helpers.js';
import { selectLanguage } from '../language/language-detector.js';
import { selectModel } from '../models/model-selector.js';
import { languageState } from '../language/language-state.js';
import { modelState } from '../models/model-state.js';

// Workflow navigation functions
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

export function initializeWorkflowNavigation() {
    // Add click handlers to step indicators
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        indicator.addEventListener('click', () => {
            console.log(`Step indicator ${stepNum} clicked`);
            console.log(`  Current step: ${workflowState.currentStep}`);
            console.log(`  Can navigate to ${stepNum}: ${canNavigateToStep(stepNum)}`);
            if (stepNum <= workflowState.currentStep || canNavigateToStep(stepNum)) {
                console.log(`  Navigating to step ${stepNum}`);
                showStep(stepNum);
            } else {
                console.log(`  Cannot navigate to step ${stepNum}`);
            }
        });
    });
    
    // Add click handlers to navigation buttons
    for (let i = 1; i <= 5; i++) {
        const prevBtn = document.getElementById(`previous-step-${i}-btn`);
        const nextBtn = document.getElementById(`next-step-${i}-btn`);
        
        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                if (i > 1) {
                    showStep(i - 1);
                }
            });
        }
        
        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                if (i < 5) {
                    // For steps 3 and 5, we need to manually trigger completion
                    if (i === 3) {
                        // Check if language is selected
                        if (languageState.selectedLanguage) {
                            markStepCompleted(i);
                            showStep(i + 1);
                        } else {
                            console.log('Cannot proceed: language not selected');
                        }
                    } else if (i === 5) {
                        // Check if model is selected
                        if (modelState.selectedModel) {
                            markStepCompleted(i);
                            // For step 5, we would run analysis
                            console.log('Running analysis...');
                        } else {
                            console.log('Cannot proceed: model not selected');
                        }
                    } else {
                        if (markStepCompleted(i)) {
                            showStep(i + 1);
                        }
                    }
                }
            });
        }
    }
    
    // Run Analysis button
    const runBtn = document.getElementById('run-analysis-btn');
    if (runBtn) {
        runBtn.addEventListener('click', () => {
            console.log('Run analysis button clicked - checking prerequisites...');
            if (canNavigateToStep(5)) {
                console.log('All prerequisites met - starting analysis');
                // Add analysis logic here
            } else {
                console.log('Prerequisites not met - cannot start analysis');
                console.log('Step completion status:', workflowState.steps);
            }
        });
    }
    
    // Add event listeners for language and model selection to auto-complete steps
    const languageSelect = document.getElementById('language-select');
    if (languageSelect) {
        languageSelect.addEventListener('change', (e) => {
            if (e.target.value) {
                console.log('Language selected via dropdown:', e.target.value);
                selectLanguage(e.target.value);
                // Mark step 3 as completed
                markStepCompleted(3);
                console.log('Step 3 completion status after language selection:', workflowState.steps[3].completed);
            }
        });
    }
    
    const modelSelect = document.getElementById('model-select');
    if (modelSelect) {
        modelSelect.addEventListener('change', (e) => {
            if (e.target.value) {
                console.log('Model selected via dropdown:', e.target.value);
                selectModel(e.target.value);
                // Mark step 5 as completed
                const completed = markStepCompleted(5);
                console.log('Step 5 completion status after model selection:', workflowState.steps[5].completed, 'markStepCompleted result:', completed);
                
                // Ensure the run button is updated immediately
                if (workflowState.currentStep === 5) {
                    const runBtn = document.getElementById('run-analysis-btn');
                    if (runBtn) {
                        runBtn.disabled = false;
                        console.log('Run analysis button enabled immediately');
                    }
                }
            }
        });
    }
}