US-005: Git Diff Extraction

As a developer
I want to extract my uncommitted changes as a diff
So that the AI can review exactly what I'm planning to commit
Acceptance Criteria:

LibGit2Sharp extracts staged and unstaged changes
Returns unified diff format with file paths and line numbers
Handles empty repositories gracefully
Limits diff to reasonable size (< 50KB)


Files: Updated GitService.cs with diff extraction