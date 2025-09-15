# US-007 Implementation Prompt: Code Review Results Display

## Context
You are implementing the results display interface for an AI code review web application. The system currently generates unstructured AI feedback and stores it in session, but users need a professional split-pane interface to review code changes alongside structured AI feedback.

## Current System State
- **Backend**: HomeController handles analysis workflow, AIService returns raw text, GitService extracts diffs
- **Frontend**: Basic form with simple text output in `<div>` container
- **Storage**: Analysis results, git diffs, and metadata stored in session with analysis IDs
- **Architecture**: .NET 8 MVC with SignalR, using vanilla JavaScript frontend

## Implementation Requirements

### 1. Create Results Display Page
**Files to create:**
- `Controllers/ResultsController.cs` - Handle results display and API endpoints
- `Views/Results/Index.cshtml` - Split-pane layout with diff and feedback
- `wwwroot/js/results.js` - Interactive functionality
- `wwwroot/css/results.css` - Layout and syntax highlighting styles

### 2. Data Structure Transformation
Transform raw AI text into structured feedback objects:
```csharp
public class FeedbackItem 
{
    public string Severity { get; set; } // Critical/Warning/Suggestion/Style
    public string FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string Message { get; set; }
    public string Category { get; set; } // Security/Performance/Style/etc
}
```

### 3. Split-Pane Layout Requirements
- **Left Pane**: Syntax-highlighted git diff with line numbers
- **Right Pane**: Categorized feedback list with severity indicators
- **Interactive Linking**: Click feedback → highlight corresponding code lines
- **Mobile Responsive**: Collapsible panes for smaller screens

### 4. Technical Specifications

**Diff Display:**
- Parse unified diff format from existing GitService
- Apply C# syntax highlighting (use highlight.js or Prism.js)
- Show file paths, line numbers, and +/- indicators
- Handle binary files gracefully

**Feedback Processing:**
- Parse AI response text to extract file references and line numbers
- Categorize by severity using keywords (Critical, Warning, Suggestion)
- Group by file or category for better organization
- Handle cases where line numbers aren't provided

**User Interactions:**
- Filter by severity level (checkboxes or dropdown)
- Expand/collapse feedback sections
- Navigate between files in large diffs
- Copy/export functionality for sharing results

### 5. Integration Points

**Session Management:**
- Retrieve analysis results using analysis ID from route parameter
- Access stored git diff data and AI response
- Handle missing/expired session data gracefully

**Navigation Flow:**
- Redirect from HomeController after analysis completion
- Support direct URL access via analysis ID
- Provide "New Analysis" button to return to main page

**Error Handling:**
- Missing analysis data → redirect to home with message
- Parsing failures → show raw AI response as fallback
- Large diffs → implement pagination or truncation

### 6. Implementation Constraints
- **Time Limit**: 30 minutes maximum
- **File Scope**: 4 files maximum (controller, view, JS, CSS)
- **Dependencies**: Use existing session storage, avoid new libraries
- **Compatibility**: Work with current SignalR and MVC setup

### 7. Acceptance Criteria
- Split-pane layout displays code changes and AI feedback side-by-side
- Syntax highlighting makes code changes clearly readable
- Feedback items link to specific lines when possible
- Severity indicators help prioritize action items
- Layout works on both desktop and mobile devices
- Users can navigate directly to results via URL with analysis ID

### 8. Suggested Implementation Order
1. **ResultsController** - Basic controller with analysis ID route and data retrieval
2. **Results/Index.cshtml** - HTML structure with split-pane layout
3. **results.css** - Styling for layout, syntax highlighting, and UI components
4. **results.js** - Interactive features and feedback-to-code linking
5. **Test** - Verify with existing analysis data and various diff sizes

## Key Success Factors
- **Visual Clarity**: Clear connection between feedback and code
- **Performance**: Fast loading even for large diffs
- **Usability**: Intuitive navigation and filtering
- **Professional Appearance**: Suitable for team sharing and presentations

Focus on delivering a functional, clean interface that transforms the current basic text output into a professional code review experience. Prioritize core functionality over advanced features within the 30-minute constraint.   