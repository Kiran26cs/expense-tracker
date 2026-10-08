import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, Observable, catchError, finalize, map, shareReplay, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { SessionBus } from './session-bus.service';
import { ToastService } from './toast.service';
import { ThemeService } from './theme.service';
import { PushNotificationService } from './push-notification.service';
import { User, ApiResponse, LinkableLoginOutcome } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private userSignal = signal<User | null>(null);
  private loadingSignal = signal(true);
  private _handlingExpiry = false;
  private refreshInProgress$: Observable<string> | null = null;

  user = this.userSignal.asReadonly();
  isLoading = this.loadingSignal.asReadonly();
  isAuthenticated = computed(() => !!this.userSignal());

  constructor(
    private api: ApiService,
    private router: Router,
    private sessionBus: SessionBus,
    private toast: ToastService,
    private themeService: ThemeService,
    private pushNotifications: PushNotificationService,
  ) {
    this.sessionBus.expired$.subscribe(() => this.onSessionExpired());
    this.checkAuth();
  }

  private onSessionExpired(): void {
    if (this._handlingExpiry) return;
    this._handlingExpiry = true;
    this.clearSession();
    this.toast.error('Your session has expired. Please log in again.');
    this.router.navigate(['/login']).finally(() => { this._handlingExpiry = false; });
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  setToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  getSessionId(): string | null {
    return localStorage.getItem('sessionId');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  private setSession(token: string, refreshToken: string, sessionId: string): void {
    localStorage.setItem('authToken', token);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('sessionId', sessionId);
  }

  clearToken(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('sessionId');
  }

  /** Silently exchanges the stored refresh token for a new access token. De-duped so
   *  concurrent 401s share one in-flight request instead of racing to refresh. */
  refreshAccessToken(): Observable<string> {
    if (this.refreshInProgress$) return this.refreshInProgress$;

    const sessionId = this.getSessionId();
    const refreshToken = this.getRefreshToken();
    if (!sessionId || !refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    this.refreshInProgress$ = this.api
      .post<ApiResponse<{ token: string; refreshToken: string; sessionId: string; user: User }>>(
        '/Auth/refresh',
        { sessionId, refreshToken },
      )
      .pipe(
        map(response => {
          if (!response.success || !response.data) throw new Error(response.error || 'Refresh failed');
          this.setSession(response.data.token, response.data.refreshToken, response.data.sessionId);
          return response.data.token;
        }),
        catchError(err => {
          this.clearSession();
          return throwError(() => err);
        }),
        finalize(() => { this.refreshInProgress$ = null; }),
        shareReplay(1),
      );

    return this.refreshInProgress$;
  }

  clearSession(): void {
    this.clearToken();
    this.userSignal.set(null);
  }

  private persistUser(user: User): void {
    localStorage.setItem('authUser', JSON.stringify(user));
    this.userSignal.set(user);
    this.pushNotifications.initPushNotifications();
  }

  async checkAuth(): Promise<void> {
    const token = this.getToken();
    if (token) {
      // Restore from localStorage immediately so UI shows correct name/email right away
      const cached = localStorage.getItem('authUser');
      if (cached) {
        try { this.userSignal.set(JSON.parse(cached)); } catch { /* ignore */ }
      }
      try {
        const response = await firstValueFrom(this.api.get<ApiResponse<User>>('/Auth/me'));
        // Guard against race condition: if the user logged in while this call was in-flight
        // the token has changed and this stale response must not overwrite the fresh login.
        if (this.getToken() === token && response.success && response.data) {
          this.persistUser(response.data);
        } else if (!cached) {
          this.clearToken();
        }
      } catch {
        // Only clear if the token is genuinely gone or network failed — not on session expiry
        // (session expiry is handled by the interceptor via SessionBus)
        if (!this.getToken()) this.clearToken();
      }
    }
    this.loadingSignal.set(false);
  }

  /** Returns `{ requiresLinking: true, preview }` without creating a session when this OTP
   *  matches an existing Google-created account that's never used OTP login before — the
   *  caller must show a confirmation and call confirmOtpLink() to proceed. */
  async login(email: string, otp: string): Promise<LinkableLoginOutcome> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{
        requiresLinking: boolean;
        preview?: { name: string; email: string; createdAt: string };
        auth?: { token: string; refreshToken: string; sessionId: string; user: User };
      }>>(`/Auth/login?otp=${otp}`, { email })
    );
    if (!response.success || !response.data) {
      throw new Error(response.error || 'Login failed');
    }

    if (response.data.requiresLinking && response.data.preview) {
      return { requiresLinking: true, preview: response.data.preview };
    }

    if (!response.data.auth) {
      throw new Error('Login failed');
    }
    this.setSession(response.data.auth.token, response.data.auth.refreshToken, response.data.auth.sessionId);
    this.persistUser(response.data.auth.user);
    return { requiresLinking: false };
  }

  async confirmOtpLink(email: string, otp: string): Promise<void> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; refreshToken: string; sessionId: string; user: User }>>('/Auth/login/confirm-link', { email, otp })
    );
    if (response.success && response.data) {
      this.setSession(response.data.token, response.data.refreshToken, response.data.sessionId);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Account linking failed');
    }
  }

  async signup(name: string, email: string, otp: string): Promise<void> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; refreshToken: string; sessionId: string; user: User }>>(`/Auth/signup?otp=${otp}`, {
        name, email, currency: 'USD', monthlyIncome: 0,
      })
    );
    if (response.success && response.data) {
      this.setSession(response.data.token, response.data.refreshToken, response.data.sessionId);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Signup failed');
    }
  }

  /** Returns `{ requiresLinking: true, preview }` without creating a session when the
   *  credential matches an existing OTP-created account that's never used Google before —
   *  the caller must show a confirmation and call confirmGoogleLink() to proceed. */
  async googleLogin(credential: string): Promise<LinkableLoginOutcome> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{
        requiresLinking: boolean;
        preview?: { name: string; email: string; createdAt: string };
        auth?: { token: string; refreshToken: string; sessionId: string; user: User };
      }>>('/Auth/google', { credential })
    );
    if (!response.success || !response.data) {
      throw new Error(response.error || 'Google login failed');
    }

    if (response.data.requiresLinking && response.data.preview) {
      return { requiresLinking: true, preview: response.data.preview };
    }

    if (!response.data.auth) {
      throw new Error('Google login failed');
    }
    this.setSession(response.data.auth.token, response.data.auth.refreshToken, response.data.auth.sessionId);
    this.persistUser(response.data.auth.user);
    return { requiresLinking: false };
  }

  async confirmGoogleLink(credential: string): Promise<void> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; refreshToken: string; sessionId: string; user: User }>>('/Auth/google/confirm-link', { credential })
    );
    if (response.success && response.data) {
      this.setSession(response.data.token, response.data.refreshToken, response.data.sessionId);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Account linking failed');
    }
  }

  async requestOTP(email: string, isLogin = false): Promise<ApiResponse<boolean>> {
    return firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/send-otp', { email, isLogin }));
  }

  async verifyOTP(email: string, otp: string): Promise<ApiResponse<boolean>> {
    return firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/verify-otp', { email, otp }));
  }

  logout(): void {
    const sessionId = this.getSessionId();
    if (sessionId) {
      // Best-effort — clear locally regardless of whether the API call succeeds.
      firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/logout', { sessionId })).catch(() => {});
    }
    this.themeService.reset();
    this.clearSession();
    this.router.navigate(['/login']);
  }

  async logoutAll(): Promise<void> {
    try {
      await firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/logout-all'));
    } catch {
      // Best-effort — still clear locally so this device is logged out either way.
    }
    this.themeService.reset();
    this.clearSession();
    this.router.navigate(['/login']);
  }
}
