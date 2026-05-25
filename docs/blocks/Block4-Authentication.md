# Block 4 — Authentication (JWT + Password Hashing)

> 🔒 This file will be filled after Block 4 is reviewed.

## Topics That Will Be Covered

- What is password hashing (vs encryption vs encoding)
- Why BCrypt — work factor, salting, how it prevents rainbow table attacks
- What is JWT — structure (Header.Payload.Signature)
- JWT Claims — what data to put in the token
- How JWT signature verification works (server doesn't need to store tokens)
- Access token vs Refresh token
- Configuring JWT Bearer authentication in ASP.NET Core
- Reading claims from the token inside a controller (`User.FindFirst(...)`)
- Why you should never store the JWT secret in code
