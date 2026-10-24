# Phase 5 — staff and attendance

Employees, leave, shifts, and the day's check-in. HRMS and Attendance start off until the super admin grants them. A restaurant admin still cannot grant modules.

## Staff

`/staff` is the staff book. It opens only when HRMS is on, and only for the super admin, the restaurant admin, or someone with the Hr role.

A hire creates a login on that house. The role is one of Manager, Cashier, Waiter, Kitchen, StoreKeeper, or Hr. The house admin is listed but is not hired, edited, or archived here. Archive is a soft delete of the login, the role link, the shift assignment, and any pending leave. Approved leave stays.

Leave is Casual, Sick, or Unpaid, at most 31 days, and cannot overlap another pending or approved request for the same person. Approve and reject are a second step. You cannot decide your own leave.

## Attendance

`/attendance` opens when Attendance is on. Anyone at the house can check themselves in and out. Only the house admin or HR can add shifts, assign them, or punch someone else.

A shift starts and ends on the same day. Grace is 0 to 90 minutes and must stay inside that day. One live assignment per person. One check-in per person per India service date. If the check-in is after the shift start plus grace, the row is late. No shift means the check-in is not late. An approved leave that covers today shows as Leave until the person checks in. The store of the punch is UTC. The service date is the India civil date.

## Audit

`staff.hired`, `staff.updated`, `staff.archived`, `staff.leave_requested`, `staff.leave_decided`, `attendance.shift_saved`, `attendance.shift_archived`, `attendance.assigned`, `attendance.checked_in`, and `attendance.checked_out` store the actor and IP.
