export type ImportStatus =
  | 'queued'
  | 'processing'
  | 'completed'
  | 'completedWithErrors'
  | 'failed';

export type ImportRecordStatus = 'pending' | 'success' | 'failed';

export interface ImportRecord {
  rowNumber: number;
  description: string;
  amount: number;
  category: string;
  date: string;
  paymentMethod: string;
  status: ImportRecordStatus;
  errorMessage?: string;
}

export interface RetryImportRequest {
  rowNumbers: number[];
}

export interface ImportSession {
  id: string;
  expenseBookId: string;
  fileName: string;
  status: ImportStatus;
  totalRecords: number;
  processedCount: number;
  successCount: number;
  failedCount: number;
  records: ImportRecord[];
  createdAt: string;
  completedAt?: string;
}

export interface ImportSessionSummary {
  id: string;
  fileName: string;
  status: ImportStatus;
  totalRecords: number;
  processedCount: number;
  successCount: number;
  failedCount: number;
  createdAt: string;
  completedAt?: string;
}

export interface StartImportRequest {
  fileName: string;
  rows: CsvExpenseRow[];
}

export interface CsvExpenseRow {
  rowNumber: number;
  description: string;
  amount: number;
  date: string;
  category: string;
  paymentMethod: string;
  notes: string;
  type: string;
  currency: string;
}
