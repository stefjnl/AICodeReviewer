// AI Code Reviewer - File Browser Functions
// Handles server-side file browsing within repository for single file analysis

import { repositoryState } from '../repository/repository-state.js';
import { selectFile, clearFileSelection } from './analysis-options.js';
import { updateAnalysisUI } from './analysis-ui.js';

let currentFilePath = '';
let fileBrowserOpen = false;

/**
 * Opens the file browser modal for selecting files within the repository
 * @returns {Promise<void>}
 */
export async function browseForFile() {
    console.log('Opening file browser for repository:', repositoryState.path);

    if (!repositoryState.path) {
        alert('Please select a repository first');
        return;
    }

    openFileBrowser();
}

/**
 * Initializes the file browser functionality
 */
export function initializeFileBrowser() {
    console.log('🔍 Initializing file browser');

    const browseBtn = document.getElementById('browse-file-btn');
    if (browseBtn) {
        browseBtn.addEventListener('click', browseForFile);
        console.log('🔍 File browse button listener attached');
    }

    // Setup modal event handlers
    setupFileModalHandlers();
}

function setupFileModalHandlers() {
    // Close modal button
    const closeBtn = document.getElementById('close-file-modal');
    if (closeBtn) {
        closeBtn.addEventListener('click', closeFileBrowser);
    }

    // Select file button
    const selectBtn = document.getElementById('select-file');
    if (selectBtn) {
        selectBtn.addEventListener('click', selectCurrentFile);
    }

    // Up level button
    const upLevelBtn = document.getElementById('file-up-level');
    if (upLevelBtn) {
        upLevelBtn.addEventListener('click', navigateUpLevel);
    }
}

function openFileBrowser() {
    const modal = document.getElementById('file-browser-modal');
    const modalContent = document.getElementById('file-modal-content-wrapper');

    if (modal && modalContent) {
        // Remove hidden class from modal
        modal.classList.remove('hidden');

        // Add animation classes to show modal with smooth transition
        setTimeout(() => {
            modalContent.classList.remove('scale-95', 'opacity-0');
            modalContent.classList.add('scale-100', 'opacity-100');
        }, 10);

        fileBrowserOpen = true;
        loadFiles('');
    }
}

function closeFileBrowser() {
    const modal = document.getElementById('file-browser-modal');
    const modalContent = document.getElementById('file-modal-content-wrapper');

    if (modal && modalContent) {
        // Remove animation classes to hide modal
        modalContent.classList.remove('scale-100', 'opacity-100');
        modalContent.classList.add('scale-95', 'opacity-0');

        // Add hidden class after animation completes
        setTimeout(() => {
            modal.classList.add('hidden');
        }, 300);

        fileBrowserOpen = false;
    }
}

async function loadFiles(path) {
    try {
        const response = await fetch(`/api/filebrowser/browse?repositoryPath=${encodeURIComponent(repositoryState.path)}&path=${encodeURIComponent(path)}`);
        if (!response.ok) {
            throw new Error(`Failed to load files: ${response.statusText}`);
        }

        const data = await response.json();
        currentFilePath = data.currentPath;

        // Update current path display
        const pathDisplay = document.getElementById('file-current-path-display');
        if (pathDisplay) {
            pathDisplay.textContent = currentFilePath || 'Repository Root';
        }

        // Render file list
        renderFileList(data.files);

    } catch (error) {
        console.error('Error loading files:', error);
        alert(`Failed to load files: ${error.message}`);
    }
}

function renderFileList(files) {
    const container = document.getElementById('file-list');
    if (!container) return;

    container.innerHTML = '';

    // Filter to show only programming files
    const programmingExtensions = ['.cs', '.js', '.ts', '.py', '.java', '.cpp', '.c', '.h', '.hpp', '.css', '.html', '.xml', '.json', '.md', '.txt'];
    const programmingFiles = files.filter(file =>
        !file.isDirectory && programmingExtensions.some(ext => file.name.toLowerCase().endsWith(ext))
    );

    if (programmingFiles.length === 0) {
        container.innerHTML = '<div class="text-center py-8 text-gray-500">No programming files found in this directory</div>';
        return;
    }

    programmingFiles.forEach(file => {
        const item = document.createElement('div');
        item.className = 'file-item p-3 hover:bg-gray-100 cursor-pointer border-b border-gray-100 transition-colors';

        // Determine file type icon based on extension
        const extension = file.name.split('.').pop()?.toLowerCase();
        let iconClass = 'text-gray-400';

        switch (extension) {
            case 'cs':
                iconClass = 'text-purple-500';
                break;
            case 'js':
            case 'ts':
                iconClass = 'text-yellow-500';
                break;
            case 'py':
                iconClass = 'text-blue-500';
                break;
            case 'java':
                iconClass = 'text-red-500';
                break;
            case 'html':
            case 'css':
                iconClass = 'text-orange-500';
                break;
            default:
                iconClass = 'text-gray-400';
        }

        item.innerHTML = `
            <div class="flex items-center">
                <svg class="w-5 h-5 mr-3 ${iconClass}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                </svg>
                <div class="flex-1">
                    <div class="font-medium text-gray-900">${file.name}</div>
                    <div class="text-xs text-gray-500">${file.size} bytes</div>
                </div>
            </div>
        `;

        item.addEventListener('dblclick', () => selectFileFromList(file));
        container.appendChild(item);
    });
}

function selectFileFromList(file) {
    // Load file content first
    loadFileContent(file.fullPath, file.name);
}

async function loadFileContent(filePath, fileName) {
    try {
        const response = await fetch('/api/filebrowser/filecontent', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                repositoryPath: repositoryState.path,
                filePath: filePath
            })
        });

        if (!response.ok) {
            throw new Error(`Failed to load file content: ${response.statusText}`);
        }

        const data = await response.json();
        if (data.success) {
            // Store file content in analysis state
            analysisState.selectedFileContent = data.content;

            // Select the file
            selectFile(filePath);

            // Close the modal
            closeFileBrowser();
        } else {
            throw new Error(data.error || 'Failed to load file content');
        }
    } catch (error) {
        console.error('Error loading file content:', error);
        alert(`Failed to load file content: ${error.message}`);
    }
}

function navigateUpLevel() {
    if (currentFilePath) {
        // Get parent directory by going up one level
        const lastSeparator = currentFilePath.lastIndexOf('/');
        if (lastSeparator > 0) {
            const parentPath = currentFilePath.substring(0, lastSeparator);
            loadFiles(parentPath);
        } else {
            // At root level
            loadFiles('');
        }
    }
}

function selectCurrentFile() {
    // This function is called when user clicks "Select This File" button
    // The file should already be selected via double-click
    if (analysisState.selectedFilePath) {
        closeFileBrowser();
    } else {
        alert('Please select a file first by double-clicking on it');
    }
}

// Export the analysisState import for the populateFileSelection function
import { analysisState } from './analysis-state.js';