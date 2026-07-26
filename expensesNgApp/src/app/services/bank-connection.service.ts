import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';
import {
  BankConnectionDto,
  BankStatementPreviewDto,
  BankSyncConfirmResultDto,
  ConfirmBankSyncRequest,
  CreateBankConnectionRequest,
  UpdateBankConnectionRequest,
} from '../models/bank-connection.model';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class BankConnectionService {
  private base = environment.apiBaseUrl;

  constructor(private api: ApiService, private http: HttpClient) {}

  getConnections() {
    return firstValueFrom(
      this.api.get<ApiResponse<BankConnectionDto[]>>('/bank-connections')
    );
  }

  createConnection(request: CreateBankConnectionRequest) {
    return firstValueFrom(
      this.api.post<ApiResponse<BankConnectionDto>>('/bank-connections', request)
    );
  }

  updateConnection(id: string, request: UpdateBankConnectionRequest) {
    return firstValueFrom(
      this.api.patch<ApiResponse<BankConnectionDto>>(`/bank-connections/${id}`, request)
    );
  }

  deleteConnection(id: string) {
    return firstValueFrom(
      this.api.delete<ApiResponse<null>>(`/bank-connections/${id}`)
    );
  }

  parseStatement(connectionId: string, file: File, password?: string) {
    const form = new FormData();
    form.append('file', file);
    if (password) form.append('password', password);
    return firstValueFrom(
      this.http.post<ApiResponse<BankStatementPreviewDto>>(
        `${this.base}/bank-sync/${connectionId}/parse`, form
      )
    );
  }

  confirmSync(sessionId: string, request: ConfirmBankSyncRequest) {
    return firstValueFrom(
      this.api.post<ApiResponse<BankSyncConfirmResultDto>>(
        `/bank-sync/sessions/${sessionId}/confirm`, request
      )
    );
  }
}
