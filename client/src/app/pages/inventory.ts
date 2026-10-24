import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { InventoryDesk, StockItem } from '../core/models';
import { ToastService } from '../core/toast.service';

interface RecipeDraft {
  stockItemId: string;
  quantity: number;
}

@Component({
  selector: 'app-inventory',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './inventory.html',
})
export class InventoryPage {
  private readonly api = inject(Api);
  private readonly toasts = inject(ToastService);
  readonly desk = inject(HouseService);
  readonly board = signal<InventoryDesk | null>(null);
  readonly error = signal<string | null>(null);
  readonly name = signal('');
  readonly unit = signal('g');
  readonly reorder = signal(0);
  readonly selectedId = signal('');
  readonly purchaseQty = signal(0);
  readonly purchaseCost = signal(0);
  readonly dishId = signal('');
  readonly recipe = signal<RecipeDraft[]>([]);

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
    this.api.inventory(id).subscribe({
      next: (board) => {
        this.board.set(board);
        this.error.set(null);
        if (!this.selectedId() && board.items[0]) this.selectedId.set(board.items[0].id);
        if (!this.dishId() && board.dishes[0]) this.useDish(board.dishes[0].id, board);
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  selected() {
    return this.board()?.items.find((item) => item.id === this.selectedId()) ?? null;
  }

  movements() {
    return this.board()?.movements.filter((row) => row.stockItemId === this.selectedId()) ?? [];
  }

  addItem() {
    const id = this.desk.activeId();
    if (!id || this.name().trim().length < 2) return;
    this.api
      .saveStockItem(id, { name: this.name().trim(), unit: this.unit(), reorderLevel: Number(this.reorder() || 0) })
      .subscribe({
        next: (item) => {
          this.toasts.show(`${item.name} is in the store.`);
          this.name.set('');
          this.selectedId.set(item.id);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  receive() {
    const id = this.desk.activeId();
    const item = this.selected();
    if (!id || !item || this.purchaseQty() <= 0) return;
    this.api
      .purchaseStock(id, { stockItemId: item.id, quantity: Number(this.purchaseQty()), unitCost: Number(this.purchaseCost() || 0) })
      .subscribe({
        next: () => {
          this.toasts.show('Stock received.');
          this.purchaseQty.set(0);
          this.load();
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }

  archive(item: StockItem) {
    const id = this.desk.activeId();
    if (!id) return;
    this.api.deleteStockItem(id, item.id).subscribe({
      next: (result) => {
        this.toasts.show(result.message);
        if (this.selectedId() === item.id) this.selectedId.set('');
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }

  useDish(menuItemId: string, board = this.board()) {
    this.dishId.set(menuItemId);
    const recipe = board?.recipes.find((row) => row.menuItemId === menuItemId);
    this.recipe.set((recipe?.lines ?? []).map((line) => ({ stockItemId: line.stockItemId, quantity: line.quantity })));
  }

  addRecipeRow() {
    const first = this.board()?.items[0];
    if (!first) return;
    this.recipe.update((rows) => [...rows, { stockItemId: first.id, quantity: 1 }]);
  }

  saveRecipe() {
    const id = this.desk.activeId();
    if (!id || !this.dishId()) return;
    this.api.saveRecipe(id, this.dishId(), this.recipe()).subscribe({
      next: () => {
        this.toasts.show('Recipe saved.');
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error)),
    });
  }
}
