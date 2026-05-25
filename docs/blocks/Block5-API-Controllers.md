# Block 5 — API Controllers + Authorization + Swagger

> 🔒 This file will be filled after Block 5 is reviewed.

## Topics That Will Be Covered

- Controller structure in Clean Architecture
- `[Authorize]` vs `[Authorize(Roles = "Admin")]`
- Extracting `userId` from JWT claims in a controller
- Global exception handling middleware (DomainException → 400, unhandled → 500)
- Swagger with JWT Bearer support
- RESTful route design best practices
- Returning proper HTTP status codes (200, 201, 400, 401, 403, 404)
- `IActionResult` vs typed returns (`ActionResult<T>`)
- Model validation with Data Annotations on DTOs
