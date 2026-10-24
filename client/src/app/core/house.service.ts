import { Injectable, inject, signal } from '@angular/core';
import { Api } from './api.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class HouseService {
  private readonly api = inject(Api);
  private readonly auth = inject(AuthService);

  readonly id = signal<string | null>(localStorage.getItem('aangan.house'));
  readonly name = signal<string | null>(localStorage.getItem('aangan.houseName'));
  readonly code = signal<string | null>(localStorage.getItem('aangan.houseCode'));
  readonly modules = signal<string[]>(this.readModules());

  activeId(): string | null {
    const user = this.auth.user();
    if (!user) {
      return null;
    }

    if (!user.isSuperAdmin && user.restaurantId) {
      return user.restaurantId;
    }

    return this.id();
  }

  has(code: string) {
    return this.modules().includes(code);
  }

  select(id: string, name: string, code: string, modules: string[]) {
    localStorage.setItem('aangan.house', id);
    localStorage.setItem('aangan.houseName', name);
    localStorage.setItem('aangan.houseCode', code);
    localStorage.setItem('aangan.modules', JSON.stringify(modules));
    this.id.set(id);
    this.name.set(name);
    this.code.set(code);
    this.modules.set(modules);
  }

  refresh() {
    const id = this.activeId();
    if (!id) {
      return;
    }

    this.api.restaurant(id).subscribe((house) => {
      this.select(
        house.id,
        house.name,
        house.code,
        house.modules.filter((module) => module.isEnabled).map((module) => module.code),
      );
    });
  }

  private readModules(): string[] {
    try {
      const parsed = JSON.parse(localStorage.getItem('aangan.modules') || '[]') as string[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
}
