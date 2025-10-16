# Implementation Prompt: Git Clone via URL for CodeGuard

## Context
CodeGuard is a .NET 9 web application deployed on Azure Container Apps that performs AI-powered code reviews. Currently, it uses a file browser to select local repositories, which doesn't work in cloud deployment. We need to replace this with Git clone functionality using URLs.

## Current Architecture
- **Backend**: ASP.NET Core .NET 9, Clean Architecture
- **Git Library**: LibGit2Sharp (already installed)
- **Frontend**: Vanilla JavaScript, Tailwind CSS
- **Existing Services**: 
  - `RepositoryManagementService` (handles Git operations)
  - Controllers in `AICodeReviewer.Web/Controllers/`
  - Frontend in `AICodeReviewer.Web/wwwroot/`

## Goal
Replace the repository file browser with a Git URL input system that:
1. Accepts Git repository URLs (HTTPS)
2. Clones repositories server-side to `/app/temp/repos/{guid}/`
3. Validates the clone succeeded
4. Integrates with existing repository validation flow
5. Cleans up temporary repos after analysis

## Requirements

### Backend Changes

**1. Update `RepositoryManagementService`**
Add method to clone from URL:
```csharp
public (bool success, string? localPath, string? error) CloneRepository(string gitUrl, string? accessToken = null)
```

Requirements:
- Generate unique directory: `/app/temp/repos/{Guid.NewGuid()}/`
- Use LibGit2Sharp to clone
- Support optional access token for private repos (format: `https://{token}@github.com/user/repo.git`)
- Validate clone succeeded (check for `.git` folder)
- Log all operations
- Handle errors gracefully (invalid URL, network issues, authentication failures)
- Return tuple with success status, local path, and error message

**2. Add Cleanup Method**
```csharp
public (bool success, string? error) CleanupRepository(string repositoryPath)
```

Requirements:
- Safely delete directory and all contents
- Handle "directory in use" errors
- Log cleanup operations

**3. Create New API Endpoint**
File: `AICodeReviewer.Web/Controllers/GitApiController.cs`

Add endpoint:
```csharp
[HttpPost("clone")]
public IActionResult CloneRepository([FromBody] CloneRepositoryRequest request)
```

Request model:
```csharp
public class CloneRepositoryRequest
{
    public string GitUrl { get; set; }
    public string? AccessToken { get; set; }
}
```

Response should return:
```json
{
  "success": true/false,
  "repositoryPath": "/app/temp/repos/{guid}/",
  "error": null or "error message"
}
```

Validate:
- URL is not empty
- URL starts with `https://` or `http://`
- URL contains valid Git hosting patterns (github.com, gitlab.com, bitbucket.org, etc.)

### Frontend Changes

**4. Update Step 2 UI**
File: `AICodeReviewer.Web/wwwroot/index.html`

Replace the file browser section with:

```html
<div class="space-y-4">
  <!-- Git URL Input -->
  <div>
    <label class="block text-sm font-medium mb-2">Git Repository URL</label>
    <input 
      id="git-url-input" 
      type="text" 
      placeholder="https://github.com/username/repository.git"
      class="w-full px-4 py-2 border rounded-lg"
    />
  </div>

  <!-- Optional Access Token -->
  <div>
    <label class="block text-sm font-medium mb-2">
      Personal Access Token (optional, for private repos)
    </label>
    <input 
      id="git-token-input" 
      type="password" 
      placeholder="ghp_xxxxxxxxxxxx"
      class="w-full px-4 py-2 border rounded-lg"
    />
  </div>

  <!-- Clone Button -->
  <button id="clone-repository-btn" class="px-6 py-2 bg-blue-600 text-white rounded-lg">
    Clone Repository
  </button>

  <!-- Status Message -->
  <div id="clone-status" class="hidden">
    <p id="clone-status-message"></p>
  </div>
</div>
```

**5. Create Git Clone API Module**
File: `AICodeReviewer.Web/wwwroot/js/api/git-clone-api.js`

```javascript
export async function cloneRepository(gitUrl, accessToken = null) {
    // POST to /api/git/clone
    // Return { success, repositoryPath, error }
}
```

**6. Update Repository Event Handlers**
File: `AICodeReviewer.Web/wwwroot/js/repository/repository-event-handlers.js`

Add handler for clone button:
```javascript
document.getElementById('clone-repository-btn').addEventListener('click', async () => {
    const gitUrl = document.getElementById('git-url-input').value;
    const accessToken = document.getElementById('git-token-input').value || null;
    
    // Show loading state
    // Call cloneRepository API
    // On success: populate repository-path input with returned path
    // Call existing validateRepository function
    // Show success/error message
});
```

**7. Update Cleanup**
Add cleanup call after analysis completes:
- In analysis completion handler
- Call new cleanup endpoint: `POST /api/git/cleanup` with `{ repositoryPath }`

## Security Considerations
1. **Never log access tokens** - use `[DataAnnotation]` or manual scrubbing
2. **Validate URLs** - prevent command injection via git URLs
3. **Rate limiting** - prevent abuse of clone endpoint
4. **Timeout** - set 5-minute timeout on clone operations
5. **Disk space** - monitor `/app/temp/repos/` size, implement quota if needed

## Error Handling
Handle these scenarios gracefully:
- Invalid Git URL format
- Repository doesn't exist (404)
- Authentication required but no token provided
- Invalid access token
- Network timeout
- Disk space full
- Repository too large (>500MB)

## Testing Checklist
- [ ] Clone public GitHub repository
- [ ] Clone private repository with valid token
- [ ] Handle invalid URL gracefully
- [ ] Handle authentication errors
- [ ] Verify cleanup after analysis
- [ ] Test with large repository
- [ ] Verify no token leakage in logs

## File Structure After Implementation
```
AICodeReviewer.Web/
├── Controllers/
│   └── GitApiController.cs (update existing or create)
├── Infrastructure/Services/
│   └── RepositoryManagementService.cs (update)
├── wwwroot/
│   ├── index.html (update Step 2)
│   └── js/
│       ├── api/
│       │   └── git-clone-api.js (new)
│       └── repository/
│           └── repository-event-handlers.js (update)
```

## Success Criteria
1. User can enter `https://github.com/dotnet/runtime` and click "Clone Repository"
2. Backend clones repo to `/app/temp/repos/{guid}/`
3. Existing validation flow works with cloned path
4. Analysis proceeds normally
5. Temporary repo is cleaned up after analysis
6. All errors show user-friendly messages

## Notes
- Keep existing file browser code commented out (don't delete) for potential local development mode
- Use async/await throughout for non-blocking operations
- Add loading spinners during clone (can take 30-60 seconds for large repos)
- Consider adding progress indication if possible with LibGit2Sharp

## Example Flow
1. User enters: `https://github.com/stefjnl/CodeGuard`
2. Clicks "Clone Repository"
3. Backend clones to `/app/temp/repos/a1b2c3d4-.../ `
4. Frontend receives path: `/app/temp/repos/a1b2c3d4-.../`
5. Existing repository validation runs
6. User proceeds through analysis steps
7. After results displayed, cleanup endpoint deletes temp directory

Implement this feature following Clean Architecture principles, maintaining separation between Domain, Application, and Infrastructure layers. Ensure all new code follows existing coding standards and includes appropriate logging.