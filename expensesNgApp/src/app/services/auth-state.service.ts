import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { User, ApiResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private userSignal = signal<User | null>(null);
  private loadingSignal = signal(true);

  user = this.userSignal.asReadonly();
  isLoading = this.loadingSignal.asReadonly();
  isAuthenticated = computed(() => !!this.userSignal());

  constructor(private api: ApiService, private router: Router) {
    this.checkAuth();
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  setToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  clearToken(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');
  }

  private persistUser(user: User): void {
    console.log('persisted user...', user);
    localStorage.setItem('authUser', JSON.stringify(user));
    this.userSignal.set(user);
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
        this.clearToken();
      }
    }
    this.loadingSignal.set(false);
  }

  async login(emailOrPhone: string, otp: string): Promise<void> {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; user: User }>>(`/Auth/login?otp=${otp}`, { email, phone })
    );
    
    if (response.success && response.data) {
      console.log('email...', email, response.data.user);
      this.setToken(response.data.token);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Login failed');
    }
  }

  async signup(name: string, emailOrPhone: string, otp: string): Promise<void> {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; user: User }>>(`/Auth/signup?otp=${otp}`, {
        name, email, phone, currency: 'INR', monthlyIncome: 0,
      })
    );
    if (response.success && response.data) {
      this.setToken(response.data.token);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Signup failed');
    }
  }

  async googleLogin(credential: string): Promise<void> {
    const response = await firstValueFrom(
      this.api.post<ApiResponse<{ token: string; user: User }>>('/Auth/google', { credential })
    );
    if (response.success && response.data) {
      this.setToken(response.data.token);
      this.persistUser(response.data.user);
    } else {
      throw new Error(response.error || 'Google login failed');
    }
  }

  async requestOTP(emailOrPhone: string): Promise<ApiResponse<boolean>> {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    return firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/send-otp', { email, phone }));
  }

  async verifyOTP(emailOrPhone: string, otp: string): Promise<ApiResponse<boolean>> {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    return firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/verify-otp', { email, phone, otp }));
  }

  logout(): void {
    this.clearToken();
    this.userSignal.set(null);
    this.router.navigate(['/login']);
  }
}
