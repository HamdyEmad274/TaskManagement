# Block 6 — API Controllers + Global Error Handling
> **Model:** Challenger — concept first, you implement, then review.

---

## Step 1: Read This Before You Touch Any Code

### The Pain

Your Services are done. `UserService`, `TaskService` — they return `Result<T>`, they validate ownership, they save to DB.

But right now **nothing can call them over HTTP**.

Open Postman, hit `POST /api/users/register` — you get 404. The app doesn't know that endpoint exists.

That's Pain #1 — **no HTTP entry point**.

Now imagine you build the Controllers. They work. But every Controller ends up looking like this:

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterUserRequest request)
{
    try
    {
        var result = await _userService.RegisterAsync(request);
        if (result.IsFailure) return BadRequest(result.Error);
        return Ok(result.Value);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);  // leaks internals
    }
}

[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    try
    {
        var result = await _userService.LoginAsync(request);
        if (result.IsFailure) return Unauthorized(result.Error);
        return Ok(result.Value);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);  // same boilerplate again
    }
}
```

That's Pain #2 — **every action has the same try/catch + same Result mapping + leaks internal exception messages in 500 responses**.

10 endpoints = 10 identical try/catch blocks.

Pain #3: **What HTTP status do you return for each failure?**
- "User not found" → 404? 400? 401?
- "Email already taken" → 400? 409?
- "Not authorized to update this task" → 401? 403?
- "Task not found" → 404
- Unexpected DB crash → 500 (but don't leak the stack trace)

These decisions need to be consistent across the entire API — not made ad-hoc in every Controller.

---

## Step 2: The Concepts

### Concept 1 — Controller Responsibility

A Controller's ONLY job:
1. Receive the HTTP request
2. Extract what the Service needs (body, route params, userId from JWT)
3. Call the Service
4. Map the Result to an HTTP response
5. Return

It does NOT:
- Contain business logic
- Directly touch Repositories or DbContext
- Handle exceptions manually (Pain #2's solution: move that out)

### Concept 2 — Extracting userId from JWT

When a user sends a request with a JWT token, ASP.NET's Authentication middleware reads the token and puts the claims into `HttpContext.User`.

You don't parse the token yourself. You read the claim:

```csharp
// The claim name is the ClaimTypes constant — same one you put IN when generating the token
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

This only works on endpoints decorated with `[Authorize]`.

### Concept 3 — HTTP Status Code Semantics

These are not optional — they're the language of REST:

| Scenario | Status | Meaning |
|---|---|---|
| Success with body | 200 OK | Here's what you asked for |
| Created something new | 201 Created | Resource created, here it is |
| Success, no body | 204 No Content | Done, nothing to return |
| Validation failed, malformed input | 400 Bad Request | You sent something wrong |
| Not authenticated | 401 Unauthorized | Who are you? Send a token. |
| Authenticated but no permission | 403 Forbidden | I know who you are, you can't do this |
| Resource doesn't exist | 404 Not Found | Doesn't exist |
| Conflict (duplicate) | 409 Conflict | Already exists |
| Our fault | 500 Internal Server Error | Something broke on our side |

### Concept 4 — Global Exception Middleware

Instead of try/catch in every Action, you write ONE middleware that sits at the top of the pipeline and catches anything that bubbles up.

```
Request
  → GlobalExceptionMiddleware (catches unhandled exceptions)
    → Authentication
      → Authorization
        → Controller Action  ← if this throws, the middleware catches it
```

The Controller never needs try/catch for unexpected exceptions again.  
`Result.IsFailure` still handles the expected failures — the middleware only handles the *unexpected* ones.

### Concept 5 — Problem Details (RFC 7807)

