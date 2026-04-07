import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStateService } from '../services/auth-state.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthStateService);
  const router = inject(Router);

  // Wait for auth check to complete
  if (auth.isLoading()) {
    await new Promise<void>(resolve => {
      const interval = setInterval(() => {
        if (!auth.isLoading()) { clearInterval(interval); resolve(); }
      }, 50);
    });
  }

  if (auth.isAuthenticated()) return true;
  router.navigate(['/login']);
  return false;
};

export const publicGuard: CanActivateFn = async () => {
  const auth = inject(AuthStateService);
  const router = inject(Router);

  if (auth.isLoading()) {
    await new Promise<void>(resolve => {
      const interval = setInterval(() => {
        if (!auth.isLoading()) { clearInterval(interval); resolve(); }
      }, 50);
    });
  }

  if (!auth.isAuthenticated()) return true;
  router.navigate(['/']);
  return false;
};
