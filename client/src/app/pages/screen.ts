import { Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { LiveService } from '../core/live.service';
import { Ticket, TokenBoard } from '../core/models';

@Component({
  selector: 'app-screen',
  templateUrl: './screen.html',
})
export class TokenScreenPage {
  private readonly api = inject(Api);
  readonly board = signal<TokenBoard | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    const code = inject(ActivatedRoute).snapshot.paramMap.get('code') ?? '';
    const live = inject(LiveService);
    const load = () =>
      this.api.tokenBoard(code).subscribe({
        next: (board) => {
          this.board.set(board);
          this.error.set(null);
          live.connect(board.restaurantId);
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

  preparing(tickets: Ticket[]) {
    return tickets.filter((ticket) => ticket.status === 'Preparing');
  }

  ready(tickets: Ticket[]) {
    return tickets.filter((ticket) => ticket.status === 'Ready');
  }
}
