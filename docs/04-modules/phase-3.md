# Phase 3 — bill, UPI, WhatsApp

The counter. Billing must be on. UPI and WhatsApp are separate switches, off until the super admin grants them.

## Desks

| Module | What it does |
| --- | --- |
| BILLING | `/billing`. Turns today's token into a bill. GST is 0, 5, 12 or 18. Discount cannot exceed the total. |
| UPI | Cash needs nothing extra. UPI needs this module and a UPI id on the house. The guest opens `/pay/{billId}` and scans the QR. |
| WHATSAPP | Prepares a message for the owner phone and a `https://wa.me` link. The counter sends it from WhatsApp. No Business API key is stored. |

A bill copies the token's dish names and prices. The server calculates GST and the total. Cash is marked Paid at once. UPI stays Unpaid until staff press Mark paid. Cutting the bill marks the token Served and frees the table when no other open token remains. Print opens the browser dialog for a slip: house, bill number, lines, GST, discount, total, and the UPI QR when that bill was just cut. Today's list can print the same slip again.

Bill numbers start at 1 each India service date. One live token has one bill that is not cancelled.

## Audit

`bill.created` and `bill.paid` store the actor and IP. `OwnerNotices` keeps the text that was prepared, including a skipped row when the owner phone is missing.
