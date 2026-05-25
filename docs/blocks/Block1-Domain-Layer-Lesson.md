# Block 1 — Domain Layer

> **The domain is the heart. It knows nothing about databases, HTTP, or any library. It only knows business.**

---

## Lesson 1 — What is Clean Architecture?

Most developers write code like this:

```
Controller → calls database directly
           → runs business logic inline
           → returns response
```

It works. Until you need to:
- Switch from SQL Server to PostgreSQL
- Write unit tests without a real database
- Reuse the same business logic in a background job

Then everything breaks because everything is tangled together.

**Clean Architecture separates concerns into layers:**

```
┌─────────────────────────────────┐
│             API                 │  HTTP, Controllers, Auth middleware
│  ┌───────────────────────────┐  │
│  │      Infrastructure       │  │  EF Core, SQL Server, BCrypt, JWT
│  │  ┌─────────────────────┐  │  │
│  │  │     Application     │  │  │  Use cases, DTOs, Service interfaces
│  │  │  ┌───────────────┐  │  │  │
│  │  │  │    Domain     │  │  │  │  Entities, rules, repository interfaces
│  │  │  └───────────────┘  │  │  │
│  │  └─────────────────────┘  │  │
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

**The rule: dependencies only point inward.**

- Domain knows nothing about anyone
- Application knows Domain only
- Infrastructure knows Domain + Application
- API knows everyone

This means you can replace Infrastructure entirely (swap SQL Server for MongoDB) and Domain + Application don't change at all.

---

## Lesson 2 — What Goes in the Domain?

Ask yourself: **"Is this a business concept or a technical detail?"**

| Concept | Layer |
|---|---|
| A User has an email | Domain ✅ |
| A Task has a status | Domain ✅ |
| Save to SQL Server | Infrastructure ❌ |
| Hash a password | Infrastructure ❌ |
| Return JSON | API ❌ |

Domain contains:
- **Entities** — the objects your business cares about
- **Enums** — fixed sets of values (status, role, priority)
- **Repository Interfaces** — contracts saying "I need something that stores users"
- **Domain Exceptions** — custom errors for business rule violations

---

## Lesson 3 — BaseEntity

Every entity in this system needs an `Id` and a `CreatedAt`. Instead of repeating that in every class, we create a base:

```csharp
public class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
}
```

**Why `Guid` instead of `int`?**

`int` IDs are sequential — `1, 2, 3...`. This leaks information (a user can guess how many records exist) and causes problems in distributed systems where two servers might generate the same number.

`Guid` is globally unique. `Guid.NewGuid()` generates something like `3f2504e0-4f89-11d3-9a0c-0305e82c3301` — impossible to guess, safe to generate anywhere.

**Why `protected set` and not `public set`?**

`protected` means only `BaseEntity` itself and classes that **inherit** it (`User`, `TaskItem`) can set these properties. Outside code — like a Controller or Service — cannot do `user.Id = someOtherId`. The domain controls its own data.

---

## Lesson 4 — Entities and the Factory Method Pattern

Here's the naive approach:

```csharp
// ❌ Anyone can create a broken User
var user = new User();
// Id is Guid.Empty, CreatedAt is DateTime.MinValue
// Name is null, Email is null
// This is a broken object
```

The fix is a **static factory method**:

```csharp
public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    private User() { }  // only EF Core can use this

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
```

**Why `private set` on all properties?**

Once a User is created via `Create()`, nothing outside the class can change its properties directly:

```csharp
user.Email = "hacked@evil.com";  // ❌ compile error — private set
```

If you need to change something later (like updating a password), you add a specific method:

```csharp
public void ChangePassword(string newHash) => PasswordHash = newHash;
```

This is intentional. Every change is explicit, named, and controllable.

**Why `private User() { }`?**

EF Core needs a parameterless constructor to rebuild entities when reading from the database. It uses reflection internally. Making it `private` means EF Core can still use it (it bypasses access modifiers via reflection), but your own code can't accidentally call `new User()`.

**Why `static Create()` instead of a normal constructor?**

A constructor named `new User(...)` is generic. `User.Create(...)` is intentional — it reads like "create a new user with these values." More importantly, if you need validation before creation (e.g., check email format), it goes inside `Create()`, not scattered in every service that creates users.

---

## Lesson 5 — Enums and Why Their Values Matter

```csharp
public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3
}
```

**Why explicit values `1, 2, 3`?**

Without explicit values, C# defaults to `Low=0, Medium=1, High=2`. Zero is also the default value of any uninitialized enum variable. That means a task with no priority set would be `Low` — silently wrong.

With `Low=1`, a task with no priority set would be `0` which doesn't match any enum value — obvious error, easy to debug.

**Why `High=3` specifically?**

Because later in `TaskRepository`, we sort tasks like this:

```csharp
.OrderByDescending(t => t.Priority)
```

`OrderByDescending` sorts numbers highest-first. So `High=3` comes before `Medium=2` before `Low=1`. This isn't an accident — it was designed this way in Block 1 to serve the query in Block 3.

---

## Lesson 6 — Repository Interfaces (Dependency Inversion)

```csharp
// In Domain
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task DeleteAsync(User user);
}
```

This interface lives in **Domain**. The actual implementation (`UserRepository` with EF Core) lives in **Infrastructure**.

**Why define the interface in Domain?**

The `UserService` (Application layer) needs to save and retrieve users. It depends on `IUserRepository`. If `IUserRepository` was defined in Infrastructure, then Application would need to reference Infrastructure — the wrong direction.

By putting `IUserRepository` in Domain, Application can depend on Domain (which it already does) and get what it needs. Infrastructure then **implements** the interface. The dependency flows correctly.

This is the **Dependency Inversion Principle** — high-level code (Application) doesn't depend on low-level code (Infrastructure). Both depend on abstractions (interfaces in Domain).

```
Application → IUserRepository (Domain interface)
                    ↑
