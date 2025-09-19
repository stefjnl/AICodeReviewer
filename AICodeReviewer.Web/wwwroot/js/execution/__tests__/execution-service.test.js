// AI Code Reviewer - Execution Service Integration Tests
// Tests for execution-service.js using Vitest

import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock all dependencies
vi.mock('../../api/api-client.js', () => ({
  apiClient: {
    post: vi.fn()
  }
}));

vi.mock('../../core/constants.js', () => ({
  apiEndpoints: {
    executionStart: '/api/execution/start'
  }
}));

vi.mock('../../repository/repository-state.js', () => ({
  repositoryState: {
    path: '/test/repo/path'
  }
}));

vi.mock('../../language/language-state.js', () => ({
  languageState: {
    selectedLanguage: 'csharp'
  }
}));

vi.mock('../../models/model-state.js', () => ({
  modelState: {
    selectedModel: { id: 'test-model' }
  }
}));

vi.mock('../../documents/document-manager.js', () => ({
  documentManager: {
    selectedDocuments: ['doc1.md'],
    selectedFolder: '/docs'
  }
}));

vi.mock('../../analysis/analysis-state.js', () => ({
  analysisState: {
    analysisType: 'uncommitted',
    selectedCommit: null
  }
}));

vi.mock('../../signalr/signalr-client.js', () => ({
  getSignalRConnection: vi.fn(() => ({
    state: 'Connected',
    invoke: vi.fn(),
    on: vi.fn()
  }))
}));

// Simple test to verify basic functionality
describe('ExecutionService', () => {
  let executionService;
  let mockApiClient;

  beforeEach(async () => {
    // Set up DOM
    document.body.innerHTML = `
      <button id="run-analysis-btn">Run Analysis</button>
      <div id="analysis-error-container"></div>
    `;

    // Import the module dynamically
    const { ExecutionService } = await import('../execution-service.js');
    executionService = new ExecutionService();
    
    // Get mock references
    mockApiClient = (await import('../../api/api-client.js')).apiClient;
  });

  it('should be defined', () => {
    expect(executionService).toBeDefined();
  });

  it('should have a startAnalysis method', () => {
    expect(executionService.startAnalysis).toBeDefined();
  });

  it('should call startAnalysis() → API succeeds → update DOM', async () => {
    // Mock successful API response
    const mockAnalysisId = 'test-analysis-123';
    mockApiClient.post.mockResolvedValue({
      success: true,
      analysisId: mockAnalysisId
    });

    // Start analysis
    const result = await executionService.startAnalysis();

    // Verify API call
    expect(mockApiClient.post).toHaveBeenCalledWith(
      '/api/execution/start',
      expect.objectContaining({
        repositoryPath: expect.any(String),
        language: expect.any(String),
        model: expect.any(String),
        analysisType: expect.any(String)
      })
    );

    // Verify loading state
    const runBtn = document.getElementById('run-analysis-btn');
    expect(runBtn.disabled).toBe(true);
    expect(runBtn.textContent).toBe('Starting Analysis...');
  });
});