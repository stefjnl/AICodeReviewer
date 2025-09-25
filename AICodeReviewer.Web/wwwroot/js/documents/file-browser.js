// AI Code Reviewer - File Browser Functions
// Handles server-side file browsing for single file analysis

import { showValidationError } from '../repository/repository-ui.js';

let currentPath = '';
let currentRepositoryPath = '';
let modalOpen = false;

/**
 * Opens the file browser modal
 * @param {string} repositoryPath - The repository path to browse within
 * @returns {Promise<void>}
 */
export async function browseForFile(repositoryPath) {
    console.log('Opening server-side file browser for repository:', repositoryPath);
    currentRepositoryPath = repositoryPath;
    openFileBrowser();
}

/**
 * Initializes the file browser functionality
 */
export function initializeFileBrowser() {
    console.log('🔍 DEBUG: initializeFileBrowser() called');

    const browseBtn = document.getElementById('browse-file-btn');
    console.log('🔍 DEBUG: browse-file-btn element:', browseBtn);

    if (browseBtn) {
        console.log('🔍 DEBUG: Browse file button found, attaching listener');

        const clickHandler = () => {
            console.log('🔍 DEBUG: browse-file-btn clicked!');
            // Get the repository path from the repository input
            const repositoryPath = document.getElementById('repository-path')?.value;
            if (!repositoryPath) {
                showValidationError('Please select a repository first');
                return;
            }
            browseForFile(repositoryPath);
        };

        browseBtn.addEventListener('click', clickHandler);
        browseBtn.title = 'Browse for file within repository using server-side file browser';

        console.log('🔍 DEBUG: Click listener attached successfully');

        // Setup modal event handlers
        setupModalHandlers();

        console.log('🔍 DEBUG: File browser initialization complete');
    } else {
        console.error('❌ ERROR: browse-file-btn not found in DOM');
        console.error('❌ ERROR: Available buttons:', document.querySelectorAll('button'));
        console.error('❌ ERROR: Step 4 content visibility:', document.getElementById('step-4-content')?.style.display);
    }
}

function setupModalHandlers() {
    console.log('🔍 DEBUG: Setting up file browser modal handlers...');

    // Close modal button
    const closeBtn = document.getElementById('close-file-modal');
    console.log('🔍 DEBUG: close-file-modal:', closeBtn);
    if (closeBtn) {
        closeBtn.addEventListener('click', closeFileBrowser);
        console.log('🔍 DEBUG: Close file modal listener attached');
    } else {
        console.error('❌ ERROR: close-file-modal not found');
    }

    // Select file button
    const selectBtn = document.getElementById('select-file');
    console.log('🔍 DEBUG: select-file:', selectBtn);
    if (selectBtn) {
        selectBtn.addEventListener('click', selectCurrentFile);
        console.log('🔍 DEBUG: Select file listener attached');
    } else {
        console.error('❌ ERROR: select-file not found');
    }

    // Up level button
    const upLevelBtn = document.getElementById('file-up-level');
    console.log('🔍 DEBUG: file-up-level:', upLevelBtn);
    if (upLevelBtn) {
        upLevelBtn.addEventListener('click', navigateUpLevel);
        console.log('🔍 DEBUG: Up level listener attached');
    } else {
        console.error('❌ ERROR: file-up-level not found');
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

        modalOpen = true;
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
        }, 300); // Match the duration of the transition (300ms)

        modalOpen = false;
    }
}

async function loadFiles(path) {
    try {
        const response = await fetch(`/api/filebrowser/browse?repositoryPath=${encodeURIComponent(currentRepositoryPath)}&path=${encodeURIComponent(path)}`);
        if (!response.ok) {
            throw new Error(`Failed to load files: ${response.statusText}`);
        }

        const data = await response.json();
        currentPath = data.currentPath;

        // Update current path display
        const pathDisplay = document.getElementById('file-current-path-display');
        if (pathDisplay) {
            pathDisplay.textContent = currentPath || 'Repository Root';
        }

        // Render file list
        renderFileList(data.files);

    } catch (error) {
        console.error('Error loading files:', error);
        showValidationError(`Failed to load files: ${error.message}`);
    }
}

