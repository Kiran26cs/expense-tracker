import {
  Component, OnInit, OnDestroy, ViewChild, ElementRef,
  AfterViewChecked, inject, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiChatService, AiChatMessage } from '../../services/ai-chat.service';
import { CurrentBookService } from '../../services/current-book.service';

@Component({
  selector: 'app-ai-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat-panel.component.html',
  styleUrls: ['./ai-chat-panel.component.css'],
})
export class AiChatPanelComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('messageList') private messageList!: ElementRef<HTMLDivElement>;

  chatService  = inject(AiChatService);
  bookService  = inject(CurrentBookService);

  inputText  = '';
  private shouldScroll = false;
  private _trackedBookId: string | undefined;

  constructor() {
    effect(() => {
      const book = this.bookService.book();
      const id   = book?.id;

      if (id !== this._trackedBookId) {
        this.chatService.resetForBook();
        this._trackedBookId = id;

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

  ngOnDestroy(): void {}

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

  async send(): Promise<void> {
    const text = this.inputText.trim();
    if (!text || this.loading || !this.book?.id) return;
    this.inputText = '';
    await this.chatService.sendMessage(this.book.id, text);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
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
