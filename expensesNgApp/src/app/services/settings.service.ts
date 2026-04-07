import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  constructor(private api: ApiService) {}

  getCategories(bookId?: string) {
    const qs = bookId ? `?expenseBookId=${bookId}` : '';
    return firstValueFrom(this.api.get<ApiResponse<any[]>>(`/settings/categories${qs}`));
  }

  createCategory(bookId: string, category: any) {
    return firstValueFrom(this.api.post<ApiResponse<any>>('/settings/categories', { ...category, expenseBookId: bookId }));
  }

  updateCategory(bookId: string, id: string, category: any) {
    return firstValueFrom(this.api.put<ApiResponse<any>>(`/settings/categories/${id}?expenseBookId=${bookId}`, category));
  }

  deleteCategory(bookId: string, id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/settings/categories/${id}?expenseBookId=${bookId}`));
  }

  importCategories(bookId: string, categories: Array<{ name: string; icon: string; color: string }>) {
    return firstValueFrom(this.api.post<ApiResponse<{ imported: number; failed: number }>>('/settings/categories/import', { categories, expenseBookId: bookId }));
  }

  getPaymentMethods(bookId?: string) {
    const qs = bookId ? `?expenseBookId=${bookId}` : '';
    return firstValueFrom(this.api.get<ApiResponse<any[]>>(`/settings/payment-methods${qs}`));
  }

  createPaymentMethod(bookId: string, method: any) {
    return firstValueFrom(this.api.post<ApiResponse<any>>('/settings/payment-methods', { ...method, expenseBookId: bookId }));
  }

  updatePaymentMethod(bookId: string, id: string, method: any) {
    return firstValueFrom(this.api.put<ApiResponse<any>>(`/settings/payment-methods/${id}`, method));
  }

  deletePaymentMethod(bookId: string, id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/settings/payment-methods/${id}`));
  }

  getSettings(bookId?: string) {
    const qs = bookId ? `?expenseBookId=${bookId}` : '';
    return firstValueFrom(this.api.get<ApiResponse<any>>(`/settings${qs}`));
  }

  updateSettings(bookId: string, settings: any) {
    return firstValueFrom(this.api.put<ApiResponse<any>>('/settings', { ...settings, expenseBookId: bookId }));
  }

  importCSV(bookId: string, formData: FormData) {
    const file = formData.get('file') as File;
    return firstValueFrom(this.api.upload<ApiResponse<any>>(`/import/preview?expenseBookId=${bookId}`, file));
  }

  confirmImport(bookId: string, expenses: any[]) {
    return firstValueFrom(this.api.post<ApiResponse<any>>('/import/confirm', { expenses, expenseBookId: bookId }));
  }
}

