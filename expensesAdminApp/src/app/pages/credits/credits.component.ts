import { Component, computed, ElementRef, inject, OnDestroy, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';

interface UserSummary {
  id: string; email: string; name: string; plan: string; bookCount: number;
}
interface UserList { users: UserSummary[]; total: number; }

interface BookSummary {
  id: string; name: string; expenseCount: number; memberCount: number;
  aiChatEnabled: boolean; createdAt: string;
}
interface BookList { books: BookSummary[]; total: number; page: number; pageSize: number; }

interface CreditBalance {
  bookId: string; bookName: string; ownerEmail: string; plan: string;
  freeCreditsLeft: number; paidCreditsLeft: number; freeCreditsLimit: number; lastResetDate: string;
}
interface CreditTransaction {
  id: string; amount: number; reason: string; toolsUsed: string[]; timestamp: string;
}
interface TransactionList { transactions: CreditTransaction[]; total: number; }

@Component({
  selector: 'admin-credits',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './credits.component.html',
  styleUrl: './credits.component.css',
})
export class CreditsComponent implements OnDestroy {
  private api = inject(AdminApiService);
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  @ViewChild('panelSearchInput') panelSearchInput?: ElementRef<HTMLInputElement>;

  // User panel
  panelOpen      = signal(false);
  userQuery      = signal('');
  dropdownUsers  = signal<UserSummary[]>([]);
  userSearching  = signal(false);
  selectedUser   = signal<UserSummary | null>(null);

  // Books list
  books          = signal<BookSummary[]>([]);
  booksTotal     = signal(0);
  booksPage      = signal(1);
  readonly PAGE_SIZE = 50;
  booksLoading   = signal(false);
  totalPages     = computed(() => Math.ceil(this.booksTotal() / this.PAGE_SIZE));

  // Modal
  modalBook      = signal<BookSummary | null>(null);
  balance        = signal<CreditBalance | null>(null);
  transactions   = signal<CreditTransaction[]>([]);
  balanceLoading = signal(false);
  grantAmount    = signal(0);
  grantNote      = signal('');
  granting       = signal(false);
  grantSuccess   = signal('');

  ngOnDestroy() {
    if (this.searchTimer) clearTimeout(this.searchTimer);
  }

  openPanel() {
    this.panelOpen.set(true);
    this.userQuery.set('');
    this.fetchUsers('');
    setTimeout(() => this.panelSearchInput?.nativeElement.focus(), 30);
  }

  closePanel() {
    this.panelOpen.set(false);
  }

  onQueryInput(val: string) {
    this.userQuery.set(val);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.fetchUsers(val), 300);
  }

  fetchUsers(query: string) {
    this.userSearching.set(true);
    const q = query.trim();
    const url = q
      ? `/admin/users?search=${encodeURIComponent(q)}&pageSize=10`
      : `/admin/users?pageSize=10`;
    this.api.get<ApiResponse<UserList>>(url).subscribe({
      next: r => {
        this.dropdownUsers.set(r.data?.users ?? []);
        this.userSearching.set(false);
      },
      error: () => {
        this.dropdownUsers.set([]);
        this.userSearching.set(false);
      },
    });
  }

  selectUser(user: UserSummary) {
    this.selectedUser.set(user);
    this.panelOpen.set(false);
    this.books.set([]);
    this.booksTotal.set(0);
    this.booksPage.set(1);
    this.closeModal();
    this.loadBooks(1);
  }

  clearUser() {
    this.selectedUser.set(null);
    this.books.set([]);
    this.booksTotal.set(0);
    this.closeModal();
  }

  loadBooks(page: number) {
    const user = this.selectedUser();
    if (!user) return;
    this.booksLoading.set(true);
    this.booksPage.set(page);
    this.api.get<ApiResponse<BookList>>(
      `/admin/books?userId=${user.id}&page=${page}&pageSize=${this.PAGE_SIZE}`
    ).subscribe({
      next: r => {
        this.books.set(r.data?.books ?? []);
        this.booksTotal.set(r.data?.total ?? 0);
        this.booksLoading.set(false);
      },
      error: () => this.booksLoading.set(false),
    });
  }

  openModal(book: BookSummary) {
    this.modalBook.set(book);
    this.balance.set(null);
    this.transactions.set([]);
    this.grantSuccess.set('');
    this.grantAmount.set(0);
    this.grantNote.set('');
    this.balanceLoading.set(true);
    this.api.get<ApiResponse<CreditBalance>>(`/admin/credits/${book.id}`).subscribe({
      next: r => {
        this.balance.set(r.data ?? null);
        this.balanceLoading.set(false);
        this.loadTransactions(book.id);
      },
      error: () => this.balanceLoading.set(false),
    });
  }

  closeModal() {
    this.modalBook.set(null);
    this.balance.set(null);
    this.transactions.set([]);
    this.grantSuccess.set('');
  }

  loadTransactions(bookId: string) {
    this.api.get<ApiResponse<TransactionList>>(`/admin/credits/${bookId}/transactions`).subscribe({
      next: r => this.transactions.set(r.data?.transactions ?? []),
    });
  }

  grant() {
    const book = this.modalBook();
    if (!book || this.grantAmount() <= 0) return;
    this.granting.set(true);
    this.grantSuccess.set('');
    this.api.post<ApiResponse<CreditBalance>>(`/admin/credits/${book.id}/grant`, {
      amount: this.grantAmount(), note: this.grantNote(),
    }).subscribe({
      next: r => {
        this.balance.set(r.data ?? null);
        this.grantSuccess.set(`Successfully granted ${this.grantAmount()} credits.`);
        this.grantAmount.set(0);
        this.grantNote.set('');
        this.granting.set(false);
        this.loadTransactions(book.id);
      },
      error: () => this.granting.set(false),
    });
  }
}
