import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api.service';
import { errorMessage } from '../core/http-error';
import { HouseService } from '../core/house.service';
import { LiveService } from '../core/live.service';
import { MenuBoard, MenuItem } from '../core/models';
import { ToastService } from '../core/toast.service';
import { apiOrigin } from '../../environments/environment';

const moneyPattern = /^(?:0|[1-9]\d{0,3})(?:\.\d{1,2})?$/;

interface PriceRow {
  name: string;
  price: string;
}

interface DishDraft {
  id?: string;
  categoryId?: string;
  name: string;
  description: string;
  foodType: string;
  price: string;
  isAvailable: boolean;
  sortOrder: number;
  variants: PriceRow[];
  addons: PriceRow[];
  imageUrl: string | null;
  imageData: string | null;
  removeImage: boolean;
  preview: string | null;
}

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
  readonly editing = signal<DishDraft>(this.blank());
  readonly openPick = signal<'category' | 'food' | null>(null);
  readonly foodTypes = [
    { value: 'Veg', label: 'Veg' },
    { value: 'NonVeg', label: 'Non-veg' },
    { value: 'Egg', label: 'Egg' },
  ];

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
      id: item.id,
      categoryId: item.categoryId,
      name: item.name,
      description: item.description ?? '',
      foodType: item.foodType,
      price: moneyText(item.price),
      isAvailable: item.isAvailable,
      sortOrder: item.sortOrder,
      variants: item.variants.map((row) => ({ name: row.name, price: moneyText(row.price) })),
      addons: item.addons.map((row) => ({ name: row.name, price: moneyText(row.price) })),
      imageUrl: item.imageUrl,
      imageData: null,
      removeImage: false,
      preview: null,
    });
  }

  reset() {
    this.editing.set(this.blank());
  }

  addRow(kind: 'variants' | 'addons') {
    const current = this.editing();
    this.editing.set({ ...current, [kind]: [...current[kind], { name: '', price: '' }] });
  }

  moneyOk(value: string) {
    return moneyPattern.test(value.trim());
  }

  categoryLabel() {
    const id = this.editing().categoryId;
    return this.board()?.categories.find((row) => row.id === id)?.name ?? 'Choose';
  }

  foodLabel(foodType: string) {
    return this.foodTypes.find((row) => row.value === foodType)?.label ?? 'Veg';
  }

  togglePick(which: 'category' | 'food') {
    this.openPick.update((current) => (current === which ? null : which));
  }

  chooseCategory(id: string) {
    this.editing.update((draft) => ({ ...draft, categoryId: id }));
    this.openPick.set(null);
  }

  chooseFood(foodType: string) {
    this.editing.update((draft) => ({ ...draft, foodType }));
    this.openPick.set(null);
  }

  @HostListener('document:click')
  closePick() {
    this.openPick.set(null);
  }

  @HostListener('document:keydown.escape')
  closePickOnEscape() {
    this.openPick.set(null);
  }

  photo(path: string | null | undefined) {
    if (!path) return null;
    return path.startsWith('http') ? path : `${apiOrigin()}${path}`;
  }

  shownPhoto() {
    const draft = this.editing();
    return draft.preview || this.photo(draft.removeImage ? null : draft.imageUrl);
  }

  async onPhoto(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    try {
      const fitted = await fitPhoto(file);
      this.editing.update((draft) => ({ ...draft, imageData: fitted.base64, preview: fitted.url, removeImage: false }));
      this.error.set(null);
    } catch {
      this.error.set('Choose a JPEG, PNG or WebP photo.');
    }
  }

  clearPhoto() {
    this.editing.update((draft) => ({ ...draft, imageData: null, preview: null, imageUrl: null, removeImage: true }));
  }

  saveItem() {
    const id = this.desk.activeId();
    const draft = this.editing();
    if (!id || !draft.categoryId || !draft.name?.trim()) return;
    const priced = [draft.price, ...draft.variants.filter((row) => row.name.trim()).map((row) => row.price), ...draft.addons.filter((row) => row.name.trim()).map((row) => row.price)];
    if (priced.some((value) => !this.moneyOk(value))) {
      this.error.set('Price can have 4 digits and 2 decimals, up to 9999.99.');
      return;
    }
    this.api
      .saveItem(id, {
        id: draft.id,
        categoryId: draft.categoryId,
        name: draft.name,
        description: draft.description,
        foodType: draft.foodType || 'Veg',
        price: Number(draft.price),
        isAvailable: draft.isAvailable !== false,
        sortOrder: draft.sortOrder || 0,
        imageData: draft.imageData,
        removeImage: draft.removeImage,
        variants: draft.variants.filter((row) => row.name.trim()).map((row) => ({ name: row.name, price: Number(row.price) })),
        addons: draft.addons.filter((row) => row.name.trim()).map((row) => ({ name: row.name, price: Number(row.price) })),
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

  private blank(): DishDraft {
    return {
      name: '',
      description: '',
      foodType: 'Veg',
      price: '',
      isAvailable: true,
      sortOrder: 0,
      variants: [],
      addons: [],
      imageUrl: null,
      imageData: null,
      removeImage: false,
      preview: null,
    };
  }
}

function moneyText(value: number) {
  return Number.isInteger(value) ? String(value) : value.toFixed(2);
}

function fitPhoto(file: File) {
  return new Promise<{ url: string; base64: string }>((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error('read'));
    reader.onload = () => {
      const image = new Image();
      image.onerror = () => reject(new Error('image'));
      image.onload = () => {
        const scale = Math.min(1, 960 / Math.max(image.width, image.height));
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, Math.round(image.width * scale));
        canvas.height = Math.max(1, Math.round(image.height * scale));
        const context = canvas.getContext('2d');
        if (!context) {
          reject(new Error('canvas'));
          return;
        }
        context.drawImage(image, 0, 0, canvas.width, canvas.height);
        const url = canvas.toDataURL('image/jpeg', 0.82);
        const base64 = url.split(',')[1] ?? '';
        if (base64.length > 1_600_000) {
          reject(new Error('large'));
          return;
        }
        resolve({ url, base64 });
      };
      image.src = String(reader.result);
    };
    reader.readAsDataURL(file);
  });
}
