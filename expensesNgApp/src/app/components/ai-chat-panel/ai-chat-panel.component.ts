import {
  Component, OnInit, OnDestroy, ViewChild, ElementRef,
  AfterViewChecked, inject, effect, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService, AiChatMessage, ReceiptExtractResponse } from '../../services/ai-chat.service';
import { CurrentBookService } from '../../services/current-book.service';
import { CurrencyService } from '../../services/currency.service';

export interface ReceiptConfirmation {
  description: string;
  amount: number | null;
  currency: string;
  date: string;
  category: string;
  paymentMethod: string;
  notes: string;
  confidence: number;
  missingFields: string[];
  previewUrl: string;
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

  receiptConversionPreview = computed(() => {
    if (!this.receiptIsForeign() || this.receiptRate() === null) return null;
    const r = this.receiptConfirmation()!;
    if (r.amount == null || r.amount <= 0) return null;
    const converted = Math.round(r.amount * this.receiptRate()! * 100) / 100;
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
        this.receiptConfirmation.set({
          description:   result.description ?? '',
          amount:        result.amount,
          currency:      result.currency ?? this.bookService.currency(),
          date:          result.date ?? new Date().toISOString().slice(0, 10),
          category:      result.category ?? '',
          paymentMethod: result.paymentMethod ?? '',
          notes:         result.notes ?? '',
          confidence:    result.confidence,
          missingFields: result.missingFields ?? [],
          previewUrl,
        });
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
        this.receiptConfirmation.set({
          description:   result.description ?? '',
          amount:        result.amount,
          currency:      result.currency ?? this.bookService.currency(),
          date:          result.date ?? new Date().toISOString().slice(0, 10),
          category:      result.category ?? '',
          paymentMethod: result.paymentMethod ?? '',
          notes:         result.notes ?? '',
          confidence:    result.confidence,
          missingFields: result.missingFields ?? [],
          previewUrl,
        });
        this.loadReceiptRate();
        this.shouldScroll = true;
      }
    } catch {
      // silent
    } finally {
      this.receiptLoading.set(false);
    }
  }

  dismissReceipt(): void {
    this.receiptConfirmation.set(null);
  }

  async addReceiptExpense(): Promise<void> {
    const r = this.receiptConfirmation();
    if (!r || !this.book?.id) return;

    const isForeign = this.receiptIsForeign();
    const parts: string[] = [];
    if (r.description) parts.push(`description "${r.description}"`);
    if (r.amount != null) {
      if (isForeign) {
        parts.push(`originalAmount ${r.amount} ${r.currency}`);
      } else {
        parts.push(`amount ${r.amount}`);
      }
    }
    if (r.date) parts.push(`date ${r.date}`);
    if (r.category) parts.push(`category "${r.category}"`);
    if (r.paymentMethod) parts.push(`payment method "${r.paymentMethod}"`);
    if (r.notes) parts.push(`notes "${r.notes}"`);

    const message = `Add expense from receipt: ${parts.join(', ')}.`;

    this.receiptConfirmation.set(null);
    this.inputText = '';
    await this.chatService.sendMessage(this.book.id, message);
  }

  updateReceiptField(field: keyof ReceiptConfirmation, value: string | number | null): void {
    const r = this.receiptConfirmation();
    if (!r) return;
    this.receiptConfirmation.set({ ...r, [field]: value });
    if (field === 'currency' || field === 'date') this.loadReceiptRate();
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
