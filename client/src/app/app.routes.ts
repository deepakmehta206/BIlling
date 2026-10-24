import { Routes } from '@angular/router';
import { authGuard, guestGuard, superGuard } from './core/auth.guard';
import { AuditPage } from './pages/audit';
import { BillingPage } from './pages/billing';
import { BookingsPage } from './pages/bookings';
import { DashboardPage } from './pages/dashboard';
import { FloorPage } from './pages/floor';
import { GuestPage } from './pages/guest';
import { AttendancePage } from './pages/attendance';
import { InventoryPage } from './pages/inventory';
import { LoginPage } from './pages/login';
import { MenuPage } from './pages/menu';
import { PayPage } from './pages/pay';
import { RestaurantEditorPage } from './pages/restaurant-editor';
import { RestaurantListPage } from './pages/restaurant-list';
import { TokenScreenPage } from './pages/screen';
import { ReportsPage } from './pages/reports';
import { ShellPage } from './pages/shell';
import { StaffPage } from './pages/staff';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  { path: 't/:token', component: GuestPage },
  { path: 'screen/:code', component: TokenScreenPage },
  { path: 'pay/:id', component: PayPage },
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
      { path: 'billing', component: BillingPage },
      { path: 'inventory', component: InventoryPage },
      { path: 'staff', component: StaffPage },
      { path: 'attendance', component: AttendancePage },
      { path: 'bookings', component: BookingsPage },
      { path: 'reports', component: ReportsPage },
      { path: 'audit', component: AuditPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
