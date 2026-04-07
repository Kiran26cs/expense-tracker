import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { DashboardSummary, DailyTransactionGroup, UpcomingPayment } from '../models/dashboard.model';
import { ApiResponse, PaginatedResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private api: ApiService) {}

  getSummary(bookId?: string, startDate?: string, endDate?: string) {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const qs = params.toString();
    return firstValueFrom(this.api.get<ApiResponse<DashboardSummary>>(`/Dashboard/summary${qs ? `?${qs}` : ''}`));
  }

  getGroupedTransactions(bookId?: string, startDate?: string, endDate?: string, page = 1, limit = 30) {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    params.append('page', page.toString());
    params.append('limit', limit.toString());
    return firstValueFrom(this.api.get<ApiResponse<DailyTransactionGroup[]>>(`/Dashboard/grouped-transactions?${params}`));
  }

  getUpcomingPayments(bookId?: string, page = 1, pageSize = 10) {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    return firstValueFrom(this.api.get<ApiResponse<PaginatedResponse<UpcomingPayment>>>(`/Dashboard/upcoming-payments?${params}`));
  }

  markUpcomingPaymentAsPaid(bookId: string, id: string, recordAsExpense = true) {
    const paidDate = new Date().toISOString();
    return firstValueFrom(this.api.post<ApiResponse<UpcomingPayment>>(`/Dashboard/upcoming-payments/${id}/mark-paid`, {
      paidDate, recordAsExpense, expenseBookId: bookId,
    }));
  }

  generateUpcomingPayments(bookId?: string) {
    return firstValueFrom(this.api.post<ApiResponse<string>>('/Dashboard/generate-upcoming-payments', { expenseBookId: bookId }));
  }
}

