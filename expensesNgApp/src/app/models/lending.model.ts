export interface Lending {
  id: string;
  expenseBookId: string;
  borrowerName: string;
  borrowerContact?: string;
  principalAmount: number;
  annualInterestRate: number;
  startDate: string;
  dueDate?: string;
  totalRepaid: number;
  outstandingPrincipal: number;
  repaymentCount: number;
  status: 'active' | 'settled';
  notes?: string;
  accruedInterest: number;
  futureInterest: number;
  projectedTotalInterest: number;
  totalToRecover: number;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Repayment {
  id: string;
  lendingId: string;
  date: string;
  amount: number;
  notes?: string;
  recordedAt: string;
}

export interface CreateLendingRequest {
  borrowerName: string;
  borrowerContact?: string;
  principalAmount: number;
  annualInterestRate: number;
  startDate: string;
  dueDate?: string;
  notes?: string;
}

export interface CreateRepaymentRequest {
  date: string;
  amount: number;
  notes?: string;
}

export interface LendingRepaymentsResponse {
  lending: Lending;
  items: Repayment[];
  total: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}
