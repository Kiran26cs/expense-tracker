import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UpgradeModalService {
  private _isOpen       = signal(false);
  private _initialPlan  = signal<'Starter' | 'Pro' | null>(null);

  isOpen      = this._isOpen.asReadonly();
  initialPlan = this._initialPlan.asReadonly();

  open(plan?: 'Starter' | 'Pro') {
    this._initialPlan.set(plan ?? null);
    this._isOpen.set(true);
  }

  close() {
    this._isOpen.set(false);
    this._initialPlan.set(null);
  }
}
