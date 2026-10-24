import { Routes } from '@angular/router';
import { authGuard, guestGuard, superGuard } from './core/auth.guard';
import { AuditPage } from './pages/audit';
import { DashboardPage } from './pages/dashboard';
import { LoginPage } from './pages/login';
import { RestaurantEditorPage } from './pages/restaurant-editor';
import { RestaurantListPage } from './pages/restaurant-list';
import { ShellPage } from './pages/shell';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  {
    path: '',
    component: ShellPage,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardPage },
      { path: 'restaurants', component: RestaurantListPage, canActivate: [superGuard] },
      { path: 'restaurants/new', component: RestaurantEditorPage, canActivate: [superGuard] },
      { path: 'restaurants/:id', component: RestaurantEditorPage },
      { path: 'audit', component: AuditPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
