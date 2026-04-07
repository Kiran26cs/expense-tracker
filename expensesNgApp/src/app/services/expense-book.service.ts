import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ExpenseBook, CreateExpenseBookRequest, UpdateExpenseBookRequest } from '../models/expense-book.model';
import { ApiResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class ExpenseBookService {
  constructor(private api: ApiService) {}

  getExpenseBooks() {
    return firstValueFrom(this.api.get<ApiResponse<ExpenseBook[]>>('/expensebooks'));
  }

  getExpenseBookById(id: string) {
    return firstValueFrom(this.api.get<ApiResponse<ExpenseBook>>(`/expensebooks/${id}`));
  }

  createExpenseBook(data: CreateExpenseBookRequest) {
    return firstValueFrom(this.api.post<ApiResponse<ExpenseBook>>('/expensebooks', data));
  }

  updateExpenseBook(id: string, data: UpdateExpenseBookRequest) {
    return firstValueFrom(this.api.put<ApiResponse<ExpenseBook>>(`/expensebooks/${id}`, data));
  }

  deleteExpenseBook(id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/expensebooks/${id}`));
  }

  getCategories() {
    return firstValueFrom(this.api.get<ApiResponse<string[]>>('/expensebooks/categories'));
  }
}
