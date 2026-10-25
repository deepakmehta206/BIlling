import { Component, DestroyRef, ElementRef, HostListener, inject, input, output, signal, viewChild } from '@angular/core';

interface Drop {
  e: string;
  p: number;
  bad: boolean;
  x: number;
  y: number;
  vy: number;
  r: number;
  vr: number;
  done: boolean;
}

interface Floater {
  x: number;
  y: number;
  t: string;
  c: string;
  a: number;
}

@Component({
  selector: 'app-thali',
  templateUrl: './thali.html',
})
export class ThaliGame {
  readonly tableName = input.required<string>();
  readonly houseName = input.required<string>();
  readonly done = output<number>();
  readonly closed = output<void>();
  readonly screen = signal<'start' | 'game' | 'over'>('start');
  readonly score = signal(0);
  readonly lives = signal(3);
  readonly seconds = signal(75);
  readonly best = signal(0);
  readonly roundScore = signal(0);
  readonly tiers = [
    { minScore: 0, discount: 0 },
    { minScore: 50, discount: 5 },
    { minScore: 100, discount: 10 },
    { minScore: 200, discount: 15 },
  ];
  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('cv');
  private frame = 0;
  private play: {
    score: number;
    lives: number;
    time: number;
    items: Drop[];
    fx: Floater[];
    px: number;
    tx: number;
    spawn: number;
    last: number;
    run: boolean;
    combo: number;
  } | null = null;
  private width = 0;
  private height = 0;

