import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';
import {
  Lending,
  Repayment,
  CreateLendingRequest,
  CreateRepaymentRequest,
  LendingRepaymentsResponse,
} from '../models/lending.model';

@Injectable({ providedIn: 'root' })
export class LendingService {
  constructor(private api: ApiService) {}

  async getLendings(bookId: string, status?: string): Promise<Lending[]> {
    const params = status ? `?status=${status}` : '';
    const res = await firstValueFrom(
      this.api.get<ApiResponse<Lending[]>>(`/expensebooks/${bookId}/lendings${params}`)
    );
    return res.data ?? [];
  }

  async createLending(bookId: string, req: CreateLendingRequest): Promise<Lending> {
    const res = await firstValueFrom(
      this.api.post<ApiResponse<Lending>>(`/expensebooks/${bookId}/lendings`, req)
    );
    return res.data!;
  }

  async updateLending(bookId: string, lendingId: string, req: Partial<CreateLendingRequest>): Promise<Lending> {
    const res = await firstValueFrom(
      this.api.put<ApiResponse<Lending>>(`/expensebooks/${bookId}/lendings/${lendingId}`, req)
    );
    return res.data!;
  }

  async deleteLending(bookId: string, lendingId: string): Promise<void> {
    await firstValueFrom(
      this.api.delete<ApiResponse<boolean>>(`/expensebooks/${bookId}/lendings/${lendingId}`)
    );
  }

  async getRepayments(bookId: string, lendingId: string, page = 1, pageSize = 50): Promise<LendingRepaymentsResponse> {
    const res = await firstValueFrom(
      this.api.get<ApiResponse<LendingRepaymentsResponse>>(
        `/expensebooks/${bookId}/lendings/${lendingId}/repayments?page=${page}&pageSize=${pageSize}`
      )
    );
    return res.data!;
  }

  async addRepayment(bookId: string, lendingId: string, req: CreateRepaymentRequest): Promise<Repayment> {
    const res = await firstValueFrom(
      this.api.post<ApiResponse<Repayment>>(
        `/expensebooks/${bookId}/lendings/${lendingId}/repayments`,
        req
      )
    );
    return res.data!;
  }

  async settleLending(bookId: string, lendingId: string, interestCollected?: number, settlementDate?: string, notes?: string): Promise<void> {
    await firstValueFrom(
      this.api.post<ApiResponse<boolean>>(`/expensebooks/${bookId}/lendings/${lendingId}/settle`, {
        interestCollected: interestCollected || null,
        settlementDate: settlementDate || null,
        notes: notes || null,
      })
    );
  }

  async deleteRepayment(bookId: string, lendingId: string, repaymentId: string): Promise<void> {
    await firstValueFrom(
      this.api.delete<ApiResponse<boolean>>(
        `/expensebooks/${bookId}/lendings/${lendingId}/repayments/${repaymentId}`
      )
    );
  }
}
