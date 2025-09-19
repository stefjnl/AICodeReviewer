
Overview as of 19/09/25

The AI Code Reviewer is a comprehensive .NET 9 web application that provides automated code review and analysis using AI-powered insights. Here's a detailed description:

## Core Functionality
- **Git Repository Integration**: Analyzes Git repositories with support for uncommitted changes, staged files, and specific commits
- **AI-Powered Analysis**: Leverages the OpenRouter API to analyze code against industry standards and best practices
- **Multi-Language Support**: Handles .NET, Python, JavaScript, and HTML/CSS with language-specific templates
- **Real-Time Progress**: Uses SignalR for real-time progress updates and status tracking
- **Structured Feedback**: Generates structured feedback with critical issues, warnings, and improvement suggestions

## Architecture & Technology Stack
- **Backend**: ASP.NET Core (.NET 9) with Clean Architecture principles
- **Frontend**: Vanilla JavaScript with HTML/CSS and Tailwind CSS
- **Real-time**: SignalR for asynchronous communication
- **AI Integration**: OpenRouter API with multiple model support
- **Git Integration**: LibGit2Sharp for repository operations
- **Containerization**: Docker support for development and deployment

## Key Components
- **Domain Layer**: [`IAnalysisService`](AICodeReviewer.Web/Domain/Interfaces/IAnalysisService.cs:8), [`IAIService`](AICodeReviewer.Web/Domain/Interfaces/IAIService.cs:1) - Core business logic interfaces
- **Application Layer**: [`AnalysisOrchestrationService`](AICodeReviewer.Web/Application/Services/AnalysisOrchestrationService.cs:1) - Orchestrates analysis workflow
- **Infrastructure**: [`AIService`](AICodeReviewer.Web/Infrastructure/Services/AIService.cs:12) - AI integration, [`RepositoryManagementService`](AICodeReviewer.Web/Infrastructure/Services/RepositoryManagementService.cs:1) - Git operations
- **API Controllers**: [`AnalysisApiController`](AICodeReviewer.Web/Controllers/AnalysisApiController.cs:11), [`GitApiController`](AICodeReviewer.Web/Controllers/GitApiController.cs:12), [`ExecutionApiController`](AICodeReviewer.Web/Controllers/ExecutionApiController.cs:10) - REST endpoints
- **Frontend**: Multi-step workflow with progress tracking and real-time updates

## Workflow Process
1. **Document Selection**: Load and manage requirements documents
2. **Repository Selection**: Validate and configure Git repository paths
3. **Language Detection**: Auto-detect programming languages from the repository
4. **Analysis Configuration**: Choose analysis type (uncommitted, staged, commit-specific)
5. **AI Model Selection**: Choose from available AI models
6. **Execution**: Run analysis with real-time progress tracking
7. **Results**: Display structured feedback with actionable insights

## AI Analysis Features
- **Structured Output**: Follows a strict format with critical issues, warnings, and improvements
- **Language-Specific Templates**: Custom templates for each supported language
- **Risk Assessment**: Provides risk levels and time estimates for fixes
- **File References**: Includes file paths and line numbers for precise issue location

## Technical Features
- **Session Management**: For storing analysis state and document content
- **Memory Cache**: Optimized caching with size limits
- **Error Handling**: Comprehensive error handling with structured responses
- **CORS Support**: Configured for cross-origin requests
- **Dependency Injection**: Properly configured service lifetimes

## Use Cases
- **Code Review Automation**: Automate routine code review tasks
- **Quality Assurance**: Ensure code quality and adherence to standards
- **Developer Training**: Provide feedback and learning opportunities
- **Pre-commit Validation**: Catch issues before code submission
- **Technical Debt Management**: Identify and track technical debt

This application represents a sophisticated AI-powered code review platform that combines Git integration with advanced AI analysis capabilities to provide developers with actionable insights and improve code quality across multiple programming languages.