# Investigation and Fix: Documents Folder Path Regression

## Problem Statement
The documents folder path resolution is broken. The application is now looking for `/Documents` (filesystem root) instead of `/app/Documents` (application directory), causing documents to not load.

## Evidence from Logs

**Working state (earlier today):**
```
"contentRootPath = /app"
"documentsFolder = /app/Documents"
"Directory.Exists(documentsFolder) = True"
"Found 5 .md files in /app/Documents"
```

**Broken state (current):**
```
"Resolved documents folder path: /Documents"
"Documents folder does not exist: /Documents"
```

## Investigation Required

### Step 1: Locate the Issue
Find all code related to documents folder path resolution:
1. `PathValidationService.GetDocumentsFolderPath()` method
2. Any calls to `GetDocumentsFolderPath()` 
3. Recent changes to document scanning logic
4. Check if Git clone implementation modified path handling

### Step 2: Identify What Changed
Compare the current implementation against this known-working pattern:
```csharp
public string GetDocumentsFolderPath(string contentRootPath)
{
    // Should check /app/Documents first (production/Docker)
    var documentsFolder = Path.Combine(contentRootPath, "Documents");
    
    if (Directory.Exists(documentsFolder))
    {
        return documentsFolder;  // Should return "/app/Documents"
    }
    
    // Fallback logic...
}
```

### Step 3: Check All Callers
Verify that `GetDocumentsFolderPath()` is being called with the correct `contentRootPath` parameter:
- Should receive: `/app` (from `IWebHostEnvironment.ContentRootPath`)
- Should NOT receive: empty string, null, or relative path

Look for code like:
```csharp
var documentsPath = _pathService.GetDocumentsFolderPath(_environment.ContentRootPath);
```

## Required Fix

### Fix Requirements
1. **Ensure `GetDocumentsFolderPath()` receives correct contentRootPath** (`/app` in Docker)
2. **First check should be**: `Path.Combine(contentRootPath, "Documents")` → `/app/Documents`
3. **Return the absolute path**: `/app/Documents`, not `/Documents`
4. **Add defensive logging**: Log the received `contentRootPath` parameter at method entry

### Expected Behavior After Fix
When documents are loaded, logs should show:
```
"GetDocumentsFolderPath called"
"contentRootPath = /app"
"documentsFolder = /app/Documents"
"Directory.Exists(documentsFolder) = True"
"Found 5 .md files in /app/Documents"
```

## Debug Logging to Add

Add these logs to `GetDocumentsFolderPath()`:
```csharp
public string GetDocumentsFolderPath(string contentRootPath)
{
    Console.WriteLine($"[DEBUG] GetDocumentsFolderPath called with: '{contentRootPath}'");
    Console.WriteLine($"[DEBUG] contentRootPath is null or empty: {string.IsNullOrEmpty(contentRootPath)}");
    
    var documentsFolder = Path.Combine(contentRootPath, "Documents");
    Console.WriteLine($"[DEBUG] Combined path result: '{documentsFolder}'");
    Console.WriteLine($"[DEBUG] Directory.Exists: {Directory.Exists(documentsFolder)}");
    
    // Rest of method...
}
```

## Files to Check

Priority files likely containing the issue:
1. `AICodeReviewer.Web/Infrastructure/Services/PathValidationService.cs`
2. `AICodeReviewer.Web/Controllers/DocumentApiController.cs`
3. `AICodeReviewer.Web/Infrastructure/Services/DocumentManagementService.cs`
4. Any new Git-related services that might be interfering with path resolution

## Success Criteria
- [ ] Documents load successfully (5 files found)
- [ ] Logs show `/app/Documents` not `/Documents`
- [ ] `GetDocumentsFolderPath()` receives `/app` as parameter
- [ ] Path.Combine correctly produces `/app/Documents`
- [ ] No hardcoded paths like `/Documents` in the code

## Context
This regression occurred after implementing Git clone functionality. The documents folder path worked correctly earlier today. The issue is specifically with how `GetDocumentsFolderPath()` is being called or what it returns - it's producing `/Documents` instead of `/app/Documents`.

**Investigate thoroughly and fix the root cause, not just the symptom. Ensure the fix works in both local Docker and Azure Container Apps environments.**