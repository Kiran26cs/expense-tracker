// Dashboard API endpoints
import { apiService } from './api.service';
import type { DashboardSummary, ApiResponse } from '@/types';

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
  getSummary: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<DashboardSummary>>(`/Dashboard/summary${queryString}`);
  },

  // Get grouped transactions by date
  getGroupedTransactions: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<DailyTransactionGroup[]>>(`/Dashboard/grouped-transactions${queryString}`);
  },

  // Migrate daily summaries (one-time migration)
  migrateDailySummaries: () => {
    return apiService.post<ApiResponse<string>>('/Dashboard/migrate-daily-summaries', {});
  },
};
