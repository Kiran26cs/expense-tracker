// Expense API endpoints
import { apiService } from './api.service';
import type { Expense, FilterOptions, PaginatedResponse, ApiResponse, RecurringExpense } from '@/types';

export const expenseApi = {
  // Get all expenses with filters (backend doesn't support pagination yet)
  getExpenses: (filters?: { startDate?: string; endDate?: string; category?: string; searchQuery?: string }) => {
    const params = new URLSearchParams();

    if (filters?.startDate) {
      params.append('startDate', filters.startDate);
    }
    if (filters?.endDate) {
      params.append('endDate', filters.endDate);
    }
    if (filters?.category && filters.category !== 'all') {
      params.append('category', filters.category);
    }
    // Note: searchQuery not supported by backend yet

    const queryString = params.toString();
    return apiService.get<ApiResponse<Expense[]>>(
      `/Expenses${queryString ? `?${queryString}` : ''}`
    );
  },

  // Get single expense
  getExpense: (id: string) => {
    return apiService.get<ApiResponse<Expense>>(`/Expenses/${id}`);
  },

  // Create expense
  createExpense: (expense: any) => {
    // Map frontend expense format to backend CreateExpenseRequest format
    const payload = {
      amount: expense.amount,
      date: expense.date,
      category: typeof expense.category === 'object' ? expense.category.id : expense.category,
      paymentMethod: typeof expense.paymentMethod === 'object' ? expense.paymentMethod.id : expense.paymentMethod,
      description: expense.description,
      notes: expense.notes,
      isRecurring: expense.isRecurring || false,
      recurringConfig: expense.recurringConfig || null,
    };
    return apiService.post<ApiResponse<Expense>>('/Expenses', payload);
  },

  // Update expense
  updateExpense: (id: string, expense: any) => {
    // Map frontend expense format to backend UpdateExpenseRequest format
    const payload = {
      amount: expense.amount,
      date: expense.date,
      category: typeof expense.category === 'object' ? expense.category.id : expense.category,
      paymentMethod: typeof expense.paymentMethod === 'object' ? expense.paymentMethod.id : expense.paymentMethod,
      description: expense.description,
      notes: expense.notes,
    };
    return apiService.put<ApiResponse<Expense>>(`/Expenses/${id}`, payload);
  },

  // Delete expense
  deleteExpense: (id: string) => {
    return apiService.delete<ApiResponse<void>>(`/Expenses/${id}`);
  },

  // Get recurring expenses with optional filters
  getRecurringExpenses: (filters?: { startDate?: string; endDate?: string }) => {
    const params = new URLSearchParams();
    
    if (filters?.startDate) {
      params.append('startDate', filters.startDate);
    }
    if (filters?.endDate) {
      params.append('endDate', filters.endDate);
    }

    const queryString = params.toString();
    return apiService.get<ApiResponse<RecurringExpense[]>>(
      `/Expenses/recurring${queryString ? `?${queryString}` : ''}`
    );
  },

  // Mark a recurring expense as paid
  markRecurringAsPaid: (recurringExpenseId: string, paidDate: string) => {
    return apiService.post<ApiResponse<Expense>>(`/Expenses/recurring/${recurringExpenseId}/mark-paid`, {
      paidDate: new Date(paidDate).toISOString(),
    });
  },

};
