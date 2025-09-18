// AI Code Reviewer - Document UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';
import { loadDocumentContent } from './document-api.js';
import { documentManager } from './document-manager.js';

export function clearError() {
    // This would need access to documentManager state
    hideElement('error-container');
}

export function clearSelection() {
    // This would need access to documentManager state
    hideElement('document-viewer');
}

export function showDocumentContent(content, title) {
    updateElementContent('selected-document-name', title);
    updateElementContent('document-content', content);
    showElement('document-viewer');
}

export function updateDocumentList(documents) {
    const documentGrid = document.getElementById('document-grid');
    const documentCount = document.getElementById('document-count');
    
    if (documentGrid && documentCount) {
        documentCount.textContent = documents.length;
        
        documentGrid.innerHTML = '';
        
        documents.forEach(doc => {
            const documentDiv = window.document.createElement('div');
            documentDiv.className = 'bg-gray-50 rounded-md p-3 hover:bg-gray-100 transition-colors';
            documentDiv.innerHTML = `
                <div class="flex items-center justify-between">
                    <div class="flex items-center">
                        <input
                            type="checkbox"
                            class="document-checkbox h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded mr-3"
                            data-document="${doc}"
                        >
                        <div>
                            <h5 class="text-sm font-medium text-gray-900">${doc}</h5>
                            <p class="text-xs text-gray-500">Markdown document</p>
                        </div>
                    </div>
                    <button
                        class="text-xs text-primary hover:text-primary/80 font-medium view-document-btn"
                        data-document="${doc}"
                    >
                        View
                    </button>
                </div>
            `;
            documentGrid.appendChild(documentDiv);
        });
        
        // Add event listeners to new view buttons
        document.querySelectorAll('.view-document-btn').forEach(button => {
            button.addEventListener('click', (e) => {
                const documentName = e.target.dataset.document;
                loadDocumentContent(documentName);
            });
        });
        
        // Add event listeners to checkboxes
        document.querySelectorAll('.document-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                const documentName = e.target.dataset.document;
                toggleDocumentSelection(documentName, e.target.checked);
            });
        });
    }
}

function toggleDocumentSelection(documentName, isSelected) {
    if (isSelected) {
        if (!documentManager.selectedDocuments.includes(documentName)) {
            documentManager.selectedDocuments.push(documentName);
        }
    } else {
        documentManager.selectedDocuments = documentManager.selectedDocuments.filter(
            doc => doc !== documentName
        );
    }
    console.log('Selected documents:', documentManager.selectedDocuments);
}

export function showError(message) {
    updateElementContent('error-message', message);
    showElement('error-container');
}