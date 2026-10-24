import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { MenuBoard, MenuItem, NamedPrice } from '../core/models';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-menu',
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './menu.html',
})
export class MenuPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly board = signal<MenuBoard | null>(null);
  readonly error = signal<string | null>(null);
  readonly categoryName = signal('');
  readonly editing = signal<Partial<MenuItem> & { variants: NamedPrice[]; addons: NamedPrice[] }>(this.blank());

  constructor() {
    const live = inject(LiveService);
    const id = this.desk.activeId();
    if (id) {
      live.connect(id);
    }
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

  itemsFor(categoryId: string) {
    return this.board()?.items.filter((item) => item.categoryId === categoryId) ?? [];
  }

  addCategory() {
    const id = this.desk.activeId();
    const name = this.categoryName().trim();
    if (!id || name.length < 2) return;
    this.api.saveCategory(id, { name, sortOrder: this.board()?.categories.length ?? 0, isActive: true }).subscribe({
      next: () => {
        this.categoryName.set('');
        this.toasts.show('Category added.');
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  removeCategory(categoryId: string) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.deleteCategory(id, categoryId).subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  edit(item: MenuItem) {
    this.editing.set({
      ...item,
      variants: item.variants.map((row) => ({ name: row.name, price: row.price })),
      addons: item.addons.map((row) => ({ name: row.name, price: row.price })),
    });
  }

  reset() {
    this.editing.set(this.blank());
  }

  addRow(kind: 'variants' | 'addons') {
    const current = this.editing();
    this.editing.set({ ...current, [kind]: [...current[kind], { name: '', price: 0 }] });
  }

  saveItem() {
    const id = this.desk.activeId();
    const draft = this.editing();
    if (!id || !draft.categoryId || !draft.name) return;
    this.api
      .saveItem(id, {
        id: draft.id,
        categoryId: draft.categoryId,
        name: draft.name,
        description: draft.description,
        foodType: draft.foodType || 'Veg',
        price: Number(draft.price || 0),
        isAvailable: draft.isAvailable !== false,
        sortOrder: draft.sortOrder || 0,
        variants: draft.variants.filter((row) => row.name.trim()),
        addons: draft.addons.filter((row) => row.name.trim()),
      })
      .subscribe({
        next: () => {
          this.toasts.show('Dish saved.');
          this.reset();
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  removeItem(itemId: string) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.deleteItem(id, itemId).subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  sizeLine(item: MenuItem) {
    return item.variants.map((row) => `${row.name} ${row.price}`).join(' · ');
  }

  private load() {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.menu(id).subscribe({
      next: (board) => {
        this.board.set(board);
        this.error.set(null);
        const draft = this.editing();
        if (!draft.categoryId && board.categories[0]) {
          this.editing.set({ ...draft, categoryId: board.categories[0].id });
        }
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  private blank() {
    return {
      name: '',
      description: '',
      foodType: 'Veg',
      price: 0,
      isAvailable: true,
      sortOrder: 0,
      variants: [] as NamedPrice[],
      addons: [] as NamedPrice[],
    };
  }
}
