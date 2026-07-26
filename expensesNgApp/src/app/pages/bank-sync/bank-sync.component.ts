import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BankConnectionService } from '../../services/bank-connection.service';
import { CurrentBookService } from '../../services/current-book.service';
import { ToastService } from '../../services/toast.service';
import {
  BankConnectionDto,
  BankName,
  ParsedBankTransactionDto,
  BankStatementPreviewDto,
} from '../../models/bank-connection.model';
import { CardComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { LoadingComponent } from '../../components/loading/loading.component';

const BANK_NAMES: BankName[] = ['HDFC', 'ICICI', 'SBI', 'Axis', 'Kotak', 'IndusInd', 'Other'];
const PAYMENT_METHODS = ['Bank Transfer', 'UPI', 'Debit Card', 'Credit Card', 'Cash', 'Other'];

type Step = 'connections' | 'upload' | 'preview' | 'done';

@Component({
  selector: 'app-bank-sync',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    CardComponent,
    ButtonComponent, ModalComponent, LoadingComponent,
  ],
  templateUrl: './bank-sync.component.html',
  styleUrl: './bank-sync.component.css',
})
export class BankSyncComponent implements OnInit {
  private svc   = inject(BankConnectionService);
  private route = inject(ActivatedRoute);
  readonly currentBook = inject(CurrentBookService);
  private toast = inject(ToastService);

  readonly bankNames      = BANK_NAMES;
  readonly paymentMethods = PAYMENT_METHODS;

  bookId      = '';
  connections = signal<BankConnectionDto[]>([]);
  loading     = signal(true);

  // ── Add connection modal ──────────────────────────────────────────────────
  showAddModal  = signal(false);
  addLoading    = signal(false);
  addError      = signal('');
  newDisplayName = '';
  newBankName: BankName = 'HDFC';

  // ── Upload flow (step = upload → preview → done) ──────────────────────────
  step               = signal<Step>('connections');
  activeConnection   = signal<BankConnectionDto | null>(null);

  // Upload step
  selectedFile       = signal<File | null>(null);
  pdfPassword        = '';
  uploadLoading      = signal(false);
  uploadError        = signal('');

  // Preview step
  preview            = signal<BankStatementPreviewDto | null>(null);
  excludedRows       = signal<Set<number>>(new Set());
  defaultPayment     = 'Bank Transfer';
  confirmLoading     = signal(false);
  confirmError       = signal('');

  // Done step
  importedCount      = signal(0);
  duplicatesCount    = signal(0);

  // ── Delete confirm ─────────────────────────────────────────────────────────
  showDeleteConfirm  = signal(false);
  connToDelete       = signal<BankConnectionDto | null>(null);
  deleteLoading      = signal(false);

  get showPasswordField(): boolean {
    const name = this.selectedFile()?.name.toLowerCase() ?? '';
    return name.endsWith('.pdf') || name.endsWith('.xls') || name.endsWith('.xlsx');
  }

  get isPdf(): boolean {
    return this.selectedFile()?.name.toLowerCase().endsWith('.pdf') ?? false;
  }

