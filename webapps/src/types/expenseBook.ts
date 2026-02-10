export interface ExpenseBook {
  id: string;
  userId: string;
  name: string;
  description?: string;
  category: string;
  isDefault: boolean;
  totalExpenses: number;
  expenseCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExpenseBookRequest {
  name: string;
  description?: string;
  category: string;
  isDefault?: boolean;
}

export interface UpdateExpenseBookRequest {
  name: string;
  description?: string;
  category: string;
  isDefault?: boolean;
}
