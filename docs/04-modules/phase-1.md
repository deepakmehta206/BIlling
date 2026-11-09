# Phase 1 — platform desk

## What you can do

- Sign in as the platform super admin.
- Open a restaurant and create its restaurant admin in the same step.
- Edit the house, pause it, or archive it.
- Turn each module on or off. Only real changes are written to the audit.
- Sign in as that restaurant admin and edit house details (name, address, GSTIN, UPI, owner). Code, active flag, and modules stay with the platform.
- Read the audit log: action, person, restaurant, IP, time. A restaurant admin sees only their house.
- Open the Desk home for a numbered quick path (menu → floor → token screen → billing → books) and today’s pulse: open tokens, occupied tables, ready-to-bill, unpaid bills, sales, and bookings when those modules are on.
- Open **Your profile** (sidebar name) to change name, phone, email, and password. House staff must keep a phone. A new password needs the current one; the API returns a fresh token.

## API

| Method | Path | Who |
| --- | --- | --- |
| POST | `/api/auth/login` | Public |
| GET | `/api/auth/me` | Signed in |
| GET | `/api/dashboard` | Signed in, scoped |
| PUT | `/api/auth/me` | Signed in — name, phone, email; password needs the current one |
| GET | `/api/restaurants?q=` | Super admin |
| GET | `/api/restaurants/{id}` | Super admin, or that house's admin |
| POST | `/api/restaurants` | Super admin |
| PUT | `/api/restaurants/{id}` | Super admin (full), or that house’s restaurant admin (details only; code and active locked) |
| DELETE | `/api/restaurants/{id}` | Super admin, soft delete |
| PUT | `/api/restaurants/{id}/modules` | Super admin |
| GET | `/api/audit-logs?page&pageSize&restaurantId&action` | Signed in, scoped |
| GET | `/api/health` | Public |

Errors return `{ "message": "..." }` and an HTTP status. `401` means sign in again, `403` means the role cannot do that, `409` means the admin email is already used.

## Not in this phase

Menu editing, the floor plan, QR orders, token screens, bills, UPI payment, WhatsApp, inventory, HR, attendance, Electron and Capacitor. The module switches exist so those desks can be granted before the screens arrive.
