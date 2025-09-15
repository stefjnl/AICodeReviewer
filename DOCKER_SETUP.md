# Docker Setup for AI Code Reviewer

This document provides instructions for running the AI Code Reviewer application using Docker and Docker Compose.

RECOMMENDED WORKFLOW:
For Docker-First Development: Use the override file to keep Docker on port 8098
For Local-First Development: Stop Docker containers when working locally
For Mixed Development: Use different ports as shown above
The Docker configuration is flexible enough to work alongside your local development environment. You can easily switch between Docker and local development without port conflicts.

Files Created:

✅ Dockerfile - Main container configuration
✅ docker-compose.yml - Orchestration setup
✅ docker-compose.override.yml - Port override for development
✅ .dockerignore - Build optimization
✅ nginx.conf - Production reverse proxy
✅ DOCKER_SETUP.md - Complete documentation

============================

## Prerequisites

- Docker Desktop installed on your machine
- Docker Compose (included with Docker Desktop)
- OpenRouter API key (get one at https://openrouter.ai/)

## Quick Start

1. **Clone the repository** (if you haven't already):
   ```bash
   git clone <your-repo-url>
   cd AICodeReviewer
   ```

2. **Set up your API key**:
   ```bash
   # Option 1: Create appsettings.json (recommended for development)
   cp AICodeReviewer.Web/appsettings.template.json AICodeReviewer.Web/appsettings.json
   # Edit appsettings.json and add your OpenRouter API key
   
   # Option 2: Use environment variable (recommended for Docker)
   export OPENROUTER_API_KEY="your-actual-api-key-here"
   ```

3. **Build and run with Docker Compose**:
   ```bash
   # Basic setup (application only)
   docker-compose up -d
   
   # Production setup (with nginx reverse proxy)
   docker-compose --profile production up -d
   ```

4. **Access the application**:
   - Basic setup: http://localhost:8097
   - Production setup: http://localhost (port 80)

## Configuration

### Environment Variables

The application supports the following environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `OPENROUTER_API_KEY` | Your OpenRouter API key | `YOUR_API_KEY_HERE` |
| `OPENROUTER_MODEL` | AI model to use | `moonshotai/kimi-k2-0905` |
| `ASPNETCORE_ENVIRONMENT` | .NET environment | `Production` |

### Docker Compose Profiles

- **Default profile**: Runs only the main application
- **Production profile**: Runs application + nginx reverse proxy with SSL support

## Docker Commands

### Building the Image
```bash
# Build the image
docker build -t aicodereviewer .

# Build with no cache
docker build --no-cache -t aicodereviewer .
```

### Running the Container
```bash
# Run with environment variables
docker run -d \
  --name aicodereviewer \
  -p 8097:8097 \
  -e OPENROUTER_API_KEY="your-api-key" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  aicodereviewer

# Run with volume for git repositories
docker run -d \
  --name aicodereviewer \
  -p 8097:8097 \
  -e OPENROUTER_API_KEY="your-api-key" \
  -v aicodereviewer_git-repos:/app/git-repos \
  aicodereviewer
```

### Managing the Container
```bash
# View logs
docker logs aicodereviewer

# Follow logs
docker logs -f aicodereviewer

# Stop the container
docker stop aicodereviewer

# Start the container
docker start aicodereviewer

# Remove the container
docker rm aicodereviewer
```

### Docker Compose Commands
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f

# Rebuild and restart
docker-compose down && docker-compose up -d --build

# View running services
docker-compose ps

# Scale the application (if needed)
docker-compose up -d --scale aicodereviewer=2
```

## Development with Docker

### Local Development
```bash
# Run in development mode
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

### Hot Reload (Development)
Create a `docker-compose.override.yml` file:
```yaml
version: '3.8'
services:
  aicodereviewer:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8097
    volumes:
      - ./AICodeReviewer.Web:/app:ro
      - /app/bin
      - /app/obj
```

## Production Deployment

### SSL/HTTPS Setup
1. Place your SSL certificates in the `./ssl` directory:
   - `cert.pem` (certificate)
   - `key.pem` (private key)

2. Update `nginx.conf` with your domain name

3. Deploy with production profile:
   ```bash
   docker-compose --profile production up -d
   ```

### Security Considerations
- The application runs as a non-root user for security
- Rate limiting is configured in nginx
- Security headers are added by nginx
- API keys should be managed securely (consider using Docker secrets)

### Monitoring
The application includes health checks that monitor:
- Application responsiveness
- API connectivity
## 🔄 Running Locally Alongside Docker

### Port Conflict Solution
Since both Docker and local development use port 8097 by default, here are your options:

### Option 1: Docker on Alternative Port (Recommended)
```bash
# Use the override file to run Docker on port 8098
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Access Docker app: http://localhost:8098
# Run local development on port 8097: cd AICodeReviewer.Web && dotnet run
```

### Option 2: Local Development on Different Port
```bash
# Run local development on port 8098 (or any available port)
cd AICodeReviewer.Web
dotnet run --urls "http://localhost:8098"

# Keep Docker running on default port 8097
```

### Option 3: Switch Between Docker and Local
```bash
# Stop Docker when working locally
docker-compose down

# Run locally on port 8097
cd AICodeReviewer.Web
dotnet run

# When done, restart Docker
docker-compose up -d
```

### Quick Reference Commands
```bash
# Check what's running on port 8097
netstat -an | findstr :8097

# Check Docker container status
docker-compose ps

# View Docker logs
docker-compose logs -f

# Stop all Docker containers
docker-compose down
```

- Resource usage

## Troubleshooting

### Common Issues

1. **Container won't start**
   ```bash
   # Check logs
   docker logs aicodereviewer
   
   # Check if port is in use
   netstat -an | grep 8097
   ```

2. **API key not working**
   - Ensure the API key is correctly set in environment variables
   - Verify the API key format (should start with `sk-or-v1-`)
   - Check OpenRouter account has sufficient credits

3. **SignalR connection issues**
   - Ensure nginx is properly configured for WebSocket support
   - Check firewall settings for WebSocket connections

4. **Build failures**
   ```bash
   # Clean build cache
   docker system prune -a
   
   # Rebuild with no cache
   docker build --no-cache -t aicodereviewer .
   ```

### Performance Tuning
- Adjust memory limits in `docker-compose.yml` if needed
- Monitor container resource usage: `docker stats`
- Consider using Docker Swarm for multi-node deployments

## Cleanup

```bash
# Stop and remove containers
docker-compose down

# Remove images
docker image rm aicodereviewer

# Remove volumes (WARNING: This will delete persisted data)
docker volume rm aicodereviewer_git-repos

# Complete cleanup
docker system prune -a
```

## Support

For issues related to:
- Docker configuration: Check this documentation
- Application functionality: Refer to the main README
- OpenRouter API: Visit https://openrouter.ai/docs