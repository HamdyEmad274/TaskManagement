# Block 5 — Mission: The Result Pattern

| | |
|---|---|
| **Mode** | Challenger — concept + contract given, implementation is yours |
| **Previous** | Block 4: Authentication (BCrypt, JWT) |
| **Builds** | `Result<T>` in Domain, then used in Block 6 services |
| **Validate** | Show your implementation, get reviewed before seeing the senior version |

---

## The Pain

You just finished Block 4. Authentication works. Now write `UserService.RegisterAsync`.

Your first instinct:

```csharp
public async Task<UserResponse> RegisterAsync(RegisterUserRequest request)
{
    var existing = await _userRepository.GetByEmailAsync(request.Email);
    if (existing != null)
        throw new Exception("Email already taken");

    // ...
}
```

And in the controller:

```csharp
try
{
    var result = await _userService.RegisterAsync(request);
    return Ok(result);
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}
```

**This is the junior pattern. Here's why it breaks down:**

`throw` is for exceptional situations — unexpected failures, things that *should not happen*.
"Email already taken" is not exceptional. It's a **predictable business outcome**.
Using exceptions for business logic means:
- Every caller must know to `try/catch` — or they get an unhandled crash
- You can't look at a method signature and know what can go wrong
- The compiler never tells you "you forgot to handle the error case"
- Performance: exceptions are 100-1000x more expensive than returning a value

A method that can fail in known ways should **say so in its return type**.

---

## The Concept

**Railway-Oriented Programming** — a method is a track. It goes one of two ways:

```
Input ──► [RegisterAsync] ──► Success track ──► UserResponse
                          └──► Failure track ──► "Email already taken"
```

The caller is **forced** to check which track they're on before using the value.
The compiler enforces this. No `try/catch`. No surprises.

This is implemented as a `Result<T>` type — a wrapper that holds **either** a success value **or** an error — never both, never neither.

```
Result<UserResponse>
  ├── IsSuccess = true  → Value = UserResponse { Id, Name, Email... }
  └── IsSuccess = false → Error = "Email already taken"
```

This pattern exists in:
- Rust: `Result<T, E>` (built into the language)
- F#: `Result<'T, 'TError>`
- C#: not built-in — you build it yourself (which is what you're doing now)
- Real .NET libraries: `ErrorOr`, `FluentResults`, `OneOf`

You're building the minimal version from scratch so you understand what those libraries actually do.

---

## Draw This Before You Code

No IDE yet. On paper or in your head — trace through this:

```
Scenario: user tries to register with an email that already exists.

WITH throw:
──────────────────────────────────────────────────────────────────
RegisterAsync()
  └── throws Exception("Email already taken")
        └── bubbles up the call stack
              └── controller catches it (if you remembered try/catch)
                    └── returns BadRequest
                          
What if another developer calls RegisterAsync and forgets try/catch?
→ Unhandled exception → 500 Internal Server Error → user sees nothing useful

WITH Result<T>:
──────────────────────────────────────────────────────────────────
RegisterAsync()
  └── returns Result<UserResponse>.Failure("Email already taken")
        └── controller receives the Result object
              └── checks result.IsSuccess  ← compiler won't let you skip this
                    └── IsSuccess=false → returns BadRequest(result.Error)

What if another developer calls RegisterAsync and ignores the Result?
→ They have to explicitly access result.Value
→ You control what happens when IsSuccess=false (guard it)
→ No invisible crash path
```

The difference is: **who is responsible for handling the failure?**
`throw` puts the burden on every caller, invisibly.
`Result<T>` makes the failure part of the contract — visible, enforced, handled at the call site.

---

## Where It Lives

```
src/TaskManagement.Domain/
└── Common/
    └── Result.cs     ← your task
```

It belongs in Domain because:
- Services in Application will return it
- Domain exceptions may eventually use it
- It has zero dependencies — pure C#, no NuGet needed

---

## The Contract

Your `Result<T>` must satisfy all of these requirements. How you implement them is up to you.

**Requirement 1 — Creation**
```csharp
// Creating a success
var ok = Result<UserResponse>.Success(userResponse);

// Creating a failure
var fail = Result<UserResponse>.Failure("Email already taken");
```

**Requirement 2 — Reading**
```csharp
if (result.IsSuccess)
    return Ok(result.Value);
else
    return BadRequest(result.Error);
```

**Requirement 3 — Non-nullable safety**
- `result.Value` when `IsSuccess = false` should not silently return null
- `result.Error` when `IsSuccess = true` should not silently return null
- How you enforce this is your design decision

**Requirement 4 — Works with void**
Some operations succeed or fail but return no value (e.g., `DeleteAsync`).
Your design must handle `Result` (non-generic) OR `Result<bool>` — your choice, justify it.

**Requirement 5 — Async compatible**
Must work naturally as `Task<Result<UserResponse>>` return type.
No special async wrapper needed — just verify your design allows it.

---

## Your Mission

**Step 1:** Implement `Result<T>` in `src/TaskManagement.Domain/Common/Result.cs`

**Step 2:** Implement `UserService.cs` in `src/TaskManagement.Application/Users/`
- `RegisterAsync(RegisterUserRequest)` → `Task<Result<UserResponse>>`
- `LoginAsync(LoginRequest)` → `Task<Result<string>>` (the JWT token)
- No `throw` for business logic — all known failures go through `Result`
- You are allowed to `throw` only for truly unexpected situations (null arguments, etc.)

**Step 3:** Implement `TaskService.cs` in `src/TaskManagement.Application/Tasks/`
- `CreateAsync(CreateTaskRequest, Guid userId)` → `Task<Result<TaskResponse>>`
- `GetUserTasksAsync(Guid userId)` → `Task<Result<IEnumerable<TaskResponse>>>`
- `UpdateStatusAsync(Guid taskId, UpdateTaskStatusRequest, Guid userId)` → `Task<Result<TaskResponse>>`

---

## Constraints

- No NuGet packages for this — pure C#
- `Result<T>` must be immutable — no public setters
- Services use constructor injection for their dependencies
- Services do NOT reference EF Core, BCrypt, or any Infrastructure type directly
- `UserService` dependencies: `IUserRepository`, `IPasswordHasher`, `IJwtTokenGenerator`
- `TaskService` dependencies: `ITaskRepository`

---

## Questions to Answer in Your Head Before You Code

These are not written questions — just think about them. If you can't answer them, you don't understand the concept yet:

1. Why is `Result<T>` a class and not a struct? (or — why might it be a struct? what's the trade-off?)
2. What happens if someone does `new Result<UserResponse>()` directly — bypassing your `Success`/`Failure` factory methods? How do you prevent it?
3. In `LoginAsync`, what are the two business failure cases? (there are exactly two)
4. In `UpdateStatusAsync`, what failure case exists that `CreateAsync` does NOT have?
5. Why does `TaskService` NOT need `IUserRepository` even though tasks belong to users?

---

## When You're Done

Post your implementation here. I will review:
- Does it satisfy all 5 requirements?
- Is the design safe (no silent nulls, no bypassing)?
- Are the service failure cases complete and correctly handled?
- What would a senior engineer change and why?

Then you'll see the production-grade version and understand exactly what you did differently.

---

**Start with `Result.cs`. Everything else depends on it.**
