# Setup

## What you need

- .NET SDK 10
- Node.js 22 and npm
- SQL Server, with Windows authentication, reachable as `localhost`

The API creates the `BillingSoftware` database on startup.

## API

From the repository root:

```powershell
dotnet run --project src/Billing.Api --launch-profile http
```

The dev profile listens on `http://localhost:5254`.

OpenAPI document: `http://localhost:5254/openapi/v1.json`

### Seed login

| | |
| --- | --- |
| Email | `superadmin@aangan.local` |
| Password | `SuperAdmin@123` |

Change `Seed` and `Jwt:Key` in `src/Billing.Api/appsettings.json` before this is hosted anywhere but your machine. If you change the seed email after the first run, the old super admin remains. The password is not rewritten on restart.

Connection string:

```text
Server=localhost;Database=BillingSoftware;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

### Migrations

Migrations live in `src/Billing.Infrastructure/Persistence/Migrations`. Startup applies them. To add one after a model change:

```powershell
dotnet ef migrations add YourChange --project src/Billing.Infrastructure --startup-project src/Billing.Api --output-dir Persistence/Migrations
```

`dotnet-ef` is a local tool (`dotnet-tools.json`).

## Web

```powershell
cd client
npm start
```

Open `http://localhost:4200`. The app calls `http://localhost:5254/api` (see `client/src/environments/environment.ts`).

## First walk-through

1. Sign in as the super admin.
2. Open a restaurant. Billing, menu, tables, QR order, token display, booking and reports start on.
3. Turn Inventory off or on and check Audit. The row includes your IP.
4. Sign out. Sign in as the restaurant admin you just created. The house is visible and the module switches are locked.
5. Archive the house as super admin. It leaves the list. In SQL Server the row remains with `IsDeleted = 1`.

## Phase 2 walk-through

1. Open a restaurant and choose **Run this house**. The sidebar switches to that house.
2. Add a menu category and a dish. Variants and add-ons are optional.
3. On Floor, add a table and drag it. Green is empty, amber is booked, red has an open token, grey is blocked.
4. Book the table for 90 minutes, or use Bookings for the day's list.
5. Open the table's guest link (`/t/{qrToken}`) on a phone. Place an order. The token number appears.
6. Open `/screen/{restaurantCode}` on a TV. Mark the ticket Ready from the floor panel. The screen moves the number.

## Phase 3 walk-through

1. On the house, turn **UPI QR** and **WhatsApp** on. The house needs a UPI id and an owner phone.
2. Open Billing. Choose an open token, keep GST at 5%, and cut a UPI bill.
3. The guest pay page shows the amount and the QR. The WhatsApp link opens a message for the owner.
4. Mark the bill paid. The pay page moves to Paid, and Audit shows `bill.created` and `bill.paid` with the IP.

## Phase 4 walk-through

1. Turn **Inventory** on for the house, then open Store.
2. Add an ingredient, set a reorder level, and receive a purchase.
3. On a dish, save a recipe. Quantity is per plate.
4. Cut a bill for that dish. The ingredient's on-hand falls by the recipe amount, and Audit shows `inventory.consumed`.

## Phase 5 walk-through

1. Turn **HRMS** and **Attendance** on for the house.
2. Open Staff. Hire someone with a house role and a password. They can sign in to that house.
3. Request leave and approve it. The house admin and HR can open this desk. Other roles cannot.
4. Open Attendance. Add a same-day shift, assign it, and check the person in. A check-in after the start plus grace is marked late.
5. Audit shows `staff.hired`, `staff.leave_decided`, and `attendance.checked_in` with the IP.
