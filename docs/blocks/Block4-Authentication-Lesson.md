# Block 4 — Authentication: Passwords & Identity

| | |
|---|---|
| **Previous** | Block 3: Infrastructure — EF Core, Repositories, Migrations |
| **Today** | Block 4: BCrypt password hashing + JWT authentication |
| **Next** | Block 5: Service Implementations — UserService, TaskService |
| **Builds** | `PasswordHasher.cs`, `JwtTokenGenerator.cs` in Infrastructure |

**Stopping point:** _______________________________________________

---

## What You Will Be Able to Do After This Block

- [ ] Explain why storing plain-text passwords is a company-ending mistake
- [ ] Explain the progression: plain text → MD5 → salted hash → BCrypt key stretching
- [ ] Explain what a rainbow table attack is and why salting defeats it
- [ ] Explain why BCrypt's slowness is a feature, not a bug
- [ ] Explain the stateless session problem at scale (1 server → 10 servers)
- [ ] Draw the JWT flow: login → token → request → verify — on paper
- [ ] Decode a JWT by hand and identify its three parts
- [ ] Explain the JWT revocation problem and how refresh tokens address it
- [ ] Implement `PasswordHasher` and `JwtTokenGenerator` from scratch

---

## Pain First — The Breach

**October 2013. Adobe Systems. 153 million user records leaked.**

The database dump included:
```
user_id | email                    | password          | hint
--------|--------------------------|-------------------|------------------
1       | john@example.com         | abc123            | "my usual one"
2       | admin@adobe.com          | password          | "can't forget this"
3       | ceo@adobe.com            | 123456789         | "the long one"
```

**Plain text. Every password. Readable by anyone who downloaded the file.**

