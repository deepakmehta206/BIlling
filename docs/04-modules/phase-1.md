# Phase 1 — platform desk

## What you can do

- Sign in as the platform super admin.
- Open a restaurant and create its restaurant admin in the same step.
- Edit the house, pause it, or archive it.
- Turn each module on or off. Only real changes are written to the audit.
- Sign in as that restaurant admin and see the house read-only, including which modules were granted.
- Read the audit log: action, person, restaurant, IP, time. A restaurant admin sees only their house.

## API

| Method | Path | Who |
| --- | --- | --- |
| POST | `/api/auth/login` | Public |
| GET | `/api/auth/me` | Signed in |
| GET | `/api/dashboard` | Signed in, scoped |
| GET | `/api/restaurants?q=` | Super admin |
| GET | `/api/restaurants/{id}` | Super admin, or that house's admin |
| POST | `/api/restaurants` | Super admin |
| PUT | `/api/restaurants/{id}` | Super admin |
| DELETE | `/api/restaurants/{id}` | Super admin, soft delete |
| PUT | `/api/restaurants/{id}/modules` | Super admin |
| GET | `/api/audit-logs?page&pageSize&restaurantId&action` | Signed in, scoped |
| GET | `/api/health` | Public |

Errors return `{ "message": "..." }` and an HTTP status. `401` means sign in again, `403` means the role cannot do that, `409` means the admin email is already used.

## Not in this phase

Menu editing, the floor plan, QR orders, token screens, bills, UPI payment, WhatsApp, inventory, HR, attendance, Electron and Capacitor. The module switches exist so those desks can be granted before the screens arrive.
