# AI Code Reviewer - Comprehensive Application Overview

## Executive Summary

CodeGuard (AI Code Reviewer) is a .NET 9 web application that provides intelligent code analysis and review capabilities by integrating with AI services (specifically OpenRouter). The application enables developers to analyze Git repository changes and single files, receiving AI-powered feedback on code quality, security, performance, and adherence to coding standards.

## Technical Architecture

### Platform & Framework
- **Runtime**: .NET 9.0
- **Web Framework**: ASP.NET Core Web Application (not MVC Razor)
- **Frontend**: Pure HTML/JavaScript with SignalR for real-time updates
- **Client**: Running on localhost:8097

### Core Technologies
- **Git Integration**: LibGit2Sharp library for repository operations
- **Real-time Communication**: SignalR for progress updates
- **Caching**: In-memory and distributed caching
- **Session Management**: ASP.NET Core session management for document selection
- **HTTP Client**: For external AI API calls with proper timeout/config

## Application Structure

### Domain Layer (`Domain/`)
Contains domain interfaces, models, enums and core business concepts:
- Interface definitions for all major services
- Enums for analysis types, severity levels, languages
- Domain constants and helper classes

### Application Layer (`Application/`)
Higher-level orchestrating services implementing business logic:
- **AnalysisOrchestrationService**: Main command handler for analysis operations
- **AnalysisPreparationService**: Prepares content and validates parameters
- **AnalysisExecutionService**: Executes the core analysis workflow
- **AnalysisContextFactory**: Creates context objects for analysis runs

### Infrastructure Layer (`Infrastructure/`)
Implementation classes for specific responsibilities:
- **AIService**: Communicates with OpenRouter API for code analysis
- **AIPromptResponseService**: Handles AI response processing
- **AIAnalysisOrchestrator**: Orchestrates AI calls with timeout/fallback handling
- **RepositoryManagementService**: Git operations (diffs, commits, branches)
- **ContentExtractionService**: Extracts/validates analysis content
- **ValidationService**: Validates analysis parameters
- **AnalysisCacheService**: Manages in-memory analysis results
- **SignalRBroadcastService**: Sends real-time updates to frontend
- **DirectoryBrowsingService**: File system operations

### Controllers (`Controllers/`)
API endpoints for frontend operations:
- **HomeController**: Main operations (run analysis, get status, repository settings)
- **AnalysisApiController**: Analysis configuration and previews
- **GitApiController**: Git-specific operations
- **DocumentApiController**: Document management
- **ResultsController**: Result handling
- **DirectoryBrowserController**: Directory browsing operations

## Key Features

### Analysis Types
The application supports multiple analysis modes:
- **Uncommitted Changes**: Analyses all unstaged + staged changes in repository
- **Staged Changes**: Analyses only git-staged changes
- **Specific Commit**: Analyses changes in a specific commit
- **Single File**: Analyses an uploaded file or specific file path
- **Pull Request Differential**: Analyses differences between two branches

### AI Integration
- **Provider**: OpenRouter API integration
- **Models**: Configurable AI models with fallback capability
- **Languages Supported**: .NET/C#, Python, JavaScript/TypeScript, HTML/CSS
- **Timeout**: 60-second timeout for API calls
- **Fallback**: Automatic retry with fallback model on rate limit errors
- **Prompt Engineering**: Specialized prompts per programming language with appropriate best practices

### Git Operations
- Commit history browsing and selection
- Branch comparison functionality
- Diff extraction for staged/unstaged/committed changes
- Repository validation and branch detection
- Large diff protection (100KB/200KB limits)

### Real-time Progress Tracking
- SignalR hub for progress updates
- Client-side position marker during analysis
- Status updates including current AI model being used
- Error handling with user-friendly messages

### Security & Validation
- Path validation and security checks (DirectoryBrowsingService)
- Content size limits (1MB for files, 100KB/200KB for diffs)
- Repository access validation
- Input sanitization and validation

## System Flow

### Code Analysis Workflow
1. User configures analysis parameters via frontend
2. Request validated by ValidationService
3. Content preparation handled by ContentExtractionService
4. Request submitted as background task with tracking ID
5. Cache initialized with "Starting" status
6. AI service called through AIAnalysisOrchestrator
7. Real-time progress sent via SignalR hub
8. Results cached when complete
9. Results available via Status API for polling

### Content Flow
- Repository changes: Git diff extraction → Analysis content
- Single files: File reading → Direct analysis content
- Prompts: Template-based generation with language-specific best practices
- Responses: AI processing → Structured feedback → Display formatting

## Service Dependencies & Injection

The application uses ASP.NET Core's dependency injection with scoped services primarily, implementing the following pattern:

### Critical Integrations
- **OpenRouter API**: Configurable via API key for AI analysis
- **Git Repositories**: LibGit2Sharp integration for repository operations
- **SignalR**: Real-time progress notifications to frontend

### Configuration
- **appsettings.json**: Core configuration settings
- **Sessions**: For temporary data like repository path, document selections
- **Caching**: In-memory storage for analysis results and progress

## Data Management

### State Management
- Repository path and document selections stored in session
- Analysis results cached with expiration
- Background tasks use a custom task management system
- Progress tracking uses in-memory cache

### File Extensions Support
Programming files supported through repository analysis:
- **.cs, .vb**: .NET languages
- **.py**: Python
- **.js, .ts, .jsx, .tsx**: JavaScript/TypeScript
- **.html, .htm, .css, .scss, .sass, .less**: Frontend languages
- **.java, .cpp, .c, .h, .hpp**: Java and C-family languages

## Error Handling & Resilience

### Key Error Handling Features
- 60-second timeouts for AI API calls
- Rate limit detection and fallback model retry
- Git operation error handling with user feedback
- Content size validation to prevent overloads
- Comprehensive exception handling with logging
- Graceful degradation for unavailable git repositories

### Logging & Monitoring
- Structured logging throughout all services
- Error diagnostics for debugging
- Performance monitoring through HTTP/API operations

## Deployment & Configuration

### Docker Support
- Dockerfile included for containerization
- docker-compose.yml configuration
- nginx.conf for reverse proxy (if needed)

### Architecture Notes
- Clean architecture design with separation of concerns
- Domain-driven approach with interfaces and implementations
- Background processing for long-running operations
- Responsive UI with real-time status updates
- Extensible design for additional analysis types

## Future Considerations

Based on the current codebase structure, potential improvements could include:
- Additional programming language support
- More sophisticated AI model selection and management
- Advanced room features for team collaboration
- Enhanced caching strategies for large repositories
- Integration with CI/CD pipelines
- More detailed analysis result breakdowns

This overview was generated on 2025-10-14 based on the current state of the AICodeReviewer application.