Adobe was fined. Users whose passwords were reused on other sites had those accounts compromised. The breach was immortalized at [haveibeenpwned.com](https://haveibeenpwned.com).

**This is not ancient history.** RockYou (2009, 32M plain text), LinkedIn (2012, 117M), Twitter (2022 — internal logs stored plain text passwords).

The question is: **how do you store a password so that even if your database is stolen, the attacker gets nothing useful?**

---

## Chapter 1 — The Evolution of Password Storage

### Generation 0: Plain Text
```
stored: "secret123"
```
Database stolen → game over. Every user's password is immediately readable.

**Eliminated because:** No transformation at all. Trivially defeated.

---

### Generation 1: Simple Hash (MD5 / SHA-1)

The idea: run the password through a one-way function. You can't reverse it.

```
MD5("secret123") = "5ebe2294ecd0e0f08eab7690d2a6ee69"
```

**Verification:** when the user logs in, hash what they typed and compare to stored hash.

**Sounds good. It isn't.**

#### The Rainbow Table Attack

A rainbow table is a pre-computed dictionary of billions of hashes:

```
"123456"    → "e10adc3949ba59abbe56e057f20f883e"
"password"  → "5f4dcc3b5aa765d61d8327deb882cf99"
"secret123" → "5ebe2294ecd0e0f08eab7690d2a6ee69"
... (billions more)
```

Attacker steals your database. They don't crack passwords — they **look them up**.
MD5 is fast — a modern GPU can compute **200 billion MD5 hashes per second**.

```
stolen hash:   "5ebe2294ecd0e0f08eab7690d2a6ee69"
rainbow table lookup: 0.003 seconds
result: "secret123"
```

**Eliminated because:** Lookup attacks defeat any fast hash function.

---

### Generation 2: Salted Hash

The fix for rainbow tables: add a random string (the "salt") to each password before hashing.

```
salt   = "xK9#mP2q"  (random, stored alongside the hash)
stored = SHA256("xK9#mP2q" + "secret123")
       = "a94b2c..." (completely different hash)
```

Now the attacker's rainbow table is useless — it was built without the salt.
To crack one password, they must build a custom rainbow table **for that specific salt**.

```
User A: salt="xK9#mP2q"  stored=SHA256("xK9#mP2q"+"secret123")
User B: salt="Lm5#vR8w"  stored=SHA256("Lm5#vR8w"+"secret123")

Same password → completely different hashes.
A rainbow table for User A's salt is useless for User B.
```

**Good. But still not enough.**

#### The Brute Force Problem

SHA-256 is designed to be **fast**. A modern GPU computes 10 billion SHA-256 hashes per second.

Even with unique salts, an attacker can brute-force one user's password:
```
for each word in dictionary (10M words):
    candidate = SHA256(stolen_salt + word)
    if candidate == stolen_hash: CRACKED

Time: 10,000,000 ÷ 10,000,000,000 = 0.001 seconds per user
```

10 billion guesses per second. Most people use weak passwords.
A 6-character lowercase password has 26^6 = ~300 million combinations.
At 10B/sec: cracked in **0.03 seconds**.

**The root problem: SHA-256 is designed for speed. That's wrong for password hashing.**

---

### Generation 3: Key Stretching — BCrypt

**The insight:** What if hashing was intentionally slow?

BCrypt was designed in 1999 specifically for passwords. It has a **cost factor** (also called work factor or rounds) that controls how many iterations of computation it performs.

```
cost factor = 10  →  ~100ms to hash one password
cost factor = 12  →  ~400ms to hash one password
cost factor = 14  →  ~1.5s  to hash one password
```

**From the user's perspective:** 100ms to log in is imperceptible. Fine.

**From the attacker's perspective:**
```
At cost=10 (100ms per hash):
GPU that did 10B SHA-256/sec now does:
  10 hashes/sec

Same 6-character password space (300M combinations):
300,000,000 ÷ 10 = 30,000,000 seconds = 347 days

One user's weak password now takes almost a year to crack.
```

**BCrypt also generates its own salt automatically** — you don't manage it separately.

The stored BCrypt hash looks like this:
```
$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
│   │  │                    │
│   │  └── salt (22 chars)  └── hash (31 chars)
│   └── cost factor (10)
└── BCrypt version
```

Everything the verifier needs is embedded in the hash string itself.

**Eliminated the need for:** separate salt columns, salt management, iteration count storage.

#### Why Not Argon2 Instead?

Argon2 won the 2015 Password Hashing Competition. It's memory-hard (harder to run on GPUs).
BCrypt is still the industry default in .NET because:
- Battle-tested since 1999
- BCrypt.Net-Next is mature, audited
- Cost factor can be increased as hardware gets faster
- Argon2 is better, but BCrypt is not broken — good enough

For this project: BCrypt. In a bank: Argon2id.

---

## Chapter 2 — The Session Problem

You've solved password storage. Now you need to answer:
**"How does the server know who is making this request?"**

### The Naive Solution: Sessions

```
1. User logs in with correct password
2. Server creates a session record in memory (or database):
   sessions["abc123xyz"] = { userId: 42, expires: tomorrow }
3. Server sends cookie: "session_id=abc123xyz"
4. Every subsequent request includes that cookie
5. Server looks up "abc123xyz" in session store → finds userId=42 → authenticated
```

**This works perfectly. On one server.**

#### The Scaling Problem

Your TaskManagement API launches. Users love it. You add a second server behind a load balancer.

```
Request 1 (login):    → Server A  → creates session "abc123xyz" in Server A's memory
Request 2 (get tasks): → Server B  → looks up "abc123xyz" → NOT FOUND → 401 Unauthorized
```

The user just logged in and immediately gets rejected.

**Solutions that exist but hurt:**

| Solution | Problem |
|---|---|
| Sticky sessions (always route same user to same server) | One server dies → all its users logged out |
| Shared database for sessions | Every request hits the DB → latency, bottleneck |
| Redis session store | Works but now you have another service to run, maintain, scale |

**The root problem:** The server holds state. State that must be shared across servers.

**What if the server held no state at all?**

---

## Chapter 3 — JWT: Stateless Authentication

**The insight:** Instead of the server remembering who you are, **you carry proof of who you are**.

### What a JWT Is

JWT = JSON Web Token. It's a string with three Base64-encoded parts separated by dots:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0MiIsImVtYWlsIjoiaGFtZHlAZW1haWwuY29tIiwicm9sZSI6IlVzZXIiLCJleHAiOjE3MTY0MDMyMDB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
│─────────────────────────────────────│─────────────────────────────────────────────────────────────────│────────────────────────────────────────────│
          HEADER                                          PAYLOAD                                              SIGNATURE
```

**Part 1 — Header** (decoded):
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```
Algorithm used to sign + token type.

**Part 2 — Payload** (decoded):
```json
{
  "sub": "42",
  "email": "hamdy@email.com",
  "role": "User",
  "exp": 1716403200
}
```
`sub` = subject (user ID). `exp` = expiry (Unix timestamp). These are called **claims**.

**Part 3 — Signature**:
```
HMACSHA256(
  base64(header) + "." + base64(payload),
  SECRET_KEY
)
```

The signature is computed using a secret key that only the server knows.

### The Verification Flow

```
CLIENT                          SERVER
  │                               │
  │──── POST /login ─────────────►│
  │     { email, password }       │  1. verify password with BCrypt
  │                               │  2. create JWT with userId, role, expiry
  │◄─── 200 OK ──────────────────│
  │     { token: "eyJ..." }       │
  │                               │
  │  (stores token in memory/     │
  │   localStorage)               │
  │                               │
  │──── GET /tasks ───────────────►│
  │     Authorization: Bearer     │  3. decode header + payload (no secret needed)
  │     eyJ...                    │  4. recompute signature using secret key
  │                               │  5. if recomputed == received signature: VALID
  │                               │  6. check exp claim: not expired?
  │◄─── 200 OK ──────────────────│  7. extract userId from payload → serve request
  │     [ ...tasks... ]           │
```

**The server never stores anything.** No session table. No Redis. No database lookup.
Any server that knows the secret key can verify any token.

```
Server A issued the token.
Server B (or C or D) can verify it.
Load balancer can route freely.
```

### Why It's Secure

The payload is Base64-encoded — not encrypted. Anyone can decode and read it:

```
base64decode("eyJzdWIiOiI0MiIsImVtYWlsIjoiaGFtZHlAZW1haWwuY29tIn0")
→ {"sub":"42","email":"hamdy@email.com","role":"User"}
```

**So what stops an attacker from changing `"role":"User"` to `"role":"Admin"`?**

The signature. If they modify the payload, the signature no longer matches.
To forge a valid signature, they need the secret key. Without it: impossible.

```
RULE: Never put sensitive data in a JWT payload.
      It's readable by anyone who has the token.
      Put only: userId, role, expiry. Nothing else.
```

---

## Chapter 4 — The JWT Revocation Problem

JWT is stateless. That's its strength. It's also its weakness.

**Scenario:** A user's account is compromised. The admin deletes the account.
The attacker still has a valid JWT that doesn't expire for another 23 hours.
**The server has no way to invalidate that token.**

```
Token is valid.
Signature checks out.
Not expired.
Server has no state → can't know it was "revoked".
Result: attacker has 23 hours of access.
```

This is a fundamental trade-off:

| | Sessions | JWT |
|---|---|---|
| Scalability | ❌ Needs shared state | ✅ Stateless |
| Revocation | ✅ Delete the session | ❌ Can't revoke until expiry |
| DB lookup per request | ❌ Yes | ✅ No |
| Logout works immediately | ✅ Yes | ❌ Not really |

### The Practical Solution: Short Expiry + Refresh Tokens

```
Access Token:   expires in 15 minutes    ← short-lived, stateless, non-revocable
Refresh Token:  expires in 7 days        ← stored in DB, can be revoked
```

**The flow:**
```
Login → receive access_token (15min) + refresh_token (7 days, stored in DB)

Normal requests: use access_token (no DB hit)

When access_token expires:
  → POST /auth/refresh with refresh_token
  → server checks refresh_token in DB (one DB hit)
  → if valid: issue new access_token
  → if revoked/expired: force re-login

Logout:
  → delete refresh_token from DB
  → access_token remains valid for up to 15 more minutes (acceptable trade-off)
  → attacker who steals access_token has max 15 min window
```

**For our TaskManagement project:** We implement access tokens only (15-minute expiry).
Refresh tokens are Block 8 (advanced auth). The trade-off is documented here — you understand it.

---

## Chapter 5 — The Secret Key

The entire security of JWT rests on one thing: the secret key.

```json
"JwtSettings": {
  "SecretKey": "your-secret-key-here",
  "Issuer": "TaskManagement",
  "Audience": "TaskManagementUsers",
  "ExpiryMinutes": 60
}
```

**Rules for the secret key:**

1. **Never hardcode it** — if it's in source code and you push to GitHub, it's compromised
2. **Minimum 256 bits (32 bytes)** — HS256 needs at least 32 characters of entropy
3. **Store in environment variables or secrets manager** — not in `appsettings.json` committed to git
4. **Rotate it** — all existing tokens become invalid when you change it (another reason for short expiry)

**For development:** `appsettings.Development.json` (not committed) or `dotnet user-secrets`.
**For production:** Azure Key Vault, AWS Secrets Manager, or environment variables in the container.

---

## Chapter 6 — What We're Building

### Folder Structure

```
Infrastructure/
├── Auth/
│   ├── PasswordHasher.cs         ← implements IPasswordHasher (BCrypt)
│   └── JwtTokenGenerator.cs      ← implements IJwtTokenGenerator
└── Extensions/
    └── InfrastructureServiceExtensions.cs  ← register the new services here
```

### The Contracts (already exist in Application)

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

---

## TODO-01 — Install BCrypt NuGet Package

```
BCrypt.Net-Next
```

Install in `TaskManagement.Infrastructure` project.

> **Why BCrypt.Net-Next and not BCrypt.Net?**
> `BCrypt.Net` is the original port, unmaintained since 2011.
> `BCrypt.Net-Next` is the actively maintained fork with security fixes and .NET 8 support.
> Always check the NuGet download count + last updated date before choosing a package.

---

## TODO-02 — Create `Auth/` folder and implement `PasswordHasher.cs`

```csharp
using BCrypt.Net;
using TaskManagement.Application.Common.Interfaces;

namespace TaskManagement.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
```

> **Why `workFactor: 12`?**
> Cost factor 10 = ~100ms. Cost factor 12 = ~400ms. Cost factor 14 = ~1.5s.
> OWASP recommends cost ≥ 10. We use 12 — strong enough, fast enough for login.
> Rule of thumb: choose the highest factor that keeps login under 1 second on your hardware.
>
> **Why not expose workFactor as a config value?**
> You could. For now it's hardcoded at 12. In production, it comes from config so you can
> increase it as hardware gets faster — without redeploying code.
>
> **Why `BCrypt.Net.BCrypt.HashPassword` (repeated namespace)?**
> The class name is `BCrypt` inside the `BCrypt.Net` namespace. Visual Studio will prompt
> you to simplify — you can use a `using static` or just leave it explicit for clarity.

---

## TODO-03 — Add JWT settings to `appsettings.json`

In `TaskManagement.API/appsettings.json`, add:

```json
"JwtSettings": {
  "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
  "Issuer": "TaskManagement",
  "Audience": "TaskManagementUsers",
  "ExpiryMinutes": 60
}
```

> **Why `ExpiryMinutes: 60`?**
> Short-lived tokens limit the damage window if a token is stolen.
> 60 minutes is a reasonable balance between security and UX for a task management app.
> A banking app might use 5-15 minutes.
>
> **⚠️ For real projects:** Move `SecretKey` to `appsettings.Development.json`
> (which is git-ignored) or use `dotnet user-secrets`.
> Never commit a real secret key to source control.

Create a settings class to hold this in Infrastructure:

```csharp
namespace TaskManagement.Infrastructure.Auth;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
```

---

## TODO-04 — Install JWT NuGet Package

```
Microsoft.AspNetCore.Authentication.JwtBearer
```

Install in `TaskManagement.API` project.

> **Why in API and not Infrastructure?**
> `JwtBearer` is ASP.NET Core middleware — it's the part that intercepts incoming HTTP requests
> and validates the token. That belongs in the API layer.
> The token *generation* (`JwtTokenGenerator`) lives in Infrastructure.
> The token *validation* (middleware) lives in API.
> Two different concerns, two different projects.

Also install in Infrastructure (for token generation):
```
System.IdentityModel.Tokens.Jwt
```

---

## TODO-05 — Implement `JwtTokenGenerator.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

> **Why `IOptions<JwtSettings>` and not just `JwtSettings` directly?**
> `IOptions<T>` is the ASP.NET Core Options Pattern — the standard way to inject config.
> It reads from `appsettings.json`, validates it, and makes it available via DI.
> If you inject `JwtSettings` directly, you'd need to wire it up manually.
> `IOptions<T>` does that for you.
>
> **What is `JwtRegisteredClaimNames.Sub`?**
> `sub` is a standard JWT claim name meaning "subject" — the entity the token is about.
> We use `user.Id` as the subject. This is how the API will know which user is making a request.
>
> **What is `Jti`?**
> `jti` = JWT ID. A unique identifier for this specific token.
> Useful later if you implement a token blocklist (for revocation).
> For now it's there as best practice — costs nothing.
>
> **Why `ClaimTypes.Role` for role instead of a custom claim?**
> `ClaimTypes.Role` is the standard .NET claim type for roles.
> ASP.NET Core's `[Authorize(Roles = "Admin")]` attribute reads this specific claim type.
> If you use a custom name, role-based authorization won't work out of the box.
>
> **Why `DateTime.UtcNow` not `DateTime.Now`?**
> JWT `exp` claim is always UTC. If you use `DateTime.Now` (local time) on a server
> in Cairo (UTC+3), the token will expire 3 hours earlier than intended for users in UTC.
> Always use UTC in backend systems.

---

## TODO-06 — Register Everything in `InfrastructureServiceExtensions`

Add to the existing `AddInfrastructure` method:

```csharp
services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
services.AddScoped<IPasswordHasher, PasswordHasher>();
services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
```

> **Why `Configure<JwtSettings>` instead of just `AddSingleton`?**
> `services.Configure<T>` binds a config section to a strongly-typed class and registers it
> as `IOptions<T>`. It handles reloading if the config file changes at runtime.
> `AddSingleton(new JwtSettings { ... })` would be hardcoded — doesn't read from config.
>
> **Why `AddScoped` for `PasswordHasher` and `JwtTokenGenerator`?**
> They're stateless — they don't hold any request-specific data.
> Technically `AddSingleton` would work. We use `Scoped` to stay consistent with
> the rest of the Infrastructure registrations and to avoid potential issues if
> they ever hold state in the future.

---

## TODO-07 — Add JWT Authentication Middleware in `Program.cs`

In `TaskManagement.API/Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// After builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// In the middleware pipeline (after app.UseRouting()):
app.UseAuthentication();
app.UseAuthorization();
```

> **Why `UseAuthentication` before `UseAuthorization`?**
> `UseAuthentication` reads the token from the request and populates `HttpContext.User`.
> `UseAuthorization` checks `HttpContext.User` to decide if access is allowed.
> Reverse the order and authorization always fails — `HttpContext.User` is empty.
>
> **Why `ValidateLifetime = true`?**
> Without this, an expired token still passes validation. Always set this to true.
>
> **What does `ValidateIssuerSigningKey = true` do?**
> Verifies the token's signature matches the secret key. This is the core security check.
> Without it, anyone can forge a token with any payload — the signature is never checked.

---

## The Bugs to Find

When you implement this block, you will encounter (or should be aware of) these common mistakes:

**Bug 1 — Wrong order: `UseAuthorization` before `UseAuthentication`**
Symptom: `[Authorize]` endpoints always return 401 even with a valid token.
Why: `HttpContext.User` is never populated.

**Bug 2 — `workFactor` too high**
Symptom: Registration endpoint takes 3-5 seconds.
Why: Each BCrypt hash at `workFactor: 14` takes ~1.5s. Start at 10 in dev, 12 in prod.

**Bug 3 — `DateTime.Now` instead of `DateTime.UtcNow` in token expiry**
Symptom: Tokens expire at wrong times for users in different timezones.
Why: JWT spec requires UTC. `DateTime.Now` is server local time.

**Bug 4 — Secret key too short**
Symptom: `System.ArgumentOutOfRangeException: IDX10720: Unable to create KeyedHashAlgorithm`
Why: HS256 requires a minimum 128-bit (16 byte) key. Best practice is 256-bit (32 bytes).
Fix: Use a key that is at least 32 characters long.

**Bug 5 — `UseAuthentication` missing entirely**
Symptom: `HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)` returns null in controllers.
Why: Token was never parsed. The claims were never populated.

---

## Trade-offs Summary

| Decision | We chose | Alternative | Why |
|---|---|---|---|
| Password hashing | BCrypt (cost=12) | Argon2id | Battle-tested, .NET ecosystem, sufficient security |
| Token type | JWT (stateless) | Sessions (stateful) | Scalability — no shared state needed |
| Token storage (client) | Memory / localStorage | HttpOnly cookie | Simpler for API-first; cookies better for browser security |
| Expiry | 60 minutes | 15 min + refresh | Simpler to implement now; refresh tokens in Block 8 |
| Signing algorithm | HS256 | RS256 | HS256 = shared secret (simpler). RS256 = public/private key pair (better for multiple services) |

---

## What's Next — Block 5: Service Implementations

Now that authentication infrastructure exists, we can implement the actual business logic:

- `UserService.RegisterAsync` — hash password → create User → save via repository
- `UserService.LoginAsync` — find user → verify password → generate JWT → return token
- `TaskService.CreateAsync` — create TaskItem → save → return response
- `TaskService.UpdateStatusAsync` — find task → verify ownership → call `UpdateStatus()` → save

Every service method will use the interfaces defined in Block 2 (`IUserRepository`, `ITaskRepository`, `IPasswordHasher`, `IJwtTokenGenerator`) — none of them know about EF Core, BCrypt, or JWT directly.

That's the architecture working as designed.

---

## Session Summary

| Concept | One-liner |
|---|---|
| Plain text storage | Career-ending. Never. |
| MD5/SHA for passwords | Fast = bad. Rainbow tables crack it in milliseconds. |
| Salt | Makes rainbow tables useless. Still crackable by brute force. |
| BCrypt cost factor | Intentional slowness — 100ms per hash defeats brute force at scale. |
| Sessions | Stateful. Works on 1 server. Breaks under load balancer. |
| JWT | Stateless. Scales infinitely. Can't be revoked until expiry. |
| JWT signature | HMAC-SHA256 of header+payload with secret key. Tamper-proof. |
| JWT payload | Readable by anyone. Never put sensitive data in it. |
| `UseAuthentication` order | Must come before `UseAuthorization`. Always. |
| `DateTime.UtcNow` | JWT is always UTC. Never use `DateTime.Now` in tokens. |
| Secret key | ≥32 chars, never in source control, rotate periodically. |
