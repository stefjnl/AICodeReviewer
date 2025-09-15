# Python Development Project

# Priority Order
1. Preserve working functionality above all else
2. Make minimal changes that solve the specific problem
3. Follow code quality standards
4. Optimize for maintainability

# Core Principles
- Provide clean, buildable, and maintainable Python code
- Break down large tasks into smaller components
- Ask clarifying questions when scope is unclear
- For small changes: make minimal, targeted edits only
- Only rewrite files when absolutely necessary for major structural changes
- Preserve existing working code whenever possible

# Maintain Conversation Context
- Reference specific files/functions discussed earlier in conversation
- Build on previous responses instead of starting from scratch
- When continuing work, start with "Building on [previous work]..."
- Reference previous work in the same conversation to maintain context

# Efficiency Guidelines
- When making small modifications, edit only the specific functions/classes needed
- Avoid regenerating entire files for minor tweaks
- Build incrementally on previous responses in the same conversation
- If unsure about scope, ask before making large changes

# Before Making Changes
- Always verify current working state first
- Test existing functionality before modifications
- Check container/deployment structure (Dockerfile, docker-compose, paths)
- Ask about environment specifics instead of assuming standard setups
- Understand the full scope before coding

# Python Code Standards
- Follow PEP 8 style guidelines
- Use type hints for function parameters and returns
- Prefer pathlib over os.path for file operations
- Use f-strings for string formatting
- Keep functions under 20 lines when possible
- Use specific exception types, not bare except:
- Prefer list/dict comprehensions when they improve readability

# Testing & Verification
- Test one thing at a time in actual deployment context
- Validate each change before moving to next
- Check container structure first (ls -la, python -c "import X")
- Look at working examples in codebase for reference
- Write unit tests for new functions when requested
- Revert to working state quickly when multiple attempts fail

# Code Quality Standards
- Validate all Python syntax before providing code
- Test import paths in target environment (container Python path, not host)
- Make incremental, testable changes (one import, then one function, not everything)
- Follow existing codebase patterns for imports and structure

# Never modify without permission:
- requirements.txt
- Dockerfile
- docker-compose.yml
- .env files
- database migration files
- configuration files in /config or /settings directories

# Dependency Management
- Add new packages to requirements.txt with exact versions
- Ask before adding heavyweight libraries (pandas, tensorflow, etc.)
- Specify production vs development dependencies clearly
- Check if similar functionality already exists in codebase

# Error Handling & Logging
- Use Python logging module, not print statements for debugging
- Include meaningful error messages with context
- Log at appropriate levels (DEBUG, INFO, WARNING, ERROR)
- Handle container-specific path and permission issues
- Validate input parameters and provide clear error messages

# Security Practices
- Never hardcode API keys, passwords, or secrets
- Use environment variables for sensitive configuration
- Validate and sanitize user inputs
- Ask about authentication/authorization patterns before implementing

# Communication Rules
- Ask instead of guessing about file structure, deployment, or patterns
- For new features: provide complete, tested solutions
- For modifications: make targeted, minimal changes to existing code
- Request clarification on environment setup when uncertain
- Explain reasoning behind structural changes
- When debugging fails after 2-3 attempts, suggest alternative approaches

# Never assume:
- Standard Python installation paths
- Default container configurations
- Existing import structures without verification
- Environment setup without checking
- Database connection strings or schemas
- Available system packages or libraries

# Always verify:
- Import statements work in target environment
- Code runs in actual deployment context
- Changes don't break existing functionality
- Container paths and Python environment
- Required environment variables are available
- Database connections and schemas match expectations