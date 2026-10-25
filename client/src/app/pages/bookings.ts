import { DatePipe } from '@angular/common';
import { Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { Booking, FloorTable } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-bookings',
  imports: [FormsModule, DatePipe],
  templateUrl: './bookings.html',
})
export class BookingsPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly rows = signal<Booking[]>([]);
  readonly tables = signal<FloorTable[]>([]);
  readonly error = signal<string | null>(null);
  readonly day = signal(this.today());
  readonly tableId = signal('');
  readonly guestName = signal('');
  readonly guestPhone = signal('');
  readonly guestCount = signal(2);
  readonly starts = signal('');
  readonly open = signal<string | null>(null);
  readonly calCursor = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
  readonly hours = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  readonly minutes = Array.from({ length: 60 }, (_, index) => index);

  constructor() {
    this.load();
  }

  load() {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.bookings(id, this.day()).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
    this.api.tables(id).subscribe({
      next: (tables) => {
        this.tables.set(tables);
        if (!this.tableId() && tables[0]) this.tableId.set(tables[0].id);
      },
      error: () => undefined,
    });
  }

  setGuests(event: Event) {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 3);
    const table = this.tables().find((row) => row.id === this.tableId());
    const cap = table?.capacity ?? 99;
    const value = digits ? Math.min(cap, Number(digits)) : 0;
    const shown = value > 0 ? String(value) : '';
    if (input.value !== shown) input.value = shown;
    this.guestCount.set(value);
  }

  create() {
    const id = this.desk.activeId();
    if (!id || !this.starts() || this.guestCount() < 1) return;
    this.api
      .createBooking(id, {
        tableId: this.tableId(),
        guestName: this.guestName(),
        guestPhone: this.guestPhone(),
        guestCount: this.guestCount(),
        startsAt: new Date(this.starts()).toISOString(),
        durationMinutes: 90,
      })
      .subscribe({
        next: () => {
          this.toasts.show('Booking saved.');
          this.guestName.set('');
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  tableLabel() {
    const table = this.tables().find((row) => row.id === this.tableId());
    return table ? `${table.name} · ${table.capacity} seats` : 'Choose a table';
  }

  dayLabel(value: string) {
    return new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }).format(this.parseDay(value));
  }

  startsLabel() {
    if (!this.starts()) return 'Choose';
    return new Intl.DateTimeFormat('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    })
      .format(this.startsDate())
      .replace(',', ' ·');
  }

  monthLabel() {
    return new Intl.DateTimeFormat('en-IN', { month: 'long', year: 'numeric' }).format(this.calCursor());
  }

  days() {
    const cursor = this.calCursor();
    const first = new Date(cursor.getFullYear(), cursor.getMonth(), 1);
    const offset = (first.getDay() + 6) % 7;
    const start = new Date(cursor.getFullYear(), cursor.getMonth(), 1 - offset);
    const selected = this.open() === 'starts' ? this.startsDate() : this.parseDay(this.day());
    const today = new Date();
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      return {
        key: `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`,
        label: date.getDate(),
        inMonth: date.getMonth() === cursor.getMonth(),
        selected: this.sameDay(date, selected),
        today: this.sameDay(date, today),
        date,
      };
    });
  }

  hour12() {
    return this.startsDate().getHours() % 12 || 12;
  }

  isPm() {
    return this.startsDate().getHours() >= 12;
  }

  minuteNow() {
    return this.startsDate().getMinutes();
  }

  pad2(value: number) {
    return String(value).padStart(2, '0');
  }

  onOpen(event: Event, which: string) {
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target.closest('.pick-list') || target.closest('.when-panel')) return;
    this.open.update((current) => (current === which ? null : which));
    if (this.open() !== 'day' && this.open() !== 'starts') return;
    const selected = this.open() === 'starts' ? this.startsDate() : this.parseDay(this.day());
    this.calCursor.set(new Date(selected.getFullYear(), selected.getMonth(), 1));
    if (this.open() === 'starts') this.frameClock();
  }

  shiftMonth(delta: number) {
    const cursor = this.calCursor();
    this.calCursor.set(new Date(cursor.getFullYear(), cursor.getMonth() + delta, 1));
  }

  chooseTable(id: string) {
    this.tableId.set(id);
    this.open.set(null);
  }

  chooseFilterDay(date: Date) {
    this.day.set(this.isoDay(date));
    this.open.set(null);
    this.load();
  }

  chooseStartDay(date: Date) {
    const next = new Date(date);
    const current = this.startsDate();
    next.setHours(current.getHours(), current.getMinutes(), 0, 0);
    this.starts.set(this.localInput(next));
    this.calCursor.set(new Date(next.getFullYear(), next.getMonth(), 1));
  }

  chooseHour(hour: number) {
    const next = this.startsDate();
    next.setHours((hour % 12) + (this.isPm() ? 12 : 0), next.getMinutes(), 0, 0);
    this.starts.set(this.localInput(next));
    this.frameClock();
  }

  chooseMinute(minute: number) {
    const next = this.startsDate();
    next.setMinutes(minute, 0, 0);
    this.starts.set(this.localInput(next));
    this.frameClock();
  }

  setPeriod(pm: boolean) {
    const next = this.startsDate();
    const hour = next.getHours() % 12;
    next.setHours(pm ? hour + 12 : hour, next.getMinutes(), 0, 0);
    this.starts.set(this.localInput(next));
    this.frameClock();
  }

  @HostListener('document:click')
  closeOpen() {
    this.open.set(null);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape() {
    this.open.set(null);
  }

  private parseDay(value: string) {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year || 2026, (month || 1) - 1, day || 1);
  }

  private startsDate() {
    if (!this.starts()) return new Date();
    const parsed = new Date(this.starts());
    return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
  }

  private isoDay(date: Date) {
    return `${date.getFullYear()}-${this.pad2(date.getMonth() + 1)}-${this.pad2(date.getDate())}`;
  }

  private localInput(date: Date) {
    return `${this.isoDay(date)}T${this.pad2(date.getHours())}:${this.pad2(date.getMinutes())}`;
  }

  private sameDay(left: Date, right: Date) {
    return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth() && left.getDate() === right.getDate();
  }

  private frameClock() {
    setTimeout(() => {
      document.querySelectorAll('.clock .on').forEach((node) => {
        const el = node as HTMLElement;
        const col = el.parentElement;
        if (!col) return;
        col.scrollTop = el.offsetTop - col.clientHeight / 2 + el.clientHeight / 2;
      });
    });
  }

  private today() {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const date = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${date}`;
  }

  cancel(booking: Booking) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.cancelBooking(id, booking.id).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error)),
    });
  }
}
