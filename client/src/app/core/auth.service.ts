import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser, LoginResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem('aangan.token'));
  readonly user = signal<CurrentUser | null>(this.readUser());
  readonly isSuperAdmin = computed(() => this.user()?.isSuperAdmin === true);

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((response) => this.persist(response.accessToken, response.user)));
  }

  hydrate() {
    if (!this.token()) {
      return;
    }

    this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`).subscribe({
      next: (user) => {
        localStorage.setItem('aangan.user', JSON.stringify(user));
        this.user.set(user);
      },
    });
  }

  logout() {
    localStorage.removeItem('aangan.token');
    localStorage.removeItem('aangan.user');
    this.token.set(null);
    this.user.set(null);
    void this.router.navigateByUrl('/login');
  }

  private persist(token: string, user: CurrentUser) {
    localStorage.setItem('aangan.token', token);
    localStorage.setItem('aangan.user', JSON.stringify(user));
    this.token.set(token);
    this.user.set(user);
  }

  private readUser(): CurrentUser | null {
    const raw = localStorage.getItem('aangan.user');
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
