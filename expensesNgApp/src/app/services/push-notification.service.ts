import { Injectable } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, of } from 'rxjs';
import { timeout, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

export type NotifStatus = 'loading' | 'unsupported' | 'blocked' | 'subscribed' | 'unsubscribed';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private clickHandlerRegistered = false;

  constructor(private swPush: SwPush, private api: ApiService) {}

  async getStatus(): Promise<NotifStatus> {
    if (!this.swPush.isEnabled) return 'unsupported';
    if (typeof Notification === 'undefined') return 'unsupported';
    if (Notification.permission === 'denied') return 'blocked';
    const sub = await firstValueFrom(
      this.swPush.subscription.pipe(timeout(3000), catchError(() => of(null)))
    );
    return sub ? 'subscribed' : 'unsubscribed';
  }

  // Called automatically on login — silently tries to subscribe if not already.
  async initPushNotifications(): Promise<void> {
    if (!this.swPush.isEnabled) {
      console.warn('[Push] SwPush not enabled — service worker not active (are you on ng serve?)');
      return;
    }
    if (typeof Notification === 'undefined' || Notification.permission === 'denied') return;

    try {
      const keyRes = await firstValueFrom(
        this.api.get<ApiResponse<string>>('/notifications/push/vapid-public-key')
      );
      if (!keyRes.success || !keyRes.data) {
        console.warn('[Push] Failed to fetch VAPID public key:', keyRes);
        return;
      }

      let sub = await firstValueFrom(
        this.swPush.subscription.pipe(timeout(5000), catchError(() => of(null)))
      );

      if (!sub) {
        console.log('[Push] No existing subscription — requesting permission...');
        sub = await this.swPush.requestSubscription({ serverPublicKey: keyRes.data });
        console.log('[Push] Subscription obtained');
      } else {
        console.log('[Push] Existing subscription found — re-registering with backend');
      }

      await this.registerSubscription(sub);
      this.ensureClickHandler();
    } catch (err) {
      console.warn('[Push] Push notification setup failed:', err);
    }
  }

  // Called explicitly from settings when the user opts in.
  async subscribe(): Promise<void> {
    const keyRes = await firstValueFrom(
      this.api.get<ApiResponse<string>>('/notifications/push/vapid-public-key')
    );
    if (!keyRes.success || !keyRes.data) throw new Error('Failed to fetch VAPID key');

    const sub = await this.swPush.requestSubscription({ serverPublicKey: keyRes.data });
    await this.registerSubscription(sub);
    this.ensureClickHandler();
  }

  async unsubscribe(): Promise<void> {
    const sub = await firstValueFrom(
      this.swPush.subscription.pipe(timeout(5000), catchError(() => of(null)))
    );
    if (!sub) return;

    await firstValueFrom(
      this.api.post<ApiResponse<boolean>>('/notifications/push/unsubscribe', {
        endpoint: sub.endpoint
      })
    );
    await this.swPush.unsubscribe();
  }

  private async registerSubscription(sub: PushSubscription): Promise<void> {
    const p256dhBuffer = sub.getKey('p256dh');
    const authBuffer  = sub.getKey('auth');
    if (!p256dhBuffer || !authBuffer) return;

    await firstValueFrom(
      this.api.post<ApiResponse<boolean>>('/notifications/push/subscribe', {
        endpoint: sub.endpoint,
        p256dh:   this.bufferToBase64Url(p256dhBuffer),
        auth:     this.bufferToBase64Url(authBuffer)
      })
    );
    console.log('[Push] Subscription registered with backend');
  }

  private ensureClickHandler(): void {
    if (this.clickHandlerRegistered) return;
    this.clickHandlerRegistered = true;
    this.swPush.notificationClicks.subscribe(({ notification }) => {
      const url = notification.data?.onActionClick?.default?.url ?? '/app';
      window.open(url, '_self');
    });
  }

  private bufferToBase64Url(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }
}
