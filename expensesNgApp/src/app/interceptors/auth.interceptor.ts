import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError, catchError, EMPTY } from 'rxjs';
import { ToastService } from '../services/toast.service';

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
  const router = inject(Router);
  const toast  = inject(ToastService);
  const token  = localStorage.getItem('authToken');

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem('authToken');
        toast.error('Your session has expired. Please log in again.');
        router.navigate(['/login']);
        return EMPTY;
      }

      const message = friendlyMessage(error);
      return throwError(() => Object.assign(new Error(message), { status: error.status }));
    })
  );
};
