// AI Code Reviewer - Model Selection Functions

import { apiEndpoints } from '../core/constants.js';
import { repositoryState } from '../repository/repository-state.js';
import { modelState } from './model-state.js';
import { updateModelUI } from './model-ui.js';

export async function loadAvailableModels() {
    console.log('🔍 loadAvailableModels() called');
    console.log('Repository path:', repositoryState.path);
    
    if (!repositoryState.path) {
        console.warn('⚠️ No repository path available');
        modelState.error = 'No repository path available';
        updateModelUI('error');
        return;
    }

    modelState.loading = true;
    modelState.error = null;
    updateModelUI('loading');

    try {
        console.log('🔄 Loading models from API...');
        const response = await fetch(apiEndpoints.modelAvailable);
        
        console.log('📡 API Response Status:', response.status, response.statusText);
        
        if (!response.ok) {
            // If API returns 404, use fallback models
            if (response.status === 404) {
                console.warn('⚠️ Model API not available, using fallback models');
                useFallbackModels();
                return;
            }
            
            const errorText = await response.text();
            console.error('❌ API Error Response:', errorText);
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const data = await response.json();
        console.log('✅ API Response Data:', data);
        
        if (data.success) {
            modelState.availableModels = data.models || [];
            console.log(`📋 Loaded ${modelState.availableModels.length} models`);
            updateModelUI('loaded');
            
            // Auto-select first model if available
            if (modelState.availableModels.length > 0) {
                selectModel(modelState.availableModels[0].id);
            }
        } else {
            modelState.error = data.error || 'Failed to load models';
            updateModelUI('error');
        }
    } catch (error) {
        console.error('❌ Error loading models:', error);
        
        // Use fallback models on any error
        if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
            console.warn('⚠️ Network error, using fallback models');
            useFallbackModels();
        } else {
            modelState.error = error.message;
            updateModelUI('error');
        }
    } finally {
        modelState.loading = false;
    }
}

// Fallback models for when API is not available
function useFallbackModels() {
    console.log('🛠️ Using fallback models...');

    // Define fallback models matching the single source of truth in appsettings.json
    const fallbackModelsData = {
        "qwen/qwen3-coder": {
            name: "Qwen3 Coder",
            provider: "Qwen",
            description: "Specialized for code analysis and review",
            icon: "🔍"
        },
        "moonshotai/kimi-k2-0905": {
            name: "Kimi K2",
            provider: "Moonshot AI",
            description: "Advanced reasoning for complex code patterns",
            icon: "🌙"
        },
        "qwen/qwen3-next-80b-a3b-instruct": {
            name: "Qwen3 Next 80B",
            provider: "Qwen",
            description: "Large model for comprehensive analysis",
            icon: "🚀"
        },
        "x-ai/grok-4-fast:free": {
            name: "Grok-4 Fast",
            provider: "xAI",
            description: "Fast and efficient code analysis",
            icon: "⚡"
        },
        "deepseek/deepseek-chat-v3.1:free": {
            name: "DeepSeek Chat v3.1",
            provider: "DeepSeek",
            description: "Advanced conversational AI for code review",
            icon: "🤖"
        },
        "openai/gpt-oss-120b:free": {
            name: "GPT-OSS 120B",
            provider: "OpenAI",
            description: "Open source large language model",
            icon: "🧠"
        },
        "z-ai/glm-4.5-air:free": {
            name: "GLM-4.5 Air",
            provider: "Z-AI",
            description: "Lightweight and efficient code analysis",
            icon: "💨"
        }
    };

    // Convert to the format expected by the UI
    modelState.availableModels = Object.keys(fallbackModelsData).map(id => ({
        id: id,
        name: fallbackModelsData[id].name,
        provider: fallbackModelsData[id].provider,
        description: fallbackModelsData[id].description,
        icon: fallbackModelsData[id].icon
    }));

    console.log('✅ Fallback models loaded:', modelState.availableModels.length);
    updateModelUI('loaded');

    // Auto-select first model
    if (modelState.availableModels.length > 0) {
        console.log('🎯 Auto-selecting first model:', modelState.availableModels[0].name);
        selectModel(modelState.availableModels[0].id);
    }
}

export function selectModel(modelId) {
    const model = modelState.availableModels.find(m => m.id === modelId);
    if (!model) return;

    modelState.selectedModel = modelId;
    updateModelUI('loaded');
    
    // Mark Step 5 as completed and enable analysis
    // We'll handle this in the workflow navigation module
    console.log(`Model selected: ${model.name}`);
}

function populateModelDropdown() {
    const modelDropdown = document.getElementById('model-select');
    if (!modelDropdown || modelState.availableModels.length === 0) return;

    modelDropdown.innerHTML = '<option value="">Choose a model...</option>';
    
    modelState.availableModels.forEach(model => {
        const option = document.createElement('option');
        option.value = model.id;
        option.textContent = `${model.icon} ${model.name}`;
        option.selected = model.id === modelState.selectedModel;
        modelDropdown.appendChild(option);
    });
}