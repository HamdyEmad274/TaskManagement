# TaskManagement — Master Plan
> **Version:** 2.0  
> **Last Updated:** May 2026  
> **Purpose:** Context file — one read is enough to know everything about this project, where we are, and where we're going.

---

## The Goal

Build a developer who companies fight to hire — not because he knows a stack, but because he understands **why systems are built the way they are**.

The target is a Senior Full Stack Engineer (.NET + Angular) who:
- Understands what happens under the hood, not just how to call an API
- Can shift to any stack because he thinks in principles, not syntax
- Has built real pain into real systems and relieved it with the right tools
- Knows when NOT to use a pattern as much as when to use it

---

## Why One Project, Not Many

Multiple isolated projects teach **topics**.  
One evolving project teaches **judgment**.

Every tool introduced here will be introduced because the project **hurts without it**.  
The pain comes first. The tool relieves it. That's how the value sticks.

```
CQRS         ← introduced when Services become fat and unmanageable
RabbitMQ     ← introduced when sync calls create bottlenecks and coupling
Redis        ← introduced when the DB is hit on every request unnecessarily
Hangfire     ← introduced when you need "do this later" and can't block the request
Docker       ← introduced when "works on my machine" becomes a real problem
Testing      ← introduced when a refactor breaks something you didn't expect
```

---

## What the Project Becomes

It starts as a simple Task Manager. It grows into a **WorkFlow Platform**:

```
WorkFlow Platform
├── Users & Teams                ← Block 1-5  (done)
├── Projects & Tasks             ← Block 1-5  (done)
├── API Layer + Error Handling   ← Block 6
├── CQRS + MediatR               ← Block 7
├── Validation Pipeline          ← Block 8
├── Redis Caching                ← Block 9
├── Notification System          ← Block 10  (RabbitMQ)
├── Background Jobs              ← Block 11  (Hangfire)
├── Structured Logging           ← Block 12  (Serilog)
├── Health + Monitoring          ← Block 13
├── Containerization + CI/CD     ← Block 14  (Docker)
├── Testing                      ← Block 15  (xUnit)
├── Microservices Concepts       ← Block 16
├── Angular Frontend             ← Block 17
└── Full Stack Integration       ← Block 18
```

---

## Learning Model — The Challenger Model

**Old way (Blocks 1-4):** Lesson written → you read → you implement following the guide.  
**New way (Block 5+):** Pain explained → concept given → contract given → **you implement** → review → senior version.

The review after your attempt IS the lesson. It's written about your code, not generic examples.  
That's why it sticks.

Each block has:
1. **The Pain** — what breaks without this
2. **The Concept** — the mental model
3. **The Mission** — what you build (contract only, no implementation)
4. **The Review** — what you got right, what to fix, and why
5. **The HTML Lesson** — Arabic deep-dive in `docs/lessons/`

---

## The Full Roadmap

### ✅ Block 1 — Domain Layer
**Pain:** No structure → business logic scattered everywhere  
**Concepts:** Clean Architecture, DDD Entities, Factory Methods, Enums with explicit values, Repository Interfaces (Dependency Inversion)  
**Built:**
- `BaseEntity` (Guid Id, CreatedAt)
- `User`, `TaskItem` with private setters + static `Create()`
- `UserRole`, `AppTaskStatus`, `TaskPriority` enums
- `IUserRepository`, `ITaskRepository`
- `DomainException`

**Key decisions:**
- Guid over int (security + distributed systems)
- `private set` for encapsulation
- `AppTaskStatus` not `TaskStatus` (conflicts with `System.Threading.Tasks.TaskStatus`)
- Interfaces in Domain, not Infrastructure (Dependency Inversion)

---

### ✅ Block 2 — Application Layer
**Pain:** No clear definition of what the system *can do* → logic ends up in controllers  
**Concepts:** Use Cases, DTOs (Data Transfer Objects), Service Interfaces, Separation of Concerns  
**Built:**
- `IUserService` (Register, Login, GetById, GetAll, Delete)
- `ITaskService` (Create, GetById, GetAll, UpdateStatus)
- `IPasswordHasher`, `IJwtTokenGenerator` in Application (not Infrastructure)
- DTOs: `RegisterUserRequest`, `LoginRequest`, `UserResponse`, `CreateTaskRequest`, `TaskResponse`

**Key decisions:**
- Auth interfaces live in Application so `UserService` can use them without referencing Infrastructure
- DTOs never expose sensitive fields (no `PasswordHash` in `UserResponse`)

---

