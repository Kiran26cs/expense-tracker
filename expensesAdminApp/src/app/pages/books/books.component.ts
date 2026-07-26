import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';

interface BookSummary {
  id: string; name: string; ownerEmail: string; ownerPlan: string;
  aiChatEnabled: boolean; isTemplate: boolean; expenseCount: number;
  memberCount: number; createdAt: string;
}
interface BookList { books: BookSummary[]; total: number; page: number; pageSize: number; }

@Component({
  selector: 'admin-books',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './books.component.html',
  styleUrl: './books.component.css',
})
export class BooksComponent implements OnInit {
  private api = inject(AdminApiService);

  books    = signal<BookSummary[]>([]);
  total    = signal(0);
  page     = signal(1);
  pageSize = 20;
  search   = signal('');
  aiFilter = signal('');
  loading  = signal(false);

  confirmDeleteId = signal<string | null>(null);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    const params = new URLSearchParams({ page: String(this.page()), pageSize: String(this.pageSize) });
    if (this.search()) params.set('search', this.search());
    if (this.aiFilter() !== '') params.set('aiEnabled', this.aiFilter());

    this.api.get<ApiResponse<BookList>>(`/admin/books?${params}`).subscribe({
      next: r => { this.books.set(r.data?.books ?? []); this.total.set(r.data?.total ?? 0); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onSearch() { this.page.set(1); this.load(); }
  nextPage() { this.page.update(p => p + 1); this.load(); }
  prevPage() { this.page.update(p => Math.max(1, p - 1)); this.load(); }
  get totalPages(): number { return Math.ceil(this.total() / this.pageSize); }

  toggleAiChat(bookId: string, current: boolean) {
    this.api.patch<ApiResponse<BookSummary>>(`/admin/books/${bookId}/ai-chat`, { enabled: !current }).subscribe({
      next: r => {
        const updated = r.data;
        if (updated) this.books.update(list => list.map(b => b.id === bookId ? { ...b, aiChatEnabled: updated.aiChatEnabled } : b));
      },
    });
  }

  confirmDelete(bookId: string) { this.confirmDeleteId.set(bookId); }
  cancelDelete() { this.confirmDeleteId.set(null); }

  deleteBook(bookId: string) {
    this.api.delete<ApiResponse<unknown>>(`/admin/books/${bookId}`).subscribe({
      next: () => { this.confirmDeleteId.set(null); this.books.update(list => list.filter(b => b.id !== bookId)); this.total.update(t => t - 1); },
    });
  }
}
