# Architecture

## Pieces

```
client/          Angular 22 web app
client/electron  Electron shell for the counter
client/android   Capacitor Android project
src/Billing.Api  ASP.NET Core 10 HTTP API
src/Billing.Application  contracts and DTOs
src/Billing.Domain       entities and role/module catalogues
src/Billing.Infrastructure  EF Core, SQL Server, seeding
```

The Angular app is the single UI. Electron and Capacitor load that build and call this API. On the counter, the API is `http://localhost:5254`. On a phone, the sign-in screen can point at the computer's address, such as `http://192.168.1.20:5254`. The same build rules live in `.cursor/rules/aangan.mdc`.

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
