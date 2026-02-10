// Dashboard API endpoints
import { apiService } from './api.service';
import type { DashboardSummary, ApiResponse, UpcomingPaymentsPaginatedResponse, UpcomingPayment } from '@/types';

export interface DailyTransactionGroup {
  date: string;
  dateLabel: string;
  categorySpending: Array<{
    category: string;
    amount: number;
    count: number;
  }>;
  totalSpent: number;
}

export const dashboardApi = {
  // Get dashboard summary
  getSummary: (bookId?: string, startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<DashboardSummary>>(`/Dashboard/summary${queryString}`);
  },

  // Get grouped transactions by date
  getGroupedTransactions: (bookId?: string, startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<DailyTransactionGroup[]>>(`/Dashboard/grouped-transactions${queryString}`);
  },

  // Migrate daily summaries (one-time migration)
  migrateDailySummaries: () => {
    return apiService.post<ApiResponse<string>>('/Dashboard/migrate-daily-summaries', {});
  },

  // Get upcoming payments with pagination
  getUpcomingPayments: (bookId?: string, page: number = 1, pageSize: number = 10) => {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    return apiService.get<ApiResponse<UpcomingPaymentsPaginatedResponse>>(`/Dashboard/upcoming-payments?${params.toString()}`);
  },

  // Mark an upcoming payment as paid
  markUpcomingPaymentAsPaid: (upcomingPaymentId: string, paidDate: string, recordAsExpense: boolean = true) => {
    return apiService.post<ApiResponse<UpcomingPayment>>(`/Dashboard/upcoming-payments/${upcomingPaymentId}/mark-paid`, {
      paidDate: new Date(paidDate).toISOString(),
      recordAsExpense,
    });
  },

  // Trigger generation of upcoming payments
  generateUpcomingPayments: () => {
    return apiService.post<ApiResponse<string>>('/Dashboard/generate-upcoming-payments', {});
  },
};
