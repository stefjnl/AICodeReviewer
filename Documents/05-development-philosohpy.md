## Development Philosophy

**Minimalist Approach**: 15-20 minute implementations, 2-4 files maximum
**Ship Fast**: Build functional solution, avoid architectural gold-plating
**Critical Evaluation**: Question every abstraction and interface
**Business Focus**: Solve real problems, not theoretical ones

### Recent Success Pattern
- **Problem**: Provider switching required config file edits
- **Solution**: Single override point in `ConfigurableAIProvider` with singleton service
- **Result**: Runtime switching in 15 minutes, 4 files touched
- **Key Learning**: Find the minimal intervention point, avoid cascading changes

## Technical Standards

**Clean Architecture**: Proper dependency flow without over-abstraction
**Single Responsibility**: Fat services acceptable if contained to single layer
**Interface Segregation**: Create interfaces only when multiple implementations exist
**Dependency Inversion**: Application services calling Infrastructure through Domain interfaces

## Next User Story Guidelines

**Scope**: 15-30 minute implementations maximum
**Files**: 2-4 files changed/created
**Testing**: Verify endpoints work, don't build comprehensive test suites
**Documentation**: Code comments only, no formal documentation

**Avoid**:
- Multi-week implementation plans
- Complex component hierarchies
- Multiple abstraction layers
- Comprehensive monitoring systems
- Enterprise-grade configuration management

## Available System Features

- Docker containerization with 3-service setup
- PostgreSQL (configured, using in-memory for development speed)
- Comprehensive REST API with health checks
- Vanilla JavaScript frontend with modular architecture
- Volume-mounted AI instruction files
- Review workflow with approval/rejection

## Critical Success Factors

1. **Validate assumptions** about existing system capabilities
2. **Find minimal intervention points** rather than rebuilding systems
3. **Question complexity** - if it takes more than 30 minutes, it's probably over-engineered
4. **Test practically** - does it work end-to-end?
5. **Ship incrementally** - working solution beats perfect architecture

Focus on building practical solutions that demonstrate senior-level engineering judgment: knowing when to stop optimizing and ship working code.