### ✅ Block 3 — Infrastructure Layer
**Pain:** No real DB → nowhere to store anything  
**Concepts:** EF Core Change Tracker, Fluent API vs Data Annotations, Repository Pattern, Migrations, DI Lifetimes  
**Built:**
- `AppDbContext` with `DbSet<User>`, `DbSet<TaskItem>`
- `UserConfiguration`, `TaskItemConfiguration` (Fluent API)
- `UserRepository`, `TaskRepository`
- `DbSeeder` for default Admin user
- `InfrastructureServiceExtensions` for DI registration
- First migration + DB creation

**Key decisions:**
- Fluent API in Infrastructure, NOT attributes on Domain entities
- Enums stored as string (`HasConversion<string>()`) — reordering members won't corrupt DB data
- All services `AddScoped` — DbContext is not thread-safe, must be per-request
- `ApplyConfigurationsFromAssembly` — auto-discovers all configurations

---

### ✅ Block 4 — Authentication
**Pain:** Anyone can call any endpoint → no identity, no security  
**Concepts:** Password storage evolution (PlainText → MD5 → Salted → BCrypt), JWT anatomy, Sessions vs Tokens, Revocation problem, Middleware order  
**Built:**
- `PasswordHasher` using BCrypt (work factor 12)
- `JwtTokenGenerator` using `Microsoft.IdentityModel.Tokens`
- `JwtSettings` bound from `appsettings.json`
- JWT middleware in `Program.cs` with correct `UseAuthentication → UseAuthorization` order

**Key decisions:**
- BCrypt work factor 12: ~400ms per hash — too slow for brute force, unnoticeable to real users
- `ClockSkew = TimeSpan.Zero` — no grace period after token expiry
- JWT payload contains only: userId, role, expiry — never sensitive data
- Same error message for "user not found" and "wrong password" — prevents user enumeration

---

### ✅ Block 5 — Result Pattern + Unit of Work
**Pain:** `throw` for business failures → invisible contract, expensive, swallowed by wrong catch blocks  
**Pain 2:** `AddAsync` stages but never commits → data disappears after request  
**Concepts:** Railway-Oriented Programming, Domain Invariants, Unit of Work, EF Core Change Tracker lifecycle, DI Lifetime consistency, IDOR vulnerability  
**Built:**
- `Result<T> : Result` in Domain/Common — inheritance, constructor guards, `Value` guard
- `UserService` — `RegisterAsync`, `LoginAsync`
- `TaskService` — `CreateAsync`, `UpdateStatusAsync`
- `IUnitOfWork` in Domain, `UnitOfWork` in Infrastructure
- DI registration for all services

**Key decisions:**
- `Result<T> : Result` inheritance → supports both void (`Task<Result>`) and value (`Task<Result<T>>`) returns
- `protected internal` constructor → forces use of `Success()`/`Failure()` factory methods
- `Value` throws on failed result → loud failure, not silent null
- Ownership check in `UpdateStatusAsync` → prevents IDOR
- `CancellationToken` threaded through → cancels DB operation when client disconnects
- `IUnitOfWork.SaveChangesAsync()` in Service, not in Repository → preserves atomicity

**What was fixed in review:**
- `Verify(hash, password)` → corrected to `Verify(password, hash)` — login was always failing
- `TaskService` namespace was `Users.Services` → corrected to `Tasks.Services`
- `PasswordHasher` and `JwtTokenGenerator` changed from `AddSingleton` to `AddScoped` for consistency

---

### 🔲 Block 6 — API Controllers + Global Error Handling
**Pain:** Services are ready but nothing exposes them to HTTP. Also: every Controller will need the same try/catch, same 401/403/400 mapping logic → massive duplication  
**Concepts:** Controller design, ActionResult, `[Authorize]`, extracting userId from JWT claims, Global Exception Middleware, Problem Details (RFC 7807), HTTP Status Code semantics  
**Will build:**
- `UsersController` (Register, Login, GetById, GetAll, Delete)
- `TasksController` (Create, GetById, GetAll, UpdateStatus)
- `GlobalExceptionMiddleware` or `IExceptionHandler`
- `ApplicationServiceExtensions` for registering Application-layer services

**Key questions you'll answer:**
- How do you get the userId from the JWT inside a Controller without touching the token string?
- What's the difference between returning `404` and `403` for a task that exists but isn't yours?
- Where does `[Authorize]` sit — on the Controller or the Action?

---

