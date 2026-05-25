# Block 2 — Application Layer

> **The Application layer defines what the system can do. It doesn't do it — it describes it.**

---

## Lesson 1 — What is the Application Layer For?

In Block 1 we built the domain — the business objects and rules. But we haven't said anything about what operations the system supports.

- Can a user register? What data do they send?
- Can a user log in? What do they get back?
- Can a user create a task? What fields are required?

The Application layer answers these questions by defining:

1. **Service interfaces** — the operations (`IUserService`, `ITaskService`)
2. **DTOs** — the data shapes flowing in and out
3. **Infrastructure contracts** — things the Application needs but can't implement itself (`IPasswordHasher`, `IJwtTokenGenerator`)

**What the Application layer does NOT contain:**

- No EF Core, no SQL — that's Infrastructure
- No HTTP, no Controllers — that's API
- No actual implementation of services — that's a later block

---

## Lesson 2 — What is a DTO?

DTO = **Data Transfer Object**. A simple class with only properties — no logic, no methods.

It answers: **"What shape does the data take when crossing a boundary?"**

The Client sends this to register:
```json
{
  "name": "Hamdy",
  "email": "hamdy@email.com",
  "password": "Secret123"
}
```

You don't pass that directly to your `User` entity. You map it through a DTO:

```csharp
public class RegisterUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**Why not just use the `User` entity directly?**

Three reasons:

1. **Security** — `User` has `PasswordHash`. If you used `User` as the request type, the client could potentially send a `PasswordHash` directly and bypass hashing.

2. **Shape mismatch** — `User` has `Id`, `CreatedAt`, `Role` etc. The client shouldn't send those. The DTO only has what the client should send.

3. **Separation** — The client's data format and your internal data model can evolve independently. If you rename `User.PasswordHash` to `User.HashedPassword`, nothing breaks for the client.

---

## Lesson 3 — Request vs Response DTOs

### Request DTOs — data coming IN

```csharp
// Client sends this to register
public class RegisterUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;   // plain text — hashing happens in the service
}

// Client sends this to log in
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Client sends this to create a task
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    // No UserId here — comes from the JWT token, not from the client
}

// Client sends this to change a task's status
public class UpdateTaskStatusRequest
{
    public AppTaskStatus Status { get; set; }
    // Only the status — this is a PATCH-style operation
}
```

### Response DTOs — data going OUT

```csharp
// What we send back after register / get user
public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;   // string, not UserRole enum
    public DateTime CreatedAt { get; set; }
    // NO PasswordHash — never send sensitive data
}

// What we send back for a task
public class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;    // string, not enum
    public string Priority { get; set; } = string.Empty;  // string, not enum
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; }
}
```

**Why is `Role` a `string` in the response instead of `UserRole` enum?**

Because the client receives JSON. If `Role` is an enum, JSON serialization returns `1` or `0` (the integer value). The client has to know what `1` means. If `Role` is a string, the client receives `"Admin"` or `"User"` — readable, self-documenting.

The conversion from `UserRole.Admin` → `"Admin"` happens in the service layer when building the response.

**Why no `UserId` in `CreateTaskRequest`?**

Security. The `UserId` comes from the JWT token the user is authenticated with — not from the request body. If the client could send a `UserId`, they could create tasks on behalf of other users. The service extracts the `userId` from the token (which the API layer passes down) and uses that.

---

## Lesson 4 — Service Interfaces

Service interfaces define the use cases — the operations the system supports.

```csharp
public interface IUserService
{
    Task<UserResponse> RegisterAsync(RegisterUserRequest request);
    Task<string> LoginAsync(LoginRequest request);
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task DeleteAsync(Guid id);
}
```

```csharp
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, Guid userId);
    Task<TaskResponse> GetByIdAsync(Guid id, Guid userId);
    Task<IEnumerable<TaskResponse>> GetAllByUserAsync(Guid userId);
    Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, Guid userId);
}
```

**Why does `LoginAsync` return `string` instead of `UserResponse`?**

Login returns a JWT token — a string. The client stores this token and sends it with every subsequent request in the `Authorization` header. There's no need to return the full user object.

**Why does every `ITaskService` method take a `Guid userId`?**

Every task operation must verify ownership. A user should only see their own tasks, update their own tasks, etc. The `userId` comes from the JWT token, passed down from the Controller.

Without this, a user could call `GetByIdAsync(someOtherUsersTaskId)` and see data they shouldn't.

**Why interfaces? Why not just implement directly?**

Same reason as `IUserRepository` in Domain — **testability and replaceability**.

With an interface, you can:
- Write unit tests using a fake implementation (`MockUserService`) without hitting a real database
- Swap the implementation later (e.g., add caching) without changing anything that uses it
- Register the correct implementation in DI and swap it per environment

---

## Lesson 5 — Infrastructure Contracts in Application

```csharp
// Application/Common/Interfaces/IPasswordHasher.cs
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

