// AI Code Reviewer - API Client Wrapper
// Handles all HTTP communication with the backend API

import { apiClient } from '../api/api-client.js';
import { apiEndpoints } from '../core/constants.js';

export class ApiClientWrapper {
    /**
     * Starts analysis via API
     * @param {Object} request Analysis request payload
     * @returns {Promise<Object>} API response
     */
    async startAnalysis(request) {
        console.log(`📞 Calling API: POST ${apiEndpoints.executionStart}`, request);
        return await apiClient.post(apiEndpoints.executionStart, request);
    }

    /**
     * Gets analysis results by ID
     * @param {string} analysisId Analysis identifier
     * @returns {Promise<Object>} Analysis results
     */
    async getAnalysisResults(analysisId) {
        const response = await fetch(`/api/results/${analysisId}`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    /**
     * Gets analysis options from API
     * @param {string} repositoryPath Repository path
     * @returns {Promise<Object>} Analysis options
     */
    async getAnalysisOptions(repositoryPath) {
        const response = await apiClient.post(apiEndpoints.analysisOptions, {
            repositoryPath: repositoryPath
        });
        return response;
    }

    /**
     * Gets changes preview from API
     * @param {Object} previewRequest Preview request payload
     * @returns {Promise<Object>} Changes preview
     */
    async getChangesPreview(previewRequest) {
        const response = await apiClient.post(apiEndpoints.analysisPreview, previewRequest);
        return response;
    }
}