# Model Display Feature Implementation

## Overview
This document describes the implementation of displaying AI model information to the front-end users during code analysis.

## Implementation Summary

### 1. Extended ProgressDto Model
- **File**: [`AICodeReviewer.Web/Models/ProgressDto.cs`](AICodeReviewer.Web/Models/ProgressDto.cs:3)
- **Changes**: Added `ModelUsed` and `FallbackModel` properties to track which models are being used
- **New Signature**: `public record ProgressDto(string Status, string? Result, string? Error, bool IsComplete, string? ModelUsed = null, string? FallbackModel = null);`

### 2. Enhanced SignalR Broadcasting
- **File**: [`AICodeReviewer.Web/Infrastructure/Services/SignalRBroadcastService.cs`](AICodeReviewer.Web/Infrastructure/Services/SignalRBroadcastService.cs:27)
- **New Methods**:
  - `BroadcastProgressWithModelAsync()` - Broadcasts progress updates with model information
  - `BroadcastCompleteWithModelAsync()` - Broadcasts completion with model information
- **Interface Updated**: [`AICodeReviewer.Web/Domain/Interfaces/ISignalRBroadcastService.cs`](AICodeReviewer.Web/Domain/Interfaces/ISignalRBroadcastService.cs:18)

### 3. Updated AnalysisService
- **File**: [`AICodeReviewer.Web/Infrastructure/Services/AnalysisService.cs`](AICodeReviewer.Web/Infrastructure/Services/AnalysisService.cs:65)
- **Key Changes**:
  - Added `fallbackWasUsed` flag to track when fallback model is triggered
  - Updated all progress broadcasts to include model information
  - Enhanced completion broadcast to show which model was actually used (primary or fallback)

### 4. Enhanced Frontend JavaScript
- **File**: [`AICodeReviewer.Web/wwwroot/js/site.js`](AICodeReviewer.Web/wwwroot/js/site.js:17)
- **Changes**:
  - Updated SignalR `UpdateProgress` handler to display model information
  - Updated polling fallback to handle model information
  - Model info is displayed as: `"Status message (Model: primary-model) [Fallback: fallback-model]"`

## Model Display Format

### Normal Operation
```
"AI analysis... (Model: qwen/qwen3-coder) [Fallback: moonshotai/kimi-k2-0905]"
```

### When Fallback is Triggered
```
"Rate limited, switching to fallback model (moonshotai/kimi-k2-0905)..."
```

### Analysis Complete
```
"Analysis complete (using: moonshotai/kimi-k2-0905)"
```

## Key Features

### 1. Real-time Model Information
- Users see which model is being used during analysis
- Fallback model availability is always displayed
- Model switches are clearly indicated

### 2. Fallback Transparency
- When rate-limiting occurs, users are informed about the model switch
- Clear messaging: "Rate limited, switching to fallback model..."
- Final completion shows which model was actually used

### 3. Fallback Detection Logic
- **Rate Limit Detection**: Uses [`IsRateLimitError()`](AICodeReviewer.Web/Infrastructure/Services/AnalysisService.cs:194) method
- **Keywords**: Checks for "429", "rate limit", "rate-limit", "Rate limit", "too many requests"
- **Automatic Retry**: Immediately retries with fallback model upon detection

### 4. Comprehensive Logging
- Logs primary model usage: `"Primary model: qwen/qwen3-coder"`
- Logs fallback availability: `"Fallback model: moonshotai/kimi-k2-0905"`
- Logs fallback triggers: `"Rate limit detected for primary model..."`
- Logs completion with actual model used

## Configuration
The models are configured in [`appsettings.json`](AICodeReviewer.Web/appsettings.template.json:9):
```json
{
  "OpenRouter": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "qwen/qwen3-coder",
    "FallbackModel": "moonshotai/kimi-k2-0905"
  }
}
```

## Testing Scenarios

### Scenario 1: Normal Operation
1. Run analysis with available quota
2. Verify display shows: `"AI analysis... (Model: qwen/qwen3-coder) [Fallback: moonshotai/kimi-k2-0905]"`
3. Check logs for primary model usage

### Scenario 2: Rate Limit Fallback
1. Exhaust quota for primary model
2. Verify fallback notification: `"Rate limited, switching to fallback model (moonshotai/kimi-k2-0905)..."`
3. Verify completion shows fallback model was used

### Scenario 3: Both Models Rate-Limited
1. Exhaust quota for both models
2. Verify appropriate error message is displayed
3. Check logs for both model attempts

## Benefits

### 1. User Transparency
- Users always know which AI model is analyzing their code
- Clear visibility into fallback mechanism operation
- Enhanced trust through transparent operation

### 2. Operational Insights
- Easy monitoring of fallback usage
- Clear identification of rate-limiting events
- Better debugging capabilities with model-specific logging

### 3. Graceful Degradation
- Automatic fallback without user intervention
- Seamless model switching experience
- Maintains service availability during rate-limiting

## Technical Implementation Details

### SignalR Integration
- Model information is included in all progress updates
- Fallback notifications are broadcast immediately when triggered
- Both SignalR and polling fallback methods support model display

### Error Handling
- Graceful handling of missing model configuration
- Null-safe model parameter handling
- Comprehensive error logging for debugging

### Performance Considerations
- Minimal overhead - model info only added to existing broadcasts
- No additional API calls required
- Efficient string formatting for display

## Future Enhancements

### 1. Model Performance Metrics
- Track response times per model
- Display success/failure rates
- Show cost differences between models

### 2. Model Selection UI
- Allow users to manually select models
- Provide model comparison information
- Enable model preference settings

### 3. Advanced Fallback Logic
- Multiple fallback models with priority ordering
- Model selection based on analysis type
- Cost-based model optimization

This implementation provides a solid foundation for transparent AI model usage in the AICodeReviewer application, enhancing user experience and operational visibility.