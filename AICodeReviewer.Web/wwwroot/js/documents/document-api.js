// AI Code Reviewer - Document API Functions

import { apiEndpoints } from '../core/constants.js';
import { apiClient } from '../api/api-client.js';
import { documentManager } from './document-manager.js';
import { showElement, hideElement, updateElementContent, setButtonState } from '../core/ui-helpers.js';
import { updateDocumentList, showError, showDocumentContent } from './document-ui.js';
import { markStepCompleted } from '../workflow/workflow-navigation.js';

export async function loadDocuments() {
    try {
        documentManager.loading = true;
        documentManager.error = null;
        
        console.log('🔄 Loading documents...');
        
        // Update UI state
        showElement('loading-spinner');
        hideElement('error-container');
        hideElement('document-list-container');
        hideElement('empty-state');
        updateElementContent('loading-text', 'Loading...');
        setButtonState('load-documents-btn', true);
        
        const response = await fetch(apiEndpoints.documentsScan);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log('✅ Documents fetched successfully:', data);
        
        if (data.success) {
            documentManager.documents = data.documents;
            console.log(`✅ Loaded ${documentManager.documents.length} documents`);
            
            // Update UI
            updateDocumentList(documentManager.documents);
            
            if (documentManager.documents.length > 0) {
                showElement('document-list-container');
                // Mark step as completed
                markStepCompleted(1);
            } else {
                showElement('empty-state');
            }
        } else {
            documentManager.error = data.error || 'Failed to load documents';
            console.error('❌ Error loading documents:', documentManager.error);
            showError(documentManager.error);
        }
        
    } catch (error) {
        documentManager.error = error.message || 'An unexpected error occurred';
        console.error('❌ API call failed:', error);
        showError(documentManager.error);
    } finally {
        documentManager.loading = false;
        hideElement('loading-spinner');
        updateElementContent('loading-text', 'Load Documents');
        setButtonState('load-documents-btn', false);
    }
}

export async function loadDocumentContent(documentName) {
    try {
        documentManager.loading = true;
        documentManager.error = null;
        
        console.log(`🔄 Loading content for: ${documentName}`);
        
        // Update UI state
        showElement('loading-state');
        setButtonState('load-documents-btn', true);
        
        const response = await fetch(`${apiEndpoints.documentsContent}/${documentName}`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        console.log(`✅ Loaded content for ${documentName}`);
        
        if (data.success) {
            documentManager.selectedDocument = documentName;
            documentManager.documentContent = data.content;
            
            // Update UI
            showDocumentContent(data.content, documentName);
        } else {
            documentManager.error = data.error || 'Failed to load document content';
            console.error('❌ Error loading document content:', documentManager.error);
            showError(documentManager.error);
        }
        
    } catch (error) {
        documentManager.error = error.message || 'An unexpected error occurred';
        console.error('❌ API call failed:', error);
        showError(documentManager.error);
    } finally {
        documentManager.loading = false;
        hideElement('loading-state');
        setButtonState('load-documents-btn', false);
    }
}

export const documentApi = {
    async fetchDocuments() {
        try {
            console.log('📁 Fetching documents from API...');
            const response = await fetch(apiEndpoints.documentsScan);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            console.log('✅ Documents fetched successfully:', data);
            return data;
            
        } catch (error) {
            console.error('❌ Error fetching documents:', error);
            throw error;
        }
    },
    
    async fetchDocumentContent(documentName) {
        try {
            console.log(`📄 Fetching document content: ${documentName}`);
            const response = await fetch(`${apiEndpoints.documentsContent}/${documentName}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            console.log('✅ Document content fetched successfully:', data);
            return data;
            
        } catch (error) {
            console.error('❌ Error fetching document content:', error);
            throw error;
        }
    }
};