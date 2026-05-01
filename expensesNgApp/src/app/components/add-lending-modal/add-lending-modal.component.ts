import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LendingService } from '../../services/lending.service';
import { ToastService } from '../../services/toast.service';
import { Lending, CreateLendingRequest } from '../../models/lending.model';
import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'app-add-lending-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './add-lending-modal.component.html',
  styleUrl: './add-lending-modal.component.css'
})
export class AddLendingModalComponent {
  @Input() bookId = '';
  @Input() isOpen = false;
  @Output() closed = new EventEmitter<void>();
  @Output() created = new EventEmitter<Lending>();

  loading = signal(false);
  error = signal('');

  borrowerName = '';
  borrowerContact = '';
  principalAmount: number | null = null;
  annualInterestRate: number = 0;
  startDate = new Date().toISOString().split('T')[0];
  dueDate = '';
  notes = '';

  constructor(
    private lendingService: LendingService,
    private toast: ToastService
  ) {}

  reset() {
    this.borrowerName = '';
    this.borrowerContact = '';
    this.principalAmount = null;
    this.annualInterestRate = 0;
    this.startDate = new Date().toISOString().split('T')[0];
    this.dueDate = '';
    this.notes = '';
    this.error.set('');
  }

  onClose() {
    this.reset();
    this.closed.emit();
  }

  async submit() {
    if (!this.borrowerName.trim()) { this.error.set('Borrower name is required'); return; }
    if (!this.principalAmount || this.principalAmount <= 0) { this.error.set('Principal amount must be positive'); return; }

    this.loading.set(true);
    this.error.set('');
    try {
      const req: CreateLendingRequest = {
        borrowerName: this.borrowerName.trim(),
        borrowerContact: this.borrowerContact.trim() || undefined,
        principalAmount: this.principalAmount,
        annualInterestRate: this.annualInterestRate || 0,
        startDate: this.startDate,
        dueDate: this.dueDate || undefined,
        notes: this.notes.trim() || undefined,
      };
      const lending = await this.lendingService.createLending(this.bookId, req);
      this.toast.success('Lending created');
      this.created.emit(lending);
      this.reset();
    } catch (e: any) {
      this.error.set(e.message || 'Failed to create lending');
    } finally {
      this.loading.set(false);
    }
  }
}
