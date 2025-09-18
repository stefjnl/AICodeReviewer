// AI Code Reviewer - Language Detection Functions

import { apiEndpoints } from '../core/constants.js';
import { languageState } from './language-state.js';
import { updateLanguageUI } from './language-ui.js';

export async function loadSupportedLanguages() {
    try {
        const response = await fetch(apiEndpoints.languageSupported);
        if (!response.ok) throw new Error('Failed to load languages');
        
        const data = await response.json();
        languageState.supportedLanguages = data.languages || [];
        populateLanguageDropdown();
        return true;
    } catch (error) {
        console.error('Error loading languages:', error);
        languageState.error = error.message;
        return false;
    }
}

export async function detectRepositoryLanguage(repositoryPath) {
    if (!repositoryPath) return;
    
    languageState.loading = true;
    languageState.error = null;
    updateLanguageUI('loading');
    
    try {
        const response = await fetch(apiEndpoints.languageDetect, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ repositoryPath })
        });
        
        if (!response.ok) throw new Error('Language detection failed');
        
        const data = await response.json();
        languageState.detectedLanguages = data.detectedLanguages || [];
        languageState.fileCounts = data.fileCounts || {};
        languageState.selectedLanguage = data.primaryLanguage || 'multi';
        
        updateLanguageUI('loaded');
        
        // Auto-select detected language
        if (languageState.selectedLanguage) {
            selectLanguage(languageState.selectedLanguage);
        }
        
        return true;
    } catch (error) {
        console.error('Error detecting language:', error);
        languageState.error = error.message;
        languageState.selectedLanguage = 'multi';
        updateLanguageUI('error');
        return false;
    } finally {
        languageState.loading = false;
    }
}

export function selectLanguage(languageId) {
    const language = languageState.supportedLanguages.find(l => l.id === languageId);
    if (!language) return;
    
    languageState.selectedLanguage = languageId;
    updateLanguageUI('selected');
    
    // Mark Step 3 as completed and enable Step 4
    // We'll handle this in the workflow navigation module
    console.log(`Language selected: ${language.name}`);
}

function populateLanguageDropdown() {
    const dropdown = document.getElementById('language-select');
    if (!dropdown) return;
    
    dropdown.innerHTML = '<option value="">Select a language...</option>';
    
    languageState.supportedLanguages.forEach(language => {
        const option = document.createElement('option');
        option.value = language.id;
        option.textContent = `${language.icon} ${language.name}`;
        option.selected = language.id === languageState.selectedLanguage;
        dropdown.appendChild(option);
    });
    
    // Add change event listener
    dropdown.addEventListener('change', (e) => {
        if (e.target.value) {
            selectLanguage(e.target.value);
        }
    });
}