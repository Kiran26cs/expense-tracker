import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';

interface ImportSession {
  id: string;
  expenseBookId: string;
  userId: string;
  fileName: string;
  status: string;
  totalRecords: number;
  successCount: number;
  failedCount: number;
  createdAt: string;
  completedAt: string | null;
}

@Component({
  selector: 'admin-jobs',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './jobs.component.html',
  styleUrl: './jobs.component.css',
})
export class JobsComponent implements OnInit {
  private api = inject(AdminApiService);

  sessions      = signal<ImportSession[]>([]);
  statusFilter  = signal('');
  loading       = signal(false);

  creditResetRunning = signal(false);
  creditResetResult  = signal<string | null>(null);
  creditResetError   = signal<string | null>(null);

  retryingId    = signal<string | null>(null);
  retryResults  = signal<Record<string, string>>({});

  ngOnInit() { this.loadSessions(); }

  loadSessions() {
    this.loading.set(true);
    const params = this.statusFilter() ? `?status=${this.statusFilter()}` : '';
    this.api.get<ApiResponse<ImportSession[]>>(`/admin/jobs/imports${params}`).subscribe({
      next: r => { this.sessions.set(r.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  triggerCreditReset() {
    if (!confirm('Run the monthly credit reset now? This will grant free credits to all eligible books.')) return;
    this.creditResetRunning.set(true);
    this.creditResetResult.set(null);
    this.creditResetError.set(null);

    this.api.post<ApiResponse<{ message: string }>>('/admin/jobs/trigger/credit-reset', {}).subscribe({
      next: r => { this.creditResetResult.set(r.data?.message ?? 'Done.'); this.creditResetRunning.set(false); },
      error: err => { this.creditResetError.set(err.error?.message ?? 'Failed.'); this.creditResetRunning.set(false); },
    });
  }

  retryImport(sessionId: string) {
    this.retryingId.set(sessionId);
    this.retryResults.update(m => ({ ...m, [sessionId]: '' }));

    this.api.post<ApiResponse<{ message: string }>>(`/admin/jobs/imports/${sessionId}/retry`, {}).subscribe({
      next: r => {
        this.retryResults.update(m => ({ ...m, [sessionId]: r.data?.message ?? 'Queued.' }));
        this.retryingId.set(null);
        // Update the session status optimistically
        this.sessions.update(list => list.map(s => s.id === sessionId ? { ...s, status: 'queued' } : s));
      },
      error: err => {
        this.retryResults.update(m => ({ ...m, [sessionId]: err.error?.message ?? 'Retry failed.' }));
        this.retryingId.set(null);
      },
    });
  }

  get failedSessions(): ImportSession[] {
    return this.sessions().filter(s => s.status === 'failed' || s.status === 'completedWithErrors');
  }

  statusClass(status: string): string {
    switch (status) {
      case 'completed':           return 'status-done';
      case 'failed':              return 'status-failed';
      case 'completedWithErrors': return 'status-warn';
      case 'processing':          return 'status-active';
      default:                    return 'status-queued';
    }
  }
}
