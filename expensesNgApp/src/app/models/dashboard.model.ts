export interface DashboardSummary {
  totalExpenses: number;
  totalIncome: number;
  savings?: number;
  netSavings?: number;
  currency?: string;
  totalBudget?: number;
  savingsRate?: number;
  transactionCount?: number;
  expensesByCategory?: CategorySpending[];
  categoryBreakdown?: CategorySpending[];
  monthlyTrend?: MonthlyTrend[];
  recentExpenses?: any[];
  recentTransactions?: any[];
}

export interface CategorySpending {
  category: string;
  amount: number;
  count?: number;
  percentage?: number;
}

export interface MonthlyTrend {
  month: string;
  amount: number;
}

export interface DailyTransactionGroup {
  date: string;
  dateLabel?: string;
  categorySpending?: CategorySpending[];
  transactions?: any[];
  totalSpent?: number;
}

export interface UpcomingPayment {
  id: string;
  description: string;
  amount: number;
  dueDate: string;
  category?: string;
  isRecurring?: boolean;
  isPaid?: boolean;
  expenseBookId?: string;
}

