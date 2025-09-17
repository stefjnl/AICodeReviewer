/**
 * Workflow Integration Functions - Handles workflow-specific functionality
 */

// Define toggle functions if they don't exist
if (typeof window.toggleStandardsSelector !== 'function') {
    window.toggleStandardsSelector = function() {
        const dropdown = document.getElementById('standardsDropdown');
        if (dropdown) {
            dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
        }
    };
}

if (typeof window.toggleModelSelector !== 'function') {
    window.toggleModelSelector = function() {
        const dropdown = document.getElementById('modelDropdown');
        if (dropdown) {
            dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
        }
    };
}

// Override dropdown functions to work with new structure
document.addEventListener('DOMContentLoaded', function() {
    if (window.horizontalWorkflow) {
        // Store original functions
        window.originalToggleStandardsSelector = window.toggleStandardsSelector;
        window.originalToggleModelSelector = window.toggleModelSelector;
        
        // Override standards selector for workflow
        window.toggleStandardsSelector = function() {
            const dropdown = document.querySelector('[data-step="1"] #standardsDropdown');
            if (!dropdown) {
                // Fallback to original function if override fails
                if (window.originalToggleStandardsSelector) {
                    return window.originalToggleStandardsSelector();
                }
                return;
            }
            dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
        };
        
        // Override model selector for workflow
        window.toggleModelSelector = function() {
            const dropdown = document.querySelector('[data-step="5"] #modelDropdown');
            if (dropdown) {
                dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
            } else if (window.originalToggleModelSelector) {
                // Fallback to original function if override fails
                return window.originalToggleModelSelector();
            }
        };
        
        // Listen for workflow step changes to auto-validate steps with default values
        window.addEventListener('workflowStepChanged', function(event) {
            const currentStep = event.detail.currentStep;
            
            // Auto-validate step 2 when user reaches it (language has default value)
            if (currentStep === 2) {
                if (window.workflowAPI) {
                    const isValid = window.workflowAPI.validateAndAdvanceStep(2);
                    
                    // If step 2 validation succeeds, automatically advance through remaining steps
                    if (isValid) {
                        // Chain the validations with appropriate delays
                        setTimeout(() => {
                            if (window.workflowAPI) {
                                // Ensure repository path is set to default if empty
                                const repoInput = document.getElementById('repositoryPathInput');
                                if (repoInput && !repoInput.value.trim()) {
                                    repoInput.value = window.repositoryPath || 'C:\\git\\AICodeReviewer\\AICodeReviewer';
                                }
                                window.workflowAPI.validateAndAdvanceStep(3);
                                window.workflowAPI.validateAndAdvanceStep(4);
                                window.workflowAPI.validateAndAdvanceStep(5);
                                window.workflowAPI.validateAndAdvanceStep(6);
                            }
                        }, 200);
                    }
                }
            }
        });
    }
});

// Start Analysis Workflow Integration
function startAnalysisFromWorkflow() {
    try {
        // Validate that all workflow steps are complete
        const currentStep = window.horizontalWorkflow?.getCurrentStep();
        
        if (window.horizontalWorkflow && currentStep >= 6) {
            // Hide workflow and show progress immediately
            const workflowContainer = document.querySelector('.workflow-grid-container');
            if (workflowContainer) {
                workflowContainer.style.display = 'none';
            }
            
            // Show the analysis results section
            const analysisResultsSection = document.getElementById('analysisResultsSection');
            if (analysisResultsSection) {
                analysisResultsSection.style.display = 'block';
                analysisResultsSection.classList.remove('hidden-analysis');
            }
            
            // Show progress container for SignalR updates
            const progressContainer = document.getElementById('progressContainer');
            if (progressContainer) {
                progressContainer.style.display = 'block';
            }
            
            // Hide bottom panel view during progress
            const bottomPanelView = document.getElementById('bottomPanelView');
            if (bottomPanelView) {
                bottomPanelView.style.display = 'none';
            }
            
            // Hide analysis result display initially
            const analysisResult = document.getElementById('analysisResult');
            if (analysisResult) {
                analysisResult.style.display = 'none';
            }
            
            // Update progress message
            const progressMessage = document.getElementById('progressMessage');
            if (progressMessage) {
                progressMessage.style.display = 'block';
                progressMessage.textContent = 'Starting analysis...';
            }
            
            // Call the existing startAnalysis function from site.js
            if (typeof startAnalysis === 'function') {
                startAnalysis();
            } else {
                alert('Analysis function not available. Please check the console for errors.');
                // Reset UI on error
                if (typeof resetAnalysisUI === 'function') {
                    resetAnalysisUI();
                }
            }
        } else {
            alert('Please complete all workflow steps before starting analysis.');
        }
    } catch (error) {
        alert('Error starting analysis: ' + error.message);
        if (typeof resetAnalysisUI === 'function') {
            resetAnalysisUI();
        }
    }
}

// Reset analysis UI on error
function resetAnalysisUI() {
    const workflowContainer = document.querySelector('.workflow-grid-container');
    if (workflowContainer) {
        workflowContainer.style.display = 'flex';
    }
    
    const analysisResultsSection = document.getElementById('analysisResultsSection');
    if (analysisResultsSection) {
        analysisResultsSection.style.display = 'none';
    }
    
    const progressMessage = document.getElementById('progressMessage');
    if (progressMessage) {
        progressMessage.style.display = 'none';
    }
}

// Auto-validate step 6 when step 5 is completed
window.addEventListener('workflowStepChanged', function(event) {
    if (event.detail.currentStep >= 6) {
        if (window.workflowAPI) {
            window.workflowAPI.validateAndAdvanceStep(6);
        }
    }
});

// Make functions globally available
window.startAnalysisFromWorkflow = startAnalysisFromWorkflow;
window.resetAnalysisUI = resetAnalysisUI;

// Handle document selection form submission
function handleDocumentSelection(event) {
    event.preventDefault();
    
    const form = document.getElementById('documentSelectionForm');
    if (!form) return;
    
    // Submit form via AJAX
    const formData = new FormData(form);
    
    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    })
    .then(response => {
        if (response.ok) {
            // Hide dropdown after successful submission
            const dropdown = document.getElementById('standardsDropdown');
            if (dropdown) {
                dropdown.style.display = 'none';
            }
            
            // Reload page to reflect changes
            window.location.reload();
        } else {
            alert('Failed to save document selection. Please try again.');
        }
    })
    .catch(error => {
        console.error('Error saving document selection:', error);
        alert('An error occurred while saving document selection.');
    });
}

// Make handleDocumentSelection globally available
window.handleDocumentSelection = handleDocumentSelection;

// Handle repository path form submission
function handleRepositoryPath(event) {
    event.preventDefault();
    
    const form = document.getElementById('repositoryPathForm');
    if (!form) return;
    
    // Submit form via AJAX
    const formData = new FormData(form);
    
    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    })
    .then(response => {
        if (response.ok) {
            // Reload page to reflect changes
            window.location.reload();
        } else {
            alert('Failed to save repository path. Please try again.');
        }
    })
    .catch(error => {
        console.error('Error saving repository path:', error);
        alert('An error occurred while saving repository path.');
    });
}

// Make handleRepositoryPath globally available
window.handleRepositoryPath = handleRepositoryPath;