import { CurrencyPipe } from '@angular/common';
import { Component, effect, HostListener, inject, signal } from '@angular/core';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { DayReport } from '../core/models';

@Component({
  selector: 'app-reports',
  imports: [CurrencyPipe],
  templateUrl: './reports.html',
})
export class ReportsPage {
  private readonly api = inject(Api);
  readonly desk = inject(HouseService);
  readonly day = signal(this.today());
  readonly report = signal<DayReport | null>(null);
  readonly error = signal<string | null>(null);
  readonly open = signal(false);
  readonly calCursor = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

  constructor() {
    effect(() => {
      const id = this.desk.activeId();
      const day = this.day();
      if (!id) return;
      this.api.dayReport(id, day).subscribe({
        next: (report) => {
          this.report.set(report);
          this.error.set(null);
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
    });
  }

  dayLabel() {
    return new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }).format(this.parseDay(this.day()));
  }

  monthLabel() {
    return new Intl.DateTimeFormat('en-IN', { month: 'long', year: 'numeric' }).format(this.calCursor());
  }

  days() {
    const cursor = this.calCursor();
    const first = new Date(cursor.getFullYear(), cursor.getMonth(), 1);
    const offset = (first.getDay() + 6) % 7;
    const start = new Date(cursor.getFullYear(), cursor.getMonth(), 1 - offset);
    const selected = this.parseDay(this.day());
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

  onOpen(event: Event) {
    event.stopPropagation();
    const target = event.target as HTMLElement;
    if (target.closest('.when-panel')) return;
    this.open.update((current) => !current);
    if (!this.open()) return;
    const selected = this.parseDay(this.day());
    this.calCursor.set(new Date(selected.getFullYear(), selected.getMonth(), 1));
  }

  shiftMonth(delta: number) {
    const cursor = this.calCursor();
    this.calCursor.set(new Date(cursor.getFullYear(), cursor.getMonth() + delta, 1));
  }

  chooseDay(date: Date) {
    this.day.set(this.isoDay(date));
    this.open.set(false);
  }

  @HostListener('document:click')
  closeOpen() {
    this.open.set(false);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape() {
    this.open.set(false);
  }

  private parseDay(value: string) {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year || 2026, (month || 1) - 1, day || 1);
  }

  private isoDay(date: Date) {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private sameDay(left: Date, right: Date) {
    return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth() && left.getDate() === right.getDate();
  }

  private today() {
    return this.isoDay(new Date());
  }
}
