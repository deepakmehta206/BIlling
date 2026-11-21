import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { BillingDesk, HouseBill, MenuItem, OpenTicket } from '../core/models';
import { ToastService } from '../core/toast.service';

interface CartLine {
  key: string;
  itemId: string;
  name: string;
  variantId?: string;
  addonIds: string[];
  quantity: number;
  unit: number;
}

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
  readonly dishes = signal<MenuItem[]>([]);
  readonly picked = signal<OpenTicket | null>(null);
  readonly latest = signal<HouseBill | null>(null);
  readonly error = signal<string | null>(null);
  readonly gst = signal(5);
  readonly discountText = signal('');
  readonly mode = signal<'Cash' | 'Upi'>('Cash');
  readonly slip = signal<HouseBill | null>(null);
  readonly printedAt = signal<Date | null>(null);
  readonly cart = signal<CartLine[]>([]);
  readonly guestName = signal('');
  readonly dishQuery = signal('');
  readonly openPick = signal<'gst' | string | null>(null);
  readonly choices = signal<Record<string, { variantId?: string; addonIds: string[] }>>({});
  readonly gstRates = [0, 5, 12, 18];
  private pendingToken: number | null = null;

  constructor() {
    const live = inject(LiveService);
    const id = this.desk.activeId();
    if (id) live.connect(id);
    this.load();
    this.loadMenu();
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
        if (this.pendingToken != null) {
          const ticket = board.tickets.find((row) => row.tokenNumber === this.pendingToken);
          this.pendingToken = null;
          if (ticket) this.pick(ticket);
        }
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  loadMenu() {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.menu(id).subscribe({
      next: (board) => this.dishes.set(board.items.filter((item) => item.isAvailable)),
      error: () => this.dishes.set([]),
    });
  }

  categories() {
    const q = this.dishQuery().trim().toLowerCase();
    const names = new Map<string, MenuItem[]>();
    for (const dish of this.dishes()) {
      if (q) {
        const hay = `${dish.name} ${dish.categoryName}`.toLowerCase();
        if (!hay.includes(q)) continue;
      }
      const list = names.get(dish.categoryName) ?? [];
      list.push(dish);
      names.set(dish.categoryName, list);
    }
    return [...names.entries()];
  }

  setDishQuery(event: Event) {
    const input = event.target as HTMLInputElement;
    this.dishQuery.set(input.value);
  }

  choice(item: MenuItem) {
    return this.choices()[item.id] ?? { addonIds: [] as string[] };
  }

  pickVariant(item: MenuItem, variantId: string) {
    const current = this.choice(item);
    this.choices.update((map) => ({ ...map, [item.id]: { ...current, variantId } }));
  }

  toggleAddon(item: MenuItem, addonId: string) {
    const current = this.choice(item);
    const addonIds = current.addonIds.includes(addonId)
      ? current.addonIds.filter((id) => id !== addonId)
      : [...current.addonIds, addonId];
    this.choices.update((map) => ({ ...map, [item.id]: { ...current, addonIds } }));
  }

  add(item: MenuItem) {
    const choice = this.choice(item);
    if (item.variants.length && !choice.variantId) {
      this.error.set(`Choose a size for ${item.name}.`);
      return;
    }
    const variant = item.variants.find((row) => row.id === choice.variantId);
    const addons = item.addons.filter((row) => choice.addonIds.includes(row.id || ''));
    const unit = (variant?.price ?? item.price) + addons.reduce((sum, row) => sum + row.price, 0);
    const key = `${item.id}:${variant?.id ?? ''}:${addons.map((row) => row.id).join(',')}`;
    this.cart.update((lines) => {
      const existing = lines.find((line) => line.key === key);
      if (existing) return lines.map((line) => (line.key === key ? { ...line, quantity: Math.min(20, line.quantity + 1) } : line));
      return [
        ...lines,
        {
          key,
          itemId: item.id,
          name: variant ? `${item.name} (${variant.name})` : item.name,
          variantId: variant?.id ?? undefined,
          addonIds: addons.map((row) => row.id || ''),
          quantity: 1,
          unit,
        },
      ];
    });
    this.error.set(null);
  }

  setQty(key: string, delta: number) {
    this.cart.update((lines) =>
      lines
        .map((line) => (line.key === key ? { ...line, quantity: line.quantity + delta } : line))
        .filter((line) => line.quantity > 0),
    );
  }

  cartCount() {
    return this.cart().reduce((sum, line) => sum + line.quantity, 0);
  }

  cartTotal() {
    return this.cart().reduce((sum, line) => sum + line.unit * line.quantity, 0);
  }

  placeCounter() {
    const id = this.desk.activeId();
    if (!id || !this.cart().length) return;
    this.api
      .placeCounterOrder(id, {
        guestName: this.guestName().trim() || null,
        lines: this.cart().map((line) => ({
          itemId: line.itemId,
          variantId: line.variantId || null,
          addonIds: line.addonIds,
          quantity: line.quantity,
        })),
      })
      .subscribe({
        next: (ticket) => {
          this.cart.set([]);
          this.guestName.set('');
          this.pendingToken = ticket.tokenNumber;
          this.toasts.show(`Token ${ticket.tokenNumber} is on the counter.`);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  pick(ticket: OpenTicket) {
    this.picked.set(ticket);
    this.latest.set(null);
    const percent = ticket.playDiscountPercent ?? 0;
    const rupees = percent > 0 ? Math.round(ticket.subtotal * percent) / 100 : 0;
    this.discountText.set(rupees > 0 ? rupees.toFixed(2) : '');
  }

  setGst(value: number) {
    this.gst.set(Number(value));
    this.openPick.set(null);
    this.clampDiscount();
  }

  togglePick(which: 'gst' | string) {
    this.openPick.update((current) => (current === which ? null : which));
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

  @HostListener('document:click')
  closePickOnOutside() {
    this.openPick.set(null);
  }

  @HostListener('document:keydown.escape')
  closePickOnEscape() {
    this.openPick.set(null);
  }
}
