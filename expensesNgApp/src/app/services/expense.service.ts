import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { Expense, RecurringExpense } from '../models/expense.model';
import { ApiResponse, PaginatedResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  constructor(private api: ApiService) {}

  getExpenses(bookId: string, filters?: { startDate?: string; endDate?: string; category?: string; type?: string; paymentMethod?: string; search?: string; page?: number; limit?: number }) {
    const params = new URLSearchParams();
    params.append('expenseBookId', bookId);
    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.category) params.append('category', filters.category);
    if (filters?.type) params.append('type', filters.type);
    if (filters?.paymentMethod) params.append('paymentMethod', filters.paymentMethod);
    if (filters?.search) params.append('search', filters.search);
    if (filters?.page) params.append('page', String(filters.page));
    if (filters?.limit) params.append('limit', String(filters.limit));
    return firstValueFrom(this.api.get<ApiResponse<PaginatedResponse<Expense>>>(`/Expenses?${params.toString()}`));
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

