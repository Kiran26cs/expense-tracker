import {
  Component, OnInit, OnDestroy, ViewChild, ElementRef,
  AfterViewChecked, inject, effect, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService, AiChatMessage, ReceiptExtractResponse } from '../../services/ai-chat.service';
import { CurrentBookService } from '../../services/current-book.service';
import { CurrencyService } from '../../services/currency.service';
import { localDateString } from '../../utils/helpers';

export interface ReceiptItemConfirmation {
  name: string;
  amount: number | null;
  category: string;
}

export interface ReceiptConfirmation {
  // shared header
  merchant: string;
  receiptNumber: string;
  currency: string;
  date: string;
  paymentMethod: string;
  notes: string;
  confidence: number;
  missingFields: string[];
  previewUrl: string;
  // itemized mode
  items: ReceiptItemConfirmation[];
  taxAmount: number | null;
  taxLabel: string;
  // single-entry mode
  singleDescription: string;
  singleAmount: number | null;
  singleCategory: string;
  // ui state
  mode: 'itemized' | 'single';
}

@Component({
  selector: 'app-ai-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat-panel.component.html',
  styleUrls: ['./ai-chat-panel.component.css'],
})
export class AiChatPanelComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messageList') private messageList!: ElementRef<HTMLDivElement>;
  @ViewChild('imageInput') private imageInputRef!: ElementRef<HTMLInputElement>;
  @ViewChild('pdfInput') private pdfInputRef!: ElementRef<HTMLInputElement>;

  chatService  = inject(AiChatService);
  bookService  = inject(CurrentBookService);
  private fxService = inject(CurrencyService);

  inputText  = '';
  private shouldScroll = false;
  private _trackedBookId: string | undefined;

  // Receipt confirmation card state
  receiptConfirmation = signal<ReceiptConfirmation | null>(null);
  receiptLoading = signal(false);
  receiptRate = signal<number | null>(null);
  receiptRateLoading = signal(false);

  receiptIsForeign = computed(() => {
    const r = this.receiptConfirmation();
    if (!r || !r.currency) return false;
    return r.currency.toUpperCase() !== this.bookService.currency().toUpperCase();
  });

  receiptTotal = computed(() => {
    const r = this.receiptConfirmation();
    if (!r) return 0;
    if (r.mode === 'single') return r.singleAmount ?? 0;
    const itemsSum = r.items.reduce((s, i) => s + (i.amount ?? 0), 0);
    return itemsSum + (r.taxAmount ?? 0);
  });

  receiptConversionPreview = computed(() => {
    if (!this.receiptIsForeign() || this.receiptRate() === null) return null;
    const total = this.receiptTotal();
    if (total <= 0) return null;
    const converted = Math.round(total * this.receiptRate()! * 100) / 100;
    return { converted, rate: this.receiptRate()!, bookCurrency: this.bookService.currency() };
  });

  receiptNoRate = computed(() =>
    this.receiptIsForeign() && !this.receiptRateLoading() && this.receiptRate() === null
  );

  // Voice recognition
  isListening = signal(false);
  hasSpeechSupport = typeof window !== 'undefined' &&
    !!(window as any).SpeechRecognition || !!(window as any).webkitSpeechRecognition;
  private recognition: any = null;

  constructor() {
    effect(() => {
      const book = this.bookService.book();
      const id   = book?.id;

      if (id !== this._trackedBookId) {
        this.chatService.resetForBook();
        this._trackedBookId = id;
        this.receiptConfirmation.set(null);

        if (id && book?.aiChatEnabled) {
          this.chatService.loadBalance(id);
        }
      }
    });

    effect(() => {
      if (this.chatService.messages().length) {
        this.shouldScroll = true;
      }
    });
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.recognition?.stop();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  get book() { return this.bookService.book(); }
  get isOpen() { return this.chatService.isOpen(); }
  get messages() { return this.chatService.messages(); }
  get loading() { return this.chatService.loading(); }
  get creditsLeft() { return this.chatService.creditsLeft(); }
  get reference() { return this.chatService.referenceContext(); }

  close() { this.chatService.close(); }
  clearReference() { this.chatService.setReference(null); }

  get noCredits(): boolean {
    return this.creditsLeft !== null && this.creditsLeft <= 0;
  }

  async send(): Promise<void> {
    const text = this.inputText.trim();
    if (!text || this.loading || !this.book?.id || this.noCredits) return;
    this.inputText = '';
    await this.chatService.sendMessage(this.book.id, text);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  // ── Voice input ───────────────────────────────────────────────────────────

  toggleVoice(): void {
    if (this.isListening()) {
      this.stopListening();
    } else {
      this.startListening();
    }
  }

  private startListening(): void {
    const SR = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SR) return;
    this.recognition = new SR();
    this.recognition.continuous = false;
    this.recognition.interimResults = false;
    this.recognition.lang = 'en-US';
    this.recognition.onresult = (e: any) => {
      const transcript = e.results[0][0].transcript;
      this.inputText = this.inputText ? `${this.inputText} ${transcript}` : transcript;
      this.isListening.set(false);
    };
    this.recognition.onerror = () => this.isListening.set(false);
    this.recognition.onend   = () => this.isListening.set(false);
    this.recognition.start();
    this.isListening.set(true);
  }

  private stopListening(): void {
    this.recognition?.stop();
    this.isListening.set(false);
  }

  // ── Receipt capture ───────────────────────────────────────────────────────

  openImageCapture(): void {
    this.imageInputRef?.nativeElement.click();
  }

  openPdfUpload(): void {
    this.pdfInputRef?.nativeElement.click();
  }

  async onImageSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    input.value = '';

    const mimeType = file.type || 'image/jpeg';
    const previewUrl = URL.createObjectURL(file);

    this.receiptLoading.set(true);
    this.receiptConfirmation.set(null);
    this.shouldScroll = true;

    try {
      const base64 = await this.compressImage(file);
      const result = await this.chatService.extractReceipt(this.book!.id, base64, mimeType);
      if (result) {
        this.receiptConfirmation.set(this.buildConfirmation(result, previewUrl));
        this.loadReceiptRate();
        this.shouldScroll = true;
      }
    } catch {
      // silent — user can retry
    } finally {
      this.receiptLoading.set(false);
    }
  }

  async onPdfSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    input.value = '';

    const mimeType = file.type || 'application/pdf';
    const isImage = mimeType.startsWith('image/');
    const previewUrl = isImage ? URL.createObjectURL(file) : '';

    this.receiptLoading.set(true);
    this.receiptConfirmation.set(null);
    this.shouldScroll = true;

    try {
      const base64 = isImage
        ? await this.compressImage(file)
        : await this.readFileAsBase64(file);
      const result = await this.chatService.extractReceipt(this.book!.id, base64, mimeType);
      if (result) {
        this.receiptConfirmation.set(this.buildConfirmation(result, previewUrl));
        this.loadReceiptRate();
        this.shouldScroll = true;
      }
    } catch {
      // silent
    } finally {
      this.receiptLoading.set(false);
    }
  }

  private buildConfirmation(result: ReceiptExtractResponse, previewUrl: string): ReceiptConfirmation {
    const hasItems = result.items && result.items.length > 0;
    const currency = result.currency ?? this.bookService.currency();
    const date = result.date ?? localDateString();
    const total = result.total ?? result.amount ?? null;

    return {
      merchant:          result.merchant ?? result.description ?? '',
      receiptNumber:     result.receiptNumber ?? '',
      currency,
      date,
      paymentMethod:     result.paymentMethod ?? '',
      notes:             result.notes ?? '',
      confidence:        result.confidence,
      missingFields:     result.missingFields ?? [],
      previewUrl,
      items:             hasItems
        ? result.items.map(i => ({ name: i.name ?? '', amount: i.amount, category: i.suggestedCategory ?? '' }))
        : [{ name: result.description ?? '', amount: total, category: result.category ?? '' }],
      taxAmount:         result.taxAmount ?? null,
      taxLabel:          result.taxLabel ?? 'Tax',
      singleDescription: result.merchant ?? result.description ?? '',
      singleAmount:      total,
      singleCategory:    result.category ?? '',
      mode:              hasItems ? 'itemized' : 'single',
    };
  }

  dismissReceipt(): void {
    this.receiptConfirmation.set(null);
  }

  setReceiptMode(mode: 'itemized' | 'single'): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, mode });
  }

  updateReceiptHeader(field: 'merchant' | 'receiptNumber' | 'currency' | 'date' | 'paymentMethod' | 'notes', value: string): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, [field]: value });
    if (field === 'currency' || field === 'date') this.loadReceiptRate();
  }

  updateReceiptItem(index: number, field: 'name' | 'amount' | 'category', value: string | number | null): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    const items = r.items.map((item, i) => i === index ? { ...item, [field]: value } : item);
    this.receiptConfirmation.set({ ...r, items });
  }

  addReceiptItem(): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, items: [...r.items, { name: '', amount: null, category: '' }] });
  }

  removeReceiptItem(index: number): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, items: r.items.filter((_, i) => i !== index) });
  }

  updateTax(field: 'taxAmount' | 'taxLabel', value: string | number | null): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, [field]: value });
  }

  updateSingle(field: 'singleDescription' | 'singleAmount' | 'singleCategory', value: string | number | null): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, [field]: value });
  }

  async addReceiptExpense(): Promise<void> {
    const r = this.receiptConfirmation();
    if (!r || !this.book?.id) return;

    const isForeign = this.receiptIsForeign();
    const currencyTag = isForeign ? ` ${r.currency}` : '';

    let message: string;

    if (r.mode === 'itemized') {
      const itemLines = r.items
        .filter(i => i.amount != null && i.amount > 0)
        .map(i => `  - name "${i.name || i.category}", amount ${i.amount}${currencyTag}, category "${i.category}"`)
        .join('\n');
      const taxLine = r.taxAmount ? `\n  - tax ${r.taxAmount}${currencyTag} (${r.taxLabel || 'Tax'})` : '';
      message = `Save receipt as itemized expenses:\n`
        + `  receipt number "${r.receiptNumber || 'N/A'}", merchant "${r.merchant}", date ${r.date}, payment "${r.paymentMethod}"\n`
        + `Items:\n${itemLines}${taxLine}`;
    } else {
      const amount = r.singleAmount ?? 0;
      message = `Add expense from receipt: description "${r.singleDescription || r.merchant}", `
        + `amount ${amount}${currencyTag}, date ${r.date}, category "${r.singleCategory}", `
        + `payment method "${r.paymentMethod}"`
        + (r.receiptNumber ? `, receipt number "${r.receiptNumber}"` : '')
        + (r.notes ? `, notes "${r.notes}"` : '') + '.';
    }

    this.receiptConfirmation.set(null);
    this.inputText = '';
    await this.chatService.sendMessage(this.book.id, message);
  }

  private async loadReceiptRate(): Promise<void> {
    const r = this.receiptConfirmation();
    if (!r) return;
    const book = this.bookService.currency();
    if (!r.currency || r.currency.toUpperCase() === book.toUpperCase()) {
      this.receiptRate.set(null);
      return;
    }
    this.receiptRateLoading.set(true);
    const rate = await this.fxService.getRate(r.currency, book, r.date);
    this.receiptRate.set(rate);
    this.receiptRateLoading.set(false);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private compressImage(file: File, maxPx = 1200): Promise<string> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      const url = URL.createObjectURL(file);
      img.onload = () => {
        let { width, height } = img;
        if (width > maxPx || height > maxPx) {
          const r = Math.min(maxPx / width, maxPx / height);
          width  = Math.round(width * r);
          height = Math.round(height * r);
        }
        const canvas = document.createElement('canvas');
        canvas.width  = width;
        canvas.height = height;
        canvas.getContext('2d')!.drawImage(img, 0, 0, width, height);
        URL.revokeObjectURL(url);
        const data = canvas.toDataURL('image/jpeg', 0.85).split(',')[1];
        resolve(data);
      };
      img.onerror = reject;
      img.src = url;
    });
  }

  private readFileAsBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve((reader.result as string).split(',')[1]);
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  }

  private scrollToBottom(): void {
    try {
      this.messageList.nativeElement.scrollTop =
        this.messageList.nativeElement.scrollHeight;
    } catch {}
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}
