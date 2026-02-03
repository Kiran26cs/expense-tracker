// Dashboard API endpoints
import { apiService } from './api.service';
import type { DashboardSummary, ApiResponse } from '@/types';

export const dashboardApi = {
  // Get dashboard summary
  getSummary: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<DashboardSummary>>(`/Dashboard/summary${queryString}`);
  },
};
