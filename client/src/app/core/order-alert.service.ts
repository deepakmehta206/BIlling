import { Injectable, inject } from '@angular/core';
import { Ticket } from './models';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class OrderAlertService {
  private readonly toasts = inject(ToastService);
  private known = new Set<string>();
  private houseId: string | null = null;
  private seeded = false;
  private audio: AudioContext | null = null;

  constructor() {
    const unlock = () => {
      void this.ensureAudio()?.resume();
      document.removeEventListener('pointerdown', unlock);
    };
    document.addEventListener('pointerdown', unlock, { once: true });
  }

  forHouse(restaurantId: string) {
    if (this.houseId === restaurantId) return;
    this.houseId = restaurantId;
    this.known.clear();
    this.seeded = false;
  }

  notice(tickets: Ticket[]) {
    if (!this.seeded) {
      for (const ticket of tickets) this.known.add(ticket.id);
      this.seeded = true;
      return;
    }

    const fresh = tickets.filter((ticket) => ticket.status === 'Preparing' && !this.known.has(ticket.id));
    for (const ticket of tickets) this.known.add(ticket.id);
    if (!fresh.length) return;

    for (const ticket of fresh) {
      const table = ticket.tableName ? ` · ${ticket.tableName}` : '';
      this.toasts.show(`New order · #${ticket.tokenNumber}${table}`);
    }
    this.chime();
  }

  private ensureAudio() {
    if (typeof AudioContext === 'undefined') return null;
    this.audio ??= new AudioContext();
    return this.audio;
  }

  private chime() {
    const ctx = this.ensureAudio();
    if (!ctx) return;
    void ctx.resume().then(() => {
      const now = ctx.currentTime;
      for (const [index, freq] of [880, 1174].entries()) {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0.0001, now);
        gain.gain.exponentialRampToValueAtTime(0.12, now + 0.02 + index * 0.08);
        gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.28 + index * 0.1);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start(now + index * 0.08);
        osc.stop(now + 0.35 + index * 0.1);
      }
    });
  }
}
