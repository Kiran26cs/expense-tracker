// Settings and import API endpoints
import { apiService } from './api.service';
import type { Settings, ImportPreview, Category, PaymentMethod, ApiResponse } from '@/types';

interface ImportCategoriesResponse {
  imported: number;
  failed: number;
  errors: string[];
}

export const settingsApi = {
  // Get all settings
  getSettings: () => {
    return apiService.get<ApiResponse<Settings>>('/settings');
  },

  // Update settings
  updateSettings: (settings: Partial<Settings>) => {
    return apiService.put<ApiResponse<Settings>>('/settings', settings);
  },

  // Get categories
  getCategories: () => {
    return apiService.get<ApiResponse<Category[]>>('/settings/categories');
  },

  // Create category
  createCategory: (category: Omit<Category, 'id'>) => {
    return apiService.post<ApiResponse<Category>>('/settings/categories', category);
  },

  // Update category
  updateCategory: (id: string, category: Partial<Category>) => {
    return apiService.put<ApiResponse<Category>>(`/settings/categories/${id}`, category);
  },

  // Delete category
  deleteCategory: (id: string) => {
    return apiService.delete<ApiResponse<void>>(`/settings/categories/${id}`);
  },

  // Import categories (bulk)
  importCategories: (categories: Array<Omit<Category, 'id'>>) => {
    return apiService.post<ApiResponse<ImportCategoriesResponse>>('/settings/categories/import', { categories });
  },

  // Get payment methods
  getPaymentMethods: () => {
    return apiService.get<ApiResponse<PaymentMethod[]>>('/settings/payment-methods');
  },

  // Import expenses from CSV
  importCSV: (file: File) => {
    return apiService.upload<ApiResponse<ImportPreview>>('/import/preview', file);
  },

  // Confirm import after preview
  confirmImport: (expenses: any[]) => {
    return apiService.post<ApiResponse<{ imported: number; failed: number }>>(
      '/import/confirm',
      { expenses }
    );
  },
};
