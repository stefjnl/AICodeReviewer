// AI Code Reviewer - Git Clone API

import { apiClient } from './api-client.js';

/**
 * Clone a Git repository from a URL
 * @param {string} gitUrl - The Git repository URL
 * @param {string|null} accessToken - Optional access token for private repositories
 * @returns {Promise<{success: boolean, repositoryPath: string|null, error: string|null}>}
 */
export async function cloneRepository(gitUrl, accessToken = null) {
    try {
        const response = await apiClient.post('/api/git/clone', {
            gitUrl,
            accessToken
        });

        return response;
    } catch (error) {
        console.error('Error cloning repository:', error);
        return {
            success: false,
            repositoryPath: null,
            error: error.message || 'Failed to clone repository'
        };
    }
}

/**
 * Clean up a cloned repository directory
 * @param {string} repositoryPath - Path to the repository to clean up
 * @returns {Promise<{success: boolean, error: string|null}>}
 */
export async function cleanupRepository(repositoryPath) {
    try {
        const response = await apiClient.post('/api/git/cleanup', {
            repositoryPath
        });

        return response;
    } catch (error) {
        console.error('Error cleaning up repository:', error);
        return {
            success: false,
            error: error.message || 'Failed to cleanup repository'
        };
    }
}
