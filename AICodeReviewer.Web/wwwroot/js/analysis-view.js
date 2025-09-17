/**
 * Analysis View Functions - Handles view switching and UI management for analysis results
 */

// View switching functions - use the global currentAnalysisId from site.js
function switchToBottomPanel() {
    const bottomPanelView = document.getElementById('bottomPanelView');
    const sidePanelView = document.getElementById('sidePanelView');
    const bottomPanelBtn = document.getElementById('bottomPanelBtn');
    const sidePanelBtn = document.getElementById('sidePanelBtn');
    
    if (bottomPanelView) bottomPanelView.style.display = 'block';
    if (sidePanelView) sidePanelView.style.display = 'none';
    
    // Update button states
    if (bottomPanelBtn) {
        bottomPanelBtn.classList.remove('btn-outline-primary');
        bottomPanelBtn.classList.add('btn-primary');
    }
    if (sidePanelBtn) {
        sidePanelBtn.classList.remove('btn-primary');
        sidePanelBtn.classList.add('btn-outline-primary');
    }
}

function switchToSidePanel() {
    const bottomPanelView = document.getElementById('bottomPanelView');
    const sidePanelView = document.getElementById('sidePanelView');
    const bottomPanelBtn = document.getElementById('bottomPanelBtn');
    const sidePanelBtn = document.getElementById('sidePanelBtn');
    
    if (bottomPanelView) bottomPanelView.style.display = 'none';
    if (sidePanelView) sidePanelView.style.display = 'block';
    
    // Update button states
    if (sidePanelBtn) {
        sidePanelBtn.classList.remove('btn-outline-primary');
        sidePanelBtn.classList.add('btn-primary');
    }
    if (bottomPanelBtn) {
        bottomPanelBtn.classList.remove('btn-primary');
        bottomPanelBtn.classList.add('btn-outline-primary');
    }
    
    // Load side panel view
    loadSidePanelView();
}

function loadSidePanelView() {
    // Use the global currentAnalysisId from site.js
    if (!window.currentAnalysisId) {
        const sidePanelView = document.getElementById('sidePanelView');
        if (sidePanelView) {
            sidePanelView.innerHTML = '<div class="workflow-alert workflow-alert-warning">No analysis ID available for side-panel view</div>';
        }
        return;
    }
    
    // Show loading state
    const sidePanelView = document.getElementById('sidePanelView');
    if (sidePanelView) {
        sidePanelView.innerHTML =
            '<div class="workflow-alert workflow-alert-info">' +
            '<div class="d-flex align-items-center">' +
            '<div class="spinner-border spinner-border-sm me-2" role="status"></div>' +
            '<span>Loading side-panel view...</span>' +
            '</div>' +
            '</div>';
        sidePanelView.style.display = 'block';
    }
    
    // Redirect to results page with the analysis ID
    window.location.href = `/results/${window.currentAnalysisId}`;
}

// Initialize view based on current state
document.addEventListener('DOMContentLoaded', function() {
    // Check if we have analysis data from session
    const analysisId = window.analysisId || ''; // Will be set by the page
    const analysisResult = window.analysisResult || ''; // Will be set by the page
    const analysisError = window.analysisError || ''; // Will be set by the page
    
    // Set global analysis ID
    if (analysisId) {
        window.currentAnalysisId = analysisId;
    }
    
    // Only initialize if the view switching elements exist
    const analysisResultsSection = document.getElementById('analysisResultsSection');
    if (analysisResultsSection) {
        // If we have analysis data, show the results section
        if (analysisId || analysisResult || analysisError) {
            analysisResultsSection.style.display = 'block';
            analysisResultsSection.classList.remove('hidden-analysis');
            
            // Update the analysis ID in the dataset
            if (analysisId) {
                analysisResultsSection.dataset.analysisId = analysisId;
            }
            
            // Set initial state to bottom panel view
            switchToBottomPanel();
            
            // Inject analysis buttons if we have an analysis ID
            if (analysisId && typeof injectAnalysisButtons === 'function') {
                injectAnalysisButtons();
            }
        } else {
            analysisResultsSection.style.display = 'none';
            analysisResultsSection.classList.add('hidden-analysis');
        }
        
        // Hide the old analysis result section if it exists
        const oldResult = document.querySelector('.analysis-results pre');
        if (oldResult && oldResult.parentElement.parentElement.classList.contains('card')) {
            oldResult.parentElement.parentElement.parentElement.style.display = 'none';
        }
    }
});

// Function to dynamically inject the view-switching buttons
function injectAnalysisButtons() {
    const container = document.getElementById('analysisButtonsContainer');
    const analysisId = document.getElementById('analysisResultsSection')?.dataset.analysisId;
    
    // If no analysis ID yet, don't inject (check for null, undefined, or empty string)
    if (!analysisId || analysisId === '') {
        return;
    }

    // Prevent duplicate injection
    if (container.querySelector('#bottomPanelBtn')) {
        return;
    }

    // Create buttons with workflow styling
    const bottomBtn = document.createElement('button');
    bottomBtn.type = 'button';
    bottomBtn.className = 'btn workflow-btn-primary';
    bottomBtn.id = 'bottomPanelBtn';
    bottomBtn.onclick = switchToBottomPanel;
    bottomBtn.textContent = '📋 Bottom Panel';

    const sideBtn = document.createElement('button');
    sideBtn.type = 'button';
    sideBtn.className = 'btn workflow-btn-outline-primary';
    sideBtn.id = 'sidePanelBtn';
    sideBtn.onclick = switchToSidePanel;
    sideBtn.textContent = '🔀 Side Panel';

    // Clear container and append
    container.innerHTML = '';
    container.appendChild(bottomBtn);
    container.appendChild(sideBtn);

    // Initially set to bottom panel
    switchToBottomPanel();
}

// Add missing workflow button style
const style = document.createElement('style');
style.textContent = `
    .workflow-btn-outline-primary {
        background-color: transparent;
        border: 1px solid #63b3ed;
        color: #63b3ed;
        border-radius: 6px;
        padding: 0.375rem 0.75rem;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s ease;
    }
    
    .workflow-btn-outline-primary:hover {
        background-color: #63b3ed;
        color: #1a202c;
    }
`;
document.head.appendChild(style);

// Only run if the section exists
document.addEventListener('DOMContentLoaded', function() {
    const section = document.getElementById('analysisResultsSection');
    if (section) {
        injectAnalysisButtons();
    }
});

// Make it globally available
window.injectAnalysisButtons = injectAnalysisButtons;