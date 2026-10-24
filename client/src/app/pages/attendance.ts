import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
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
