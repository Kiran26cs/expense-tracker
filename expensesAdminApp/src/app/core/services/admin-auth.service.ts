import { Injectable, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminDto, AdminAuthResponse, ApiResponse } from '../../shared/models/admin.models';

const TOKEN_KEY = 'nw_admin_token';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private router = inject(Router);
  private http    = inject(HttpClient);

  private _admin = signal<AdminDto | null>(null);

  readonly currentAdmin    = this._admin.asReadonly();
  readonly isAuthenticated = computed(() => !!this._admin());

  hasPermission(permission: string): boolean {
    return this._admin()?.permissions.includes(permission) ?? false;
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  async sendOtp(email: string): Promise<void> {
    await firstValueFrom(
      this.http.post<ApiResponse<unknown>>(
        `${environment.apiBaseUrl}/admin/auth/send-otp`,
        { email }
      )
    );
  }

  async verifyOtp(email: string, otp: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<ApiResponse<AdminAuthResponse>>(
        `${environment.apiBaseUrl}/admin/auth/verify-otp`,
        { email, otp }
      )
    );

    if (!res.success || !res.data)
      throw new Error(res.error ?? 'Login failed.');

    localStorage.setItem(TOKEN_KEY, res.data.token);
    this._admin.set(res.data.admin);
  }

  async loadAdminFromToken(): Promise<void> {
    const token = this.getToken();
    if (!token) return;

    // Decode the JWT locally — no network call needed.
    // This keeps the session alive across backend restarts: the token stays in
    // localStorage and the guard passes even before the backend responds.
    try {
      const base64  = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(base64));

      // Token has already expired — clear immediately
      if (payload.exp && payload.exp * 1000 < Date.now()) {
        this.clearSession();
        return;
      }

      const rawPerms = payload['permission'];
      const permissions: string[] = rawPerms
        ? (Array.isArray(rawPerms) ? rawPerms : [rawPerms])
        : [];

      // Hydrate admin from claims so auth guard passes without waiting for network
      // 'name' is not a JWT claim here, so we fall back to email until the /me call updates it
      this._admin.set({
        id:          payload.sub   ?? '',
        email:       payload.email ?? '',
        name:        payload.email ?? '',
        permissions,
      });
    } catch {
      this.clearSession();
      return;
    }

    // Background verify: refreshes the full admin profile from the database.
    // Only a 401 (invalid/revoked token) triggers logout.
    // Network errors, 5xx, or connection refused during a restart are ignored —
    // the locally-decoded session stays valid until the token actually expires.
    this.http.get<ApiResponse<AdminDto>>(`${environment.apiBaseUrl}/admin/auth/me`).subscribe({
      next: res => {
        if (res.success && res.data) this._admin.set(res.data);
        else this.clearSession();
      },
      error: (err: any) => {
        if (err?.status === 401) this.clearSession();
      },
    });
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._admin.set(null);
  }
}
