import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import {
  Dashboard,
  ModuleGrant,
  PagedAudit,
  RestaurantDetail,
  RestaurantListItem,
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
}
