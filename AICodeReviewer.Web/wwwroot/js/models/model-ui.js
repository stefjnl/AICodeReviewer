// AI Code Reviewer - Model UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';
import { modelState } from './model-state.js';

export function updateModelUI(state) {
    const loadingEl = document.getElementById('model-loading');
    const errorEl = document.getElementById('model-error');
    const contentEl = document.getElementById('model-content');
    const modelSelectEl = document.getElementById('model-select');
    const selectedEl = document.getElementById('model-selected');
    const errorMessageEl = document.getElementById('model-error-message');

    if (!loadingEl || !errorEl || !contentEl) return;

    // Reset all states
    loadingEl.classList.add('hidden');
    errorEl.classList.add('hidden');
    contentEl.classList.add('hidden');

    switch (state) {
        case 'loading':
            loadingEl.classList.remove('hidden');
            break;
        case 'error':
            errorEl.classList.remove('hidden');
            if (errorMessageEl) errorMessageEl.textContent = modelState.error || 'An error occurred';
            contentEl.classList.remove('hidden');
            break;
        case 'loaded':
            contentEl.classList.remove('hidden');
            
            // Show dropdown if models available
            if (modelState.availableModels.length > 0 && modelSelectEl) {
                populateModelDropdown();
            }
            
            // Show selected model if any
            if (modelState.selectedModel && selectedEl) {
                selectedEl.classList.remove('hidden');
                const selectedModel = modelState.availableModels.find(m => m.id === modelState.selectedModel);
                if (selectedModel) {
                    const selectedModelNameEl = document.getElementById('selected-model-name');
                    if (selectedModelNameEl) {
                        selectedModelNameEl.textContent = `${selectedModel.icon} ${selectedModel.name}`;
                    }
                }
            }
            break;
        default:
            contentEl.classList.remove('hidden');
            break;
    }
}

function populateModelDropdown() {
    const modelDropdown = document.getElementById('model-select');
    if (!modelDropdown || modelState.availableModels.length === 0) return;

    modelDropdown.innerHTML = '<option value="">Choose a model...</option>';
    
    modelState.availableModels.forEach(model => {
        const option = document.createElement('option');
        option.value = model.id;
        option.textContent = `${model.icon} ${model.name}`;
        option.selected = model.id === modelState.selectedModel;
        modelDropdown.appendChild(option);
    });
}