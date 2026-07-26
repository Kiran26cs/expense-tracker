import { inject, Injectable, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PwaService {
  private swUpdate = inject(SwUpdate);
  private deferredPrompt: BeforeInstallPromptEvent | null = null;

  readonly installable      = signal(false);
  readonly updateAvailable  = signal(false);
  readonly isOnline         = signal(navigator.onLine);

  constructor() {
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      this.deferredPrompt = e as BeforeInstallPromptEvent;
      this.installable.set(true);
    });

    window.addEventListener('appinstalled', () => {
      this.installable.set(false);
      this.deferredPrompt = null;
    });

    window.addEventListener('online',  () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));

    if (this.swUpdate.isEnabled) {
      this.swUpdate.versionUpdates
        .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
        .subscribe(() => this.updateAvailable.set(true));
    }
  }

  async install(): Promise<void> {
    if (!this.deferredPrompt) return;
    await this.deferredPrompt.prompt();
    const { outcome } = await this.deferredPrompt.userChoice;
    if (outcome === 'accepted') this.installable.set(false);
    this.deferredPrompt = null;
  }

  reload(): void {
    window.location.reload();
  }
}

// BeforeInstallPromptEvent is not yet in TypeScript's lib.dom.d.ts
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}
