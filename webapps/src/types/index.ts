// Type definitions for the application

export interface User {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  currency: string;
  monthlyIncome: number;
  createdAt?: string;
}

export interface Expense {
  id: string;
  amount: number;
  date: string;
  category: Category;
  paymentMethod: PaymentMethod;
  description: string;
  notes?: string;
  isRecurring: boolean;
  recurring?: RecurringConfig;
  createdAt: string;
  updatedAt: string;
}

export interface RecurringConfig {
  frequency: 'daily' | 'weekly' | 'monthly' | 'yearly';
  nextDueDate: string;
  reminderEnabled: boolean;
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
  createdAt: string;
  updatedAt: string;
}

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

export interface Budget {
  id: string;
  userId?: string;
  category: string;
  limit: number;
  spent: number;
  period: string;
  startDate: string;
  endDate: string;
  alertThreshold: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface DashboardSummary {
  totalExpenses: number;
  totalIncome: number;
  savings: number;
  categoryBreakdown: Array<{
    category: string;
    amount: number;
    percentage: number;
  }>;
  recentTransactions: Array<{
    id: string;
    description: string;
    category: string;
    amount: number;
    date: string;
    paymentMethod: string;
  }>;
}

export interface CashForecast {
  cashRunway: number;
  projectedBalance: Array<{
    date: string;
    balance: number;
    income: number;
    expenses: number;
  }>;
  breakdown: {
    expectedIncome: number;
    plannedExpenses: number;
    minimumSavings: number;
  };
  insights: string[];
}

export interface PurchaseSimulation {
  amount: number;
  categoryId: string;
  plannedDate: string;
  result: {
    budgetImpact: {
      before: number;
      after: number;
      percentage: number;
    };
    savingsImpact: {
      before: number;
      after: number;
      percentage: number;
    };
    cashRisk: 'safe' | 'risky' | 'avoid';
    recommendation: string;
  };
}

export interface ImportPreview {
  totalRows: number;
  validRows: number;
  invalidRows: number;
  expenses: Array<Partial<Expense> & { rowNumber: number; errors?: string[] }>;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface FilterOptions {
  dateRange?: {
    start: string;
    end: string;
  };
  categories?: string[];
  paymentMethods?: string[];
  minAmount?: number;
  maxAmount?: number;
  searchQuery?: string;
}

export interface Settings {
  categories: Category[];
  paymentMethods: PaymentMethod[];
  currency: string;
  minimumMonthlySavings: number;
  theme: 'light' | 'dark';
  notifications: {
    recurringExpenses: boolean;
    budgetWarnings: boolean;
    lowBalance: boolean;
  };
}

export interface AuthCredentials {
  email?: string;
  phone?: string;
  otp?: string;
}

export interface SignupData {
  name: string;
  email?: string;
  phone?: string;
  currency?: string;
  monthlyIncome?: number;
  otp?: string;
}

export type RiskLevel = 'safe' | 'warning' | 'risk';

export type Theme = 'light' | 'dark';

export type TransactionFilter = 'last50' | 'last30days' | 'thisWeek';
