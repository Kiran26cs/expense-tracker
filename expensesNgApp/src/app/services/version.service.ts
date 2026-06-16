import { Injectable, OnDestroy, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwUpdate } from '@angular/service-worker';
import { BehaviorSubject, Subscription, fromEvent, interval, merge } from 'rxjs';
import { APP_VERSION } from '../version';

@Injectable({ providedIn: 'root' })
export class VersionService implements OnDestroy {
  private http = inject(HttpClient);
  private swUpdate = inject(SwUpdate);

  private _updateAvailable = new BehaviorSubject<boolean>(false);
  readonly updateAvailable$ = this._updateAvailable.asObservable();

  private knownApiVersion: string | null = null;
  private updateSource: 'sw' | 'poll' | 'api' = 'poll';
  private initialized = false;
  private subs = new Subscription();

  init() {
    if (this.initialized) return;
    this.initialized = true;
    this.listenToSwUpdates();
    this.startUiVersionPolling();
  }

  checkApiVersion(version: string) {
    if (!this.knownApiVersion) {
      this.knownApiVersion = version;
      return;
    }
    if (version !== this.knownApiVersion) {
      this.updateSource = 'api';
      this._updateAvailable.next(true);
    }
  }

  async refresh() {
    if (this.updateSource === 'sw' && this.swUpdate.isEnabled) {
      await this.swUpdate.activateUpdate();
    } else if (this.updateSource === 'poll') {
      if ('caches' in window) {
        const names = await caches.keys();
        await Promise.all(names.map(n => caches.delete(n)));
      }
    }
    window.location.reload();
  }

  private listenToSwUpdates() {
    if (!this.swUpdate.isEnabled) return;
    this.subs.add(
      this.swUpdate.versionUpdates.subscribe(evt => {
        if (evt.type === 'VERSION_READY') {
          this.updateSource = 'sw';
          this._updateAvailable.next(true);
        }
      })
    );
  }

  private startUiVersionPolling() {
    const FIVE_MIN = 5 * 60 * 1000;
    this.subs.add(
      merge(interval(FIVE_MIN), fromEvent(window, 'focus')).subscribe(() =>
        this.checkUiVersion()
      )
    );
    setTimeout(() => this.checkUiVersion(), 10_000);
  }

  private checkUiVersion() {
    this.http.get<{ version: string }>('/version.json').subscribe({
      next: ({ version }) => {
        if (version !== APP_VERSION) {
          this.updateSource = 'poll';
          this._updateAvailable.next(true);
        }
      },
      error: () => {},
    });
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }
}
