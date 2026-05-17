import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SessionBus {
  private readonly _expired = new Subject<void>();
  readonly expired$ = this._expired.asObservable();

  notifyExpired(): void {
    this._expired.next();
  }
}
