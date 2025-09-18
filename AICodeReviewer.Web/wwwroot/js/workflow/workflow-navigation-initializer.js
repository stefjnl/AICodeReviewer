// AI Code Reviewer - Workflow Navigation Initializer

import { showStep } from './workflow-navigation-core.js';
import { markStepCompleted } from './workflow-navigation-completion.js';
import { canNavigateToStep } from './workflow-navigation-core.js';
import { selectLanguage } from '../language/language-detector.js';
import { selectModel } from '../models/model-selector.js';
import { languageState } from '../language/language-state.js';
import { modelState } from '../models/model-state.js';
import { executionService } from '../execution/execution-service.js';
import { resultsDisplay } from '../execution/results-display.js';
import { workflowState } from './workflow-state.js';

// Move the initializeWorkflowNavigation function here without importing itself
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
        runBtn.addEventListener('click', async () => {
            console.log('Run analysis button clicked - checking prerequisites...');
            if (canNavigateToStep(5)) {
                console.log('All prerequisites met - starting analysis');
                
                try {
                    await executionService.startAnalysis();
                } catch (error) {
                    console.error('Error starting analysis:', error);
                    executionService.showErrorState(error.message || 'Failed to start analysis');
                }
            } else {
                console.log('Prerequisites not met - cannot start analysis');
                console.log('Step completion status:', workflowState.steps);
                
                // Show user-friendly error
                const errorMessage = getPrerequisitesError();
                executionService.showErrorState(errorMessage);
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

    // Initialize results display
    resultsDisplay.initialize();
    
    // Add prerequisites error helper - This needs to be global
    window.getPrerequisitesError = function() {
        const missing = [];
        
        if (!workflowState.steps[1].completed) missing.push('Step 1: Repository validation');
        if (!workflowState.steps[2].completed) missing.push('Step 2: Requirements documents');
        if (!workflowState.steps[3].completed) missing.push('Step 3: Language selection');
        if (!workflowState.steps[4].completed) missing.push('Step 4: Analysis configuration');
        if (!workflowState.steps[5].completed) missing.push('Step 5: AI model selection');
        
        return `Please complete the following steps before running analysis: ${missing.join(', ')}`;
    };
}