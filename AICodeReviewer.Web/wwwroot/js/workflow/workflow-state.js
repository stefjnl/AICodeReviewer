// AI Code Reviewer - Workflow State Management

// Import required state modules for step completion criteria
import { documentManager } from '../documents/document-manager.js';
import { repositoryState } from '../repository/repository-state.js';
import { languageState } from '../language/language-state.js';
import { analysisState } from '../analysis/analysis-state.js';
import { modelState } from '../models/model-state.js';

export const workflowState = {
    currentStep: 1,
    completedSteps: [],
    steps: {
        1: { name: 'documents', completed: false, required: false },
        2: { name: 'repository', completed: false, required: true },
        3: { name: 'language', completed: false, required: true },
        4: { name: 'analysis', completed: false, required: true },
        5: { name: 'results', completed: false, required: true }
    }
};

// Step completion criteria
export const stepCompletionCriteria = {
    1: function() {
        // Step 1: Documents - completed when documents are loaded
        return documentManager.documents.length > 0;
    },
    2: function() {
        // Step 2: Repository - completed when repository is validated
        return repositoryState.isValid === true;
    },
    3: function() {
        // Step 3: Language - completed when language is selected
        return languageState.selectedLanguage !== null;
    },
    4: function() {
        // Step 4: Analysis - completed when analysis type is selected
        return analysisState.analysisType !== null;
    },
    5: function() {
        // Step 5: Model Selection - completed when model is selected
        return modelState.selectedModel !== null;
    }
};