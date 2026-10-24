import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
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

  create() {
    const id = this.desk.activeId();
    if (!id || !this.starts()) return;
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
