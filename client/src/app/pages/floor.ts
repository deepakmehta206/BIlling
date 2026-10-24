import { DatePipe } from '@angular/common';
import { Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { FloorTable, Ticket } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-floor',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './floor.html',
})
export class FloorPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly room = viewChild<ElementRef<HTMLElement>>('room');
  readonly tables = signal<FloorTable[]>([]);
  readonly tickets = signal<Ticket[]>([]);
  readonly selected = signal<FloorTable | null>(null);
  readonly error = signal<string | null>(null);
  readonly moment = signal(this.localInput(new Date()));
  readonly tableName = signal('');
  readonly seats = signal(4);
  readonly section = signal('Hall');
  readonly guestName = signal('');
  readonly guestPhone = signal('');
  readonly guestCount = signal(2);
  private dragged = false;

  constructor() {
    const live = inject(LiveService);
    const id = this.desk.activeId();
    if (id) live.connect(id);
    this.load();
    let last = live.tick();
    const timer = setInterval(() => {
      const next = live.tick();
      if (next !== last) {
        last = next;
        this.load();
      }
    }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  tone(table: FloorTable) {
    if (table.status === 'Blocked') return 'blocked';
    if (table.status === 'Occupied' || table.openToken) return 'occupied';
    if (table.booking) return 'booked';
    return 'available';
  }

  guestLink(table: FloorTable) {
    return `${location.origin}/t/${table.qrToken}`;
  }

  addTable() {
    const id = this.desk.activeId();
    if (!id || !this.tableName().trim()) return;
    this.api
      .saveTable(id, { name: this.tableName(), section: this.section(), capacity: this.seats(), status: 'Available', posX: 0, posY: 0 })
      .subscribe({
        next: () => {
          this.tableName.set('');
          this.toasts.show('Table is on the floor.');
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  pick(table: FloorTable) {
    if (this.dragged) {
      this.dragged = false;
      return;
    }
    this.selected.set(table);
    this.guestCount.set(Math.min(2, table.capacity));
  }

  startDrag(event: PointerEvent, table: FloorTable) {
    const room = this.room()?.nativeElement;
    if (!room) return;
    const originX = event.clientX;
    const originY = event.clientY;
    this.dragged = false;
    const move = (ev: PointerEvent) => {
      if (Math.abs(ev.clientX - originX) + Math.abs(ev.clientY - originY) > 6) this.dragged = true;
      const rect = room.getBoundingClientRect();
      const posX = this.clamp(((ev.clientX - rect.left) / rect.width) * 100);
      const posY = this.clamp(((ev.clientY - rect.top) / rect.height) * 100);
      this.tables.update((rows) => rows.map((row) => (row.id === table.id ? { ...row, posX, posY } : row)));
    };
    const up = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      if (!this.dragged) return;
      const current = this.tables().find((row) => row.id === table.id);
      const id = this.desk.activeId();
      if (!current || !id) return;
      this.api.saveTable(id, current).subscribe({ error: (error) => this.error.set(errorMessage(error)) });
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
  }

  saveSelected(status?: string) {
    const id = this.desk.activeId();
    const table = this.selected();
    if (!id || !table) return;
    this.api.saveTable(id, { ...table, status: status ?? table.status }).subscribe({
      next: (saved) => {
        this.selected.set(saved);
        this.toasts.show(`${saved.name} is ${saved.status}.`);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  archiveSelected() {
    const id = this.desk.activeId();
    const table = this.selected();
    if (!id || !table) return;
    this.api.deleteTable(id, table.id).subscribe({
      next: (result) => {
        this.selected.set(null);
        this.toasts.show(result.message);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  book() {
    const id = this.desk.activeId();
    const table = this.selected();
    if (!id || !table) return;
    this.api
      .createBooking(id, {
        tableId: table.id,
        guestName: this.guestName(),
        guestPhone: this.guestPhone(),
        guestCount: this.guestCount(),
        startsAt: new Date(this.moment()).toISOString(),
        durationMinutes: 90,
      })
      .subscribe({
        next: () => {
          this.toasts.show('Table booked.');
          this.guestName.set('');
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  mark(ticket: Ticket, status: string) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.setTicketStatus(id, ticket.id, status).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  showAt(value: string) {
    this.moment.set(value);
    this.load();
  }

  private load() {
    const id = this.desk.activeId();
    if (!id) return;
    const at = new Date(this.moment()).toISOString();
    this.api.tables(id, at).subscribe({
      next: (tables) => {
        this.tables.set(tables);
        const current = this.selected();
        if (current) this.selected.set(tables.find((table) => table.id === current.id) ?? null);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
    this.api.tickets(id).subscribe({
      next: (tickets) => this.tickets.set(tickets),
      error: () => undefined,
    });
  }

  private clamp(value: number) {
    return Math.min(84, Math.max(2, Math.round(value)));
  }

  private localInput(date: Date) {
    const pad = (value: number) => String(value).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
