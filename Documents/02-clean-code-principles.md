## Clean Architecture Layer Responsibilities

### 🏛️ **Domain Layer**
**What:** Core business rules and entities
- **Entities** - Business objects with identity
- **Value Objects** - Immutable business concepts  
- **Domain Services** - Business logic that doesn't belong to a single entity
- **Interfaces** - Contracts for external dependencies
- **Business Rules** - Validation, calculations, domain logic

**Example:** `ProviderConfiguration.IsValid()`, `User`, `OrderTotal.Calculate()`

### 🔧 **Application Layer** 
**What:** Use cases and orchestration
- **Services** - Coordinate between domain and infrastructure
- **Use Cases** - Application-specific business flows
- **DTOs** - Data transfer objects for API contracts
- **Orchestration** - Combine multiple domain services
- **Transaction Management** - Cross-service coordination

**Example:** `CreateUserService`, `ProcessOrderUseCase`, `ProviderManagementService`

### 🌐 **Infrastructure Layer**
**What:** External concerns and implementation details
- **Repositories** - Data access implementations
- **External APIs** - Third-party service clients
- **File System** - File operations, logging
- **Frameworks** - Entity Framework, HTTP clients
- **Configuration** - Settings, connection strings

**Example:** `SqlUserRepository`, `EmailService`, `ClaudeApiClient`

### 🚫 **Key Rule**
- **Domain** = No dependencies on anything
- **Application** = Depends only on Domain
- **Infrastructure** = Implements Domain interfaces, no business logic