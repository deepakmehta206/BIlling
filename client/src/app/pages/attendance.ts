import { DatePipe } from '@angular/common';
import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { AttendanceDesk } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-attendance',
  imports: [FormsModule, DatePipe],
  templateUrl: './attendance.html',
})
export class AttendancePage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly board = signal<AttendanceDesk | null>(null);
  readonly error = signal<string | null>(null);
  readonly shiftName = signal('Morning');
  readonly startsAt = signal('09:00');
  readonly endsAt = signal('18:00');
  readonly grace = signal(10);
  readonly assignUser = signal('');
  readonly assignShift = signal('');
  readonly open = signal<string | null>(null);
  readonly hours = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  readonly minutes = Array.from({ length: 60 }, (_, index) => index);

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

  load() {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.attendance(id).subscribe({
      next: (board) => {
        this.board.set(board);
        this.error.set(null);
        if (!this.assignUser() && board.rows[0]) this.assignUser.set(board.rows[0].userId);
        if (!this.assignShift() && board.shifts[0]) this.assignShift.set(board.shifts[0].id);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  setGrace(event: Event) {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 2);
    const value = digits ? Math.min(90, Number(digits)) : 0;
    const shown = value > 0 ? String(value) : '';
    if (input.value !== shown) input.value = shown;
    this.grace.set(value);
  }

  saveShift() {
    const id = this.desk.activeId();
    if (!id || this.shiftName().trim().length < 2) return;
    this.api
      .saveShift(id, {
        name: this.shiftName().trim(),
        startsAt: this.startsAt(),
        endsAt: this.endsAt(),
        graceMinutes: Number(this.grace() || 0),
      })
      .subscribe({
        next: (shift) => {
          this.toasts.show(`${shift.name} is on the roster.`);
          this.assignShift.set(shift.id);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  personLabel() {
    return this.board()?.rows.find((row) => row.userId === this.assignUser())?.fullName ?? 'Choose a person';
  }

  shiftLabel() {
    return this.board()?.shifts.find((shift) => shift.id === this.assignShift())?.name ?? 'Choose a shift';
  }

  timeLabel(value: string) {
    const date = new Date();
    date.setHours(this.hour24(value), this.minuteNow(value), 0, 0);
    return new Intl.DateTimeFormat('en-IN', { hour: 'numeric', minute: '2-digit', hour12: true }).format(date);
  }

  hour12(value: string) {
    return this.hour24(value) % 12 || 12;
  }

  isPm(value: string) {
    return this.hour24(value) >= 12;
  }

  minuteNow(value: string) {
    return Number(value.split(':')[1]) || 0;
  }

  pad2(value: number) {
    return String(value).padStart(2, '0');
  }

  onOpen(event: Event, which: string) {
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target.closest('.when-panel') || target.closest('.pick-list')) return;
    this.open.update((current) => (current === which ? null : which));
    if (this.open() === 'starts' || this.open() === 'ends') this.frameClock();
  }

  chooseHour(hour: number) {
    const current = this.activeTime();
    this.writeTime((hour % 12) + (this.isPm(current) ? 12 : 0), this.minuteNow(current));
  }

  chooseMinute(minute: number) {
    const current = this.activeTime();
    this.writeTime(this.hour24(current), minute);
  }

  setPeriod(pm: boolean) {
    const current = this.activeTime();
    const hour = this.hour24(current) % 12;
    this.writeTime(pm ? hour + 12 : hour, this.minuteNow(current));
  }

  choosePerson(id: string) {
    this.assignUser.set(id);
    this.open.set(null);
  }

  chooseShift(id: string) {
    this.assignShift.set(id);
    this.open.set(null);
  }

  @HostListener('document:click')
  closeOpen() {
    this.open.set(null);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape() {
    this.open.set(null);
  }

  assign() {
    const id = this.desk.activeId();
    if (!id || !this.assignUser() || !this.assignShift()) return;
    this.api.assignShift(id, this.assignUser(), this.assignShift()).subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  private hour24(value: string) {
    return Number(value.split(':')[0]) || 0;
  }

  private activeTime() {
    return this.open() === 'ends' ? this.endsAt() : this.startsAt();
  }

  private writeTime(hours: number, minute: number) {
    const value = `${this.pad2(hours)}:${this.pad2(minute)}`;
    if (this.open() === 'ends') this.endsAt.set(value);
    else this.startsAt.set(value);
    this.frameClock();
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

  punch(userId: string, direction: 'in' | 'out') {
    const id = this.desk.activeId();
    if (!id) return;
    const call = direction === 'in' ? this.api.checkIn(id, userId) : this.api.checkOut(id, userId);
    call.subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }
}
