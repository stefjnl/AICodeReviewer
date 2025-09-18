// AI Code Reviewer - Language UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';
import { languageState } from './language-state.js';

export function updateLanguageUI(state) {
    const loadingEl = document.getElementById('language-loading');
    const errorEl = document.getElementById('language-error');
    const contentEl = document.getElementById('language-content');
    const selectedEl = document.getElementById('language-selected');
    const detectionResultsEl = document.getElementById('detection-results');
    const detectedLanguagesEl = document.getElementById('detected-languages');
    const fileCountSummaryEl = document.getElementById('file-count-summary');
    const fileCountsEl = document.getElementById('file-counts');
    const selectedLanguageNameEl = document.getElementById('selected-language-name');
    
    if (!loadingEl || !errorEl || !contentEl || !selectedEl) return;
    
    // Reset all states
    loadingEl.classList.add('hidden');
    errorEl.classList.add('hidden');
    contentEl.classList.add('hidden');
    selectedEl.classList.add('hidden');
    if (detectionResultsEl) detectionResultsEl.classList.add('hidden');
    if (fileCountSummaryEl) fileCountSummaryEl.classList.add('hidden');
    
    switch (state) {
        case 'loading':
            loadingEl.classList.remove('hidden');
            break;
        case 'error':
            errorEl.classList.remove('hidden');
            const errorMessageEl = document.getElementById('language-error-message');
            if (errorMessageEl) errorMessageEl.textContent = languageState.error || 'An error occurred';
            contentEl.classList.remove('hidden');
            break;
        case 'selected':
            selectedEl.classList.remove('hidden');
            const selectedLanguage = languageState.supportedLanguages.find(l => l.id === languageState.selectedLanguage);
            if (selectedLanguage && selectedLanguageNameEl) {
                selectedLanguageNameEl.textContent = `${selectedLanguage.icon} ${selectedLanguage.name}`;
            }
            contentEl.classList.remove('hidden');
            break;
        default:
            contentEl.classList.remove('hidden');
            // Show detection results and file counts
            if (languageState.detectedLanguages.length > 0 && detectionResultsEl) {
                detectionResultsEl.classList.remove('hidden');
                if (detectedLanguagesEl) {
                    detectedLanguagesEl.innerHTML = '';
                    languageState.detectedLanguages.forEach(langId => {
                        const lang = languageState.supportedLanguages.find(l => l.id === langId);
                        if (lang) {
                            const badge = document.createElement('span');
                            badge.className = 'inline-flex items-center px-3 py-1 rounded-full text-sm bg-blue-100 text-blue-800';
                            badge.textContent = `${lang.icon} ${lang.name}`;
                            detectedLanguagesEl.appendChild(badge);
                        }
                    });
                }
            }
            
            if (Object.keys(languageState.fileCounts).length > 0 && fileCountSummaryEl) {
                fileCountSummaryEl.classList.remove('hidden');
                if (fileCountsEl) {
                    fileCountsEl.innerHTML = '';
                    Object.entries(languageState.fileCounts).forEach(([langId, count]) => {
                        const lang = languageState.supportedLanguages.find(l => l.id === langId);
                        if (lang) {
                            const div = document.createElement('div');
                            div.className = 'flex justify-between';
                            div.innerHTML = `<span>${lang.icon} ${lang.name}</span><span class="font-medium">${count} files</span>`;
                            fileCountsEl.appendChild(div);
                        }
                    });
                }
            }
            break;
    }
    
    // Update dropdown selection
    const dropdown = document.getElementById('language-select');
    if (dropdown) {
        dropdown.value = languageState.selectedLanguage || '';
    }
}