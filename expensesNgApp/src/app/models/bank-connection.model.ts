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
