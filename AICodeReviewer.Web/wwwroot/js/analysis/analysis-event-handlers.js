// AI Code Reviewer - Analysis Event Handlers

import { selectAnalysisType, selectCommit, selectSourceBranch, selectTargetBranch, loadAnalysisOptions } from './analysis-options.js';

export function initializeAnalysisEventListeners() {
    // Analysis type selection
    document.addEventListener('change', (e) => {
        if (e.target.name === 'analysis-type') {
            selectAnalysisType(e.target.value);
        }
    });

    // Commit selection
    const commitDropdown = document.getElementById('commit-dropdown');
    if (commitDropdown) {
        commitDropdown.addEventListener('change', (e) => {
            selectCommit(e.target.value);
        });
    }

    // Source branch selection
    const sourceBranchDropdown = document.getElementById('source-branch-dropdown');
    if (sourceBranchDropdown) {
        sourceBranchDropdown.addEventListener('change', (e) => {
            selectSourceBranch(e.target.value);
        });
    }

    // Target branch selection
    const targetBranchDropdown = document.getElementById('target-branch-dropdown');
    if (targetBranchDropdown) {
        targetBranchDropdown.addEventListener('change', (e) => {
            selectTargetBranch(e.target.value);
        });
    }

    // Auto-load analysis options when Step 4 becomes visible
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                const step4Content = document.getElementById('step-4-content');
                if (step4Content && step4Content.classList.contains('active')) {
                    loadAnalysisOptions();
                }
            }
        });
    });

    const step4Content = document.getElementById('step-4-content');
    if (step4Content) {
        observer.observe(step4Content, { attributes: true });
    }
}