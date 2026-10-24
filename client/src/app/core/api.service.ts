import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import {
  BillingDesk,
  Booking,
  Dashboard,
  DishRecipe,
  Employee,
  LeaveRow,
  FloorTable,
  HouseBill,
  AttendanceDesk,
  InventoryDesk,
  MenuBoard,
  MenuCategory,
  MenuItem,
  ModuleGrant,
  PagedAudit,
  PublicBill,
  PublicOrder,
  Shift,
  StaffDesk,
  PublicTable,
  RestaurantDetail,
  RestaurantListItem,
  StockItem,
  Ticket,
  TokenBoard,
} from './models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  dashboard() {
    return this.http.get<Dashboard>(`${this.base}/dashboard`);
  }

  restaurants(search?: string) {
    const params = search ? new HttpParams().set('q', search) : undefined;
    return this.http.get<RestaurantListItem[]>(`${this.base}/restaurants`, { params });
  }

  restaurant(id: string) {
    return this.http.get<RestaurantDetail>(`${this.base}/restaurants/${id}`);
  }

  createRestaurant(body: unknown) {
    return this.http.post<RestaurantDetail>(`${this.base}/restaurants`, body);
  }

  updateRestaurant(id: string, body: unknown) {
    return this.http.put<RestaurantDetail>(`${this.base}/restaurants/${id}`, body);
  }

  deleteRestaurant(id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/restaurants/${id}`);
  }

  updateModules(id: string, modules: { code: string; isEnabled: boolean }[]) {
    return this.http.put<ModuleGrant[]>(`${this.base}/restaurants/${id}/modules`, { modules });
  }

  audit(page: number, restaurantId?: string) {
    let params = new HttpParams().set('page', page).set('pageSize', '12');
    if (restaurantId) {
      params = params.set('restaurantId', restaurantId);
    }

    return this.http.get<PagedAudit>(`${this.base}/audit-logs`, { params });
  }

  private house(id: string) {
    return new HttpParams().set('restaurantId', id);
  }

  menu(restaurantId: string) {
    return this.http.get<MenuBoard>(`${this.base}/menu`, { params: this.house(restaurantId) });
  }

  saveCategory(restaurantId: string, body: unknown) {
    return this.http.post<MenuCategory>(`${this.base}/menu/categories`, body, { params: this.house(restaurantId) });
  }

  deleteCategory(restaurantId: string, id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/menu/categories/${id}`, { params: this.house(restaurantId) });
  }

  saveItem(restaurantId: string, body: unknown) {
    return this.http.post<MenuItem>(`${this.base}/menu/items`, body, { params: this.house(restaurantId) });
  }

  deleteItem(restaurantId: string, id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/menu/items/${id}`, { params: this.house(restaurantId) });
  }

  tables(restaurantId: string, at?: string) {
    let params = this.house(restaurantId);
    if (at) params = params.set('at', at);
    return this.http.get<FloorTable[]>(`${this.base}/tables`, { params });
  }

  saveTable(restaurantId: string, body: unknown) {
    return this.http.post<FloorTable>(`${this.base}/tables`, body, { params: this.house(restaurantId) });
  }

  deleteTable(restaurantId: string, id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/tables/${id}`, { params: this.house(restaurantId) });
  }

  bookings(restaurantId: string, day: string) {
    return this.http.get<Booking[]>(`${this.base}/bookings`, { params: this.house(restaurantId).set('day', day) });
  }

  createBooking(restaurantId: string, body: unknown) {
    return this.http.post<Booking>(`${this.base}/bookings`, body, { params: this.house(restaurantId) });
  }

  cancelBooking(restaurantId: string, id: string) {
    return this.http.post<Booking>(`${this.base}/bookings/${id}/cancel`, {}, { params: this.house(restaurantId) });
  }

  tickets(restaurantId: string) {
    return this.http.get<Ticket[]>(`${this.base}/orders`, { params: this.house(restaurantId) });
  }

  setTicketStatus(restaurantId: string, id: string, status: string) {
    return this.http.put<Ticket>(`${this.base}/orders/${id}/status`, { status }, { params: this.house(restaurantId) });
  }

  publicTable(qrToken: string) {
    return this.http.get<PublicTable>(`${this.base}/public/tables/${qrToken}`);
  }

  placeOrder(qrToken: string, body: unknown) {
    return this.http.post<PublicOrder>(`${this.base}/public/tables/${qrToken}/orders`, body);
  }

  publicOrder(id: string) {
    return this.http.get<PublicOrder>(`${this.base}/public/orders/${id}`);
  }

  tokenBoard(code: string) {
    return this.http.get<TokenBoard>(`${this.base}/public/board/${code}`);
  }

  billingDesk(restaurantId: string) {
    return this.http.get<BillingDesk>(`${this.base}/bills`, { params: this.house(restaurantId) });
  }

  createBill(restaurantId: string, body: unknown) {
    return this.http.post<HouseBill>(`${this.base}/bills`, body, { params: this.house(restaurantId) });
  }

  markBillPaid(restaurantId: string, id: string) {
    return this.http.post<HouseBill>(`${this.base}/bills/${id}/paid`, {}, { params: this.house(restaurantId) });
  }

  publicBill(id: string) {
    return this.http.get<PublicBill>(`${this.base}/public/bills/${id}`);
  }

  inventory(restaurantId: string) {
    return this.http.get<InventoryDesk>(`${this.base}/inventory`, { params: this.house(restaurantId) });
  }

  saveStockItem(restaurantId: string, body: unknown) {
    return this.http.post<StockItem>(`${this.base}/inventory/items`, body, { params: this.house(restaurantId) });
  }

  deleteStockItem(restaurantId: string, id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/inventory/items/${id}`, { params: this.house(restaurantId) });
  }

  purchaseStock(restaurantId: string, body: unknown) {
    return this.http.post<StockItem>(`${this.base}/inventory/purchases`, body, { params: this.house(restaurantId) });
  }

  saveRecipe(restaurantId: string, menuItemId: string, lines: { stockItemId: string; quantity: number }[]) {
    return this.http.put<DishRecipe>(`${this.base}/inventory/recipes/${menuItemId}`, { lines }, { params: this.house(restaurantId) });
  }

  staff(restaurantId: string) {
    return this.http.get<StaffDesk>(`${this.base}/staff`, { params: this.house(restaurantId) });
  }

  hire(restaurantId: string, body: unknown) {
    return this.http.post<Employee>(`${this.base}/staff/employees`, body, { params: this.house(restaurantId) });
  }

  updateEmployee(restaurantId: string, id: string, body: unknown) {
    return this.http.put<Employee>(`${this.base}/staff/employees/${id}`, body, { params: this.house(restaurantId) });
  }

  archiveEmployee(restaurantId: string, id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/staff/employees/${id}`, { params: this.house(restaurantId) });
  }

  requestLeave(restaurantId: string, body: unknown) {
    return this.http.post<LeaveRow>(`${this.base}/staff/leave`, body, { params: this.house(restaurantId) });
  }

  decideLeave(restaurantId: string, id: string, status: string) {
    return this.http.post<LeaveRow>(`${this.base}/staff/leave/${id}/decision`, { status }, { params: this.house(restaurantId) });
  }

  attendance(restaurantId: string) {
    return this.http.get<AttendanceDesk>(`${this.base}/attendance`, { params: this.house(restaurantId) });
  }

  saveShift(restaurantId: string, body: unknown) {
    return this.http.post<Shift>(`${this.base}/attendance/shifts`, body, { params: this.house(restaurantId) });
  }

  assignShift(restaurantId: string, userId: string, shiftId: string) {
    return this.http.put<{ message: string }>(`${this.base}/attendance/assignments`, { userId, shiftId }, { params: this.house(restaurantId) });
  }

  checkIn(restaurantId: string, userId: string) {
    return this.http.post<{ message: string }>(`${this.base}/attendance/in`, { userId }, { params: this.house(restaurantId) });
  }

  checkOut(restaurantId: string, userId: string) {
    return this.http.post<{ message: string }>(`${this.base}/attendance/out`, { userId }, { params: this.house(restaurantId) });
  }
}
