export type BankName = 'HDFC' | 'ICICI' | 'SBI' | 'Axis' | 'Kotak' | 'IndusInd' | 'Other';
export type BankMode = 'manual' | 'auto' | 'disabled';

export interface BankConnectionDto {
  id: string;
  displayName: string;
  bankName: BankName;
  mode: BankMode;
  lastSyncedAt?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateBankConnectionRequest {
  displayName: string;
  bankName: BankName;
  mode: BankMode;
}

export interface UpdateBankConnectionRequest {
  displayName?: string;
  mode?: BankMode;
}

export interface ParsedBankTransactionDto {
  rowNumber: number;
  date: string;
  description: string;
  amount: number;
  type: 'expense' | 'income';
  // Suggested category (payee memory, then AI) — editable in the preview step before confirming.
  category: string;
  // Stable payee identifier — lets the UI recognize repeat payees within this statement and
  // propagate a chosen category across them. Null when the narration wasn't recognized.
  payeeKey?: string | null;
}

export interface BankStatementPreviewDto {
  sessionId: string;
  bankName: string;
  detectedFormat: string;
  totalCount: number;
  transactions: ParsedBankTransactionDto[];
}

export interface ConfirmBankSyncRequest {
  expenseBookId: string;
  defaultPaymentMethod: string;
  excludeRowNumbers: number[];
  // Final category per row (rowNumber -> category name) as reviewed/edited on the preview screen.
  categoryOverrides: Record<number, string>;
}

export interface BankSyncConfirmResultDto {
  importSession: { id: string; status: string; totalRecords: number };
  imported: number;
  duplicatesSkipped: number;
}

export interface BookBankConnectionDto {
  expenseBookId: string;
  bankConnectionId: string | null;
  connection: BankConnectionDto | null;
}
