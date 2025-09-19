import { defineConfig } from 'vitest/config';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  test: {
    environment: 'jsdom',
    include: ['**/__tests__/**/*.test.js', '**/*.test.js'],
    resolve: {
      alias: {
        '^../api/api-client.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/api/api-client.js'),
        '^../core/constants.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/core/constants.js'),
        '^../workflow/workflow-state.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/workflow/workflow-state.js'),
        '^../repository/repository-state.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/repository/repository-state.js'),
        '^../language/language-state.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/language/language-state.js'),
        '^../models/model-state.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/models/model-state.js'),
        '^../documents/document-manager.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/documents/document-manager.js'),
        '^../analysis/analysis-state.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/analysis/analysis-state.js'),
        '^../signalr/signalr-client.js': path.resolve(__dirname, 'AICodeReviewer.Web/wwwroot/js/signalr/signalr-client.js')
      }
    }
  },
});