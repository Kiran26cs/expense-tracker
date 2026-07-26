import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError, catchError } from 'rxjs';
import { AdminAuthService } from '../services/admin-auth.service';

export const adminAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth   = inject(AdminAuthService);
  const router = inject(Router);
  const token  = auth.getToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.logout();
        router.navigate(['/login']);
        return throwError(() => error);
      }
      const message =
        error.error?.error ?? error.error?.message ?? 'An unexpected error occurred.';
      return throwError(() => Object.assign(new Error(message), { status: error.status }));
    })
  );
};
