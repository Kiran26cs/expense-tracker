import { Injectable, signal } from '@angular/core';

const DISMISSED_KEY = 'pwa_install_dismissed_until';

declare global {
  interface Window { __pwaPrompt: any; }
}

@Injectable({ providedIn: 'root' })
export class PwaService {
  private deferredPrompt: any = null;
  canInstall = signal(false);
  isIos = signal(false);
  showIosInstructions = signal(false);

  constructor() {
    const ua = navigator.userAgent.toLowerCase();
    const ios = /iphone|ipad|ipod/.test(ua);
    const standalone = ('standalone' in window.navigator) && !!(window.navigator as any)['standalone'];

    if (ios) {
      this.isIos.set(true);
      if (!standalone && !this.isDismissed()) {
        this.showIosInstructions.set(true);
      }
      return;
    }

    // Pick up event captured before Angular loaded
    if (window.__pwaPrompt) {
      this.deferredPrompt = window.__pwaPrompt;
      window.__pwaPrompt = null;
      if (!this.isDismissed()) {
        this.canInstall.set(true);
      }
    }

    // Also listen for future firings (e.g. after SW update)
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      this.deferredPrompt = e;
      if (!this.isDismissed()) {
        this.canInstall.set(true);
      }
    });

    window.addEventListener('appinstalled', () => {
      this.deferredPrompt = null;
      this.canInstall.set(false);
    });
  }

  async promptInstall(): Promise<void> {
    if (!this.deferredPrompt) return;
    this.deferredPrompt.prompt();
    const { outcome } = await this.deferredPrompt.userChoice;
    if (outcome === 'accepted') {
      this.deferredPrompt = null;
      this.canInstall.set(false);
    }
  }

  dismiss(): void {
    const until = Date.now() + 7 * 24 * 60 * 60 * 1000;
    localStorage.setItem(DISMISSED_KEY, String(until));
    this.canInstall.set(false);
    this.showIosInstructions.set(false);
  }

  private isDismissed(): boolean {
    const until = Number(localStorage.getItem(DISMISSED_KEY) ?? 0);
    return Date.now() < until;
  }
}
