// AI Code Reviewer - Model Event Handlers

import { selectModel, loadAvailableModels } from './model-selector.js';

export function initializeModelEventListeners() {
    // Model selection
    const modelDropdown = document.getElementById('model-select');
    if (modelDropdown) {
        modelDropdown.addEventListener('change', (e) => {
            if (e.target.value) {
                selectModel(e.target.value);
            }
        });
    }

    // Auto-load models when Step 5 becomes visible
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                const step5Content = document.getElementById('step-5-content');
                if (step5Content && step5Content.classList.contains('active')) {
                    loadAvailableModels();
                }
            }
        });
    });

    const step5Content = document.getElementById('step-5-content');
    if (step5Content) {
        observer.observe(step5Content, { attributes: true });
    }
}