  constructor() {
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(this.frame));
  }

  discountFor(score: number) {
    return this.tiers.reduce((won, tier) => (score >= tier.minScore ? tier.discount : won), 0);
  }

  nextTier(score: number) {
    return this.tiers.find((tier) => tier.minScore > score);
  }

  band(index: number) {
    const tier = this.tiers[index];
    const next = this.tiers[index + 1];
    return next ? `${tier.minScore}–${next.minScore - 1}` : `${tier.minScore}+`;
  }

  start() {
    this.screen.set('game');
    setTimeout(() => this.begin());
  }

  private begin() {
    this.resize();
    this.play = {
      score: 0,
      lives: 3,
      time: 75,
      items: [],
      fx: [],
      px: this.width / 2,
      tx: this.width / 2,
      spawn: 0,
      last: performance.now(),
      run: true,
      combo: 0,
    };
    this.score.set(0);
    this.lives.set(3);
    this.seconds.set(75);
    this.screen.set('game');
    cancelAnimationFrame(this.frame);
    this.frame = requestAnimationFrame((now) => this.loop(now));
  }

  again() {
    this.start();
  }

  stop() {
    this.finish();
  }

  claim() {
    this.done.emit(this.best());
  }

  close() {
    if (this.play) this.play.run = false;
    this.closed.emit();
  }

  aim(event: PointerEvent) {
    if (!this.play) return;
    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.play.tx = event.clientX - bounds.left;
  }

  @HostListener('document:keydown', ['$event'])
  keys(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.close();
      return;
    }
    if (!this.play) return;
    if (event.key === 'ArrowLeft') this.play.tx -= 60;
    if (event.key === 'ArrowRight') this.play.tx += 60;
  }

  private resize() {
    const canvas = this.canvas()?.nativeElement;
    if (!canvas) return;
    const ratio = Math.min(devicePixelRatio || 1, 2);
    this.width = canvas.clientWidth || window.innerWidth;
    this.height = canvas.clientHeight || window.innerHeight;
    canvas.width = this.width * ratio;
    canvas.height = this.height * ratio;
    canvas.getContext('2d')?.setTransform(ratio, 0, 0, ratio, 0, 0);
  }

  private loop(now: number) {
    const state = this.play;
    const canvas = this.canvas()?.nativeElement;
    const ctx = canvas?.getContext('2d');
    if (!state || !state.run || !ctx) return;
    const dt = Math.min((now - state.last) / 1000, 0.05);
    state.last = now;
    state.time -= dt;
    if (state.time <= 0) {
      this.finish();
      return;
    }
    const progress = 1 - state.time / 75;
    state.spawn -= dt;
    if (state.spawn <= 0) {
      this.spawn(progress);
      state.spawn = Math.max(0.32, 0.9 - progress * 0.55);
    }
    state.tx = Math.max(45, Math.min(this.width - 45, state.tx));
    state.px += (state.tx - state.px) * Math.min(1, dt * 18);
    const plate = this.height - 90;
    for (const item of state.items) {
      item.y += item.vy * dt;
      item.r += item.vr * dt;
      if (!item.done && item.y > plate - 25 && item.y < plate + 20 && Math.abs(item.x - state.px) < 55) {
        item.done = true;
        if (item.bad) {
          state.lives -= 1;
          state.combo = 0;
          this.lives.set(state.lives);
          state.fx.push({ x: item.x, y: plate - 30, t: 'Ouch', c: '#9d3a32', a: 1 });
          if (state.lives <= 0) {
            this.finish();
            return;
          }
        } else {
          state.combo += 1;
          const points = item.p + (state.combo >= 5 ? 5 : 0);
          state.score += points;
          state.fx.push({ x: item.x, y: plate - 30, t: `+${points}`, c: '#1d6a45', a: 1 });
        }
      }
      if (!item.done && !item.bad && item.y > this.height + 30) {
        item.done = true;
        state.combo = 0;
      }
    }
    state.items = state.items.filter((item) => !item.done && item.y < this.height + 40);
    this.draw(ctx, state, plate, dt);
    this.score.set(state.score);
    this.seconds.set(Math.ceil(state.time));
    this.frame = requestAnimationFrame((stamp) => this.loop(stamp));
  }

  private spawn(progress: number) {
    const state = this.play;
    if (!state) return;
    const bad = Math.random() < 0.18 + progress * 0.17;
    const good = [
      { e: '🍛', p: 10 },
      { e: '🥘', p: 10 },
      { e: '🍚', p: 5 },
      { e: '🫓', p: 5 },
      { e: '🍲', p: 10 },
      { e: '🥟', p: 5 },
      { e: '🍮', p: 15 },
      { e: '🥭', p: 20 },
    ];
    const pick = good[Math.floor(Math.random() * good.length)];
    const item: Drop = bad
      ? { e: Math.random() < 0.5 ? '🌶️' : '💣', p: 0, bad: true, x: 0, y: -40, vy: 0, r: 0, vr: 0, done: false }
      : { e: pick.e, p: pick.p, bad: false, x: 0, y: -40, vy: 0, r: 0, vr: 0, done: false };
    item.x = 30 + Math.random() * (this.width - 60);
    item.vy = (140 + Math.random() * 80) * (1 + progress * 1.1);
    item.r = Math.random() * 6.28;
    item.vr = (Math.random() - 0.5) * 3;
    state.items.push(item);
  }

  private draw(ctx: CanvasRenderingContext2D, state: NonNullable<ThaliGame['play']>, plate: number, dt: number) {
    ctx.clearRect(0, 0, this.width, this.height);
    ctx.font = '40px serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    for (const item of state.items) {
      ctx.save();
      ctx.translate(item.x, item.y);
      ctx.rotate(item.r);
      ctx.fillText(item.e, 0, 0);
      ctx.restore();
    }
    ctx.save();
    ctx.translate(state.px, plate);
    ctx.fillStyle = '#3c2f27';
    ctx.beginPath();
    ctx.ellipse(0, 6, 62, 16, 0, 0, 6.28);
    ctx.fill();
    ctx.fillStyle = '#e7d8c4';
    ctx.beginPath();
    ctx.ellipse(0, 0, 58, 14, 0, 0, 6.28);
    ctx.fill();
    ctx.fillStyle = '#fffaf4';
    ctx.beginPath();
    ctx.ellipse(0, -2, 44, 9, 0, 0, 6.28);
    ctx.fill();
    ['#e07a1a', '#9d3a32', '#1d6a45'].forEach((color, index) => {
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.ellipse(-26 + index * 26, -4, 9, 4, 0, 0, 6.28);
      ctx.fill();
    });
    ctx.restore();
    ctx.font = 'bold 20px Outfit, sans-serif';
    for (const floater of state.fx) {
      ctx.globalAlpha = floater.a;
      ctx.fillStyle = floater.c;
      ctx.fillText(floater.t, floater.x, floater.y);
      floater.y -= 50 * dt;
      floater.a -= dt * 1.2;
    }
    ctx.globalAlpha = 1;
    state.fx = state.fx.filter((floater) => floater.a > 0);
  }

  private finish() {
    const state = this.play;
    if (!state) return;
    state.run = false;
    this.best.update((current) => Math.max(current, state.score));
    this.roundScore.set(state.score);
    this.play = null;
    this.screen.set('over');
  }
}
