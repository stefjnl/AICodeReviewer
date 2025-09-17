// Grid Workflow Manager - Desktop Optimized
class GridWorkflowManager {
    constructor() {
        this.currentStep = 1;
        this.maxSteps = 5; // Only 5 main steps now, Start Analysis is separate
        this.steps = document.querySelectorAll('.workflow-grid-item');
        this.isInitialized = false;
        this.initialize();
    }

    initialize() {
        if (this.steps.length === 0) return;
        
        this.setupInitialState();
        this.bindStepEvents();
        this.setupProgressiveWorkflow();
        this.isInitialized = true;
        console.log('Horizontal Workflow Manager initialized');
    }

    setupInitialState() {
        // Start with only first step active and fully visible
        this.currentStep = 1;
        this.updateStepStates();
        
        // Ensure first step is properly configured
        const firstStep = this.steps[0];
        if (firstStep) {
            firstStep.classList.add('active');
            firstStep.classList.remove('disabled');
        }
    }

    updateStepStates() {
        this.steps.forEach((step, index) => {
            const stepNumber = index + 1;
            if (stepNumber <= this.currentStep) {
                step.classList.add('active');
                step.classList.remove('disabled');
            } else {
                step.classList.remove('active');
                step.classList.add('disabled');
            }
        });
    }

