import { Injectable, signal, computed } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';
import {
  ImportSession, ImportSessionSummary, StartImportRequest, RetryImportRequest
} from '../models/import.model';

const TERMINAL_STATUSES = new Set(['completed', 'completedWithErrors', 'failed']);
const POLL_INTERVAL_MS  = 4000;

@Injectable({ providedIn: 'root' })
export class ImportService {
  readonly sessions  = signal<ImportSessionSummary[]>([]);
  readonly isOpen    = signal(false);
  readonly activeImportId = signal<string | null>(null);
  readonly activeDetail   = signal<ImportSession | null>(null);
  readonly detailLoading  = signal(false);

  readonly activeCount = computed(() =>
    this.sessions().filter(s => !TERMINAL_STATUSES.has(s.status)).length
  );

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private currentBookId = '';

  constructor(private api: ApiService) {}

  // ── Drawer control ──────────────────────────────────────────────────────────

  openDrawer(bookId: string) {
    this.currentBookId = bookId;
    this.isOpen.set(true);
    this.activeImportId.set(null);
    this.activeDetail.set(null);
    this.loadSessions(bookId);
    this.startPollingIfNeeded();
  }

  closeDrawer() {
    this.isOpen.set(false);
    this.stopPolling();
  }

  openDetail(importId: string) {
    this.activeImportId.set(importId);
    this.loadDetail(importId);
  }

  backToList() {
    this.activeImportId.set(null);
    this.activeDetail.set(null);
  }

  // ── API calls ───────────────────────────────────────────────────────────────

  startImport(bookId: string, request: StartImportRequest) {
    return firstValueFrom(
      this.api.post<ApiResponse<ImportSession>>(`/${bookId}/imports`, request)
    );
  }

  retryFailed(bookId: string, importId: string, rowNumbers: number[] = []) {
    const body: RetryImportRequest = { rowNumbers };
    return firstValueFrom(
      this.api.post<ApiResponse<ImportSession>>(
        `/${bookId}/imports/${importId}/retry`, body
      )
    );
  }

  refreshDetail() {
    const importId = this.activeImportId();
    if (importId) this.loadDetail(importId);
  }

  setActiveDetail(session: ImportSession) {
    this.activeDetail.set(session);
  }

  pollImportSession(bookId: string, sessionId: string) {
    return firstValueFrom(
      this.api.get<ApiResponse<ImportSession>>(`/${bookId}/imports/${sessionId}`)
    );
  }

  async loadSessions(bookId: string) {
    try {
      const res = await firstValueFrom(
        this.api.get<ApiResponse<ImportSessionSummary[]>>(`/${bookId}/imports`)
      );
      if (res.success && res.data) this.sessions.set(res.data);
    } catch { /* silent — drawer stays open */ }
  }

  async loadDetail(importId: string) {
    if (!this.currentBookId || !importId) return;
    this.detailLoading.set(true);
    try {
      const res = await firstValueFrom(
        this.api.get<ApiResponse<ImportSession>>(
          `/${this.currentBookId}/imports/${importId}`
        )
      );
      if (res.success && res.data) this.activeDetail.set(res.data);
    } catch { /* silent */ }
    finally { this.detailLoading.set(false); }
  }

  // ── Polling ──────────────────────────────────────────────────────────────────

  startPollingIfNeeded() {
    this.stopPolling();
    this.pollTimer = setInterval(async () => {
      if (!this.isOpen()) { this.stopPolling(); return; }

      const hasActive = this.sessions().some(s => !TERMINAL_STATUSES.has(s.status));
      if (!hasActive) { this.stopPolling(); return; }

      await this.loadSessions(this.currentBookId);

      // Refresh detail view if it's showing an in-progress session
      const detailId = this.activeImportId();
      if (detailId) {
        const summary = this.sessions().find(s => s.id === detailId);
        if (summary && !TERMINAL_STATUSES.has(summary.status))
          await this.loadDetail(detailId);
      }
    }, POLL_INTERVAL_MS);
  }

  stopPolling() {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  // Called by expense-list after a new import is kicked off
  notifyNewImport(bookId: string, session: ImportSession) {
    this.currentBookId = bookId;
    const summary: ImportSessionSummary = {
      id:             session.id,
      fileName:       session.fileName,
      status:         session.status,
      totalRecords:   session.totalRecords,
      processedCount: session.processedCount,
      successCount:   session.successCount,
      failedCount:    session.failedCount,
      createdAt:      session.createdAt,
      completedAt:    session.completedAt
    };
    this.sessions.update(list => [summary, ...list]);
    this.startPollingIfNeeded();
  }
}
