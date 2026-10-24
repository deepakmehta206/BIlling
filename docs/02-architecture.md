# Architecture

## Pieces

```
client/          Angular 22 web app
src/Billing.Api  ASP.NET Core 10 HTTP API
src/Billing.Application  contracts and DTOs
src/Billing.Domain       entities and role/module catalogues
src/Billing.Infrastructure  EF Core, SQL Server, seeding
```

The Angular app is the single UI. Later, Electron wraps it for the billing counter and Capacitor wraps it for phones. They will call this same API. Phase 1 ships the web app only.

## Request path

1. Angular stores a JWT in `localStorage` and sends it as `Authorization: Bearer`.
2. The API reads the user id, restaurant id and super-admin flag from the token.
3. `HttpRequestContext` also reads the client IP (`X-Forwarded-For`, otherwise the socket address) and the user agent.
4. Services check the role again. A restaurant admin never receives another house's row, even with a guessed id.
5. `AppDbContext.SaveChanges` stamps audit columns and turns a hard delete into a soft delete before SQL runs.

## Modules

Module codes live in `Billing.Domain.Security.ModuleCodes`. Creating a restaurant inserts one `RestaurantModules` row per module. The super admin flips `IsEnabled`. Later phases will hide a screen when its module is off.

Default on: Billing, Menu, Tables, QR order, Token display, Booking, Reports.

Default off: Inventory, HRMS, Attendance, WhatsApp, UPI.

## Auth

JWT is signed with HMAC SHA-256. Issuer `Aangan`, audience `Aangan.Web`. The dev signing key is in `src/Billing.Api/appsettings.json` and must be replaced before any public host.

Passwords are hashed with ASP.NET Core `PasswordHasher` (PBKDF2). The API never returns a password.
