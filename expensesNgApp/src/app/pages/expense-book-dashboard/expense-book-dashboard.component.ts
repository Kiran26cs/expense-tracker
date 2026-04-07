import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExpenseBookService } from '../../services/expense-book.service';
import { AuthStateService } from '../../services/auth-state.service';
import { ToastService } from '../../services/toast.service';
import { ExpenseBook, CreateExpenseBookRequest } from '../../models/expense-book.model';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent } from '../../components/input/input.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { LoadingComponent, EmptyStateComponent, ErrorStateComponent } from '../../components/loading/loading.component';
import { formatCurrency } from '../../utils/helpers';

@Component({
  selector: 'app-expense-book-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent, ModalComponent, LoadingComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './expense-book-dashboard.component.html',
  styleUrl: './expense-book-dashboard.component.css'
})
export class ExpenseBookDashboardComponent implements OnInit {
  books = signal<ExpenseBook[]>([]);
  loading = signal(true);
  error = signal('');
  showCreateModal = signal(false);
  showDeleteModal = signal(false);
  bookToDelete = signal<ExpenseBook | null>(null);
  createLoading = false;
  deleteLoading = false;
  createError = '';
  createForm: CreateExpenseBookRequest = { name: '', description: '', currency: 'USD', icon: '📖' };

  private bookService = inject(ExpenseBookService);
  private auth = inject(AuthStateService);
  private toast = inject(ToastService);
  private router = inject(Router);

  ngOnInit() { this.loadBooks(); }

  async loadBooks() {
    this.loading.set(true);
    this.error.set('');
    try {
      const res = await this.bookService.getExpenseBooks();
      if (res.success) this.books.set(res.data || []);
      else this.error.set(res.error || 'Failed to load expense books');
    } catch (e: any) { this.error.set(e.message || 'Failed to load'); }
    finally { this.loading.set(false); }
  }

  navigateToBook(book: ExpenseBook) {
    this.router.navigate([`/${book.id}/dashboard`]);
  }

  openCreateModal() { this.createForm = { name: '', description: '', currency: 'USD', icon: '📖' }; this.createError = ''; this.showCreateModal.set(true); }
  closeCreateModal() { this.showCreateModal.set(false); }

  async handleCreate() {
    this.createError = '';
    if (!this.createForm.name.trim()) { this.createError = 'Book name is required'; return; }
    this.createLoading = true;
    try {
      const res = await this.bookService.createExpenseBook(this.createForm);
      if (res.success && res.data) {
        this.books.update(b => [...b, res.data!]);
        this.toast.success('Expense book created');
        this.closeCreateModal();
      } else { this.createError = res.error || 'Failed to create'; }
    } catch (e: any) { this.createError = e.message || 'Failed to create'; }
    finally { this.createLoading = false; }
  }

  openDeleteConfirm(book: ExpenseBook, event: Event) {
    event.stopPropagation();
    this.bookToDelete.set(book);
    this.showDeleteModal.set(true);
  }
  closeDeleteModal() { this.showDeleteModal.set(false); this.bookToDelete.set(null); }

  async handleDelete() {
    const book = this.bookToDelete();
    if (!book) return;
    this.deleteLoading = true;
    try {
      const res = await this.bookService.deleteExpenseBook(book.id);
      if (res.success) {
        this.books.update(b => b.filter(x => x.id !== book.id));
        this.toast.success('Expense book deleted');
        this.closeDeleteModal();
      } else { this.toast.error(res.error || 'Failed to delete'); }
    } catch (e: any) { this.toast.error(e.message || 'Failed to delete'); }
    finally { this.deleteLoading = false; }
  }

  handleLogout() { this.auth.logout(); this.router.navigate(['/login']); }
  protected readonly formatCurrency = formatCurrency;
}
