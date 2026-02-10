// Budget API endpoints
import { apiService } from './api.service';
import type { Budget, ApiResponse } from '@/types';

export const budgetApi = {
  // Get all budgets for a month
  getBudgets: (expenseBookId?: string, month?: string) => {
    const params = new URLSearchParams();
    if (expenseBookId) params.append('expenseBookId', expenseBookId);
    if (month) params.append('month', month);
    const queryString = params.toString() ? `?${params.toString()}` : '';
    return apiService.get<ApiResponse<Budget[]>>(`/budgets${queryString}`);
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
