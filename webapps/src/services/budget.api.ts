// Budget API endpoints
import { apiService } from './api.service';
import type { Budget, ApiResponse } from '@/types';

export const budgetApi = {
  // Get all budgets for a month
  getBudgets: (month?: string) => {
    const params = month ? `?month=${month}` : '';
    return apiService.get<ApiResponse<Budget[]>>(`/budgets${params}`);
  },

  // Get single budget
  getBudget: (id: string) => {
    return apiService.get<ApiResponse<Budget>>(`/budgets/${id}`);
  },

  // Create budget
  upsertBudget: (budget: any) => {
    return apiService.post<ApiResponse<Budget>>('/budgets', budget);
  },

  // Update budget
  updateBudget: (id: string, budget: any) => {
    return apiService.put<ApiResponse<Budget>>(`/budgets/${id}`, budget);
  },

  // Delete budget
  deleteBudget: (id: string) => {
    return apiService.delete<ApiResponse<void>>(`/budgets/${id}`);
  },
};
