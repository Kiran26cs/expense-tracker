import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';

interface InvalidateResult { message: string; }

@Component({
  selector: 'admin-cache',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './cache.component.html',
  styleUrl: './cache.component.css',
})
export class CacheComponent {
  private api = inject(AdminApiService);

  userId   = signal('');
  bookId   = signal('');

  userLoading = signal(false);
  bookLoading = signal(false);

  userResult  = signal<string | null>(null);
  bookResult  = signal<string | null>(null);

  userError   = signal<string | null>(null);
  bookError   = signal<string | null>(null);

  invalidateUser() {
    const id = this.userId().trim();
    if (!id) return;
    this.userLoading.set(true);
    this.userResult.set(null);
    this.userError.set(null);

    this.api.post<ApiResponse<InvalidateResult>>(`/admin/cache/invalidate/user/${id}`, {}).subscribe({
      next: r => { this.userResult.set(r.data?.message ?? 'Cache cleared.'); this.userLoading.set(false); },
      error: err => { this.userError.set(err.error?.message ?? 'Failed to invalidate cache.'); this.userLoading.set(false); },
    });
  }

  invalidateBook() {
    const id = this.bookId().trim();
    if (!id) return;
    this.bookLoading.set(true);
    this.bookResult.set(null);
    this.bookError.set(null);

    this.api.post<ApiResponse<InvalidateResult>>(`/admin/cache/invalidate/book/${id}`, {}).subscribe({
      next: r => { this.bookResult.set(r.data?.message ?? 'Cache cleared.'); this.bookLoading.set(false); },
      error: err => { this.bookError.set(err.error?.message ?? 'Failed to invalidate cache.'); this.bookLoading.set(false); },
    });
  }
}
