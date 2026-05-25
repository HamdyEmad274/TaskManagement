# TaskManagement API

A backend REST API built with **ASP.NET Core** following **Clean Architecture** principles.

---

## What is Clean Architecture?

Imagine 4 circles, one inside another:

```
┌─────────────────────────────┐
│           API               │  ← Talks to the outside world (HTTP)
│  ┌───────────────────────┐  │
│  │    Infrastructure     │  │  ← Talks to the database
│  │  ┌─────────────────┐  │  │
│  │  │   Application   │  │  │  ← Defines what the system can do
│  │  │  ┌───────────┐  │  │  │
│  │  │  │  Domain   │  │  │  │  ← The heart — pure business logic
│  │  │  └───────────┘  │  │  │
│  │  └─────────────────┘  │  │
│  └───────────────────────┘  │
└─────────────────────────────┘
```

**The golden rule:** arrows only point inward. Domain knows nothing about anyone. API knows everyone.

---

## Project Structure

```
src/
├── TaskManagement.Domain/          # Block 1
├── TaskManagement.Application/     # Block 2
├── TaskManagement.Infrastructure/  # Block 3 + 4
└── TaskManagement.API/             # Block 5 + 6
```

---

## Layer by Layer

### Domain — The Heart

> No external libraries. No EF Core. No ASP.NET. Pure C#.

This layer answers: **"What are the rules of this system?"**

| File | What it is |
|---|---|
| `BaseEntity.cs` | Every entity has an `Id` (Guid) and `CreatedAt` |
| `User.cs` | A user has Name, Email, hashed Password, and a Role |
| `TaskItem.cs` | A task has Title, Description, Status, Priority, and belongs to a User |
| `UserRole.cs` | Enum: `User`, `Admin` |
| `AppTaskStatus.cs` | Enum: `Pending`, `InProgress`, `Done` |
| `TaskPriority.cs` | Enum: `Low=1`, `Medium=2`, `High=3` |
| `IUserRepository.cs` | Contract: "I need something that can store and retrieve users" |
| `ITaskRepository.cs` | Contract: "I need something that can store and retrieve tasks" |
| `DomainException.cs` | A custom exception for business rule violations |

**Why no `new User { Name = "..." }`?**

All entities use a `static Create()` factory method:

```csharp
var user = User.Create("Hamdy", "hamdy@email.com", hashedPassword, UserRole.User);
```

This guarantees `Id` and `CreatedAt` are always set correctly. Properties have `private set` so nothing outside can change them directly.

---

### Application — The Use Cases

> Only depends on Domain. Defines operations, not implementations.

This layer answers: **"What can this system do?"**

```
Application/
├── Common/Interfaces/
│   ├── IPasswordHasher.cs       ← "I need something that can hash passwords"
│   └── IJwtTokenGenerator.cs    ← "I need something that can generate JWT tokens"
├── Users/
│   ├── DTOs/
│   │   ├── RegisterUserRequest  ← what comes IN from the client
│   │   ├── LoginRequest         ← email + password
│   │   └── UserResponse         ← what goes OUT (no PasswordHash!)
│   └── Interfaces/
│       └── IUserService.cs      ← Register, Login, GetById, GetAll, Delete
└── Tasks/
    ├── DTOs/
    │   ├── CreateTaskRequest    ← Title, Description, Priority
    │   ├── UpdateTaskStatusRequest ← just the new Status
    │   └── TaskResponse         ← what goes OUT to the client
    └── Interfaces/
        └── ITaskService.cs      ← Create, GetById, GetAll, UpdateStatus
```

**Why are `IPasswordHasher` and `IJwtTokenGenerator` in Application, not Infrastructure?**

Because the `UserService` (in a later block) needs to call them. If they were defined in Infrastructure, then Application would need to reference Infrastructure — which is the wrong direction. Application defines **what it needs**, Infrastructure **provides it**.

**What is a DTO?**

DTO = Data Transfer Object. It's a simple class that carries data between layers.
- `RegisterUserRequest` — what the client sends you
- `UserResponse` — what you send back to the client (carefully — no sensitive fields)

---

### Infrastructure — The Database

> Implements everything Application and Domain defined.

This layer answers: **"How does the system actually do it?"**

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs               ← EF Core entry point
│   ├── Configurations/
│   │   ├── UserConfiguration.cs      ← Maps User entity to "Users" table
│   │   └── TaskItemConfiguration.cs  ← Maps TaskItem to "Tasks" table + indexes
│   ├── Repositories/
│   │   ├── UserRepository.cs         ← Implements IUserRepository
│   │   └── TaskRepository.cs         ← Implements ITaskRepository
│   └── Seeders/
│       └── DbSeeder.cs               ← Creates the first Admin on startup
└── Extensions/
    └── InfrastructureServiceExtensions.cs  ← Registers everything in DI
```

**Why Fluent API (`UserConfiguration`) instead of `[Required]` attributes on the entity?**

```csharp
// ❌ Wrong — Domain now depends on EF Core
public class User : BaseEntity
{
    [Required]           // ← this is an EF Core / ASP.NET attribute
    [MaxLength(100)]     // ← Domain should not know this exists
    public string Name { get; private set; }
}

// ✅ Right — Domain is clean, EF Core config lives in Infrastructure
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
    }
}
```

**Why `AddInfrastructure()` in `Program.cs` and not the individual lines?**

```csharp
// ❌ Wrong — Program.cs knows too much about Infrastructure internals
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
// ...and more lines every time you add something

// ✅ Right — Program.cs just says "set up infrastructure"
builder.Services.AddInfrastructure(builder.Configuration);
```

Each layer registers itself. `Program.cs` stays clean. When you add `PasswordHasher` and `JwtTokenGenerator` in Block 4, you add them inside `InfrastructureServiceExtensions` — `Program.cs` doesn't change.

**Why `AddScoped` and not `AddSingleton`?**

| Lifetime | Meaning | Use for |
|---|---|---|
| `Singleton` | One instance for the entire app lifetime | Stateless helpers |
| `Scoped` | One instance per HTTP request | DbContext, Repositories |
| `Transient` | New instance every time it's requested | Lightweight, stateless |

DbContext is not thread-safe. Two simultaneous requests using the same DbContext = corruption. `Scoped` gives each request its own instance.

---

## The Dependency Flow — Why It Matters

```
API calls → IUserService (Application)
                ↓
         UserService uses → IUserRepository (Domain interface)
                                  ↓
                         UserRepository (Infrastructure) → AppDbContext → SQL Server
```

The `UserService` never mentions `UserRepository` by name. It only knows `IUserRepository`.
This means you can swap the entire database layer — Oracle, MongoDB, in-memory for tests — without touching a single line of business logic.

---

## Blocks Completed

| Block | Layer | Status |
|---|---|---|
| Block 1 | Domain | ✅ Done |
| Block 2 | Application | ✅ Done |
| Block 3 | Infrastructure (EF Core + Repos) | 🔄 In Progress |
| Block 4 | Authentication (JWT + BCrypt) | ⬜ Not Started |
| Block 5 | Service Implementations | ⬜ Not Started |
| Block 6 | API Controllers | ⬜ Not Started |

---

## Tech Stack

| Technology | Purpose |
|---|---|
| ASP.NET Core | Web framework |
| Entity Framework Core | ORM (Object-Relational Mapper) |
| SQL Server | Database |
| BCrypt.Net | Password hashing |
| JWT Bearer | Authentication tokens |