### 🔲 Block 7 — CQRS + MediatR
**Pain:** Services are fat — `UserService` handles registration, login, fetching, deletion. One class with 5+ unrelated responsibilities. One change risks breaking another.  
**Concepts:** Command Query Responsibility Segregation, MediatR pipeline, Handlers, why reads and writes deserve different models  
**Will build:**
- Commands: `RegisterUserCommand`, `LoginCommand`, `CreateTaskCommand`, `UpdateTaskStatusCommand`, `DeleteUserCommand`
- Queries: `GetUserByIdQuery`, `GetAllUsersQuery`, `GetTaskByIdQuery`, `GetAllTasksByUserQuery`
- Handlers for each
- Remove fat Services, replace with thin MediatR dispatching in Controllers

---

### 🔲 Block 8 — Validation Pipeline
**Pain:** Validation logic scattered — some in Controller, some in Service, some missing entirely. An invalid request reaches deep into the stack before failing.  
**Concepts:** Fail Fast principle, FluentValidation, MediatR Pipeline Behaviors, Validation as a cross-cutting concern  
**Will build:**
- `FluentValidation` validators for all Commands
- `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior
- Automatic validation before any Handler runs

---

### 🔲 Block 9 — Redis Caching
**Pain:** `GetAllTasks` hits the DB on every request. Dashboard endpoints called 100 times/minute — 100 identical DB queries.  
**Concepts:** Cache-aside pattern, TTL, cache invalidation (the hard problem), when NOT to cache, Redis data structures  
**Will build:**
- `IRedisCacheService` in Application
- `RedisCacheService` in Infrastructure
- Caching on `GetAllByUser` and `GetById` queries
- Cache invalidation on `CreateTask` and `UpdateStatus`

---

### 🔲 Block 10 — Event-Driven with RabbitMQ
**Pain:** After task creation, you need to send a notification to the user. Doing it synchronously in the Handler blocks the request and couples two unrelated concerns.  
**Concepts:** Pub/Sub, Message Queues, Eventual Consistency, Decoupling with Events, why async messaging beats sync HTTP calls for side effects  
**Will build:**
- `TaskCreatedEvent`, `UserRegisteredEvent` Domain Events
- `IEventPublisher` in Application
- `RabbitMQEventPublisher` in Infrastructure
- `NotificationConsumer` as a background service

---

### 🔲 Block 11 — Background Jobs with Hangfire
**Pain:** "Send weekly summary every Monday at 9am" — can't do this in a request. Fire-and-forget jobs need persistence and retry logic.  
**Concepts:** Recurring jobs, fire-and-forget, delayed jobs, job persistence, retry policies  
**Will build:**
- Hangfire setup with SQL Server storage
- `WeeklySummaryJob`
- Dashboard for job monitoring

---

### 🔲 Block 12 — Structured Logging with Serilog
**Pain:** Something fails in Production. Console.WriteLine is gone. You have no idea what happened, in what order, with what data.  
**Concepts:** Structured vs flat logging, log levels, correlation IDs, Serilog sinks, what to log and what NOT to log  
**Will build:**
- Serilog with Console + File sinks
- Request logging middleware
- Correlation ID per request
- Logging in critical service paths

---

### 🔲 Block 13 — Health Checks + Monitoring
**Pain:** Is the app up? Is the DB reachable? Is Redis responding? You don't know until users complain.  
**Concepts:** Health check endpoints, liveness vs readiness, dependency checks  
**Will build:**
- `/health` endpoint
- DB health check
- Redis health check
- Custom checks

---

### 🔲 Block 14 — Docker + CI/CD
**Pain:** "Works on my machine." Deploying manually. No reproducible environment.  
**Concepts:** Containerization, Dockerfile layers, docker-compose for local dev, GitHub Actions pipeline, environment variables in containers  
**Will build:**
- `Dockerfile` for the API
- `docker-compose.yml` (API + SQL Server + Redis + RabbitMQ)
- GitHub Actions workflow: build → test → publish

---

### 🔲 Block 15 — Testing (Unit + Integration)
**Pain:** A refactor breaks something in a different part of the system. You find out when a user reports it, not when you wrote the code.  
**Concepts:** Unit vs Integration tests, test pyramid, mocking with Moq, `WebApplicationFactory`, test isolation, what to test and what not to  
**Will build:**
- Unit tests for all Handlers (pure logic, no DB)
- Integration tests for critical API endpoints (real HTTP, real DB, in-memory or test container)
- Test coverage for Result Pattern edge cases

---

### 🔲 Block 16 — Microservices Concepts
**Pain:** The monolith is working — why would you ever break it? (Most tutorials skip this question.)  
**Concepts:** When microservices make sense (and when they don't), service boundaries, API Gateway, distributed tracing, the cost of distribution  
**Will build:**
- Extract Notification into a separate service
- API Gateway concept with YARP
- Understanding: this is not always the right answer

---

### 🔲 Block 17 — Angular Frontend
**Pain:** The API is built. Nobody can use it without a UI.  
**Concepts:** Angular architecture, Component → Service → HTTP → API flow, Reactive Forms, Guards for route protection, JWT handling in Angular, Error interceptors  
**Will build:**
- Login / Register pages
- Task dashboard
- Auth guard for protected routes
- HTTP interceptor for attaching JWT
- Error handling interceptor

---

### 🔲 Block 18 — Full Stack Integration
**Pain:** Frontend and Backend built separately. Real integration reveals assumptions that were wrong.  
**Concepts:** CORS, shared error contracts, end-to-end flow from Angular click to DB write and back  
**Will build:**
- CORS configuration
- Shared error response shape consumed by Angular
- Full registration → login → create task → see task flow working end-to-end

---

## Current Status

| Block | Topic | Status |
|---|---|---|
| 1 | Domain Layer | ✅ Done |
| 2 | Application Layer | ✅ Done |
| 3 | Infrastructure + EF Core | ✅ Done |
| 4 | Authentication (BCrypt + JWT) | ✅ Done |
| 5 | Result Pattern + Unit of Work | ✅ Done |
| 6 | API Controllers + Error Handling | � In Progress |
| 7 | CQRS + MediatR | 🔲 Pending |
| 8 | Validation Pipeline | 🔲 Pending |
| 9 | Redis Caching | 🔲 Pending |
| 10 | RabbitMQ + Event-Driven | 🔲 Pending |
| 11 | Hangfire Background Jobs | 🔲 Pending |
| 12 | Serilog Logging | 🔲 Pending |
| 13 | Health Checks | 🔲 Pending |
| 14 | Docker + CI/CD | 🔲 Pending |
| 15 | Testing (Unit + Integration) | 🔲 Pending |
| 16 | Microservices Concepts | 🔲 Pending |
| 17 | Angular Frontend | 🔲 Pending |
| 18 | Full Stack Integration | 🔲 Pending |

---

## File Structure Reference

```
TaskManagement/
├── MASTER-PLAN.md                          ← this file
├── README.md                               ← project overview (English)
├── docs/
│   ├── blocks/                             ← Mission files (Challenger Model)
│   │   ├── Block1-Domain-Layer-Lesson.md
│   │   ├── Block3-Infrastructure-Layer.md
│   │   ├── Block4-Authentication-Lesson.md
│   │   ├── Block5-Result-Pattern-Mission.md
│   │   └── Block5b-UnitOfWork-Mission.md
│   └── lessons/                            ← Arabic HTML deep-dives
│       ├── Block3-Infrastructure/index.html
│       ├── Block4-Authentication/index.html
│       └── Block5-ResultPattern/index.html
└── src/
    ├── TaskManagement.Domain/
    │   ├── Common/Result.cs                ← Result<T> pattern
    │   ├── Entities/User.cs
    │   ├── Entities/TaskItem.cs
    │   ├── Enums/
    │   ├── Interfaces/
    │   │   ├── IUnitOfWork.cs
    │   │   └── Repositories/
    ├── TaskManagement.Application/
    │   ├── Common/Interfaces/              ← IPasswordHasher, IJwtTokenGenerator
    │   ├── Users/
    │   │   ├── DTOs/
    │   │   ├── Interfaces/IUserService.cs
    │   │   └── Services/UserService.cs
    │   └── Tasks/
    │       ├── DTOs/
    │       ├── Interfaces/ITaskService.cs
    │       └── Services/TaskService.cs
    ├── TaskManagement.Infrastructure/
    │   ├── Auth/PasswordHasher.cs
    │   ├── Auth/JwtTokenGenerator.cs
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs
    │   │   ├── Configurations/
    │   │   ├── Repositories/
    │   │   ├── Seeders/DbSeeder.cs
    │   │   └── UnitOfWork.cs
    │   └── Extensions/InfrastructureServiceExtensions.cs
    └── TaskManagement.API/
        └── Program.cs
