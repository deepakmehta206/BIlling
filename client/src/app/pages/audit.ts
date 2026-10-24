import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { PagedAudit } from '../core/models';

@Component({
  selector: 'app-audit',
  imports: [DatePipe],
  templateUrl: './audit.html',
})
export class AuditPage {
  private readonly api = inject(Api);
  readonly pageData = signal<PagedAudit | null>(null);
  readonly error = signal<string | null>(null);
  readonly page = signal(1);

  constructor() {
    this.load(1);
  }

  load(page: number) {
    this.page.set(page);
    this.api.audit(page).subscribe({
      next: (data) => {
        this.pageData.set(data);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  get lastPage() {
    const data = this.pageData();
    if (!data || data.pageSize === 0) {
      return 1;
    }

    return Math.max(1, Math.ceil(data.total / data.pageSize));
  }
}