function renderFileList(files) {
    const container = document.getElementById('file-list');
    if (!container) return;

    container.innerHTML = '';

    files.forEach(file => {
        const item = document.createElement('div');
        item.className = 'file-item p-2 hover:bg-gray-100 cursor-pointer border-b border-gray-100';

        if (file.isDirectory) {
            // Directory item
            item.innerHTML = `
                <div class="flex items-center">
                    <svg class="w-5 h-5 mr-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2H5a2 2 0 00-2-2z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M8 5a2 2 0 012-2h4a2 2 0 012 2v2H8V5z"></path>
                    </svg>
                    <span class="flex-1 text-blue-600 font-medium">${file.name}</span>
                    <span class="text-xs text-gray-400">Directory</span>
                </div>
            `;
            item.addEventListener('dblclick', () => loadFiles(file.fullPath));
        } else {
            // File item
            const fileExtension = file.name.split('.').pop()?.toLowerCase() || '';
            const isCodeFile = ['cs', 'js', 'ts', 'py', 'java', 'cpp', 'c', 'h', 'hpp', 'css', 'html', 'xml', 'json', 'md', 'txt'].includes(fileExtension);

            if (isCodeFile) {
                item.innerHTML = `
                    <div class="flex items-center">
                        <svg class="w-5 h-5 mr-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                        </svg>
                        <span class="flex-1 text-gray-900">${file.name}</span>
                        <span class="text-xs text-gray-500">${formatFileSize(file.size)}</span>
                    </div>
                `;
                item.addEventListener('dblclick', () => selectFile(file.fullPath, file.name));
            }
        }

        container.appendChild(item);
    });
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

function navigateUpLevel() {
    if (currentPath) {
        // Get parent directory by going up one level
        const lastSeparator = currentPath.lastIndexOf('\\');
        if (lastSeparator > 0) {
            const parentPath = currentPath.substring(0, lastSeparator);
            loadFiles(parentPath);
        } else if (currentPath.length > 2 && currentPath[1] === ':') {
            // Handle drive roots on Windows (e.g., C:\ -> show drives)
            loadFiles('');
        }
    }
}

function selectCurrentFile() {
    // Get the selected file from the UI
    const selectedItem = document.querySelector('.file-item.bg-blue-100');
    if (selectedItem) {
        const fileName = selectedItem.querySelector('span').textContent;
        const fullPath = selectedItem.dataset.fullPath;
        selectFile(fullPath, fileName);
    }
}

function selectFile(filePath, fileName) {
    const filePathInput = document.getElementById('selected-file-path');
    if (filePathInput) {
        filePathInput.value = filePath;

        // Clear any existing validation states
        const validIcon = document.getElementById('file-valid-icon');
        const invalidIcon = document.getElementById('file-invalid-icon');

        if (validIcon) validIcon.classList.remove('hidden');
        if (invalidIcon) invalidIcon.classList.add('hidden');

        // Load file content preview
        loadFilePreview(filePath, fileName);
    }
    closeFileBrowser();
}

async function loadFilePreview(filePath, fileName) {
    try {
        const response = await fetch('/api/filebrowser/filecontent', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                repositoryPath: currentRepositoryPath,
                filePath: filePath
            })
        });

        if (!response.ok) {
            throw new Error(`Failed to load file content: ${response.statusText}`);
        }

        const data = await response.json();

        if (data.success) {
            // Show file content preview
            const previewContainer = document.getElementById('file-content-preview');
            const previewText = document.getElementById('file-content-text');

            if (previewContainer && previewText) {
                previewText.textContent = data.content;
                previewContainer.classList.remove('hidden');
            }
        }
    } catch (error) {
        console.error('Error loading file preview:', error);
        // Don't show error for preview failure, just hide the preview
        const previewContainer = document.getElementById('file-content-preview');
        if (previewContainer) {
            previewContainer.classList.add('hidden');
        }
    }
}