```

---

## Tech Stack — Current + Planned

| Technology | Purpose | Status |
|---|---|---|
| ASP.NET Core 8 | Web framework | ✅ In use |
| Entity Framework Core | ORM | ✅ In use |
| SQL Server | Primary database | ✅ In use |
| BCrypt.Net | Password hashing | ✅ In use |
| JWT Bearer | Authentication | ✅ In use |
| MediatR | CQRS dispatcher | 🔲 Block 7 |
| FluentValidation | Input validation | 🔲 Block 8 |
| Redis | Distributed caching | 🔲 Block 9 |
| RabbitMQ | Message broker | 🔲 Block 10 |
| Hangfire | Background jobs | 🔲 Block 11 |
| Serilog | Structured logging | 🔲 Block 12 |
| Docker | Containerization | 🔲 Block 14 |
| xUnit + Moq | Testing | 🔲 Block 15 |
| YARP | API Gateway concept | 🔲 Block 16 |
| Angular 17+ | Frontend framework | 🔲 Block 17 |

---

## Foundation Context — What's Known and What Isn't

**Background:** Not a CS graduate. Self-taught .NET/C# developer.  
**Parallel roadmap:** `E:\Hamdy\Learning\FAANG Level Software Engineer\` — 18-week Core Engineering Foundations program.

**What's already done in Foundations:**
- Sorting algorithms: Basic Sorts, Merge Sort, Quick Sort ✅
- External Sort + first Dockerfile (Day 21) ✅
- TaskManagement itself covers: EF Core, JWT, Architecture patterns (ahead of schedule)

**What's NOT done yet (Week 1-2 of Foundations):**
- Arrays, Linked Lists, Stacks, Queues, Hash Tables, Trees, Heaps
- OS: Threads, Thread Pool, Memory Models
- Async/Await state machine internals
- Networking: TCP/IP, HTTP internals, DNS
- Database Internals: B-Trees, Execution Plans, WAL, MVCC

**How we handle this in TaskManagement:**  
Every block that touches a Foundation topic gets a "Under The Hood" section in its HTML lesson — explaining the CS concept in context. Example: Redis block explains Hash Tables. RabbitMQ block explains Queues. Docker block explains process isolation.

The Foundation program continues in parallel and gets easier because TaskManagement shows the real-world use case first.

**How the two tracks connect — Trigger-Based, NOT true parallel:**

Do NOT work on both tracks on the same day. TaskManagement sets the pace.  
Foundation days are triggered by the Block that needs them — inserted right before, not weeks ahead.

```
Block 6  → API Controllers             (no Foundation prerequisite)
Block 7  → CQRS + MediatR              (no Foundation prerequisite)
             ↓ trigger: Foundation Day 22 — Binary Search Variants (1 day)
