# AI Code Reviewer - Project Context

## Project Overview

The AI Code Reviewer is a comprehensive .NET 9 web application that provides automated code review and analysis using AI-powered insights. It's designed to help developers improve code quality by automatically reviewing code changes using AI models.

### Key Features
- **Git Repository Integration**: Analyzes Git repositories with support for uncommitted changes, staged files, and specific commits
- **AI-Powered Analysis**: Leverages the OpenRouter API to analyze code against industry standards and best practices
- **Multi-Language Support**: Handles .NET, Python, JavaScript, and HTML/CSS with language-specific templates
- **Real-Time Progress**: Uses SignalR for real-time progress updates and status tracking
- **Structured Feedback**: Generates structured feedback with critical issues, warnings, and improvement suggestions
- **Model Fallback System**: Includes primary and fallback AI models with transparent display of which model is being used

### Architecture & Technology Stack
- **Backend**: ASP.NET Core (.NET 9) with Clean Architecture principles
- **Frontend**: Vanilla JavaScript with HTML/CSS and Tailwind CSS
- **Real-time**: SignalR for asynchronous communication
- **AI Integration**: OpenRouter API with multiple model support
- **Git Integration**: LibGit2Sharp for repository operations
- **Containerization**: Docker support for development and deployment

### Core Components
- **Domain Layer**: Interfaces for analysis and AI services
- **Application Layer**: Analysis orchestration service
- **Infrastructure**: AI integration and repository management services
- **API Controllers**: REST endpoints for analysis, Git operations, and execution
- **Frontend**: Multi-step workflow with progress tracking

## Building and Running

### Prerequisites
- .NET 9 SDK
- Docker Desktop (for containerized deployment)
- OpenRouter API key

### Local Development
1. Configure the application:
   ```bash
   cp AICodeReviewer.Web/appsettings.template.json AICodeReviewer.Web/appsettings.json
   ```
2. Add your OpenRouter API key to `appsettings.json`
3. Run the application:
   ```bash
   cd AICodeReviewer.Web
   dotnet run
   ```
4. Visit http://localhost:8097 to use the application

### Docker Deployment
1. Set up environment variables or appsettings.json
2. Build and run with Docker Compose:
   ```bash
   docker-compose up -d
   ```
3. Access the application at http://localhost:8097

### Testing
- Run .NET tests: `dotnet test` (or use the configured test project)
- Run JavaScript tests: `npm test` (uses vitest as configured in package.json)

## Development Conventions

### Coding Style
- Uses .NET 9 with nullable reference types enabled
- Follows Clean Architecture principles with separation of concerns
- Dependency injection for all services
- Asynchronous programming patterns throughout

### Testing Practices
- xUnit for .NET unit testing
- Moq for mocking dependencies
- Vitest for JavaScript testing
- Comprehensive test coverage for critical functionality

### Configuration Management
- API keys stored in appsettings.json (gitignored for security)
- Support for multiple AI models with fallback capabilities
- Environment-specific configuration support
- CORS configured for local development

### File Structure
- **AICodeReviewer.Web**: Main web application with controllers, services, and frontend assets
- **AICodeReviewer.Web.Tests**: Unit and integration tests
- **docs**: Documentation files and implementation plans
- **Resources**: Embedded resource files for AI prompts
- **wwwroot**: Static web assets (HTML, CSS, JavaScript)

### Security Considerations
- Application runs as non-root user in Docker containers
- API keys stored in environment variables or configuration files (not committed)
- Session management with timeout and security settings
- CORS configured with specific origin restrictions
- Input validation and sanitization throughout the application

### AI Model Configuration
- Primary and fallback model configuration in appsettings.json
- Rate limiting handling with automatic fallback to alternative models
- Transparent model usage display for users
- Multiple available models with detailed configuration

## Key Files and Directories
- `AICodeReviewer.sln`: Visual Studio solution file
- `AICodeReviewer.Web.csproj`: Main application project file
- `Program.cs`: Application startup configuration
- `Dockerfile`: Container image configuration
- `docker-compose.yml`: Multi-container orchestration
- `appsettings.template.json`: Configuration template
- `overview.md`: Detailed project overview
- `SETUP.md`: Setup instructions
- `DOCKER_SETUP.md`: Docker deployment documentation
- `model-display-feature.md`: Model display feature implementation
- `component-extraction-plan.md`: Component architecture plan
- `AICodeReviewer.Web/Controllers/*`: API and MVC controllers
- `AICodeReviewer.Web/Infrastructure/Services/*`: Core service implementations
- `AICodeReviewer.Web/wwwroot/*`: Frontend assets
- `AICodeReviewer.Web/Hubs/*`: SignalR hubs for real-time communication