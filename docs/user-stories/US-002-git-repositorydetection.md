US-002: Git Repository Detection
* As a developer
* I want the app to detect my current git repository and branch
* So that I don't have to manually specify my project location
* Acceptance Criteria:
   * Service detects .git folder in current directory or parent directories
   * Displays current branch name on homepage
   * Shows "No git repository found" if outside a git project
   * Basic error handling for inaccessible repositories
* Files: GitService.cs, HomeController.cs, Index.cshtml