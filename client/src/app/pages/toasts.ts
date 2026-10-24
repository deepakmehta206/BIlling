import { Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toasts',
  template: `
    @if (toasts.message(); as message) {
      <div class="toast" role="status">{{ message }}</div>
    }
  `,
})
export class Toasts {
  readonly toasts = inject(ToastService);
}
