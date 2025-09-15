# Implementation Prompt: US-006B SignalR Real-time Updates

## Context & Current State

You have a working AI code review web application with:
- Async analysis via `StartAnalysisAsync()` endpoint
- Background processing in `RunBackgroundAnalysisWithCache()`
- Status polling every 1 second via `GetAnalysisStatus()`
- Session-based progress tracking

**Problem**: Inefficient polling creates 1-second delays and unnecessary server requests.

**Goal**: Replace polling with instant WebSocket updates using SignalR.

## Requirements

**User Story**: As a developer, I want real-time progress updates instead of polling, so I get immediate feedback without delays.

**Acceptance Criteria**:
- SignalR hub broadcasts progress updates instantly
- No more 1-second polling delays
- User isolation (multiple sessions don't interfere)
- Graceful fallback if SignalR fails
- Maintain all existing functionality

**Time Limit**: 20 minutes maximum

## Step-by-Step Implementation

### Step 1: Add SignalR Package (2 minutes)
```bash
dotnet add package Microsoft.AspNetCore.SignalR
```

### Step 2: Create ProgressHub (3 minutes)
Create `Hubs/ProgressHub.cs`:
```csharp
using Microsoft.AspNetCore.SignalR;

namespace AICodeReviewer.Web.Hubs;

public class ProgressHub : Hub
{
    public async Task JoinAnalysisGroup(string analysisId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, analysisId);
    }
    
    public async Task LeaveAnalysisGroup(string analysisId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, analysisId);
    }
}
```

### Step 3: Register SignalR in Program.cs (2 minutes)
Add these lines in the correct locations:
```csharp
// In service registration section
builder.Services.AddSignalR();

// After app.UseRouting()
app.MapHub<ProgressHub>("/hubs/progress");
```

### Step 4: Update HomeController (5 minutes)

**4a. Add Hub Context Injection:**
```csharp
private readonly IHubContext<ProgressHub> _hubContext;

public HomeController(IMemoryCache cache, IHubContext<ProgressHub> hubContext)
{
    _cache = cache;
    _hubContext = hubContext;
}
```

**4b. Modify StartAnalysisAsync to Return Analysis ID:**
```csharp
[HttpPost]
public IActionResult StartAnalysisAsync(string repositoryPath, string requirements, List<string> selectedStandards)
{
    // ... existing validation ...
    
    var analysisId = Guid.NewGuid().ToString();
    HttpContext.Session.SetString("CurrentAnalysisId", analysisId);
    
    Task.Run(() => RunBackgroundAnalysisWithCache(repositoryPath, requirements, selectedStandards, analysisId));
    
    return Json(new { success = true, analysisId = analysisId });
}
```

**4c. Update Background Analysis Method:**
Replace all `_cache.Set(cacheKey, status)` calls with:
```csharp
private async Task RunBackgroundAnalysisWithCache(string repositoryPath, string requirements, List<string> selectedStandards, string analysisId)
{
    var cacheKey = $"analysis_{analysisId}";
    
    try
    {
        // Replace: _cache.Set(cacheKey, new { status = "Reading git changes...", isComplete = false });
        // With:
        await BroadcastProgress(analysisId, "Reading git changes...");
        
        // ... existing git operations ...
        
        await BroadcastProgress(analysisId, "Loading coding standards...");
        
        // ... existing document loading ...
        
        await BroadcastProgress(analysisId, "Analyzing with AI...");
        
        // ... existing AI analysis ...
        
        await BroadcastComplete(analysisId, result);
    }
    catch (Exception ex)
    {
        await BroadcastError(analysisId, ex.Message);
    }
}

private async Task BroadcastProgress(string analysisId, string status)
{
    try
    {
        await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", new {
            status = status,
            result = (string?)null,
            error = (string?)null,
            isComplete = false
        });
        
        // Keep cache for fallback
        _cache.Set($"analysis_{analysisId}", new { status = status, isComplete = false });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SignalR broadcast failed: {ex.Message}");
    }
}

private async Task BroadcastComplete(string analysisId, string result)
{
    try
    {
        await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", new {
            status = "Analysis complete",
            result = result,
            error = (string?)null,
            isComplete = true
        });
        
        _cache.Set($"analysis_{analysisId}", new { status = "Complete", result = result, isComplete = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SignalR broadcast failed: {ex.Message}");
    }
}

private async Task BroadcastError(string analysisId, string error)
{
    try
    {
        await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", new {
            status = "Analysis failed",
            result = (string?)null,
            error = error,
            isComplete = true
        });
        
        _cache.Set($"analysis_{analysisId}", new { status = "Error", error = error, isComplete = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SignalR broadcast failed: {ex.Message}");
    }
}
```

### Step 5: Update Frontend JavaScript (8 minutes)

**5a. Add SignalR Client Library to Index.cshtml:**
Add before closing `</body>` tag:
```html
<script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
```

**5b. Replace Polling with SignalR in site.js:**
```javascript
// Add these global variables at top
let signalRConnection = null;
let currentAnalysisId = null;

// Initialize SignalR connection
function initializeSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/progress")
        .build();

    signalRConnection.on("UpdateProgress", function (data) {
        document.getElementById('progress-text').innerText = data.status;
        
        if (data.result) {
            document.getElementById('analysis-result').innerHTML = data.result;
            document.getElementById('result-section').style.display = 'block';
        }
        
        if (data.error) {
            document.getElementById('analysis-result').innerHTML = `<div class="alert alert-danger">Error: ${data.error}</div>`;
            document.getElementById('result-section').style.display = 'block';
        }
        
        if (data.isComplete) {
            hideProgress();
            if (currentAnalysisId) {
                signalRConnection.invoke("LeaveAnalysisGroup", currentAnalysisId);
            }
        }
    });

    signalRConnection.start().then(function () {
        console.log("SignalR connected successfully");
    }).catch(function (err) {
        console.error("SignalR connection failed:", err);
        // Fallback to polling if SignalR fails
        startPollingFallback();
    });
}

// Modified startAnalysis function
function startAnalysis() {
    var form = document.getElementById('analysis-form');
    var formData = new FormData(form);
    
    showProgress();
    
    fetch('/Home/StartAnalysisAsync', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            currentAnalysisId = data.analysisId;
            if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Connected) {
                signalRConnection.invoke("JoinAnalysisGroup", currentAnalysisId)
                    .catch(err => {
                        console.error("Failed to join SignalR group:", err);
                        startPollingFallback();
                    });
            } else {
                startPollingFallback();
            }
        } else {
            hideProgress();
            alert('Analysis failed to start: ' + (data.message || 'Unknown error'));
        }
    })
    .catch(error => {
        console.error('Error:', error);
        hideProgress();
        alert('Failed to start analysis');
    });
}

// Fallback polling function (keep existing pollStatus logic)
function startPollingFallback() {
    console.log("Using polling fallback");
    pollStatus(); // Your existing polling function
}

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
});
```

## Testing Checklist

**✅ Verify These Work:**
1. SignalR connection establishes on page load (check browser console)
2. Analysis starts and shows instant progress updates
3. Multiple browser tabs work independently
4. Error states display correctly
5. Analysis completion shows results
6. Fallback polling works if SignalR fails

**✅ Test Scenarios:**
- Normal analysis completion
- Analysis with errors
- Multiple simultaneous analyses
- Network disconnection during analysis
- Browser refresh during analysis

## Critical Implementation Notes

**Dependency Injection**: Use proper DI pattern for `IHubContext<ProgressHub>` in HomeController constructor.

**Error Handling**: Wrap all SignalR broadcasts in try-catch blocks to prevent analysis failures.

**User Isolation**: Use `analysisId` as SignalR group name to prevent cross-user interference.

**Fallback Strategy**: Keep existing polling mechanism as backup for SignalR connection failures.

**Session Management**: Maintain existing session storage for compatibility and fallback scenarios.

## Common Pitfalls to Avoid

❌ **Don't** remove existing cache/session logic - keep as fallback
❌ **Don't** make SignalR broadcasts blocking - use fire-and-forget pattern
❌ **Don't** forget to join SignalR groups before starting analysis
❌ **Don't** break existing UI - maintain same HTML element IDs
❌ **Don't** overcomplicate - this should be a simple drop-in replacement

## Success Criteria

**Performance**: Progress updates appear instantly (< 100ms vs 1-second polling)
**Functionality**: All existing features work exactly the same
**Reliability**: Graceful fallback if SignalR fails
**User Experience**: No breaking changes to UI or workflow

**Implementation Time**: Target 15-20 minutes maximum. If taking longer, simplify the approach.