// AI Code Reviewer - Analysis UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';
import { analysisState } from './analysis-state.js';

export function updateAnalysisUI(state) {
    const loadingEl = document.getElementById('analysis-loading');
    const errorEl = document.getElementById('analysis-error');
    const contentEl = document.getElementById('analysis-content');
    const optionsEl = document.getElementById('analysis-options');
    const previewEl = document.getElementById('changes-preview');
    const typeSelectionEl = document.getElementById('analysis-type-selection');
    const commitSelectionEl = document.getElementById('commit-selection');
    const errorMessageEl = document.getElementById('analysis-error-message');

    if (!loadingEl || !errorEl || !contentEl) return;

    // Reset all states
    loadingEl.classList.add('hidden');
    errorEl.classList.add('hidden');
    contentEl.classList.add('hidden');
    if (optionsEl) optionsEl.classList.add('hidden');
    if (previewEl) previewEl.classList.add('hidden');
    if (typeSelectionEl) typeSelectionEl.classList.add('hidden');
    if (commitSelectionEl) commitSelectionEl.classList.add('hidden');

    switch (state) {
        case 'loading':
            loadingEl.classList.remove('hidden');
            break;
        case 'error':
            errorEl.classList.remove('hidden');
            if (errorMessageEl) errorMessageEl.textContent = analysisState.error || 'An error occurred';
            contentEl.classList.remove('hidden');
            break;
        case 'loaded':
            contentEl.classList.remove('hidden');
            
            // Show type selection
            if (typeSelectionEl) typeSelectionEl.classList.remove('hidden');
            
            // Show commit selection if needed
            if (analysisState.analysisType === 'commit' && commitSelectionEl) {
                commitSelectionEl.classList.remove('hidden');
                populateCommitDropdown();
            }
            
            // Show options if available
            if ((analysisState.availableOptions.commits.length > 0 ||
                 analysisState.availableOptions.modifiedFiles.length > 0 ||
                 analysisState.availableOptions.stagedFiles.length > 0) &&
                optionsEl) {
                optionsEl.classList.remove('hidden');
                populateAnalysisOptions();
            }
            
            // Show preview if available
            if (analysisState.changesSummary && previewEl) {
                previewEl.classList.remove('hidden');
                displayChangesSummary();
            }
            break;
        default:
            contentEl.classList.remove('hidden');
            break;
    }
}

// Analysis options population
function populateAnalysisOptions() {
    // Populate commits list (always shown)
    const commitsList = document.getElementById('commits-list');
    if (commitsList && analysisState.availableOptions.commits.length > 0) {
        commitsList.innerHTML = '';
        analysisState.availableOptions.commits.forEach(commit => {
            const commitDiv = document.createElement('div');
            commitDiv.className = 'text-xs text-gray-600 mb-1';
            commitDiv.textContent = `${commit.id} - ${commit.message} (${commit.date})`;
            commitsList.appendChild(commitDiv);
        });
    } else if (commitsList) {
        commitsList.innerHTML = '<div class="text-xs text-gray-500">No recent commits</div>';
    }

    // Populate files list (always shown)
    const filesList = document.getElementById('files-list');
    if (filesList) {
        filesList.innerHTML = '';
        
        // Show modified files
        if (analysisState.availableOptions.modifiedFiles.length > 0) {
            const modifiedDiv = document.createElement('div');
            modifiedDiv.className = 'mb-2';
            modifiedDiv.innerHTML = '<div class="font-medium text-gray-700">Modified:</div>';
            analysisState.availableOptions.modifiedFiles.forEach(file => {
                const fileDiv = document.createElement('div');
                fileDiv.className = 'text-xs text-gray-600 ml-2';
                fileDiv.textContent = `• ${file}`;
                modifiedDiv.appendChild(fileDiv);
            });
            filesList.appendChild(modifiedDiv);
        }

        // Show staged files
        if (analysisState.availableOptions.stagedFiles.length > 0) {
            const stagedDiv = document.createElement('div');
            stagedDiv.className = 'mb-2';
            stagedDiv.innerHTML = '<div class="font-medium text-gray-700">Staged:</div>';
            analysisState.availableOptions.stagedFiles.forEach(file => {
                const fileDiv = document.createElement('div');
                fileDiv.className = 'text-xs text-gray-600 ml-2';
                fileDiv.textContent = `• ${file}`;
                stagedDiv.appendChild(fileDiv);
            });
            filesList.appendChild(stagedDiv);
        }

        if (filesList.innerHTML === '') {
            filesList.innerHTML = '<div class="text-xs text-gray-500">No files detected</div>';
        }
    }
}

// Commit dropdown population
function populateCommitDropdown() {
    const commitDropdown = document.getElementById('commit-dropdown');
    if (commitDropdown && analysisState.availableOptions.commits.length > 0) {
        commitDropdown.innerHTML = '<option value="">Choose a commit...</option>';
        analysisState.availableOptions.commits.forEach(commit => {
            const option = document.createElement('option');
            option.value = commit.id;
            option.textContent = `${commit.id} - ${commit.message}`;
            commitDropdown.appendChild(option);
        });
    } else if (commitDropdown) {
        commitDropdown.innerHTML = '<option value="">No commits available</option>';
    }
}

// Changes summary display
function displayChangesSummary() {
    const summaryDiv = document.getElementById('changes-summary');
    if (!summaryDiv || !analysisState.changesSummary) return;

    const summary = analysisState.changesSummary;
    summaryDiv.innerHTML = `
        <div class="grid grid-cols-3 gap-4 text-center">
            <div>
                <div class="text-2xl font-bold text-blue-600">${summary.filesModified || 0}</div>
                <div class="text-xs text-gray-600">Files Modified</div>
            </div>
            <div>
                <div class="text-2xl font-bold text-green-600">+${summary.additions || 0}</div>
                <div class="text-xs text-gray-600">Additions</div>
            </div>
            <div>
                <div class="text-2xl font-bold text-red-600">-${summary.deletions || 0}</div>
                <div class="text-xs text-gray-600">Deletions</div>
            </div>
        </div>
        ${summary.fileList && summary.fileList.length > 0 ? `
            <div class="mt-4 pt-4 border-t">
                <div class="text-sm font-medium text-gray-700 mb-2">Modified Files:</div>
                <div class="text-xs text-gray-600 max-h-32 overflow-y-auto">
                    ${summary.fileList.map(file => `• ${file}`).join('<br>')}
                </div>
            </div>
        ` : ''}
    `;
}