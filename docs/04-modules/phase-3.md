# Phase 3 — bill, UPI, WhatsApp

The counter. Billing must be on. UPI and WhatsApp are separate switches, off until the super admin grants them.

## Desks

| Module | What it does |
| --- | --- |
| BILLING | `/billing`. Turns today's token into a bill. GST is 0, 5, 12 or 18. Discount cannot exceed the total. |
| UPI | Cash needs nothing extra. UPI needs this module and a UPI id on the house. The guest opens `/pay/{billId}` and scans the QR. |
| WHATSAPP | Prepares a message for the owner phone and a `https://wa.me` link. The counter sends it from WhatsApp. No Business API key is stored. |
| PLAY | Guest plays Thali catch on the token while the food cooks. Off until the super admin grants it. A restaurant admin cannot turn it on. |

One claim per token. The server turns the score into 0, 5, 10 or 15 percent and stores it on the order. The guest token shows that percent. Cutting the bill uses it as the discount when the counter has not typed a larger one, and it never exceeds the bill. The unpaid UPI total drops by the same amount.

A bill copies the token's dish names and prices. The server calculates GST and the total. The guest token shows a UPI QR for subtotal plus 5% GST, and that same QR is stored on the bill. Pressing Paid cuts the bill as soon as payment is received: the token is Served, the table is freed when no other open token remains, stock is consumed, and the owner WhatsApp is prepared. Cash still cuts a paid bill from the form, with the GST and discount the counter chooses. An unpaid UPI bill can still be marked paid later. Print opens an 80mm slip with the house, lines, GST, discount, total, and the payment QR. Today's list prints that same slip.

Bill numbers start at 1 each India service date. One live token has one bill that is not cancelled.

## Audit

`bill.created` and `bill.paid` store the actor and IP. `OwnerNotices` keeps the text that was prepared, including a skipped row when the owner phone is missing.