Block 8  → Validation Pipeline         (no Foundation prerequisite)
             ↓ trigger: Foundation Day 23-24 — Two Pointers, Sliding Window (2 days)
Block 9  → Redis Caching               ← trigger: Foundation Day 4 — Hash Tables (1 day first)
Block 10 → RabbitMQ + Events           ← trigger: Foundation Day 3 — Queues + Producer-Consumer (1 day first)
Block 11 → Hangfire Jobs               ← trigger: Foundation Day 33 — Thread Pool (1 day first)
Block 14 → Docker + CI/CD              ← already done: Day 21 External Sort + Dockerfile ✅
Block 15 → Testing                     ← trigger: Foundation Day 28 — xUnit GATE (before Block 15)
```

After Block 18 (TaskManagement complete):
→ Return to Foundation from Day 1 (Week 1-2 gaps: Arrays, Trees, Heaps)
→ Every topic lands 10x faster because you already saw its real-world use case in TaskManagement

**Rule:** I will tell you "take Foundation Day X before this Block" at the right moment. You don't need to decide.

---

## Principles That Never Change

Regardless of the block, these never get violated:

1. **Pain before tool** — we don't add a pattern unless the project hurts without it
2. **Domain knows nothing** — no EF Core, no ASP.NET, no NuGet in Domain
3. **Interfaces define the contract** — implementations can be swapped without touching business logic
4. **Failures are values, not exceptions** — expected failures return `Result`, unexpected ones throw
5. **Services own the transaction boundary** — Repository stages, UnitOfWork commits
6. **Security is not optional** — ownership checks, no enumeration leaks, no sensitive data in tokens
7. **The Challenger Model** — you implement first, review second, senior version last

---

## How to Use This File

**At the start of every session:**  
Read the Current Status table. Find the next 🔲. That's where we are.

**When starting a new block:**  
The Pain section tells you what to break first before introducing the solution.

**When context is lost:**  
This file has everything. One read = full context restored.
