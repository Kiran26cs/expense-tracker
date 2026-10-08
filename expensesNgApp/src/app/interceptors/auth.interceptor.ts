import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError, catchError, switchMap, EMPTY } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { SessionBus } from '../services/session-bus.service';
import { AuthStateService } from '../services/auth-state.service';

// Auth-bootstrap endpoints: a 401 here means bad credentials, not an expired session —
// there's no access token/session to refresh yet, so don't attempt silent refresh.
const AUTH_BOOTSTRAP_PATHS = ['/Auth/refresh', '/Auth/login', '/Auth/signup', '/Auth/google', '/Auth/send-otp', '/Auth/verify-otp'];

const STATUS_MESSAGES: Record<number, string> = {
  400: 'Invalid request. Please check your input.',
  401: 'Your session has expired. Please log in again.',
  403: 'You don\'t have permission to perform this action.',
  404: 'The requested resource was not found.',
  409: 'A conflict occurred. This item may already exist.',
  422: 'The submitted data could not be processed.',
  429: 'Too many requests. Please wait a moment and try again.',
  500: 'Something went wrong on our end. Please try again.',
  502: 'Service is temporarily unavailable. Please try again.',
  503: 'Service is under maintenance. Please try again later.',
};

function friendlyMessage(error: HttpErrorResponse): string {
  const body = error.error;
  if (typeof body === 'object' && body !== null) {
    if (typeof body.error   === 'string' && body.error)   return body.error;
    if (typeof body.message === 'string' && body.message) return body.message;
    if (typeof body.title   === 'string' && body.title)   return body.title;
  }
  if (typeof body === 'string' && body.length && body.length < 200) return body;
  return STATUS_MESSAGES[error.status] ?? 'An unexpected error occurred. Please try again.';
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router     = inject(Router);
  const toast      = inject(ToastService);
  const sessionBus = inject(SessionBus);
  const authState  = inject(AuthStateService);
  const token      = localStorage.getItem('authToken');

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  const canSilentlyRefresh = !AUTH_BOOTSTRAP_PATHS.some(path => req.url.includes(path));

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && canSilentlyRefresh) {
        return authState.refreshAccessToken().pipe(
          switchMap(newToken => next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }))),
          catchError(() => {
            sessionBus.notifyExpired();
            return EMPTY;
          }),
        );
      }

      // A 401 on an auth-bootstrap endpoint (bad OTP/credential/expired link-confirmation) is
      // a normal error for the caller to show inline — not a "your session expired" event,
      // since there's no session to have expired yet.
      const message = friendlyMessage(error);
      return throwError(() => Object.assign(new Error(message), { status: error.status }));
    })
  );
};
