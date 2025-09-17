/**
 * Model Selector Functions - Handles model selection UI
 */

// Function to toggle model selector
function toggleModelSelector() {
    const dropdown = document.getElementById('modelDropdown');
    if (dropdown) {
        dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
    }
}

// Update model display when selection changes
document.addEventListener('DOMContentLoaded', function() {
    const modelSelect = document.getElementById('modelSelect');
    const modelValue = document.getElementById('modelValue');
    const modelDropdown = document.getElementById('modelDropdown');
    
    if (modelSelect && modelValue) {
        modelSelect.addEventListener('change', function() {
            modelValue.textContent = this.value;
            if (modelDropdown) {
                modelDropdown.style.display = 'none';
            }
        });
    }
    
    // Also handle workflow-specific model selector
    const workflowModelSelect = document.querySelector('[data-step="5"] #modelSelect');
    const workflowModelValue = document.querySelector('[data-step="5"] #modelValue');
    const workflowModelDropdown = document.querySelector('[data-step="5"] #modelDropdown');
    
    if (workflowModelSelect && workflowModelValue) {
        workflowModelSelect.addEventListener('change', function() {
            workflowModelValue.textContent = this.value;
            if (workflowModelDropdown) {
                workflowModelDropdown.style.display = 'none';
            }
        });
    }
});