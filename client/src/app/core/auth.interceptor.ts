import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = localStorage.getItem('aangan.token');
  const authed = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authed).pipe(
    catchError((error) => {
      if (error.status === 401 && !request.url.includes('/auth/login')) {
        localStorage.removeItem('aangan.token');
        localStorage.removeItem('aangan.user');
        inject(Router).navigateByUrl('/login');
      }

      return throwError(() => error);
    }),
  );
};
