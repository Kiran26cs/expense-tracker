import { Injectable, signal } from '@angular/core';
import { firstValueFrom, Subject } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

export interface ReferenceContext {
  type: 'expense' | 'budget' | 'category' | 'member';
  id: string;
  displayLabel: string;
}

export interface AiChatMessage {
  role: 'user' | 'assistant';
  content: string;
  toolsUsed?: string[];
  timestamp: Date;
  imagePreview?: string;
}

export interface ReceiptLineItem {
  name: string | null;
  amount: number | null;
  suggestedCategory: string | null;
}

export interface ReceiptExtractResponse {
  receiptNumber: string | null;
  merchant: string | null;
  currency: string | null;
  date: string | null;
  paymentMethod: string | null;
  items: ReceiptLineItem[];
  taxAmount: number | null;
  taxLabel: string | null;
  subtotal: number | null;
  total: number | null;
  notes: string | null;
  confidence: number;
  missingFields: string[];
  // flat fallback fields for single-entry
  description: string | null;
  amount: number | null;
  category: string | null;
}

export interface ChatHistoryMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AiChatRequest {
  message: string;
  bookId: string;
  referenceContext?: { type: string; id: string } | null;
  history: ChatHistoryMessage[];
}

export interface AiChatResponse {
  reply: string;
  creditsUsed: number;
  creditsLeft: number;
  toolsUsed: string[];
}

export interface CreditBalance {
  expenseBookId: string;
  freeCreditsLeft: number;
  paidCreditsLeft: number;
  totalCreditsLeft: number;
  freeCreditsLimit: number;
  lastResetDate: string;
}

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private _isOpen = signal(false);
  private _messages = signal<AiChatMessage[]>([]);
  private _referenceContext = signal<ReferenceContext | null>(null);
  private _creditsLeft = signal<number | null>(null);
  private _loading = signal(false);

  // Emits after any response that executed a data-mutating tool
  readonly dataChanged$ = new Subject<void>();

  private static readonly MUTATING_TOOLS = new Set([
    'create_expense', 'update_expense', 'delete_expense',
    'create_recurring_expense',
    'create_lending',
    'set_budget', 'delete_budget',
    'invite_member', 'remove_member',
    'create_category', 'update_category', 'delete_category',
  ]);

  isOpen = this._isOpen.asReadonly();
  messages = this._messages.asReadonly();
  referenceContext = this._referenceContext.asReadonly();
  creditsLeft = this._creditsLeft.asReadonly();
  loading = this._loading.asReadonly();

  constructor(private api: ApiService) {}

  open() { this._isOpen.set(true); }
  close() { this._isOpen.set(false); }
  toggle() { this._isOpen.update(v => !v); }

  setReference(ctx: ReferenceContext | null) {
    this._referenceContext.set(ctx);
  }

  clearMessages() {
    this._messages.set([]);
  }

  resetForBook() {
    this._isOpen.set(false);
    this._messages.set([]);
    this._referenceContext.set(null);
    this._creditsLeft.set(null);
  }

  async loadBalance(bookId: string): Promise<void> {
    try {
      const res = await firstValueFrom(
        this.api.get<ApiResponse<CreditBalance>>(`/expensebooks/${bookId}/credits`)
      );
      if (res.success && res.data) {
        this._creditsLeft.set(res.data.totalCreditsLeft);
      }
    } catch {
      // non-fatal: credits display simply won't show
    }
  }

  async sendMessage(bookId: string, message: string): Promise<void> {
    const ref = this._referenceContext();

    // Snapshot history BEFORE adding the current user message
    const history: ChatHistoryMessage[] = this._messages()
      .slice(-20)
      .map(m => ({ role: m.role, content: m.content }));

    this._messages.update(msgs => [...msgs, {
      role: 'user',
      content: message,
      timestamp: new Date(),
    }]);

    this._loading.set(true);

    try {
      const body: AiChatRequest = {
        message,
        bookId,
        referenceContext: ref ? { type: ref.type, id: ref.id } : null,
        history,
      };

      const res = await firstValueFrom(
        this.api.post<ApiResponse<AiChatResponse>>('/ai/chat', body)
      );

      if (res.success && res.data) {
        this._messages.update(msgs => [...msgs, {
          role: 'assistant',
          content: res.data!.reply,
          toolsUsed: res.data!.toolsUsed,
          timestamp: new Date(),
        }]);
        this._creditsLeft.set(res.data.creditsLeft);
        const usedMutating = (res.data.toolsUsed || []).some(t => AiChatService.MUTATING_TOOLS.has(t));
        if (usedMutating) this.dataChanged$.next();
      } else {
        this._messages.update(msgs => [...msgs, {
          role: 'assistant',
          content: res.error ?? 'Something went wrong. Please try again.',
          timestamp: new Date(),
        }]);
      }
    } catch (err: any) {
      const errorMsg = err?.error?.error ?? 'Failed to reach the AI. Please try again.';
      this._messages.update(msgs => [...msgs, {
        role: 'assistant',
        content: errorMsg,
        timestamp: new Date(),
      }]);
    } finally {
      this._loading.set(false);
    }
  }

  async extractReceipt(bookId: string, fileBase64: string, mimeType: string): Promise<ReceiptExtractResponse | null> {
    try {
      const res = await firstValueFrom(
        this.api.post<ApiResponse<ReceiptExtractResponse>>('/ai/extract-receipt', { bookId, fileBase64, mimeType })
      );
      if (res.success && res.data) {
        return res.data;
      }
      return null;
    } catch {
      return null;
    }
  }

  async adminGrant(bookId: string, amount: number): Promise<CreditBalance | null> {
    try {
      const res = await firstValueFrom(
        this.api.post<ApiResponse<CreditBalance>>(
          `/expensebooks/${bookId}/credits/admin-grant`, { amount })
      );
      if (res.success && res.data) {
        this._creditsLeft.set(res.data.totalCreditsLeft);
        return res.data;
      }
      return null;
    } catch {
      return null;
    }
  }
}
