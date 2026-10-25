# Phase 2 — menu, floor, booking, QR, tokens

The dining room. A house can only use a desk when the super admin left that module on.

## Desks

| Module | Screen | Who |
| --- | --- | --- |
| MENU | `/menu` | Staff. Categories and dishes, with sizes, add-ons and a photo. |
| TABLES | `/floor` | Staff. Create a table, drag it, block it, archive it. |
| BOOKING | `/bookings` and the floor panel | Staff. Hold a table for a guest. |
| QR_ORDER | `/t/{qrToken}` | Guest, no login. The table's QR opens this page. |
| TOKEN_DISPLAY | `/screen/{restaurantCode}` | Kitchen or the dining room TV. No login. |

Turning a module off returns 403 from the matching API. The public QR page also checks `QR_ORDER`. The public token screen checks `TOKEN_DISPLAY`.

## Floor colours

| Colour | Meaning |
| --- | --- |
| Green | Empty |
| Amber | A booking covers the moment you are looking at |
| Red | An order is Preparing or Ready |
| Grey | Blocked. Guests cannot order and staff cannot book it. |

A new order marks the table Occupied. Served or Cancelled returns it to Available when no other open order remains. A booking does not by itself occupy the table.

## Orders

A dish price, and each size or add-on price, can have 4 digits and 2 decimals, up to 9999.99. A photo is a JPEG kept beside the API and shown on the menu card and the guest page. The path is stored on the dish.

The server prices every line. The guest page shows a running total for convenience; the saved price is the menu price at that moment, plus the chosen variant and add-ons. If a dish has sizes, one size is required. Token numbers start at 1 each India service date and rise by one. The guest sees Preparing until staff marks Ready, then Served.

Live updates use SignalR at `/hubs/floor`. A page joins `house:{restaurantId}` and reloads when the event `changed` arrives.

## Audit

Menu, table, booking and order writes are soft deletes or status changes. Each row stores CreatedIp / UpdatedIp / DeletedIp. A guest order records the guest name, or `guest`, as the actor.
