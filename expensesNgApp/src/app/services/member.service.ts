import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';
import {
  ExpenseBookMember,
  InviteMemberRequest,
  InviteMemberResponse,
  UpdateMemberRequest,
  AcceptInviteResponse,
  ResolvedPermissions,
  PendingInvite,
} from '../models/member.model';
import { Category } from '../models/expense.model';

@Injectable({ providedIn: 'root' })
export class MemberService {
  constructor(private api: ApiService) {}

  getMembers(bookId: string) {
    return firstValueFrom(
      this.api.get<ApiResponse<ExpenseBookMember[]>>(`/expensebooks/${bookId}/members`)
    );
  }

  getMyPermissions(bookId: string) {
    return firstValueFrom(
      this.api.get<ApiResponse<ResolvedPermissions>>(`/expensebooks/${bookId}/members/me`)
    );
  }

  inviteMember(bookId: string, request: InviteMemberRequest) {
    return firstValueFrom(
      this.api.post<ApiResponse<InviteMemberResponse>>(
        `/expensebooks/${bookId}/members/invite`,
        request
      )
    );
  }

  updateMember(bookId: string, memberId: string, request: UpdateMemberRequest) {
    return firstValueFrom(
      this.api.put<ApiResponse<ExpenseBookMember>>(
        `/expensebooks/${bookId}/members/${memberId}`,
        request
      )
    );
  }

  removeMember(bookId: string, memberId: string) {
    return firstValueFrom(
      this.api.delete<ApiResponse<void>>(`/expensebooks/${bookId}/members/${memberId}`)
    );
  }

  acceptInvite(token: string) {
    return firstValueFrom(
      this.api.post<ApiResponse<AcceptInviteResponse>>(
        `/members/accept?token=${encodeURIComponent(token)}`,
        {}
      )
    );
  }

  getPendingInvites() {
    return firstValueFrom(
      this.api.get<ApiResponse<PendingInvite[]>>('/members/pending')
    );
  }

  declineInvite(token: string) {
    return firstValueFrom(
      this.api.post<ApiResponse<void>>(
        `/members/decline?token=${encodeURIComponent(token)}`,
        {}
      )
    );
  }

  getAccessibleCategories(bookId: string) {
    return firstValueFrom(
      this.api.get<ApiResponse<Category[]>>(`/expensebooks/${bookId}/members/categories`)
    );
  }
}
