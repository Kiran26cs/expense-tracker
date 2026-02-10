import { apiService } from './api.service';
import type { ExpenseBook, CreateExpenseBookRequest, UpdateExpenseBookRequest } from '../types/expenseBook';
import type { ApiResponse } from '../types';

export const expenseBookService = {
  // Get all expense books for the current user
  async getExpenseBooks(): Promise<ApiResponse<ExpenseBook[]>> {
    return apiService.get<ApiResponse<ExpenseBook[]>>('/expensebooks');
  },

  // Get default expense book
  async getDefaultExpenseBook(): Promise<ApiResponse<ExpenseBook | null>> {
    try {
      return await apiService.get<ApiResponse<ExpenseBook>>('/expensebooks/default');
    } catch (error) {
      return { success: false, error: 'No default book found' } as ApiResponse<ExpenseBook | null>;
    }
  },

  // Get expense book by ID
  async getExpenseBookById(id: string): Promise<ApiResponse<ExpenseBook>> {
    return apiService.get<ApiResponse<ExpenseBook>>(`/expensebooks/${id}`);
  },

  // Create new expense book
  async createExpenseBook(data: CreateExpenseBookRequest): Promise<ApiResponse<ExpenseBook>> {
    return apiService.post<ApiResponse<ExpenseBook>>('/expensebooks', data);
  },

  // Update expense book
  async updateExpenseBook(id: string, data: UpdateExpenseBookRequest): Promise<ApiResponse<ExpenseBook>> {
    return apiService.put<ApiResponse<ExpenseBook>>(`/expensebooks/${id}`, data);
  },

  // Delete expense book
  async deleteExpenseBook(id: string): Promise<ApiResponse<void>> {
    return apiService.delete<ApiResponse<void>>(`/expensebooks/${id}`);
  },

  // Get all categories
  async getCategories(): Promise<ApiResponse<string[]>> {
    return apiService.get<ApiResponse<string[]>>('/expensebooks/categories');
  },

  // Refresh expense book statistics
  async refreshStats(id: string): Promise<ApiResponse<ExpenseBook>> {
    return apiService.post<ApiResponse<ExpenseBook>>(`/expensebooks/${id}/refresh-stats`);
  },
};
