import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LendingService } from '../../services/lending.service';
import { ToastService } from '../../services/toast.service';
import { Lending, Repayment, CreateRepaymentRequest, LendingRepaymentsResponse } from '../../models/lending.model';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { formatCurrency, formatDate } from '../../utils/helpers';

@Component({
  selector: 'app-lending-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmDialogComponent],
  templateUrl: './lending-panel.component.html',
  styleUrl: './lending-panel.component.css'
})
export class LendingPanelComponent implements OnChanges {
  @Input() bookId = '';
  @Input() lending: Lending | null = null;
  @Input() currency = 'INR';
  @Output() closed = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();

  repayments = signal<Repayment[]>([]);
  lendingData = signal<Lending | null>(null);
  loading = signal(false);
  page = signal(1);
  hasMore = signal(false);
  total = signal(0);
  pageSize = 50;

  // Add repayment form
  showAddRepayment = signal(false);
  addLoading = signal(false);
  repaymentDate = new Date().toISOString().split('T')[0];
  repaymentAmount: number | null = null;
  repaymentNotes = '';

  // Delete repayment
  showDeleteConfirm = signal(false);
  repaymentToDelete = signal<Repayment | null>(null);
  deleteLoading = signal(false);

  // Delete lending
  showDeleteLendingConfirm = signal(false);
  deleteLendingLoading = signal(false);

  // Settle
  showSettleForm = signal(false);
  settleLoading = signal(false);
  settleInterestAmount: number | null = null;
  settleDate = new Date().toISOString().split('T')[0];
  settleNotes = '';

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDate = formatDate;

  constructor(
    private lendingService: LendingService,
    private toast: ToastService
  ) {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['lending'] && this.lending) {
      this.lendingData.set(this.lending);
      this.page.set(1);
      this.repayments.set([]);
      this.loadRepayments();
    }
  }

  async loadRepayments(append = false) {
    if (!this.lending) return;
    this.loading.set(true);
    try {
      const res = await this.lendingService.getRepayments(this.bookId, this.lending.id, this.page(), this.pageSize);
      this.lendingData.set(res.lending);
      if (append) {
        this.repayments.update(prev => [...prev, ...res.items]);
      } else {
        this.repayments.set(res.items);
      }
      this.total.set(res.total);
      this.hasMore.set(res.hasMore);
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to load repayments');
    } finally {
      this.loading.set(false);
    }
  }

  loadMore() {
    this.page.update(p => p + 1);
    this.loadRepayments(true);
  }

  openAddRepayment() {
    this.repaymentDate = new Date().toISOString().split('T')[0];
    this.repaymentAmount = null;
    this.repaymentNotes = '';
    this.showAddRepayment.set(true);
  }

  async submitRepayment() {
    if (!this.repaymentAmount || !this.repaymentDate || !this.lending) return;
    this.addLoading.set(true);
    try {
      const req: CreateRepaymentRequest = {
        date: this.repaymentDate,
        amount: this.repaymentAmount,
        notes: this.repaymentNotes || undefined,
      };
      await this.lendingService.addRepayment(this.bookId, this.lending.id, req);
      this.toast.success('Repayment recorded');
      this.showAddRepayment.set(false);
      this.page.set(1);
      await this.loadRepayments();
      this.updated.emit();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to add repayment');
    } finally {
      this.addLoading.set(false);
    }
  }

  openDeleteRepayment(r: Repayment) {
    this.repaymentToDelete.set(r);
    this.showDeleteConfirm.set(true);
  }

  async confirmDeleteRepayment() {
    const r = this.repaymentToDelete();
    if (!r || !this.lending) return;
    this.deleteLoading.set(true);
    try {
      await this.lendingService.deleteRepayment(this.bookId, this.lending.id, r.id);
      this.toast.success('Repayment deleted');
      this.showDeleteConfirm.set(false);
      this.page.set(1);
      await this.loadRepayments();
      this.updated.emit();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to delete');
    } finally {
      this.deleteLoading.set(false);
    }
  }

  openSettleForm() {
    const ld = this.lendingData();
    this.settleInterestAmount = ld?.projectedTotalInterest && ld.projectedTotalInterest > 0
      ? ld.projectedTotalInterest
      : null;
    this.settleDate = new Date().toISOString().split('T')[0];
    this.settleNotes = 'Interest settlement';
    this.showSettleForm.set(true);
  }

  async confirmSettle() {
    if (!this.lending) return;
    this.settleLoading.set(true);
    try {
      await this.lendingService.settleLending(
        this.bookId,
        this.lending.id,
        this.settleInterestAmount ?? undefined,
        this.settleDate,
        this.settleNotes || undefined
      );
      this.toast.success('Lending marked as settled');
      this.showSettleForm.set(false);
      await this.loadRepayments();
      this.updated.emit();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to settle');
    } finally {
      this.settleLoading.set(false);
    }
  }

  async confirmDeleteLending() {
    if (!this.lending) return;
    this.deleteLendingLoading.set(true);
    try {
      await this.lendingService.deleteLending(this.bookId, this.lending.id);
      this.toast.success('Lending deleted');
      this.showDeleteLendingConfirm.set(false);
      this.closed.emit();
      this.updated.emit();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to delete lending');
    } finally {
      this.deleteLendingLoading.set(false);
    }
  }

  canAddRepayment(): boolean {
    const ld = this.lendingData();
    return !!ld && ld.status !== 'settled' && ld.totalToRecover > 0;
  }

  canSettle(): boolean {
    const ld = this.lendingData();
    return !!ld && ld.status !== 'settled' && ld.outstandingPrincipal === 0;
  }

}
