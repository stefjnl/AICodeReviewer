// AI Code Reviewer - SignalR UI Functions

import { showElement, hideElement, updateElementContent } from '../core/ui-helpers.js';

// Update SignalR status display
export function updateSignalRStatus(status, isConnected) {
    const statusElement = document.getElementById('signalr-status');
    if (statusElement) {
        statusElement.textContent = status;
        statusElement.className = `text-xs ${isConnected ? 'text-green-600' : 'text-red-600'}`;
    }
}