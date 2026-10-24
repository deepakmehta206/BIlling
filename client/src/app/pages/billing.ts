import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { BillingDesk, HouseBill, OpenTicket } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-billing',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  templateUrl: './billing.html',
})
export class BillingPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly board = signal<BillingDesk | null>(null);
  readonly picked = signal<OpenTicket | null>(null);
  readonly latest = signal<HouseBill | null>(null);
  readonly error = signal<string | null>(null);
  readonly gst = signal(5);
  readonly discount = signal(0);
  readonly mode = signal<'Cash' | 'Upi'>('Upi');
  readonly slip = signal<HouseBill | null>(null);
  readonly printedAt = signal<Date | null>(null);

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
    this.api.billingDesk(id).subscribe({
      next: (board) => {
        this.board.set(board);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  pick(ticket: OpenTicket) {
    this.picked.set(ticket);
    this.latest.set(null);
  }

  preview() {
    const ticket = this.picked();
    if (!ticket) return 0;
    const gst = Math.round(ticket.subtotal * this.gst()) / 100;
    return Math.max(0, ticket.subtotal + gst - Number(this.discount() || 0));
  }

  cut() {
    const id = this.desk.activeId();
    const ticket = this.picked();
    if (!id || !ticket) return;
    this.api
      .createBill(id, {
        orderId: ticket.orderId,
        paymentMode: this.mode(),
        gstPercent: this.gst(),
        discount: Number(this.discount() || 0),
        publicOrigin: location.origin,
      })
      .subscribe({
        next: (bill) => {
          this.latest.set(bill);
          this.picked.set(null);
          this.toasts.show(`Bill ${bill.billNumber} is ${bill.status}.`);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  paid(bill: HouseBill) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.markBillPaid(id, bill.id).subscribe({
      next: (saved) => {
        this.latest.set(saved);
        this.toasts.show(`Bill ${saved.billNumber} is paid.`);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  qr(bill: HouseBill) {
    return bill.qrPng ? `data:image/png;base64,${bill.qrPng}` : null;
  }

  payLink(bill: HouseBill) {
    return `${location.origin}/pay/${bill.id}`;
  }

  print(bill: HouseBill) {
    this.slip.set(bill);
    this.printedAt.set(new Date());
    setTimeout(() => window.print());
  }
}
