import { Injectable, signal, computed } from '@angular/core';
import { ExpenseBook } from '../models/expense-book.model';

@Injectable({ providedIn: 'root' })
export class CurrentBookService {
  private bookSignal = signal<ExpenseBook | null>(null);

  book = this.bookSignal.asReadonly();
  currency = computed(() => this.bookSignal()?.currency ?? 'USD');

  setBook(book: ExpenseBook | null) {
    this.bookSignal.set(book);
  }
}
