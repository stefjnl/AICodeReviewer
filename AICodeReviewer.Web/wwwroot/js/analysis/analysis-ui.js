// AI Code Reviewer - Analysis UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';
import { analysisState } from './analysis-state.js';
import { selectSourceBranch, selectTargetBranch } from './analysis-options.js';

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
    
    // Also reset branch selection
    const branchSelectionEl = document.getElementById('branch-selection');
    if (branchSelectionEl) branchSelectionEl.classList.add('hidden');

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
            
            // Get branch selection element
            const branchSelectionEl = document.getElementById('branch-selection');
            
            // Show branch selection if needed for pull request analysis
            if (analysisState.analysisType === 'pullrequest' && branchSelectionEl) {
                branchSelectionEl.classList.remove('hidden');
                populateBranchDropdowns();
            }

            // Show file selection if needed for single file analysis
            if (analysisState.analysisType === 'singlefile') {
                const fileSelectionEl = document.getElementById('file-selection');
                if (fileSelectionEl) {
                    fileSelectionEl.classList.remove('hidden');
                    populateFileSelection();
                }
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

// Branch dropdowns population for pull request analysis
function populateBranchDropdowns() {
    const sourceBranchDropdown = document.getElementById('source-branch-dropdown');
    const targetBranchDropdown = document.getElementById('target-branch-dropdown');

    if (sourceBranchDropdown && analysisState.availableOptions.branches.length > 0) {
        sourceBranchDropdown.innerHTML = '<option value="">Choose source branch...</option>';
        analysisState.availableOptions.branches.forEach(branch => {
            const option = document.createElement('option');
            option.value = branch.name; // Extract the branch name from the object
            option.textContent = branch.name;
            // Highlight the current branch if it's the current one
            if (branch.isCurrent) {
                option.textContent += ' (current)';
            }
            sourceBranchDropdown.appendChild(option);
        });
    } else if (sourceBranchDropdown) {
        sourceBranchDropdown.innerHTML = '<option value="">No branches available</option>';
    }

    if (targetBranchDropdown && analysisState.availableOptions.branches.length > 0) {
        targetBranchDropdown.innerHTML = '<option value="">Choose target branch...</option>';
        analysisState.availableOptions.branches.forEach(branch => {
            const option = document.createElement('option');
            option.value = branch.name; // Extract the branch name from the object
            option.textContent = branch.name;
            // Highlight the current branch if it's the current one
            if (branch.isCurrent) {
                option.textContent += ' (current)';
            }
            targetBranchDropdown.appendChild(option);
        });
    } else if (targetBranchDropdown) {
        targetBranchDropdown.innerHTML = '<option value="">No branches available</option>';
    }
}

// File selection population
function populateFileSelection() {
    const filePathInput = document.getElementById('selected-file-path');
    const filePreview = document.getElementById('file-content-preview');
    const fileContentText = document.getElementById('file-content-text');

    if (filePathInput) {
        if (analysisState.selectedFilePath) {
            filePathInput.value = analysisState.selectedFilePath;

            // Show file content preview if available
            if (analysisState.selectedFileContent && filePreview && fileContentText) {
                fileContentText.textContent = analysisState.selectedFileContent;
                filePreview.classList.remove('hidden');
            } else {
                filePreview.classList.add('hidden');
            }

            // Update validation icons
            const validIcon = document.getElementById('file-valid-icon');
            const invalidIcon = document.getElementById('file-invalid-icon');

            if (validIcon && invalidIcon) {
                validIcon.classList.remove('hidden');
                invalidIcon.classList.add('hidden');
            }
        } else {
            filePathInput.value = '';
            filePreview.classList.add('hidden');

            // Hide validation icons
            const validIcon = document.getElementById('file-valid-icon');
            const invalidIcon = document.getElementById('file-invalid-icon');

            if (validIcon && invalidIcon) {
                validIcon.classList.add('hidden');
                invalidIcon.classList.add('hidden');
            }
        }
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