    bindStepEvents() {
        // Handle form submissions and input changes for auto-advancement
        this.steps.forEach((step, index) => {
            const stepNumber = index + 1;
            console.log(`[Workflow] Binding events for step ${stepNumber}`);
            
            // Handle all form submissions within the step
            const forms = step.querySelectorAll('form');
            console.log(`[Workflow] Found ${forms.length} forms in step ${stepNumber}`);
            forms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    console.log(`[Workflow] Form submitted in step ${stepNumber}`);
                    // Let the form submit naturally, then advance if successful
                    setTimeout(() => {
                        const isValid = this.validateStep(stepNumber);
                        console.log(`[Workflow] Step ${stepNumber} validation after form submit: ${isValid}`);
                        if (isValid) {
                            this.nextStep();
                        }
                    }, 300);
                });
            });

            // Handle input changes for real-time validation
            const inputs = step.querySelectorAll('input, select');
            console.log(`[Workflow] Found ${inputs.length} inputs in step ${stepNumber}`);
            inputs.forEach(input => {
                input.addEventListener('change', () => {
                    console.log(`[Workflow] Input changed in step ${stepNumber}: ${input.name || input.id || input.type}`);
                    // Small delay to allow form processing
                    setTimeout(() => {
                        const isValid = this.validateStep(stepNumber);
                        console.log(`[Workflow] Step ${stepNumber} validation after input change: ${isValid}`);
                        if (isValid) {
                            // Only auto-advance if this is the current step
                            if (stepNumber === this.currentStep) {
                                console.log(`[Workflow] Auto-advancing from step ${stepNumber} to ${stepNumber + 1}`);
                                this.nextStep();
                            }
                        }
                    }, 500);
                });
            });
        });
    }

    setupProgressiveWorkflow() {
        // Add visual feedback for step completion
        this.steps.forEach((step, index) => {
            const stepNumber = index + 1;
            
            // Add completion checkmark for completed steps
            if (stepNumber < this.currentStep) {
                this.addCompletionIndicator(step);
            }
        });
    }

    addCompletionIndicator(step) {
        if (step.querySelector('.step-completed')) return;
        
        const indicator = document.createElement('div');
        indicator.className = 'step-completed';
        indicator.innerHTML = '✓';
        indicator.style.cssText = `
            position: absolute;
            top: -5px;
            right: -5px;
            background-color: #38a169;
            color: white;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 12px;
            font-weight: bold;
            z-index: 20;
        `;
        step.appendChild(indicator);
    }

    removeCompletionIndicator(step) {
        const indicator = step.querySelector('.step-completed');
        if (indicator) {
            indicator.remove();
        }
    }

    validateStep(stepNumber) {
        const step = document.querySelector(`[data-step="${stepNumber}"]`);
        if (!step) {
            console.log(`[Workflow] Step ${stepNumber} validation FAILED - step element not found`);
            return false;
        }

        console.log(`[Workflow] === VALIDATING STEP ${stepNumber} ===`);
        
        // Check for required inputs
        const requiredInputs = step.querySelectorAll('[required]');
        console.log(`[Workflow] Found ${requiredInputs.length} required inputs in step ${stepNumber}`);
        
        if (requiredInputs.length === 0) {
            console.log(`[Workflow] No required inputs found in step ${stepNumber}, checking for alternative validation`);
            
            // Custom validation logic for each step
            switch (stepNumber) {
                case 1: // Requirements
                    const selectedDocuments = step.querySelectorAll('input[name="selectedDocuments"]:checked');
                    const hasSelectedDocs = selectedDocuments.length > 0;
                    console.log(`[Workflow] Step 1 - Selected documents: ${selectedDocuments.length}, Valid: ${hasSelectedDocs}`);
                    if (!hasSelectedDocs) {
                        console.log(`[Workflow] Step 1 validation FAILED - no documents selected`);
                    }
                    return hasSelectedDocs;
                    
                case 2: // Language
                    const languageSelect = step.querySelector('#languageSelect');
                    // Check if language select exists and has a valid value, or if it doesn't exist (meaning default is set)
                    const hasLanguage = languageSelect && (languageSelect.value !== '' && languageSelect.value !== null && languageSelect.value !== undefined);
                    console.log(`[Workflow] Step 2 - Language selected: ${languageSelect?.value}, Valid: ${hasLanguage}`);
                    // Step 2 should be valid by default since .NET is pre-selected
                    const step2Valid = hasLanguage || !languageSelect;
                    if (!step2Valid) {
                        console.log(`[Workflow] Step 2 validation FAILED - no language selected`);
                    }
                    return step2Valid;
                    
                case 3: // Repository
                    const repositoryPath = step.querySelector('input[name="repositoryPath"]');
                    let hasRepositoryPath = repositoryPath && repositoryPath.value.trim() !== '';
                    
                    // If no repository path is set, provide a default
                    if (!hasRepositoryPath) {
                        console.log(`[Workflow] Step 3 - No repository path found, setting default`);
                        // Set default repository path
                        const defaultPath = 'C:\\git\\AICodeReviewer\\AICodeReviewer'; // Fallback default
                        if (repositoryPath) {
                            repositoryPath.value = defaultPath;
                            hasRepositoryPath = true;
                            console.log(`[Workflow] Step 3 - Set default repository path: ${defaultPath}`);
                        } else {
                            console.log(`[Workflow] Step 3 - Repository input not found, cannot set default`);
                        }
                    } else {
                        console.log(`[Workflow] Step 3 - Repository path: ${repositoryPath.value}, Valid: ${hasRepositoryPath}`);
                    }
                    
                    if (!hasRepositoryPath) {
                        console.log(`[Workflow] Step 3 validation FAILED - no repository path set and no default available`);
                    }
                    return hasRepositoryPath;
                    
                case 4: // Analysis
                    const analysisType = step.querySelector('input[name="analysisType"]:checked');
                    const hasAnalysisType = analysisType && analysisType.value !== '';
                    console.log(`[Workflow] Step 4 - Analysis type: ${analysisType?.value}, Valid: ${hasAnalysisType}`);
                    
                    if (analysisType && analysisType.value === 'commit') {
                        const commitId = step.querySelector('#commitId');
                        const hasCommitId = commitId && commitId.value.trim() !== '';
                        console.log(`[Workflow] Step 4 - Commit analysis, Commit ID: ${commitId?.value}, Valid: ${hasCommitId}`);
                        if (!hasCommitId) {
                            console.log(`[Workflow] Step 4 validation FAILED - commit analysis selected but no commit ID provided`);
                        }
                        return hasCommitId;
                    }
                    
                    if (analysisType && analysisType.value === 'singlefile') {
                        const filePath = step.querySelector('#filePath');
                        const hasFilePath = filePath && filePath.value.trim() !== '';
                        console.log(`[Workflow] Step 4 - Single file analysis, File path: ${filePath?.value}, Valid: ${hasFilePath}`);
                        if (!hasFilePath) {
                            console.log(`[Workflow] Step 4 validation FAILED - single file analysis selected but no file path provided`);
                        }
                        return hasFilePath;
                    }
                    
                    if (!hasAnalysisType) {
                        console.log(`[Workflow] Step 4 validation FAILED - no analysis type selected`);
                    }
                    return hasAnalysisType;
                    
                case 5: // Model
                    const modelSelect = step.querySelector('#modelSelect');
                    // Check if model select exists and has a valid value, or if it doesn't exist (meaning default is set)
                    const hasModel = modelSelect && (modelSelect.value !== '' && modelSelect.value !== null && modelSelect.value !== undefined);
                    console.log(`[Workflow] Step 5 - Model selected: ${modelSelect?.value}, Valid: ${hasModel}`);
                    // Step 5 should be valid by default since first model is pre-selected
                    const step5Valid = hasModel || !modelSelect;
                    if (!step5Valid) {
                        console.log(`[Workflow] Step 5 validation FAILED - no model selected`);
                    }
                    return step5Valid;
                    
                default:
                    console.log(`[Workflow] Step ${stepNumber} - No validation rules, defaulting to true`);
                    return true;
            }
        }

        const validationResult = Array.from(requiredInputs).every(input => {
            if (input.type === 'checkbox') {
                return input.checked;
            }
            if (input.type === 'radio') {
                const radioGroup = step.querySelectorAll(`input[name="${input.name}"]`);
                return Array.from(radioGroup).some(radio => radio.checked);
            }
            return input.value.trim() !== '';
        });
        
        console.log(`[Workflow] Step ${stepNumber} - Required inputs validation result: ${validationResult}`);
        return validationResult;
    }

    nextStep() {
        console.log(`[Workflow] Attempting to advance from step ${this.currentStep}`);
        console.log(`[Workflow] Max steps: ${this.maxSteps}, Current step: ${this.currentStep}`);
        
        if (this.currentStep < this.maxSteps) {
            // Add completion indicator to current step
            const currentStepElement = this.steps[this.currentStep - 1];
            if (currentStepElement) {
                this.addCompletionIndicator(currentStepElement);
            }
            
            this.currentStep++;
            this.updateStepStates();
            this.triggerStepChange();
            
            console.log(`[Workflow] Advanced to step ${this.currentStep}`);
        } else {
            console.log(`[Workflow] Reached max step ${this.maxSteps}, advancing to step 6 (Start Analysis)`);
            // Advance to step 6 (Start Analysis) when step 5 is completed
            this.currentStep = 6;
            this.triggerStepChange();
            console.log(`[Workflow] Advanced to step 6 - workflow complete`);
        }
    }
    
    triggerStep6() {
        console.log('[Workflow] Triggering step 6 (Start Analysis)');
        // Dispatch custom event for step 6 (Start Analysis)
        window.dispatchEvent(new CustomEvent('workflowStepChanged', {
            detail: { currentStep: 6 }
        }));
        console.log('[Workflow] Step 6 event dispatched');
    }

    previousStep() {
        if (this.currentStep > 1) {
            this.currentStep--;
            
            // Remove completion indicator from the step we're leaving
            const stepElement = this.steps[this.currentStep];
            if (stepElement) {
                this.removeCompletionIndicator(stepElement);
            }
            
            this.updateStepStates();
            this.scrollToStep(this.currentStep);
            this.triggerStepChange();
            
            console.log(`Returned to step ${this.currentStep}`);
        }
    }

    // Grid layout doesn't need scrolling, but keep method for compatibility
    scrollToStep(stepNumber) {
        console.log(`[Workflow] Grid layout - no scrolling needed for step ${stepNumber}`);
    }

    triggerStepChange() {
        // Dispatch custom event for other components to listen to
        window.dispatchEvent(new CustomEvent('workflowStepChanged', {
            detail: { currentStep: this.currentStep }
        }));
    }

    // Public API methods
    goToStep(stepNumber) {
        if (stepNumber >= 1 && stepNumber <= this.maxSteps) {
            this.currentStep = stepNumber;
            this.updateStepStates();
            this.scrollToStep(stepNumber);
            this.triggerStepChange();
        }
    }

    getCurrentStep() {
        console.log(`[Workflow] getCurrentStep() called, returning: ${this.currentStep}`);
        return this.currentStep;
    }

    isStepValid(stepNumber) {
        return this.validateStep(stepNumber);
    }

    validateAndAdvanceStep(stepNumber) {
        console.log(`[Workflow] Manual validation request for step ${stepNumber}`);
        const isValid = this.validateStep(stepNumber);
        console.log(`[Workflow] Step ${stepNumber} manual validation result: ${isValid}`);
        
        if (isValid && stepNumber === this.currentStep) {
            console.log(`[Workflow] Manually advancing from step ${stepNumber}`);
            this.nextStep();
            return true;
        } else if (isValid && stepNumber === 6) {
            // Special case for step 6 - allow validation even if it's not the "current" step
            console.log(`[Workflow] Step 6 validation requested, treating as workflow complete`);
            this.currentStep = 6;
            this.triggerStepChange();
            return true;
        }
        return false;
    }

    enableAllSteps() {
        this.currentStep = this.maxSteps;
        this.updateStepStates();
    }

    reset() {
        this.currentStep = 1;
        this.updateStepStates();
        
        // Remove all completion indicators
        this.steps.forEach(step => {
            this.removeCompletionIndicator(step);
        });
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('[Workflow] DOMContentLoaded - Checking for workflow grid container');
    
    if (document.querySelector('.workflow-grid-container')) {
        console.log('[Workflow] Found workflow grid container, initializing workflow manager');
        window.horizontalWorkflow = new GridWorkflowManager();
        
        // Expose API globally for other scripts
        window.workflowAPI = {
            nextStep: () => window.horizontalWorkflow?.nextStep(),
            previousStep: () => window.horizontalWorkflow?.previousStep(),
            goToStep: (step) => window.horizontalWorkflow?.goToStep(step),
            getCurrentStep: () => window.horizontalWorkflow?.getCurrentStep(),
            reset: () => window.horizontalWorkflow?.reset(),
            validateAndAdvanceStep: (step) => window.horizontalWorkflow?.validateAndAdvanceStep(step),
            isStepValid: (step) => window.horizontalWorkflow?.isStepValid(step)
        };
        
        console.log('[Workflow] Grid Workflow API available at window.workflowAPI');
        console.log('[Workflow] Initial workflow step:', window.horizontalWorkflow?.getCurrentStep());
    } else {
        console.log('[Workflow] No workflow grid container found');
    }
});

// Handle window resize - grid layout doesn't need scroll position maintenance
window.addEventListener('resize', () => {
    if (window.horizontalWorkflow) {
        console.log('[Workflow] Grid layout - resize handled automatically by CSS Grid');
    }
});