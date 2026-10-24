import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/http-error';
import { Dashboard } from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardPage {
  private readonly api = inject(Api);
  readonly auth = inject(AuthService);
  readonly data = signal<Dashboard | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    this.api.dashboard().subscribe({
      next: (data) => this.data.set(data),
      error: (error) => this.error.set(errorMessage(error)),
    });
  }
}
