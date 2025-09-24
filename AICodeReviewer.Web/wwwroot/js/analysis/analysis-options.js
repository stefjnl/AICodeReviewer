// AI Code Reviewer - Analysis Options Functions

import { apiEndpoints } from '../core/constants.js';
import { repositoryState } from '../repository/repository-state.js';
import { analysisState } from './analysis-state.js';
import { updateAnalysisUI } from './analysis-ui.js';
import { markStepCompleted } from '../workflow/workflow-navigation.js';

export async function loadAnalysisOptions() {
    if (!repositoryState.path) {
        analysisState.error = 'No repository path available';
        updateAnalysisUI('error');
        return;
    }

    analysisState.loading = true;
    analysisState.error = null;
    updateAnalysisUI('loading');

    try {
        const response = await fetch(apiEndpoints.analysisOptions, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ repositoryPath: repositoryState.path })
        });

        if (!response.ok) throw new Error('Failed to load analysis options');

        const data = await response.json();
        if (data.success) {
            analysisState.availableOptions = {
                commits: data.commits || [],
                branches: data.branches || [],
                modifiedFiles: data.modifiedFiles || [],
                stagedFiles: data.stagedFiles || []
            };
            
            // Auto-select uncommitted if there are changes
            if (data.modifiedFiles && data.modifiedFiles.length > 0) {
                analysisState.analysisType = 'uncommitted';
            } else if (data.stagedFiles && data.stagedFiles.length > 0) {
                analysisState.analysisType = 'staged';
            }

            updateAnalysisUI('loaded');
            
            // Auto-preview default selection
            if (analysisState.analysisType) {
                await previewChanges();
            }
        } else {
            analysisState.error = data.error || 'Failed to load options';
            updateAnalysisUI('error');
        }
    } catch (error) {
        console.error('Error loading analysis options:', error);
        analysisState.error = error.message;
        updateAnalysisUI('error');
    } finally {
        analysisState.loading = false;
    }
}

export async function previewChanges() {
    if (!repositoryState.path || !analysisState.analysisType) {
        analysisState.changesSummary = null;
        updateAnalysisUI('loaded');
        return;
    }

    try {
        const requestBody = {
            repositoryPath: repositoryState.path,
            analysisType: analysisState.analysisType,
            targetCommit: analysisState.selectedCommit
        };

        // Add branch parameters for pull request analysis
        if (analysisState.analysisType === 'pullrequest') {
            requestBody.sourceBranch = analysisState.selectedSourceBranch;
            requestBody.targetBranch = analysisState.selectedTargetBranch;
        }

        const response = await fetch(apiEndpoints.analysisPreview, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) throw new Error('Failed to preview changes');

        const data = await response.json();
        if (data.success) {
            analysisState.changesSummary = data.changesSummary;
            updateAnalysisUI('loaded');
            
            // Enable Step 4 completion
            markStepCompleted(4);
        } else {
            analysisState.error = data.error || 'Failed to preview changes';
            updateAnalysisUI('error');
        }
    } catch (error) {
        console.error('Error previewing changes:', error);
        analysisState.error = error.message;
        updateAnalysisUI('error');
    }
}

export function selectAnalysisType(type) {
    const validTypes = ['uncommitted', 'staged', 'commit', 'singlefile', 'pullrequest'];
    if (!validTypes.includes(type)) return;

    analysisState.analysisType = type;
    analysisState.selectedCommit = null; // Reset commit selection
    analysisState.selectedSourceBranch = null; // Reset branch selections
    analysisState.selectedTargetBranch = null;
    analysisState.selectedFilePath = null; // Reset file selection
    analysisState.selectedFileContent = null;

    // Update UI to show relevant options
    updateAnalysisUI('loaded');

    // Auto-preview changes
    previewChanges();
}

export function selectCommit(commitId) {
    if (!commitId) return;
    
    analysisState.selectedCommit = commitId;
    updateAnalysisUI('loaded');
    
    // Auto-preview for commit analysis
    if (analysisState.analysisType === 'commit') {
        previewChanges();
    }
}

export function selectSourceBranch(branchName) {
    if (!branchName) return;
    
    analysisState.selectedSourceBranch = branchName;
    updateAnalysisUI('loaded');
    
    // Auto-preview for branch analysis
    if (analysisState.analysisType === 'pullrequest') {
        previewChanges();
    }
}

export function selectTargetBranch(branchName) {
    if (!branchName) return;

    analysisState.selectedTargetBranch = branchName;
    updateAnalysisUI('loaded');

    // Auto-preview for branch analysis
    if (analysisState.analysisType === 'pullrequest') {
        previewChanges();
    }
}

export function selectFile(filePath) {
    if (!filePath) return;

    analysisState.selectedFilePath = filePath;
    updateAnalysisUI('loaded');

    // Auto-preview for single file analysis
    if (analysisState.analysisType === 'singlefile') {
        previewChanges();
    }
}

export function clearFileSelection() {
    analysisState.selectedFilePath = null;
    analysisState.selectedFileContent = null;
    updateAnalysisUI('loaded');
}