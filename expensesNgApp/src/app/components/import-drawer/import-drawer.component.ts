import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ImportService } from '../../services/import.service';
import { ImportSessionSummary } from '../../models/import.model';
import { CurrentBookService } from '../../services/current-book.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-import-drawer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './import-drawer.component.html',
  styleUrl: './import-drawer.component.css'
})
export class ImportDrawerComponent {
  importService   = inject(ImportService);
  private currentBook = inject(CurrentBookService);
  private toast       = inject(ToastService);

  retrying = false;

  get sessions()     { return this.importService.sessions(); }
  get isOpen()       { return this.importService.isOpen(); }
  get activeDetail() { return this.importService.activeDetail(); }
  get detailLoading(){ return this.importService.detailLoading(); }
  get showDetail()   { return !!this.importService.activeImportId(); }

  get inProgressRecords() {
    return this.activeDetail?.records.filter(r => r.status === 'pending') ?? [];
  }
  get successRecords() {
    return this.activeDetail?.records.filter(r => r.status === 'success') ?? [];
  }
  get failedRecords() {
    return this.activeDetail?.records.filter(r => r.status === 'failed') ?? [];
  }

  close()                { this.importService.closeDrawer(); }
  openDetail(id: string) { this.importService.openDetail(id); }
  back()                 { this.importService.backToList(); }
  refresh()              { this.importService.refreshDetail(); }

  async retryAll() {
    await this.doRetry([]);
  }

  async retrySingle(rowNumber: number) {
    await this.doRetry([rowNumber]);
  }

  private async doRetry(rowNumbers: number[]) {
    const detail  = this.activeDetail;
    const bookId  = this.currentBook.book()?.id;
    if (!detail || !bookId || this.retrying) return;

    this.retrying = true;
    try {
      const res = await this.importService.retryFailed(bookId, detail.id, rowNumbers);
      if (res.success && res.data) {
        this.importService.setActiveDetail(res.data);
        this.importService.startPollingIfNeeded();
        this.toast.success('Retry queued — records are being reprocessed');
      } else {
        this.toast.error(res.message ?? 'Retry failed');
      }
    } catch {
      this.toast.error('Failed to queue retry');
    } finally {
      this.retrying = false;
    }
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      queued: 'Queued', processing: 'Processing',
      completed: 'Completed', completedWithErrors: 'Done with errors', failed: 'Failed'
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    if (s === 'completed')           return 'badge-success';
    if (s === 'completedWithErrors') return 'badge-warning';
    if (s === 'failed')              return 'badge-error';
    if (s === 'processing')          return 'badge-info';
    return 'badge-muted';
  }

  progressPercent(session: ImportSessionSummary): number {
    if (!session.totalRecords) return 0;
    return Math.round((session.processedCount / session.totalRecords) * 100);
  }

  isTerminal(status: string): boolean {
    return ['completed', 'completedWithErrors', 'failed'].includes(status);
  }
}