When an API returns an error, it should return a consistent shape. The industry standard is RFC 7807 — Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 400,
  "detail": "Email already exists.",
  "instance": "/api/users/register"
}
```

ASP.NET Core has built-in support for this with `ProblemDetails`.  
Senior APIs return this shape — not raw strings.

---

## Step 3: The Contracts (What You Build)

### File Locations

```
src/TaskManagement.API/
├── Controllers/
│   ├── UsersController.cs
│   └── TasksController.cs
└── Middleware/
    └── GlobalExceptionMiddleware.cs
```

### UsersController Contract

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Constructor: inject IUserService

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken ct);
    // → 201 Created on success
    // → 409 Conflict if email already exists

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request);
    // → 200 OK with token on success
    // → 401 Unauthorized on failure

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id);
    // → 200 OK with UserResponse
    // → 404 Not Found if not exists

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll();
    // → 200 OK with list

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id);
    // → 204 No Content on success
    // → 404 Not Found if not exists
}
```

### TasksController Contract

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]                         // ← ALL task endpoints require auth
public class TasksController : ControllerBase
{
    // Constructor: inject ITaskService

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken ct);
    // → 201 Created
    // → extract userId from JWT claims

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id);
    // → 200 OK or 404 Not Found
    // → ownership: pass userId from JWT

    [HttpGet]
    public async Task<IActionResult> GetAll();
    // → 200 OK with user's tasks only
    // → extract userId from JWT

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken ct);
    // → 200 OK on success
    // → 404 if not found
    // → 403 if not owner
}
```

### GlobalExceptionMiddleware Contract

```csharp
public class GlobalExceptionMiddleware
{
    // Constructor: inject RequestDelegate and ILogger<GlobalExceptionMiddleware>

    public async Task InvokeAsync(HttpContext context);
    // → catches any unhandled exception
    // → logs it
    // → returns 500 ProblemDetails response
    // → never leaks exception details to the client
}
```

---

## Step 4: Your Mission

**Order matters. Build in this sequence:**

### Mission A — GlobalExceptionMiddleware
Build it first. Register it in Program.cs as the FIRST middleware (before Authentication).  
Test: throw `new Exception("test")` from a temp endpoint — verify you get a clean 500 JSON back, not a stack trace page.

### Mission B — UsersController
Build `Register` and `Login` first — these don't need `[Authorize]`.  
Test with Postman: register a user, login, get a token back.

### Mission C — TasksController  
Build `Create` first.  
The challenge: extract userId from the JWT. Hint: `User.FindFirstValue(...)`.  
Test: use the token from Mission B to create a task.

### Mission D — Remaining endpoints
`GetById`, `GetAll`, `Delete` on Users.  
`GetById`, `GetAll`, `UpdateStatus` on Tasks.

---

## Questions to Answer Before You Code

1. `[Authorize]` on the class vs on individual methods — what's the difference?
2. The `Register` endpoint — should it return `200 OK` or `201 Created`? What's the difference and why does it matter?
3. For `TasksController`, the `[Authorize]` is on the class. But what if you later add a `GET /api/tasks/public` that shouldn't need auth — how do you handle that?
4. What claim name did `JwtTokenGenerator` use when it put the userId INTO the token? (Check Block 4 code — this is what you'll use to read it back.)
5. The `GlobalExceptionMiddleware` logs the exception. Should it log `ex.Message` only, or `ex.ToString()`? What's the difference?

---

## What NOT to Do

- ❌ Don't put `try/catch` in Controller Actions for unexpected exceptions — that's the Middleware's job
- ❌ Don't inject `IUserRepository` directly into Controllers — always go through the Service
- ❌ Don't return `Ok(result.Error)` on failure — map to the correct status code
- ❌ Don't expose stack traces in 500 responses — log them server-side, return a clean message to the client
- ❌ Don't forget `AddControllers()` and `MapControllers()` in Program.cs (check if they're already there)

---

## When You're Done

Tell me "Block 6 done, review it" and paste or describe:
1. How you mapped Result failures to HTTP status codes — did you use if/else or something smarter?
2. How you extracted the userId from JWT
3. What your GlobalExceptionMiddleware returns on 500

That's the review session.
