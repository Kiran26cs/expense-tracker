import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChildren, QueryList, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ExpenseBookService } from '../../services/expense-book.service';
import { ImportService } from '../../services/import.service';
import { MemberService } from '../../services/member.service';
import { AuthStateService } from '../../services/auth-state.service';
import { ToastService } from '../../services/toast.service';
import { CurrentBookService } from '../../services/current-book.service';
import { ExpenseBook, CreateExpenseBookRequest } from '../../models/expense-book.model';
import { PendingInvite } from '../../models/member.model';
import { TopbarComponent } from '../../components/topbar/topbar.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent } from '../../components/input/input.component';
import { SelectComponent } from '../../components/input/select.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { LoadingComponent, EmptyStateComponent, ErrorStateComponent } from '../../components/loading/loading.component';
import { TruncateDirective } from '../../directives/truncate.directive';
import { formatCurrency, CURRENCY_OPTIONS, BOOK_ICON_OPTIONS } from '../../utils/helpers';
import { UpgradeModalService } from '../../services/upgrade-modal.service';

const LOCALE_CURRENCY: Record<string, string> = {
  IN: 'INR', US: 'USD', GB: 'GBP', AU: 'AUD', CA: 'CAD',
  SG: 'SGD', AE: 'AED', JP: 'JPY', CN: 'CNY', KR: 'KRW',
  DE: 'EUR', FR: 'EUR', IT: 'EUR', ES: 'EUR', NL: 'EUR',
  CH: 'CHF', SE: 'SEK', NO: 'NOK', DK: 'DKK', NZ: 'NZD',
  ZA: 'ZAR', HK: 'HKD', MX: 'MXN', BR: 'BRL'
};

@Component({
  selector: 'app-expense-book-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, TopbarComponent, ButtonComponent, InputComponent, SelectComponent, ModalComponent, LoadingComponent, EmptyStateComponent, ErrorStateComponent, TruncateDirective],
  templateUrl: './expense-book-dashboard.component.html',
  styleUrl: './expense-book-dashboard.component.css'
})
export class ExpenseBookDashboardComponent implements OnInit, OnDestroy {
  @ViewChildren('bookNameInput') bookNameInputs!: QueryList<ElementRef<HTMLInputElement>>;
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
  iconSelection   = signal('fa fa-book');
  showCustomIcon  = signal(false);

  // Template creation state
  templateLoading = signal(false);
  templateStatus  = signal<'idle' | 'processing' | 'done' | 'error'>('idle');

  readonly hasContent     = computed(() => this.books().length > 0 || this.pendingInvites().length > 0);
  readonly hasTemplate    = computed(() => this.books().some(b => b.isTemplate));
  // Hide the button once processing started OR a template book already exists
  readonly showTemplatBtn = computed(() =>
    !this.hasTemplate() &&
    this.templateStatus() !== 'processing' &&
    this.templateStatus() !== 'done'
  );

  // Inline title editing
  editingBookId = signal<string | null>(null);
  editingName   = '';

  readonly currencyOptions = CURRENCY_OPTIONS;
  readonly iconOptions     = BOOK_ICON_OPTIONS;

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  private bookService   = inject(ExpenseBookService);
  private importService = inject(ImportService);
  private memberService = inject(MemberService);
  auth                  = inject(AuthStateService);
  upgradeModal          = inject(UpgradeModalService);
  private toast         = inject(ToastService);
  private router        = inject(Router);
  private currentBook   = inject(CurrentBookService);

  get userPlan(): string { return this.auth.user()?.plan ?? 'Free'; }

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
    this.currentBook.setBook(book);
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

  onIconSelect(val: string) {
    if (val === '__other__') {
      this.showCustomIcon.set(true);
      this.createForm.icon = '';
    } else {
      this.showCustomIcon.set(false);
      this.createForm.icon = val;
      this.iconSelection.set(val);
    }
  }

  openCreateModal() {
    const defaultCurrency = this.auth.user()?.currency || 'USD';
    this.createForm = { name: '', description: '', currency: defaultCurrency, icon: 'fa fa-book' };
    this.createError = '';
    this.iconSelection.set('fa fa-book');
    this.showCustomIcon.set(false);
    this.showCreateModal.set(true);
  }
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

  // ── Template book ─────────────────────────────────────────────────────────

  async createFromTemplate() {
    if (this.templateLoading()) return;
    this.templateLoading.set(true);
    this.templateStatus.set('processing');

    const currency = this.detectCurrency();

    try {
      const res = await this.bookService.createFromTemplate(currency);
      if (!res.success || !res.data) {
        this.toast.error(res.error ?? 'Failed to start demo book creation');
        this.templateStatus.set('error');
        return;
      }
      const { bookId, sessionId } = res.data;
      this.startTemplatePolling(bookId, sessionId);
    } catch (e: any) {
      const msg = e?.error?.error ?? e?.message ?? 'Failed to create demo book';
      this.toast.error(msg);
      this.templateStatus.set('error');
    } finally {
      this.templateLoading.set(false);
    }
  }

  private startTemplatePolling(bookId: string, sessionId: string) {
    this.stopTemplatePolling();
    this.pollTimer = setInterval(async () => {
      try {
        const res = await this.importService.pollImportSession(bookId, sessionId);
        if (!res.success || !res.data) return;

        const status = res.data.status;
        if (status === 'completed' || status === 'completedWithErrors') {
          this.stopTemplatePolling();
          this.templateStatus.set('done');
          await this.loadAll();
          this.toast.success('Demo Expense Book is ready! Click to explore.');
        } else if (status === 'failed') {
          this.stopTemplatePolling();
          this.templateStatus.set('error');
          await this.loadAll();
          this.toast.error('Demo book creation encountered errors.');
        }
      } catch { /* ignore transient errors, keep polling */ }
    }, 5000);
  }

  private stopTemplatePolling() {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private detectCurrency(): string {
    const lang   = navigator.language ?? 'en-US';
    const region = lang.split('-')[1]?.toUpperCase() ?? '';
    return LOCALE_CURRENCY[region] ?? 'USD';
  }

  // ── Inline book title editing ─────────────────────────────────────────────

  startEditBookName(book: ExpenseBook, event: Event) {
    event.stopPropagation();
    this.editingBookId.set(book.id);
    this.editingName = book.name;
    setTimeout(() => this.bookNameInputs.first?.nativeElement.focus(), 0);
  }

  async saveBookName(book: ExpenseBook) {
    const name = this.editingName.trim();
    if (!name || name === book.name) { this.cancelEditBookName(); return; }
    try {
      const res = await this.bookService.updateExpenseBook(book.id, { name });
      if (res.success && res.data) {
        this.books.update(list => list.map(b => b.id === book.id ? { ...b, name: res.data!.name } : b));
        this.toast.success('Book name updated');
      } else {
        this.toast.error(res.error || 'Failed to rename book');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to rename book');
    } finally {
      this.cancelEditBookName();
    }
  }

  cancelEditBookName() { this.editingBookId.set(null); this.editingName = ''; }


  onEditKeydown(event: KeyboardEvent, book: ExpenseBook) {
    if (event.key === 'Enter')  { event.preventDefault(); this.saveBookName(book); }
    if (event.key === 'Escape') { this.cancelEditBookName(); }
  }

  ngOnDestroy() { this.stopTemplatePolling(); }

  protected readonly formatCurrency = formatCurrency;
}