  get includedCount(): number {
    const p = this.preview();
    if (!p) return 0;
    return p.transactions.filter(t => !this.excludedRows().has(t.rowNumber)).length;
  }

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadConnections();
    });
  }

  async loadConnections() {
    this.loading.set(true);
    try {
      const res = await this.svc.getConnections();
      if (res.success && res.data) this.connections.set(res.data);
    } catch { /* silent */ }
    finally { this.loading.set(false); }
  }

  // ── Add connection ──────────────────────────────────────────────────────────
  openAddModal() {
    this.newDisplayName = '';
    this.newBankName    = 'HDFC';
    this.addError.set('');
    this.showAddModal.set(true);
  }

  async handleAddConnection() {
    if (!this.newDisplayName.trim()) { this.addError.set('Display name is required'); return; }
    this.addLoading.set(true);
    this.addError.set('');
    try {
      const res = await this.svc.createConnection({
        displayName: this.newDisplayName.trim(),
        bankName:    this.newBankName,
        mode:        'manual',
      });
      if (res.success && res.data) {
        this.connections.update(list => [...list, res.data!]);
        this.showAddModal.set(false);
        this.toast.success(`${res.data.displayName} added`);
      } else {
        this.addError.set(res.error || 'Failed to add connection');
      }
    } catch (e: any) {
      this.addError.set(e?.error?.error ?? e?.message ?? 'Failed to add connection');
    } finally {
      this.addLoading.set(false);
    }
  }

  // ── Delete connection ───────────────────────────────────────────────────────
  promptDelete(conn: BankConnectionDto) {
    this.connToDelete.set(conn);
    this.showDeleteConfirm.set(true);
  }

  async confirmDelete() {
    const conn = this.connToDelete();
    if (!conn) return;
    this.deleteLoading.set(true);
    try {
      const res = await this.svc.deleteConnection(conn.id);
      if (res.success) {
        this.connections.update(list => list.filter(c => c.id !== conn.id));
        this.toast.success('Connection removed');
      } else {
        this.toast.error(res.error || 'Failed to remove');
      }
    } catch (e: any) {
      this.toast.error(e?.error?.error ?? 'Failed to remove');
    } finally {
      this.deleteLoading.set(false);
      this.showDeleteConfirm.set(false);
      this.connToDelete.set(null);
    }
  }

  // ── Upload flow ─────────────────────────────────────────────────────────────
  startUpload(conn: BankConnectionDto) {
    this.activeConnection.set(conn);
    this.selectedFile.set(null);
    this.pdfPassword    = '';
    this.uploadError.set('');
    this.step.set('upload');
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.selectedFile.set(file);
    this.uploadError.set('');
    // Reset password if switching to non-PDF
    if (file && !file.name.toLowerCase().endsWith('.pdf')) this.pdfPassword = '';
  }

  async handleUpload() {
    const file = this.selectedFile();
    const conn = this.activeConnection();
    if (!file || !conn) return;

    this.uploadLoading.set(true);
    this.uploadError.set('');
    try {
      const res = await this.svc.parseStatement(conn.id, file, this.pdfPassword || undefined);
      if (res.success && res.data) {
        this.preview.set(res.data);
        this.excludedRows.set(new Set());
        this.defaultPayment = 'Bank Transfer';
        this.confirmError.set('');
        this.step.set('preview');
      } else {
        this.uploadError.set(res.error || 'Could not parse this file');
      }
    } catch (e: any) {
      this.uploadError.set(e?.error?.error ?? e?.message ?? 'Upload failed');
    } finally {
      this.uploadLoading.set(false);
    }
  }

  // ── Preview step ────────────────────────────────────────────────────────────
  toggleRow(rowNumber: number) {
    this.excludedRows.update(set => {
      const next = new Set(set);
      if (next.has(rowNumber)) next.delete(rowNumber); else next.add(rowNumber);
      return next;
    });
  }

  isExcluded(rowNumber: number): boolean {
    return this.excludedRows().has(rowNumber);
  }

  selectAll()  { this.excludedRows.set(new Set()); }
  deselectAll() {
    const p = this.preview();
    if (!p) return;
    this.excludedRows.set(new Set(p.transactions.map(t => t.rowNumber)));
  }

  async handleConfirm() {
    const p    = this.preview();
    const conn = this.activeConnection();
    if (!p || !conn || !this.bookId) return;

    if (this.includedCount === 0) {
      this.confirmError.set('Select at least one transaction to import');
      return;
    }

    this.confirmLoading.set(true);
    this.confirmError.set('');
    try {
      const res = await this.svc.confirmSync(p.sessionId, {
        expenseBookId:        this.bookId,
        defaultPaymentMethod: this.defaultPayment,
        excludeRowNumbers:    Array.from(this.excludedRows()),
      });
      if (res.success && res.data) {
        this.importedCount.set(res.data.imported);
        this.duplicatesCount.set(res.data.duplicatesSkipped);
        this.step.set('done');
        // Refresh connection list to update lastSyncedAt
        await this.loadConnections();
      } else {
        this.confirmError.set(res.error || 'Import failed');
      }
    } catch (e: any) {
      this.confirmError.set(e?.error?.error ?? e?.message ?? 'Import failed');
    } finally {
      this.confirmLoading.set(false);
    }
  }

  backToConnections() {
    this.step.set('connections');
    this.activeConnection.set(null);
    this.preview.set(null);
    this.selectedFile.set(null);
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatAmount(amount: number, type: string): string {
    const sign = type === 'income' ? '+' : '-';
    return `${sign}₹${Math.abs(amount).toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  bankIcon(bankName: string): string {
    const icons: Record<string, string> = {
      HDFC: 'fa-solid fa-h', ICICI: 'fa-solid fa-i',
      SBI:  'fa-solid fa-s', Axis:  'fa-solid fa-a',
      Kotak:'fa-solid fa-k', Other: 'fa-solid fa-building-columns',
    };
    return icons[bankName] ?? 'fa-solid fa-building-columns';
  }

  sinceLabel(dateStr?: string): string {
    if (!dateStr) return 'Never synced';
    const diff = Date.now() - new Date(dateStr).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return 'Today';
    if (days === 1) return 'Yesterday';
    return `${days} days ago`;
  }
}
