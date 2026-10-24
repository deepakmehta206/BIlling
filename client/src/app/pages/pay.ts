import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { LiveService } from '../core/live.service';
import { PublicBill } from '../core/models';

@Component({
  selector: 'app-pay',
  imports: [CurrencyPipe],
  templateUrl: './pay.html',
})
export class PayPage {
  private readonly api = inject(Api);
  readonly bill = signal<PublicBill | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    const id = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';
    const live = inject(LiveService);
    const load = () =>
      this.api.publicBill(id).subscribe({
        next: (bill) => {
          this.bill.set(bill);
          this.error.set(null);
          live.connect(bill.restaurantId);
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
    load();
    let last = 0;
    const timer = setInterval(() => {
      const tick = live.tick();
      if (tick !== last) {
        last = tick;
        load();
      }
    }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  qr(bill: PublicBill) {
    return bill.qrPng ? `data:image/png;base64,${bill.qrPng}` : null;
  }
}
