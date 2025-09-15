# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["AICodeReviewer.sln", "."]
COPY ["AICodeReviewer.Web/AICodeReviewer.Web.csproj", "AICodeReviewer.Web/"]

# Restore dependencies
RUN dotnet restore "AICodeReviewer.Web/AICodeReviewer.Web.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "AICodeReviewer.Web/AICodeReviewer.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AICodeReviewer.Web/AICodeReviewer.Web.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install git for LibGit2Sharp dependency
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -g 1000 appuser && \
    useradd -r -u 1000 -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Create directory for git repositories with secure permissions
RUN mkdir -p /app/git-repos && \
    chown -R appuser:appuser /app && \
    chmod 750 /app/git-repos

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8097

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8097/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8097
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "AICodeReviewer.Web.dll"]