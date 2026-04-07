export interface Category {
  id: string;
  name: string;
  icon: string;
  color: string;
}

export interface PaymentMethod {
  id: string;
  name: string;
  type: 'cash' | 'card' | 'upi' | 'bank-transfer' | 'other';
}

export interface RecurringConfig {
  frequency: 'daily' | 'weekly' | 'monthly' | 'yearly';
  nextDueDate?: string;
  reminderEnabled?: boolean;
}

export interface Expense {
  id: string;
  type: 'expense' | 'income';
  amount: number;
  date: string;
  currency?: string;
  category: Category | string;
  paymentMethod?: PaymentMethod | string;
  description: string;
  notes?: string;
  isRecurring?: boolean;
  recurring?: RecurringConfig;
  recurringConfig?: RecurringConfig;
  expenseBookId?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface RecurringExpense {
  id: string;
  amount: number;
  category: string;
  paymentMethod: string;
  description?: string;
  frequency: 'daily' | 'weekly' | 'monthly' | 'yearly';
  startDate: string;
  endDate?: string;
  nextOccurrence: string;
  lastProcessed?: string;
  isActive: boolean;
  expenseBookId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpcomingPayment {
  _id: string;
  id?: string;
  recurringExpenseId: string;
  amount: number;
  category: string;
  paymentMethod: string;
  description?: string;
  frequency: 'daily' | 'weekly' | 'monthly' | 'yearly';
  dueDate: string;
  status: 'upcoming' | 'due' | 'overdue' | 'pending';
  dueDateLabel: string;
  createdAt: string;
}

export interface UpcomingPaymentsPaginatedResponse {
  items: UpcomingPayment[];
  total: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}
