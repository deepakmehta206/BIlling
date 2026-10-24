import { Routes } from '@angular/router';
import { authGuard, guestGuard, superGuard } from './core/auth.guard';
import { AuditPage } from './pages/audit';
import { BookingsPage } from './pages/bookings';
import { DashboardPage } from './pages/dashboard';
import { FloorPage } from './pages/floor';
import { GuestPage } from './pages/guest';
import { LoginPage } from './pages/login';
import { MenuPage } from './pages/menu';
import { RestaurantEditorPage } from './pages/restaurant-editor';
import { RestaurantListPage } from './pages/restaurant-list';
import { TokenScreenPage } from './pages/screen';
import { ShellPage } from './pages/shell';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  { path: 't/:token', component: GuestPage },
  { path: 'screen/:code', component: TokenScreenPage },
  {
    path: '',
    component: ShellPage,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardPage },
      { path: 'restaurants', component: RestaurantListPage, canActivate: [superGuard] },
      { path: 'restaurants/new', component: RestaurantEditorPage, canActivate: [superGuard] },
      { path: 'restaurants/:id', component: RestaurantEditorPage },
      { path: 'menu', component: MenuPage },
      { path: 'floor', component: FloorPage },
      { path: 'bookings', component: BookingsPage },
      { path: 'audit', component: AuditPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
