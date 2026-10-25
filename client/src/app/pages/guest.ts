import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { LiveService } from '../core/live.service';
import { PublicDish, PublicOrder, PublicTable } from '../core/models';
import { ThaliGame } from './thali';
import { apiOrigin } from '../../environments/environment';

interface CartLine {
  key: string;
  itemId: string;
  name: string;
  variantId?: string;
  variantName?: string;
  addonIds: string[];
  quantity: number;
  unit: number;
}

@Component({
  selector: 'app-guest',
  imports: [FormsModule, CurrencyPipe, ThaliGame],
  templateUrl: './guest.html',
})
export class GuestPage {
  private readonly api = inject(Api);
  private readonly live = inject(LiveService);
  readonly table = signal<PublicTable | null>(null);
  readonly order = signal<PublicOrder | null>(null);
  readonly cart = signal<CartLine[]>([]);
  readonly cartOpen = signal(false);
  readonly playing = signal(false);
  readonly error = signal<string | null>(null);
  readonly guestName = signal('');
  readonly choices = signal<Record<string, { variantId?: string; addonIds: string[] }>>({});
  private qr = '';

  constructor() {
    this.qr = inject(ActivatedRoute).snapshot.paramMap.get('token') ?? '';
    this.api.publicTable(this.qr).subscribe({
      next: (table) => {
        this.table.set(table);
        this.live.connect(table.restaurantId);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
    let last = 0;
    const timer = setInterval(() => {
      const tick = this.live.tick();
      const current = this.order();
      if (current && tick !== last) {
        last = tick;
        this.api.publicOrder(current.id).subscribe((order) => this.order.set(order));
      }
    }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  photo(path: string | null) {
    if (!path) return null;
    return path.startsWith('http') ? path : `${apiOrigin()}${path}`;
  }

  choice(item: PublicDish) {
    return this.choices()[item.id] ?? { addonIds: [] as string[] };
  }

  pickVariant(item: PublicDish, variantId: string) {
    const current = this.choice(item);
    this.choices.update((map) => ({ ...map, [item.id]: { ...current, variantId } }));
  }

  toggleAddon(item: PublicDish, addonId: string) {
    const current = this.choice(item);
    const addonIds = current.addonIds.includes(addonId)
      ? current.addonIds.filter((id) => id !== addonId)
      : [...current.addonIds, addonId];
    this.choices.update((map) => ({ ...map, [item.id]: { ...current, addonIds } }));
  }

  add(item: PublicDish) {
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
      if (existing) return lines.map((line) => (line.key === key ? { ...line, quantity: line.quantity + 1 } : line));
      return [
        ...lines,
        {
          key,
          itemId: item.id,
          name: variant ? `${item.name} (${variant.name})` : item.name,
          variantId: variant?.id ?? undefined,
          variantName: variant?.name,
          addonIds: addons.map((row) => row.id || ''),
          quantity: 1,
          unit,
        },
      ];
    });
    this.error.set(null);
  }

  count() {
    return this.cart().reduce((sum, line) => sum + line.quantity, 0);
  }

  total() {
    return this.cart().reduce((sum, line) => sum + line.unit * line.quantity, 0);
  }

  openCart() {
    if (this.cart().length) this.cartOpen.set(true);
  }

  closeCart() {
    this.cartOpen.set(false);
  }

  setQty(key: string, delta: number) {
    this.cart.update((lines) =>
      lines
        .map((line) => (line.key === key ? { ...line, quantity: line.quantity + delta } : line))
        .filter((line) => line.quantity > 0),
    );
    if (!this.cart().length) this.cartOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape() {
    if (this.playing()) {
      this.playing.set(false);
      return;
    }
    this.cartOpen.set(false);
  }

  claimPlay(score: number) {
    const order = this.order();
    if (!order) return;
    this.api.claimPlay(order.id, score).subscribe({
      next: (saved) => {
        this.order.set(saved);
        this.playing.set(false);
        this.error.set(null);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  pay(ticket: PublicOrder) {
    return ticket.qrPng ? `data:image/png;base64,${ticket.qrPng}` : null;
  }

  place() {
    if (!this.cart().length) return;
    this.api
      .placeOrder(this.qr, {
        guestName: this.guestName(),
        lines: this.cart().map((line) => ({
          itemId: line.itemId,
          variantId: line.variantId,
          addonIds: line.addonIds,
          quantity: line.quantity,
        })),
      })
      .subscribe({
        next: (order) => {
          this.order.set(order);
          this.cart.set([]);
          this.cartOpen.set(false);
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
}
