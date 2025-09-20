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

// Event listener tracker
const eventListeners = [];

/**
 * Cleanup all workflow navigation event listeners
 */
export function cleanupWorkflowNavigation() {
    eventListeners.forEach(({ element, event, handler }) => {
        element.removeEventListener(event, handler);
    });
    eventListeners.length = 0; // Clear the array
}

/**
 * Initialize workflow navigation with cleanup capability
 */
export function initializeWorkflowNavigation() {
    // Clean up any existing listeners first
    cleanupWorkflowNavigation();

    // Track step indicators
    document.querySelectorAll('.step-indicator').forEach((indicator, index) => {
        const stepNum = index + 1;
        const handler = () => {
            console.log(`Step indicator ${stepNum} clicked`);
            console.log(`  Current step: ${workflowState.currentStep}`);
            console.log(`  Can navigate to ${stepNum}: ${canNavigateToStep(stepNum)}`);
            if (stepNum <= workflowState.currentStep || canNavigateToStep(stepNum)) {
                console.log(`  Navigating to step ${stepNum}`);
                showStep(stepNum);
            } else {
                console.log(`  Cannot navigate to step ${stepNum}`);
            }
        };
        indicator.addEventListener('click', handler);
        eventListeners.push({ element: indicator, event: 'click', handler });
    });
    
    // Track navigation buttons
    for (let i = 1; i <= 5; i++) {
        const prevBtn = document.getElementById(`previous-step-${i}-btn`);
        const nextBtn = document.getElementById(`next-step-${i}-btn`);
        
        if (prevBtn) {
            const handler = () => {
                if (i > 1) {
                    showStep(i - 1);
                }
            };
            prevBtn.addEventListener('click', handler);
            eventListeners.push({ element: prevBtn, event: 'click', handler });
        }
        
        if (nextBtn) {
            const handler = () => {
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
            };
            nextBtn.addEventListener('click', handler);
            eventListeners.push({ element: nextBtn, event: 'click', handler });
        }
    }
    
    // Track run analysis button
    const runBtn = document.getElementById('run-analysis-btn');
    if (runBtn) {
        const handler = async () => {
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
        };
        runBtn.addEventListener('click', handler);
        eventListeners.push({ element: runBtn, event: 'click', handler });
    }
    
    // Track language and model selection
    const languageSelect = document.getElementById('language-select');
    if (languageSelect) {
        const handler = (e) => {
            if (e.target.value) {
                console.log('Language selected via dropdown:', e.target.value);
                selectLanguage(e.target.value);
                // Mark step 3 as completed
                markStepCompleted(3);
                console.log('Step 3 completion status after language selection:', workflowState.steps[3].completed);
            }
        };
        languageSelect.addEventListener('change', handler);
        eventListeners.push({ element: languageSelect, event: 'change', handler });
    }
    
    const modelSelect = document.getElementById('model-select');
    if (modelSelect) {
        const handler = (e) => {
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
        };
        modelSelect.addEventListener('change', handler);
        eventListeners.push({ element: modelSelect, event: 'change', handler });
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

/**
 * Reinitialize workflow navigation with cleanup
 */
export function reinitializeWorkflowNavigation() {
    cleanupWorkflowNavigation();
    initializeWorkflowNavigation();
}