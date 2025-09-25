# AI Code Reviewer

AI Code Reviewer is a comprehensive .NET 9 web application that provides automated code review and analysis using AI-powered insights. The application integrates with Git repositories to analyze code changes and provide structured feedback on potential issues, best practices, and improvement suggestions.

## 🚀 Features

- **Git Repository Integration**: Analyzes Git repositories with support for uncommitted changes, staged files, and specific commits
- **AI-Powered Analysis**: Leverages the OpenRouter API to analyze code against industry standards and best practices
- **Multi-Language Support**: Handles .NET, Python, JavaScript, and HTML/CSS with language-specific templates
- **Real-Time Progress**: Uses SignalR for real-time progress updates and status tracking
- **Structured Feedback**: Generates structured feedback with critical issues, warnings, and improvement suggestions
- **Model Fallback System**: Includes primary and fallback AI models with transparent display of which model is being used
- **Docker Support**: Containerized deployment for easy setup and management

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core (.NET 9)
- **Frontend**: HTML, CSS, JavaScript with Tailwind CSS
- **Real-time Communication**: SignalR
- **AI Integration**: OpenRouter API
- **Git Integration**: LibGit2Sharp
- **Containerization**: Docker & Docker Compose
- **Testing**: xUnit, Moq, Vitest

## 📋 Prerequisites

- .NET 9 SDK
- Docker Desktop (for containerized deployment)
- OpenRouter API key (required for AI analysis)

## 🔧 Setup

### Local Development

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd AICodeReviewer
   ```

2. **Configure the application settings**:
   ```bash
   cp AICodeReviewer.Web/appsettings.template.json AICodeReviewer.Web/appsettings.json
   ```

3. **Add your OpenRouter API key** to `AICodeReviewer.Web/appsettings.json`:
   ```json
   {
     "OpenRouter": {
       "ApiKey": "sk-or-v1-your-actual-api-key-here",
       "Model": "qwen/qwen3-coder",
       "FallbackModel": "moonshotai/kimi-k2-0905"
     }
   }
   ```

4. **Run the application**:
   ```bash
   cd AICodeReviewer.Web
   dotnet run
   ```

5. **Access the application** at `http://localhost:8097`

### Docker Deployment

1. **Set up your API key** as an environment variable:
   ```bash
   export OPENROUTER_API_KEY="your-actual-api-key-here"
   ```

2. **Build and run with Docker Compose**:
   ```bash
   docker-compose up -d
   ```

3. **Access the application** at `http://localhost:8097`

## 🚦 Workflow

The AI Code Reviewer application follows a multi-step workflow:

1. **Document Selection**: Load and manage requirements documents
2. **Repository Selection**: Validate and configure Git repository paths
3. **Language Detection**: Auto-detect programming languages from the repository
4. **Analysis Configuration**: Choose analysis type (uncommitted, staged, commit-specific)
5. **AI Model Selection**: Choose from available AI models
6. **Execution**: Run analysis with real-time progress tracking
7. **Results**: Display structured feedback with actionable insights

## 🤖 AI Analysis Features

- **Structured Output**: Follows a strict format with critical issues, warnings, and improvements
- **Language-Specific Templates**: Custom templates for each supported language
- **Risk Assessment**: Provides risk levels and time estimates for fixes
- **File References**: Includes file paths and line numbers for precise issue location
- **Fallback System**: Automatically switches to fallback model on rate limiting

## 🧪 Testing

### Unit Tests
Run .NET unit tests:
```bash
dotnet test
```

### JavaScript Tests
Run JavaScript tests using Vitest:
```bash
npm test
```

## 🔐 Security Considerations

- API keys are stored in configuration files that are gitignored
- The application runs as a non-root user in Docker containers
- CORS is configured with specific origin restrictions
- Session management with timeout and security settings
- Input validation and sanitization throughout the application

## 📁 Project Structure

- **AICodeReviewer.Web**: Main web application with controllers, services, and frontend assets
- **AICodeReviewer.Web.Tests**: Unit and integration tests
- **Resources**: Embedded resource files for AI prompts
- **wwwroot**: Static web assets (HTML, CSS, JavaScript)
- **docs**: Documentation files and implementation plans

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Commit your changes (`git commit -m 'Add some amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For issues related to:
- Application functionality: Open an issue in this repository
- OpenRouter API: Visit https://openrouter.ai/docs
- Docker configuration: Check the DOCKER_SETUP.md documentation