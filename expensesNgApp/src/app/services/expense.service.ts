import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { Expense, RecurringExpense } from '../models/expense.model';
import { ApiResponse, PaginatedResponse } from '../models/user.model';

export interface ExpensePagedResponse {
  items: Expense[];
  total: number;
  nextCursor: string | null;
  prevCursor: string | null;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface ExpensePagedFilters {
  search?: string;
  type?: string;
  category?: string;
  paymentMethod?: string;
  startDate?: string;
  endDate?: string;
  sortField?: 'date' | 'amount';
  sortDir?: 'asc' | 'desc';
  cursor?: string;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  constructor(private api: ApiService) {}

  getExpenses(bookId: string, filters?: ExpensePagedFilters) {
    const params = new URLSearchParams();
    params.append('expenseBookId', bookId);
    if (filters?.search)        params.append('search', filters.search);
    if (filters?.type)          params.append('type', filters.type);
    if (filters?.category)      params.append('category', filters.category);
    if (filters?.paymentMethod) params.append('paymentMethod', filters.paymentMethod);
    if (filters?.startDate)     params.append('startDate', filters.startDate);
    if (filters?.endDate)       params.append('endDate', filters.endDate);
    if (filters?.sortField)     params.append('sortField', filters.sortField);
    if (filters?.sortDir)       params.append('sortDir', filters.sortDir);
    if (filters?.cursor)        params.append('cursor', filters.cursor);
    if (filters?.pageSize)      params.append('pageSize', String(filters.pageSize));
    return firstValueFrom(this.api.get<ApiResponse<ExpensePagedResponse>>(`/Expenses?${params.toString()}`));
  }

  getExpense(bookId: string, id: string) {
    return firstValueFrom(this.api.get<ApiResponse<Expense>>(`/Expenses/${id}?expenseBookId=${bookId}`));
  }

  createExpense(bookId: string, expense: any) {
    const payload = { ...expense, expenseBookId: bookId };
    return firstValueFrom(this.api.post<ApiResponse<Expense>>('/Expenses', payload));
  }

  updateExpense(bookId: string, id: string, expense: any) {
    return firstValueFrom(this.api.put<ApiResponse<Expense>>(`/Expenses/${id}`, { ...expense, expenseBookId: bookId }));
  }

  deleteExpense(bookId: string, id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/Expenses/${id}?expenseBookId=${bookId}`));
  }

  getRecurringExpenses(bookId: string, filters?: { page?: number; limit?: number }) {
    const params = new URLSearchParams();
    params.append('expenseBookId', bookId);
    if (filters?.page) params.append('page', String(filters.page));
    if (filters?.limit) params.append('limit', String(filters.limit));
    return firstValueFrom(this.api.get<ApiResponse<PaginatedResponse<RecurringExpense>>>(`/Expenses/recurring?${params.toString()}`));
  }

  deleteRecurringExpense(id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/Expenses/recurring/${id}`));
  }
}