// Application/Common/Interfaces/IJwtTokenGenerator.cs
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
```

These are **not** implementations. They're contracts.

**Why does `IPasswordHasher` live in Application and not Infrastructure?**

The `UserService` (which will be in Application) needs to hash passwords when a user registers. If `IPasswordHasher` was defined in Infrastructure, then Application would need to reference Infrastructure — the wrong direction.

```
// ❌ Wrong direction
Application → Infrastructure (to get IPasswordHasher)

// ✅ Correct
Application defines IPasswordHasher
Infrastructure implements IPasswordHasher
Application depends on the interface, not the implementation
```

**Why does `IJwtTokenGenerator` take a `User` entity instead of separate parameters?**

```csharp
// Option 1 — separate parameters
string GenerateToken(Guid userId, string email, string role);

// Option 2 — the full entity (what we chose)
string GenerateToken(User user);
```

A JWT token needs `userId`, `email`, and `role` at minimum. Option 1 works now, but if you add a new claim later (like `userName`), you have to change the interface signature everywhere. Option 2 passes the whole object — adding a new claim just means reading another property inside the implementation, no signature change needed.

---

## Lesson 6 — The Dependency Inversion Principle in Action

Here's the complete picture of how these layers connect:

```
Controller (API)
    │
    │ calls
    ▼
IUserService (Application interface)
    │
    │ implemented by
    ▼
UserService (Application — Block 5)
    │
    │ uses
    ├──► IUserRepository (Domain interface)
    │        │ implemented by UserRepository (Infrastructure)
    │
    ├──► IPasswordHasher (Application interface)
    │        │ implemented by PasswordHasher (Infrastructure — Block 4)
    │
    └──► IJwtTokenGenerator (Application interface)
             │ implemented by JwtTokenGenerator (Infrastructure — Block 4)
```

Nothing in Application imports anything from Infrastructure. Infrastructure imports Application to implement its interfaces. This is the dependency flowing inward.

---

## Lesson 7 — The Folder Structure and Why

```
Application/
├── Common/
│   └── Interfaces/
│       ├── IPasswordHasher.cs       ← cross-cutting contracts
│       └── IJwtTokenGenerator.cs
├── Users/
│   ├── DTOs/
│   │   ├── RegisterUserRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── UserResponse.cs
│   └── Interfaces/
│       └── IUserService.cs
└── Tasks/
    ├── DTOs/
    │   ├── CreateTaskRequest.cs
    │   ├── UpdateTaskStatusRequest.cs
    │   └── TaskResponse.cs
    └── Interfaces/
        └── ITaskService.cs
```

**Feature folders, not type folders.**

A common mistake is organizing by type:
```
Application/
├── DTOs/           ← all DTOs together
├── Interfaces/     ← all interfaces together
```

This doesn't scale. When you have 20 features, you have 40 DTOs in one folder and you have to search to find which DTO belongs to which feature.

Feature folders (`Users/`, `Tasks/`) group everything related to one feature together. You open `Users/` and you see everything related to users — DTOs, interfaces, and later the implementation.

---

## Common Mistakes

| Mistake | Why it's wrong | Fix |
|---|---|---|
| Using the `User` entity as a request DTO | Exposes internal fields, breaks encapsulation | Create a dedicated `RegisterUserRequest` |
| Returning `User` entity directly from service | Exposes `PasswordHash` and internal state | Always map to `UserResponse` before returning |
| Putting implementation in Application | Application only defines contracts | Implementations go in Infrastructure (Block 3/4) or a Service class (Block 5) |
| `RegisterAsync` returns `Task` (void) | Caller has no confirmation of what was created | Return `Task<UserResponse>` |
| `ITaskService` methods return bare `Task` | Service can't communicate results back | Return `Task<TaskResponse>` or `Task<IEnumerable<TaskResponse>>` |
| Passing `UserId` in the task request body | Client could impersonate another user | Extract `userId` from JWT in the Controller, pass it to the service |

---

## What We Built

```
Application/
├── Common/Interfaces/
│   ├── IPasswordHasher.cs           ← Hash(password), Verify(password, hash)
│   └── IJwtTokenGenerator.cs        ← GenerateToken(User user)
├── Users/DTOs/
│   ├── RegisterUserRequest.cs       ← Name, Email, Password
│   ├── LoginRequest.cs              ← Email, Password
│   └── UserResponse.cs              ← Id, Name, Email, Role (string), CreatedAt
├── Users/Interfaces/
│   └── IUserService.cs              ← Register, Login, GetById, GetAll, Delete
├── Tasks/DTOs/
│   ├── CreateTaskRequest.cs         ← Title, Description, Priority (enum)
│   ├── UpdateTaskStatusRequest.cs   ← Status (enum) only
│   └── TaskResponse.cs              ← Id, Title, Description, Status/Priority (strings), CreatedAt, UserId
└── Tasks/Interfaces/
    └── ITaskService.cs              ← Create, GetById, GetAllByUser, UpdateStatus (all with userId)
```
