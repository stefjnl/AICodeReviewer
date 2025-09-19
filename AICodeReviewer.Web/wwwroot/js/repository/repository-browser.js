// AI Code Reviewer - Repository Browser Functions
// Handles server-side directory browsing for cross-browser compatibility

import { showValidationError, updateRepositoryUI } from './repository-ui.js';

let currentPath = '';
let modalOpen = false;

/**
 * Opens the directory browser modal
 * @returns {Promise<void>}
 */
export async function browseForRepository() {
    console.log('Opening server-side directory browser');
    openDirectoryBrowser();
}

/**
 * Initializes the repository browser functionality
 */
export function initializeRepositoryBrowser() {
    const browseBtn = document.getElementById('browse-repository-btn');

    if (browseBtn) {
        console.log('Initializing repository browser (server-side)');
        
        browseBtn.addEventListener('click', browseForRepository);
        browseBtn.title = 'Browse for repository directory using server-side file browser';
        
        // Setup modal event handlers
        setupModalHandlers();
        
        console.log('Repository browser initialized');
    }
}

function setupModalHandlers() {
    // Close modal button
    const closeBtn = document.getElementById('close-directory-modal');
    if (closeBtn) {
        closeBtn.addEventListener('click', closeDirectoryBrowser);
    }
    
    // Select directory button
    const selectBtn = document.getElementById('select-directory');
    if (selectBtn) {
        selectBtn.addEventListener('click', selectCurrentDirectory);
    }
    
    // Up level button
    const upLevelBtn = document.getElementById('directory-up-level');
    if (upLevelBtn) {
        upLevelBtn.addEventListener('click', navigateUpLevel);
    }
}

function openDirectoryBrowser() {
    const modal = document.getElementById('directory-browser-modal');
    if (modal) {
        modal.classList.remove('hidden');
        modalOpen = true;
        loadDirectory('');
    }
}

function closeDirectoryBrowser() {
    const modal = document.getElementById('directory-browser-modal');
    if (modal) {
        modal.classList.add('hidden');
        modalOpen = false;
    }
}

async function loadDirectory(path) {
    try {
        const response = await fetch(`/api/directorybrowser/browse?path=${encodeURIComponent(path)}`);
        if (!response.ok) {
            throw new Error(`Failed to load directory: ${response.statusText}`);
        }
        
        const data = await response.json();
        currentPath = data.currentPath;
        
        // Update current path display
        const pathDisplay = document.getElementById('current-path-display');
        if (pathDisplay) {
            pathDisplay.textContent = currentPath;
        }
        
        // Render directory list
        renderDirectoryList(data.directories);
        
    } catch (error) {
        console.error('Error loading directory:', error);
        showValidationError(`Failed to load directory: ${error.message}`);
    }
}

function renderDirectoryList(directories) {
    const container = document.getElementById('directory-list');
    if (!container) return;
    
    container.innerHTML = '';
    
    directories.forEach(dir => {
        const item = document.createElement('div');
        item.className = 'directory-item p-2 hover:bg-gray-100 cursor-pointer border-b border-gray-100';
        item.innerHTML = `
            <div class="flex items-center">
                <svg class="w-5 h-5 mr-2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                          d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2H5a2 2 0 00-2-2z"></path>
                </svg>
                <span class="flex-1">${dir.name}</span>
                ${dir.isGitRepository ? '<span class="ml-2 px-2 py-1 text-xs bg-green-100 text-green-800 rounded">Git</span>' : ''}
            </div>
        `;
        
        item.addEventListener('dblclick', () => loadDirectory(dir.fullPath));
        container.appendChild(item);
    });
}

function navigateUpLevel() {
    if (currentPath) {
        // Get parent directory by going up one level
        const lastSeparator = currentPath.lastIndexOf('\\');
        if (lastSeparator > 0) {
            const parentPath = currentPath.substring(0, lastSeparator);
            loadDirectory(parentPath);
        } else if (currentPath.length > 2 && currentPath[1] === ':') {
            // Handle drive roots on Windows (e.g., C:\ -> show drives)
            loadDirectory('');
        }
    }
}

function selectCurrentDirectory() {
    const pathInput = document.getElementById('repository-path');
    if (pathInput && currentPath) {
        pathInput.value = currentPath;
        
        // Clear any existing validation states
        updateRepositoryUI('clear');
        
        // Show immediate feedback for git repository
        const gitRepoIndicator = document.querySelector('.directory-item:has(.bg-green-100)');
        if (gitRepoIndicator) {
            showGitRepositoryDetected(currentPath);
        }
    }
    closeDirectoryBrowser();
}

/**
 * Shows immediate feedback when a Git repository is detected
 * @param {string} directoryPath - The path of the selected directory
 */
function showGitRepositoryDetected(directoryPath) {
    // Clear any existing validation states
    updateRepositoryUI('clear');

    // Show success message for browser detection
    const successEl = document.getElementById('validation-success');
    const successMessageEl = document.getElementById('validation-success-message');
    const infoEl = document.getElementById('repository-info');

    if (successEl && successMessageEl) {
        const directoryName = directoryPath.split('\\').pop() || directoryPath;
        successMessageEl.textContent = `Git repository detected in "${directoryName}"`;
        successEl.classList.remove('hidden'); // Show the success element
        // Hide the info element since we don't have detailed info yet
        if (infoEl) infoEl.classList.add('hidden');
    }

    // Update visual indicators
    const pathInput = document.getElementById('repository-path');
    const validIcon = document.getElementById('path-valid-icon');
    const invalidIcon = document.getElementById('path-invalid-icon');

    if (pathInput) {
        pathInput.classList.remove('border-red-500', 'focus:border-red-500');
        pathInput.classList.add('border-green-500', 'focus:border-green-500');
    }
    if (validIcon) validIcon.classList.remove('hidden');
    if (invalidIcon) invalidIcon.classList.add('hidden');
}