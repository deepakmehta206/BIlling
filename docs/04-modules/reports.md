# Reports

The day book. The Reports module must be on. It is on for a new house.

`/reports` reads one India service date. The date picker sends `yyyy-MM-dd`. An empty date is today in India.

Sales, cash, UPI, discount, and GST are the paid bills. Unpaid bills stay in "still open" and in the day-end list, and they are not in the tax or dish totals. Cancelled bills are omitted. Tax is grouped by the GST rate stored on each paid bill. Dishes are the top eight lines on paid bills, priced as quantity times the unit price copied onto the bill.

The server calculates every figure. The page only displays them.
