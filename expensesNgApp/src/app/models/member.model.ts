export type MemberRole = 'owner' | 'admin' | 'member' | 'viewer';
export type InviteStatus = 'pending' | 'accepted' | 'revoked';
export type PageAccess = 'write' | 'view' | 'none';

export interface PagePermissions {
  dashboard?: PageAccess | null;
  expenses?: PageAccess | null;
  budgets?: PageAccess | null;
  settings?: PageAccess | null;
  insights?: PageAccess | null;
}

export interface ResolvedPermissions {
  role: MemberRole | 'none';
  dashboard: PageAccess;
  expenses: PageAccess;
  budgets: PageAccess;
  settings: PageAccess;
  insights: PageAccess;
  canDeleteExpenses: boolean;
  canManageMembers: boolean;
  canModifyBook: boolean;
  isOwner: boolean;
  allowedCategoryIds: string[];
}

export interface ExpenseBookMember {
  id: string;
  expenseBookId: string;
  userId?: string;
  invitedEmail: string;
  inviteStatus: InviteStatus;
  role: MemberRole;
  permissions?: PagePermissions | null;
  allowedCategoryIds: string[];
  canDeleteExpenses: boolean;
  addedBy: string;
  addedAt: string;
  updatedAt: string;
}

export interface InviteMemberRequest {
  email: string;
  role: MemberRole;
  permissions?: PagePermissions | null;
  allowedCategoryIds?: string[];
  canDeleteExpenses?: boolean;
}

export interface InviteMemberResponse {
  member: ExpenseBookMember;
  inviteLink: string;
}

export interface UpdateMemberRequest {
  role?: MemberRole;
  permissions?: PagePermissions | null;
  allowedCategoryIds?: string[];
  canDeleteExpenses?: boolean;
}

export interface AcceptInviteResponse {
  member: ExpenseBookMember;
  expenseBookId: string;
  expenseBookName: string;
}

export interface PendingInvite {
  memberId: string;
  expenseBookId: string;
  expenseBookName: string;
  expenseBookIcon?: string;
  expenseBookColor?: string;
  expenseBookCurrency?: string;
  role: MemberRole;
  inviteToken: string;
  addedAt: string;
}
