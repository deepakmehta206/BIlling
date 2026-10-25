import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Api } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { HouseService } from '../core/house.service';
import { RestaurantListItem } from '../core/models';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './shell.html',
})
export class ShellPage {
  private readonly api = inject(Api);
  readonly auth = inject(AuthService);
  readonly desk = inject(HouseService);
  readonly houses = signal<RestaurantListItem[]>([]);
  readonly collapsed = signal(localStorage.getItem('aangan.nav') === 'collapsed');

  constructor() {
    this.auth.hydrate();
    effect(() => {
      const user = this.auth.user();
      if (!user) return;
      this.desk.refresh();
      if (user.isSuperAdmin) {
        this.api.restaurants().subscribe((rows) => this.houses.set(rows));
      }
    });
  }

  roleLabel() {
    const user = this.auth.user();
    if (!user) return '';
    if (user.isSuperAdmin) return 'Platform admin';
    const labels: Record<string, string> = {
      RestaurantAdmin: 'Restaurant admin',
      Manager: 'Manager',
      Cashier: 'Cashier',
      Waiter: 'Waiter',
      Kitchen: 'Kitchen',
      StoreKeeper: 'Store keeper',
      Hr: 'HR',
    };
    return labels[user.roles[0]] ?? 'House';
  }

  canStaff() {
    const user = this.auth.user();
    if (!user) return false;
    return user.isSuperAdmin || user.roles.includes('RestaurantAdmin') || user.roles.includes('Hr');
  }

  initials() {
    const parts = (this.auth.user()?.fullName ?? '').trim().split(/\s+/).filter(Boolean);
    const letters = `${parts[0]?.[0] ?? ''}${parts.length > 1 ? parts[1][0] : ''}`;
    return (letters || 'A').toUpperCase();
  }

  toggleNav() {
    this.collapsed.update((value) => !value);
    localStorage.setItem('aangan.nav', this.collapsed() ? 'collapsed' : 'open');
  }

  pickHouse(id: string) {
    const house = this.houses().find((row) => row.id === id);
    if (!house) return;
    this.desk.select(house.id, house.name, house.code, []);
    this.desk.refresh();
  }
}
