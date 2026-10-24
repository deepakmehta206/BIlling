import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly message = signal<string | null>(null);
  private timer = 0;

  show(message: string) {
    this.message.set(message);
    window.clearTimeout(this.timer);
    this.timer = window.setTimeout(() => this.message.set(null), 4200);
  }
}
