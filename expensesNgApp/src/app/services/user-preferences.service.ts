import { Injectable, inject } from '@angular/core';
import { AuthStateService } from './auth-state.service';

@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private auth = inject(AuthStateService);

  private key(name: string, bookId: string): string {
    const uid = this.auth.user()?.id ?? 'guest';
    return `pref-${name}-${uid}-${bookId}`;
  }

  get<T>(name: string, bookId: string): T | null {
    try {
      const raw = localStorage.getItem(this.key(name, bookId));
      return raw ? (JSON.parse(raw) as T) : null;
    } catch { return null; }
  }

  set<T>(name: string, bookId: string, value: T): void {
    localStorage.setItem(this.key(name, bookId), JSON.stringify(value));
  }

  clear(name: string, bookId: string): void {
    localStorage.removeItem(this.key(name, bookId));
  }
}
