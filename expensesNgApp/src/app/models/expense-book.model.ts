export interface ExpenseBook {
  id: string;
  userId: string;
  name: string;
  description?: string;
  category?: string;
  color?: string;
  icon?: string;
  currency?: string;
  isDefault: boolean;
  totalExpenses?: number;
  expenseCount?: number;
  createdAt?: string;
  updatedAt?: string;
  /** null = requesting user is the owner; otherwise their role in this shared book */
  memberRole?: string | null;
}

export interface CreateExpenseBookRequest {
  name: string;
  description?: string;
  category?: string;
  color?: string;
  icon?: string;
  currency?: string;
  isDefault?: boolean;
}

export interface UpdateExpenseBookRequest {
  name?: string;
  description?: string;
  category?: string;
  color?: string;
  icon?: string;
  currency?: string;
  isDefault?: boolean;
}
