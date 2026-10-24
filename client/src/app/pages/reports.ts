import { CurrencyPipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { DayReport } from '../core/models';

@Component({
  selector: 'app-reports',
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './reports.html',
})
export class ReportsPage {
  private readonly api = inject(Api);
  readonly desk = inject(HouseService);
  readonly day = signal(this.today());
  readonly report = signal<DayReport | null>(null);
  readonly error = signal<string | null>(null);

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

  private today() {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const date = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${date}`;
  }
}
