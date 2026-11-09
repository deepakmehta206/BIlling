import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Api } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { Dashboard } from '../core/models';

export type DeskStep = {
  n: number;
  title: string;
  hint: string;
  path: string | string[];
};

export type DeskPulse = {
  openTokens: number;
  preparing: number;
  ready: number;
  occupiedTables: number;
  toBill: number;
  unpaidBills: number;
  paidBills: number;
  sales: number;
  bookings: number;
};

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  private readonly api = inject(Api);
  private readonly live = inject(LiveService);
  readonly auth = inject(AuthService);
  readonly desk = inject(HouseService);
  readonly data = signal<Dashboard | null>(null);
  readonly pulse = signal<DeskPulse | null>(null);
  readonly error = signal<string | null>(null);
  readonly steps = signal<DeskStep[]>([]);

  constructor() {
    this.loadBook();
    effect(() => {
      const id = this.desk.activeId();
      this.steps.set(this.buildSteps());
      this.loadPulse(id);
      if (id) this.live.connect(id);
    });

    let last = this.live.tick();
    const timer = setInterval(() => {
      const next = this.live.tick();
      if (next === last) return;
      last = next;
      this.loadPulse(this.desk.activeId());
    }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  houseTitle() {
    if (this.auth.isSuperAdmin()) {
      return this.desk.name() || 'Platform desk';
    }
    return this.auth.user()?.restaurantName || 'Your house';
  }

  private loadBook() {
    this.api.dashboard().subscribe({
      next: (data) => {
        this.data.set(data);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  private loadPulse(id: string | null) {
    if (!id) {
      this.pulse.set(null);
      return;
    }

    const day = this.serviceDay();
    const tables$ = this.desk.has('TABLES') ? this.api.tables(id).pipe(catchError(() => of([]))) : of([]);
    const tickets$ = this.desk.has('TABLES') ? this.api.tickets(id).pipe(catchError(() => of([]))) : of([]);
    const bills$ = this.desk.has('BILLING') ? this.api.billingDesk(id).pipe(catchError(() => of(null))) : of(null);
    const report$ = this.desk.has('REPORTS') ? this.api.dayReport(id, day).pipe(catchError(() => of(null))) : of(null);
    const bookings$ = this.desk.has('BOOKING') ? this.api.bookings(id, day).pipe(catchError(() => of([]))) : of([]);

    forkJoin({ tables: tables$, tickets: tickets$, bills: bills$, report: report$, bookings: bookings$ }).subscribe({
      next: ({ tables, tickets, bills, report, bookings }) => {
        this.pulse.set({
          openTokens: tickets.length,
          preparing: tickets.filter((ticket) => ticket.status === 'Preparing').length,
          ready: tickets.filter((ticket) => ticket.status === 'Ready').length,
          occupiedTables: tables.filter((table) => table.status === 'Occupied' || table.openToken).length,
          toBill: bills?.tickets.length ?? 0,
          unpaidBills: report?.unpaidBills ?? bills?.bills.filter((bill) => bill.status === 'Unpaid').length ?? 0,
          paidBills: report?.paidBills ?? bills?.bills.filter((bill) => bill.status === 'Paid').length ?? 0,
          sales: report?.sales ?? 0,
          bookings: bookings.filter((row) => row.status === 'Booked' || row.status === 'Seated').length,
        });
      },
      error: () => this.pulse.set(null),
    });
  }

  private buildSteps(): DeskStep[] {
    const steps: DeskStep[] = [];
    const push = (title: string, hint: string, path: string | string[]) => {
      steps.push({ n: steps.length + 1, title, hint, path });
    };

    if (this.auth.isSuperAdmin()) {
      push('Houses', 'Open or pick a restaurant', '/restaurants');
    } else if (this.auth.user()?.restaurantId) {
      push('Your house', 'Name, UPI, and modules', ['/restaurants', this.auth.user()!.restaurantId!]);
    }

    if (this.desk.has('MENU')) push('Menu', 'Dishes, sizes, and photos', '/menu');
    if (this.desk.has('TABLES')) push('Floor', 'Tables, QR, and open tokens', '/floor');
    if (this.desk.has('TOKEN_DISPLAY') && this.desk.code()) {
      push('Token screen', 'Kitchen queue board', ['/screen', this.desk.code()!]);
    }
    if (this.desk.has('BILLING')) push('Billing', 'Cut the bill and take payment', '/billing');
    if (this.desk.has('INVENTORY')) push('Store', 'Stock and recipes', '/inventory');
    if (this.desk.has('BOOKING')) push('Bookings', 'Today’s reservations', '/bookings');
    if (this.desk.has('HRMS') && this.canStaff()) push('Staff', 'Team logins and roles', '/staff');
    if (this.desk.has('ATTENDANCE')) push('Attendance', 'Shifts and leave', '/attendance');
    if (this.desk.has('REPORTS')) push('Reports', 'Today’s sales and GST', '/reports');
    push('Audit', 'Who changed what', '/audit');
    return steps;
  }

  private canStaff() {
    const user = this.auth.user();
    if (!user) return false;
    return user.isSuperAdmin || user.roles.includes('RestaurantAdmin') || user.roles.includes('Hr');
  }

  private serviceDay() {
    return new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Asia/Kolkata',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(new Date());
  }
}
