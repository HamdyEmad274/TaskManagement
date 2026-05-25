# Block 5b — Mission: Unit of Work

| | |
|---|---|
| **Mode** | Challenger — concept given, implementation is yours |
| **Depends on** | Block 5 Result Pattern |
| **Builds** | `IUnitOfWork`, `UnitOfWork.cs`, updated services |
| **Concept** | EF Core change tracker, transaction atomicity, separation of concerns |

---

## The Pain

You implemented `RegisterAsync`. It looks correct:

```csharp
var user = User.Create(...);
await _userRepository.AddAsync(user);
return Result.Success(response);
```

Run it. Register a user. Check the database.

**The user isn't there.**

`AddAsync` calls this under the hood:

```csharp
await _context.Users.AddAsync(user);
```

EF Core does NOT write to the database here. It puts the entity into its **change tracker** — an in-memory list of pending operations. Nothing hits SQL Server until someone calls `SaveChangesAsync()` on the `DbContext`.

```
Your code path right now:
──────────────────────────────────────────────────────────────────
_userRepository.AddAsync(user)
  └── _context.Users.AddAsync(user)     ← entity staged in memory
                                        ← NO SQL executed here
return Result.Success(response)         ← method ends, DbContext disposed
                                        ← all staged changes DISCARDED
```

The entity existed for the lifetime of the request — then disappeared.

---

## Why EF Core Works This Way

This is not a bug. It's a deliberate design that enables **atomicity**.

Consider a real scenario: a user registers, and you want to send them a welcome notification stored in the DB:

```csharp
await _userRepository.AddAsync(user);
await _notificationRepository.AddAsync(welcomeNotification);
// SaveChangesAsync() here — both writes in ONE transaction
```

If `SaveChangesAsync` is called after each `AddAsync` separately:
- First save succeeds — user written to DB
- Second save fails — notification write throws an exception
- **Result:** user exists, no welcome notification — inconsistent state

If `SaveChangesAsync` is called once at the end:
- Both operations are wrapped in **one SQL transaction**
- Either both succeed or both roll back
- **Result:** always consistent

This is the **A in ACID** — Atomicity. Either everything commits or nothing does.

---

## The Change Tracker — Mental Model

Think of EF Core's `DbContext` as a shopping cart:

```
AddAsync(user)               → adds item to cart
AddAsync(notification)       → adds item to cart
UpdateAsync(task)            → marks item as "modified" in cart
DeleteAsync(user)            → marks item as "remove" in cart

SaveChangesAsync()           → checkout — ALL cart items processed in one transaction
```

The cart (change tracker) knows about every entity you've touched since the `DbContext` was created. One `SaveChangesAsync` generates one SQL transaction containing all the pending changes.

```sql
-- What SaveChangesAsync generates for RegisterAsync:
BEGIN TRANSACTION
  INSERT INTO Users (Id, Name, Email, PasswordHash, Role, CreatedAt)
  VALUES ('...', 'Hamdy', 'hamdy@email.com', '$2a$12$...', 'User', '2026-05-24')
COMMIT
```

---

## The Separation of Concerns Problem

So why not just call `_context.SaveChangesAsync()` directly inside `UserRepository.AddAsync`?

```csharp
// ❌ Repository with save responsibility
public async Task AddAsync(User user)
{
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();  // ← wrong
}
```

**Problem 1 — Breaks atomicity:**
If a service method calls `AddAsync` on two repositories, the first one saves immediately. If the second throws, the first write is already committed — no rollback possible.

**Problem 2 — Wrong responsibility:**
A repository's job: *store and retrieve objects*. 
Deciding *when to commit* is the service's job — it's the service that knows the full scope of the operation.

**Problem 3 — Untestable:**
In tests, you want to verify that `AddAsync` was called without actually hitting a database. If `AddAsync` always saves, you can't test service logic without a real DB context.

---

## The Unit of Work Pattern

**Definition:** A Unit of Work tracks all changes made during a business operation and flushes them to the database in a single transaction when the operation is complete.

In EF Core, the `DbContext` already IS a Unit of Work. You don't need to build one from scratch — you just need to expose `SaveChangesAsync` through an interface so services can call it without knowing about `DbContext` directly.

```
Domain defines the contract:
  IUnitOfWork
    └── Task SaveChangesAsync(CancellationToken ct = default)

Infrastructure implements it:
  UnitOfWork : IUnitOfWork
    └── wraps AppDbContext.SaveChangesAsync()

Service uses it:
  UserService
    ├── IUserRepository  (stage changes)
    └── IUnitOfWork      (commit changes)
```

The service never touches `DbContext` directly — that stays in Infrastructure where it belongs.

---

## Draw This Before You Code

```
RegisterAsync flow WITH Unit of Work:
──────────────────────────────────────────────────────────────────
1. _userRepository.AddAsync(user)
      → _context.Users.AddAsync(user)     [EF change tracker: +1 pending INSERT]

2. await _unitOfWork.SaveChangesAsync()
      → _context.SaveChangesAsync()
      → EF generates SQL:
          BEGIN TRANSACTION
            INSERT INTO Users VALUES (...)
          COMMIT
      → user now exists in database ✅

3. return Result.Success(response)
```

---

## The Contract

```csharp
// Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Your Mission

**Step 1** — Create `IUnitOfWork` in:
```
src/TaskManagement.Domain/Interfaces/IUnitOfWork.cs
```

**Step 2** — Implement `UnitOfWork` in:
```
src/TaskManagement.Infrastructure/Persistence/UnitOfWork.cs
```
It takes `AppDbContext` via constructor injection and delegates to it.

**Step 3** — Register in `InfrastructureServiceExtensions`:
```csharp
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

**Step 4** — Inject `IUnitOfWork` into `UserService` and `TaskService`.
Call `await _unitOfWork.SaveChangesAsync()` at the end of every method that writes.

---

## Questions to Answer Before You Code

1. Should `UnitOfWork` be `AddScoped` or `AddSingleton`? Why does it matter here?
2. `UserService` already injects `IUserRepository`. The `UnitOfWork` wraps the same `DbContext`. How does DI ensure they share the same `DbContext` instance within one request?
3. Why does `CancellationToken` have a default value of `default`? When would you pass an actual token?

---

## When You're Done

Come back with all 4 steps complete. Review will cover:
- Is `UnitOfWork` implemented correctly?
- Are all write operations in services now followed by `SaveChangesAsync`?
- Are the DI lifetimes consistent?
