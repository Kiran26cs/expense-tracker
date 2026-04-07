import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { Budget } from '../models/budget.model';
import { ApiResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class BudgetService {
  constructor(private api: ApiService) {}

  getBudgets(bookId?: string, month?: string) {
    const params = new URLSearchParams();
    if (bookId) params.append('expenseBookId', bookId);
    if (month) params.append('month', month);
    const qs = params.toString();
    return firstValueFrom(this.api.get<ApiResponse<Budget[]>>(`/budgets${qs ? `?${qs}` : ''}`));
  }

  upsertVersion(bookId: string, category: string, amount: number, effectiveDate: string, effectivePeriod: string) {
    return firstValueFrom(this.api.post<ApiResponse<Budget>>('/budgets/upsert-version', {
      expenseBookId: bookId, category, amount, effectiveDate, effectivePeriod
    }));
  }

  deleteBudget(bookId: string, id: string) {
    return firstValueFrom(this.api.delete<ApiResponse<void>>(`/budgets/${id}?expenseBookId=${bookId}`));
  }
}

