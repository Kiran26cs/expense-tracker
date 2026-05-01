import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExpenseBookService } from '../../services/expense-book.service';
import { MemberService } from '../../services/member.service';
import { AuthStateService } from '../../services/auth-state.service';
import { ToastService } from '../../services/toast.service';
import { ExpenseBook, CreateExpenseBookRequest } from '../../models/expense-book.model';
import { PendingInvite } from '../../models/member.model';
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
  books          = signal<ExpenseBook[]>([]);
  pendingInvites = signal<PendingInvite[]>([]);
  loading        = signal(true);
  error          = signal('');

  showCreateModal = signal(false);
  showDeleteModal = signal(false);
  bookToDelete    = signal<ExpenseBook | null>(null);
  createLoading   = false;
  deleteLoading   = false;
  createError     = '';
  createForm: CreateExpenseBookRequest = { name: '', description: '', currency: 'USD', icon: 'fa fa-book' };

  acceptingToken  = signal<string | null>(null);
  decliningToken  = signal<string | null>(null);

  readonly hasContent = computed(() => this.books().length > 0 || this.pendingInvites().length > 0);

  private bookService   = inject(ExpenseBookService);
  private memberService = inject(MemberService);
  private auth          = inject(AuthStateService);
  private toast         = inject(ToastService);
  private router        = inject(Router);

  ngOnInit() { this.loadAll(); }

  async loadAll() {
    this.loading.set(true);
    this.error.set('');
    try {
      const [booksRes, pendingRes] = await Promise.all([
        this.bookService.getExpenseBooks(),
        this.memberService.getPendingInvites(),
      ]);
      if (booksRes.success)   this.books.set(booksRes.data || []);
      else this.error.set(booksRes.error || 'Failed to load expense books');
      if (pendingRes.success) this.pendingInvites.set(pendingRes.data || []);
    } catch (e: any) {
      this.error.set(e.message || 'Failed to load');
    } finally {
      this.loading.set(false);
    }
  }

  navigateToBook(book: ExpenseBook) {
    this.router.navigate([`/${book.id}/dashboard`]);
  }

  isShared(book: ExpenseBook): boolean {
    return book.memberRole != null;
  }

  roleLabel(book: ExpenseBook): string {
    return book.memberRole ?? 'owner';
  }

  // ── Pending invite actions ────────────────────────────────────────────────

  async acceptPendingInvite(invite: PendingInvite) {
    this.acceptingToken.set(invite.inviteToken);
    try {
      const res = await this.memberService.acceptInvite(invite.inviteToken);
      if (res.success && res.data) {
        this.pendingInvites.update(list => list.filter(i => i.inviteToken !== invite.inviteToken));
        this.router.navigate(['/', res.data.expenseBookId, 'dashboard']);
      } else {
        this.toast.error(res.error || 'Failed to accept invite.');
      }
    } catch (e: any) {
      const msg = e?.error?.error || e?.error?.message || e?.message || 'Failed to accept invite.';
      this.toast.error(msg);
    } finally {
      this.acceptingToken.set(null);
    }
  }

  async declinePendingInvite(invite: PendingInvite) {
    this.decliningToken.set(invite.inviteToken);
    try {
      await this.memberService.declineInvite(invite.inviteToken);
      this.pendingInvites.update(list => list.filter(i => i.inviteToken !== invite.inviteToken));
    } catch (e: any) {
      const msg = e?.error?.error || e?.error?.message || e?.message || 'Failed to decline invite.';
      this.toast.error(msg);
    } finally {
      this.decliningToken.set(null);
    }
  }

  // ── Create / Delete ───────────────────────────────────────────────────────

  openCreateModal()  { this.createForm = { name: '', description: '', currency: 'USD', icon: 'fa fa-book' }; this.createError = ''; this.showCreateModal.set(true); }
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
