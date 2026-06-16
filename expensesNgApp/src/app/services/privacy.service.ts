import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

const SESSION_VERIFIED_KEY = 'privacy_verified';
const BIOMETRIC_CRED_KEY = 'privacy_biometric_cred_id';

@Injectable({ providedIn: 'root' })
export class PrivacyService {
  private api = inject(ApiService);

  private _isHidden = signal(true);
  isHidden = this._isHidden.asReadonly();

  get isVerifiedThisSession(): boolean {
    return sessionStorage.getItem(SESSION_VERIFIED_KEY) === '1';
  }

  /** Called after successful PIN or biometric verification — unlocks for this session. */
  markVerified(): void {
    sessionStorage.setItem(SESSION_VERIFIED_KEY, '1');
    this._isHidden.set(false);
  }

  /** Re-hide without clearing session verification — no credential needed to show again. */
  hide(): void {
    this._isHidden.set(true);
  }

  /** Toggle visibility. No credential prompt — only valid after markVerified() this session. */
  toggleVisibility(): void {
    this._isHidden.update(v => !v);
  }

  /** True when the user needs to enter credentials before values can be shown. */
  needsCredentials(): boolean {
    return !this.isVerifiedThisSession;
  }

  async hashPin(pin: string): Promise<string> {
    const data = new TextEncoder().encode(pin);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    return Array.from(new Uint8Array(hashBuffer))
      .map(b => b.toString(16).padStart(2, '0'))
      .join('');
  }

  async checkPinExists(): Promise<boolean> {
    const res = await firstValueFrom(
      this.api.get<ApiResponse<{ hasPinSet: boolean }>>('/settings/privacy-pin/status')
    );
    return res.success && (res.data?.hasPinSet ?? false);
  }

  async setPin(pinHash: string): Promise<void> {
    const res = await firstValueFrom(
      this.api.post<ApiResponse<boolean>>('/settings/privacy-pin', { pinHash })
    );
    if (!res.success) throw new Error(res.error || 'Failed to save PIN');
  }

  async verifyPin(pinHash: string): Promise<boolean> {
    const res = await firstValueFrom(
      this.api.post<ApiResponse<{ valid: boolean }>>('/settings/privacy-pin/verify', { pinHash })
    );
    return res.success && (res.data?.valid ?? false);
  }

  async removePin(): Promise<void> {
    await firstValueFrom(this.api.delete<ApiResponse<boolean>>('/settings/privacy-pin'));
  }

  // ── Biometric (WebAuthn platform authenticator) ────────────────────────────

  async isBiometricAvailable(): Promise<boolean> {
    try {
      if (typeof PublicKeyCredential === 'undefined') return false;
      return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    } catch {
      return false;
    }
  }

  hasBiometricRegistered(): boolean {
    return !!localStorage.getItem(BIOMETRIC_CRED_KEY);
  }

  async registerBiometric(userId: string): Promise<boolean> {
    try {
      const challenge = crypto.getRandomValues(new Uint8Array(32)) as unknown as ArrayBuffer;
      const credential = await navigator.credentials.create({
        publicKey: {
          challenge,
          rp: { name: 'Expense Tracker', id: window.location.hostname },
          user: {
            id: new TextEncoder().encode(userId).buffer as ArrayBuffer,
            name: userId,
            displayName: 'Expense Tracker',
          },
          pubKeyCredParams: [
            { type: 'public-key', alg: -7 },   // ES256
            { type: 'public-key', alg: -257 },  // RS256
          ],
          authenticatorSelection: {
            authenticatorAttachment: 'platform',
            userVerification: 'required',
            residentKey: 'preferred',
          },
          timeout: 60000,
        },
      }) as PublicKeyCredential | null;

      if (!credential) return false;
      localStorage.setItem(BIOMETRIC_CRED_KEY, credential.id);
      return true;
    } catch {
      return false;
    }
  }

  async unlockWithBiometric(): Promise<boolean> {
    try {
      const credId = localStorage.getItem(BIOMETRIC_CRED_KEY);
      if (!credId) return false;

      const challenge = crypto.getRandomValues(new Uint8Array(32)) as unknown as ArrayBuffer;
      const credential = await navigator.credentials.get({
        publicKey: {
          challenge,
          rpId: window.location.hostname,
          userVerification: 'required',
          allowCredentials: [{ id: this.base64urlToUint8Array(credId) as unknown as ArrayBuffer, type: 'public-key' }],
          timeout: 60000,
        },
      });
      return !!credential;
    } catch {
      return false;
    }
  }

  private base64urlToUint8Array(base64url: string): Uint8Array {
    const padding = '='.repeat((4 - (base64url.length % 4)) % 4);
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/') + padding;
    const raw = atob(base64);
    const result = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) result[i] = raw.charCodeAt(i);
    return result;
  }
}
