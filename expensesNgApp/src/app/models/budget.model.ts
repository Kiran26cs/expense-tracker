export interface BudgetVersion {
  versionNumber: number;
  effectivePeriod?: string;
  effectiveDate: string;
  amount: number;
  createdAt?: string;
}

export interface Budget {
  id: string;
  userId?: string;
  category: string;
  amount: number;
  latestVersionNumber: number;
  versions: BudgetVersion[];
  spent: number;
  period: string;
  currency?: string;
  expenseBookId?: string;
  createdAt?: string;
  updatedAt?: string;
}
