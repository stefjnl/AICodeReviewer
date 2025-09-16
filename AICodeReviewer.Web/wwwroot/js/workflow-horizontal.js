// Horizontal Workflow Manager - Desktop Only
class HorizontalWorkflowManager {
    constructor() {
        this.currentStep = 1;
        this.maxSteps = 4;
        this.steps = document.querySelectorAll('.workflow-step');
        this.connectors = document.querySelectorAll('.workflow-step-connector');
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

        // Update connector visibility based on progression
        this.connectors.forEach((connector, index) => {
            const nextStep = index + 2;
            connector.style.opacity = nextStep <= this.currentStep ? '0.8' : '0.3';
        });
    }

    bindStepEvents() {
        // Handle form submissions and input changes for auto-advancement
        this.steps.forEach((step, index) => {
            const stepNumber = index + 1;
            
            // Handle all form submissions within the step
            const forms = step.querySelectorAll('form');
            forms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    // Let the form submit naturally, then advance if successful
                    setTimeout(() => {
                        if (this.validateStep(stepNumber)) {
                            this.nextStep();
                        }
                    }, 300);
                });
            });

            // Handle input changes for real-time validation
            const inputs = step.querySelectorAll('input, select');
            inputs.forEach(input => {
                input.addEventListener('change', () => {
                    // Small delay to allow form processing
                    setTimeout(() => {
                        if (this.validateStep(stepNumber)) {
                            // Only auto-advance if this is the current step
                            if (stepNumber === this.currentStep) {
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
        if (!step) return false;

        // Check for required inputs
        const requiredInputs = step.querySelectorAll('[required]');
        if (requiredInputs.length === 0) return true;

        return Array.from(requiredInputs).every(input => {
            if (input.type === 'checkbox') {
                return input.checked;
            }
            if (input.type === 'radio') {
                const radioGroup = step.querySelectorAll(`input[name="${input.name}"]`);
                return Array.from(radioGroup).some(radio => radio.checked);
            }
            return input.value.trim() !== '';
        });
    }

    nextStep() {
        if (this.currentStep < this.maxSteps) {
            // Add completion indicator to current step
            const currentStepElement = this.steps[this.currentStep - 1];
            if (currentStepElement) {
                this.addCompletionIndicator(currentStepElement);
            }
            
            this.currentStep++;
            this.updateStepStates();
            this.scrollToStep(this.currentStep);
            this.triggerStepChange();
            
            console.log(`Advanced to step ${this.currentStep}`);
        }
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

    scrollToStep(stepNumber) {
        const step = document.querySelector(`[data-step="${stepNumber}"]`);
        if (step) {
            step.scrollIntoView({ 
                behavior: 'smooth', 
                block: 'nearest', 
                inline: 'center' 
            });
        }
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
        return this.currentStep;
    }

    isStepValid(stepNumber) {
        return this.validateStep(stepNumber);
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
    if (document.querySelector('.workflow-horizontal-container')) {
        window.horizontalWorkflow = new HorizontalWorkflowManager();
        
        // Expose API globally for other scripts
        window.workflowAPI = {
            nextStep: () => window.horizontalWorkflow?.nextStep(),
            previousStep: () => window.horizontalWorkflow?.previousStep(),
            goToStep: (step) => window.horizontalWorkflow?.goToStep(step),
            getCurrentStep: () => window.horizontalWorkflow?.getCurrentStep(),
            reset: () => window.horizontalWorkflow?.reset()
        };
        
        console.log('Horizontal Workflow API available at window.workflowAPI');
    }
});

// Handle window resize to maintain horizontal scroll position
window.addEventListener('resize', () => {
    if (window.horizontalWorkflow) {
        const currentStep = window.horizontalWorkflow.getCurrentStep();
        window.horizontalWorkflow.scrollToStep(currentStep);
    }
});