Infrastructure implements it
```

**Why `Task<User?>` with the `?`?**

`GetByIdAsync` might not find a user with the given ID. Instead of throwing an exception (expensive, noisy) or returning a default `User` object (dangerous), we return `null`. The `?` makes this explicit — the caller is forced to handle the null case.

**Why async (`Task<>`) everywhere?**

Database calls are I/O operations — they block while waiting for the disk or network. `async/await` releases the thread while waiting, so your server can handle other requests instead of just waiting. Under load, this is the difference between handling 100 requests/second and 10,000.

---

## Lesson 7 — DomainException

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

Simple but important. When a business rule is violated, you throw this:

```csharp
if (string.IsNullOrEmpty(email))
    throw new DomainException("Email cannot be empty");
```

**Why a custom exception instead of just `throw new Exception(...)`?**

In the API layer, you can catch `DomainException` specifically and return `400 Bad Request` (client error). A generic `Exception` gets caught and returned as `500 Internal Server Error` (server error). Different problem, different HTTP status code.

```csharp
// In API middleware later:
catch (DomainException ex)
{
    return BadRequest(ex.Message);   // 400 — client did something wrong
}
catch (Exception ex)
{
    return StatusCode(500, "Something went wrong");  // 500 — we did something wrong
}
```

---

## Common Mistakes

| Mistake | Why it's wrong | Fix |
|---|---|---|
| Putting `[Required]` on Domain entities | Domain now depends on EF Core / ASP.NET | Use Fluent API in Infrastructure Configuration |
| Using `int` IDs | Sequential, guessable, distributed system problems | Use `Guid` |
| Public setters on all properties | Anything can mutate your entity from anywhere | Use `private set`, change only via explicit methods |
| Not using async in repository interfaces | Blocks threads on I/O, kills performance under load | Always `Task<>` for any DB operation |
| Forgetting `Id` and `CreatedAt` in `Create()` | Object saved with `Guid.Empty` as ID | Always set them in the factory method |
| Enum without explicit values | Default value `0` is ambiguous | Start from `1`, assign values that serve your sorting needs |

---

## What We Built

```
Domain/
├── Entities/
│   ├── BaseEntity.cs       ← Guid Id, DateTime CreatedAt (protected set)
│   ├── User.cs             ← private set + static Create() + private constructor
│   └── TaskItem.cs         ← same pattern + UpdateStatus() method
├── Enums/
│   ├── UserRole.cs         ← User, Admin
│   ├── AppTaskStatus.cs    ← Pending, InProgress, Done
│   └── TaskPriority.cs     ← Low=1, Medium=2, High=3
├── Interfaces/Repositories/
│   ├── IUserRepository.cs  ← Task<User?> return types, fully async
│   └── ITaskRepository.cs  ← Task<TaskItem?> return types, fully async
└── Exceptions/
    └── DomainException.cs  ← Custom exception for business rule violations
```
