# Aangan

What this product is, and who holds the keys.

## Product

Aangan is a multi-restaurant billing platform. One company (the platform) hosts many restaurants. Each restaurant has its own admin, and later its own menu, floor, bills, stock and staff.

Phase 1 is the platform: identity, restaurants, module grants, and an audit trail. Phase 2 is the dining room: menu, a visual floor, table booking, a guest QR order, and a token screen. POS billing, UPI, WhatsApp, inventory, HR and attendance come later.

## People

| Person | Scope |
| --- | --- |
| Super admin | Every restaurant. Opens houses, pauses them, and grants modules. |
| Restaurant admin | One restaurant. Can see the house and which modules were granted. Cannot change another house. |
| Later roles | Manager, cashier, waiter, kitchen, store keeper, HR. The roles exist in the database so later phases can assign them. |

## Rules that do not change

- SQL Server is the only database.
- A delete is a soft delete: `IsDeleted`, who, when, and the IP. The row stays.
- Create and update stamp the user and the IP on that row.
- The super admin decides which module a restaurant receives. A restaurant admin cannot grant modules to themselves.

## Phase map

1. **Now.** Auth, restaurants, module switches, audit, this documentation.
2. **Now.** Menu, visual tables, booking, QR order, token display.
3. POS billing, UPI QR for the guest, WhatsApp message to the owner.
4. Inventory and recipe stock.
5. HRMS and attendance.
6. Electron desktop and Capacitor mobile from the same Angular app.
