// AI Code Reviewer - Analysis State Management

export const analysisState = {
    analysisType: null,
    availableOptions: {
        commits: [],
        branches: [],
        modifiedFiles: [],
        stagedFiles: []
    },
    selectedCommit: null,
    selectedSourceBranch: null,
    selectedTargetBranch: null,
    changesSummary: null,
    loading: false,
    error: null
};

export function initializeAnalysisState() {
    console.log('🔧 Initializing analysis state');
    // Reset analysis state
    analysisState.analysisType = null;
    analysisState.selectedCommit = null;
    analysisState.selectedSourceBranch = null;
    analysisState.selectedTargetBranch = null;
    analysisState.changesSummary = null;
    analysisState.availableOptions = {
        commits: [],
        branches: [],
        modifiedFiles: [],
        stagedFiles: []
    };
}