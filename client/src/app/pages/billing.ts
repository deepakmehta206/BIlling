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
  readonly discountText = signal('');
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
    this.discountText.set('');
  }

  setGst(value: number) {
    this.gst.set(Number(value));
    this.clampDiscount();
  }

  setDiscount(event: Event) {
    const input = event.target as HTMLInputElement;
    let next = input.value.replace(/[^\d.]/g, '');
    const dot = next.indexOf('.');
    if (dot >= 0) next = `${next.slice(0, dot + 1)}${next.slice(dot + 1).replace(/\./g, '')}`;
    if (next.startsWith('.')) next = `0${next}`;
    const [whole, fraction] = next.split('.');
    next = fraction === undefined ? (whole ?? '').slice(0, 8) : `${(whole ?? '').slice(0, 8)}.${fraction.slice(0, 2)}`;
    if (next && !next.endsWith('.') && Number(next) > this.discountCap()) next = this.discountCap().toFixed(2);
    if (input.value !== next) input.value = next;
    this.discountText.set(next);
  }

  preview() {
    const ticket = this.picked();
    if (!ticket) return 0;
    return Math.max(0, this.discountCap() - this.discountAmount());
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
        discount: this.discountAmount(),
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

  private discountCap() {
    const ticket = this.picked();
    if (!ticket) return 0;
    const gst = Math.round(ticket.subtotal * this.gst()) / 100;
    return Math.round((ticket.subtotal + gst) * 100) / 100;
  }

  private discountAmount() {
    const amount = Number(this.discountText());
    if (!this.discountText() || Number.isNaN(amount)) return 0;
    return Math.min(amount, this.discountCap());
  }

  private clampDiscount() {
    const text = this.discountText();
    if (!text || text.endsWith('.')) return;
    if (Number(text) > this.discountCap()) this.discountText.set(this.discountCap().toFixed(2));
  }

  receive(ticket: OpenTicket) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api
      .createBill(id, {
        orderId: ticket.orderId,
        paymentMode: 'Upi',
        gstPercent: 5,
        discount: 0,
        received: true,
        publicOrigin: location.origin,
      })
      .subscribe({
        next: (bill) => {
          this.latest.set(bill);
          this.picked.set(null);
          this.toasts.show(`Bill ${bill.billNumber} cut. Payment received.`);
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

  qrTicket(ticket: OpenTicket) {
    return ticket.qrPng ? `data:image/png;base64,${ticket.qrPng}` : null;
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
