import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { startWith } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AiChatService } from '../../services/ai-chat.service';
import { ExpenseService } from '../../services/expense.service';
import { ExpenseBookService } from '../../services/expense-book.service';
import { MemberService } from '../../services/member.service';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';
import { ExpenseBook } from '../../models/expense-book.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent, TextareaComponent } from '../../components/input/input.component';
import { LoadingComponent } from '../../components/loading/loading.component';

type PageStatus = 'loading' | 'book-select' | 'parsing' | 'review' | 'saving' | 'error';

@Component({
  selector: 'app-share-target',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent,
    ButtonComponent, InputComponent, SelectComponent, TextareaComponent, LoadingComponent,
  ],
  templateUrl: './share-target.component.html',
  styleUrl: './share-target.component.css',
})
export class ShareTargetComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private aiChat         = inject(AiChatService);
  private expenseService = inject(ExpenseService);
  private bookService    = inject(ExpenseBookService);
  private memberService  = inject(MemberService);
  private settingsService = inject(SettingsService);
  private toast          = inject(ToastService);
  private fb             = inject(FormBuilder);

  status        = signal<PageStatus>('loading');
  errorMessage  = signal('');
  sharedText    = signal('');
  books         = signal<ExpenseBook[]>([]);
  selectedBook  = signal<ExpenseBook | null>(null);
  categories    = signal<any[]>([]);
  paymentMethods = signal<any[]>([]);
  parseConfidence = signal<number | null>(null);
  aiUnavailable   = signal(false);

  form: FormGroup = this.fb.group({
    type:          ['expense'],
    description:   ['', Validators.required],
    amount:        [null, [Validators.required, Validators.min(0.01)]],
    date:          ['', Validators.required],
    category:      [''],
    paymentMethod: [''],
    notes:         [''],
  });

  private selectedType = toSignal(
    this.form.get('type')!.valueChanges.pipe(startWith('expense')),
    { initialValue: 'expense' }
  );

  categoryOptions = computed(() => [
    { value: '', label: 'Select category' },
    ...this.categories()
      .filter(c => (c.type ?? 'expense') === this.selectedType())
      .map(c => ({ value: c.id, label: c.name })),
  ]);

  paymentMethodOptions = computed(() => [
    { value: '', label: 'Select payment method' },
    ...this.paymentMethods().map(p => ({ value: String(p.id), label: p.name })),
  ]);

  bookOptions = computed(() =>
    this.books().map(b => ({ value: b.id, label: `${b.icon ?? ''} ${b.name}`.trim() }))
  );

  ngOnInit() {
    const params = this.route.snapshot.queryParamMap;
    const text = (params.get('text') || params.get('url') || '').trim();

    if (!text) {
      this.router.navigate(['/app']);
      return;
    }

    this.sharedText.set(text);
    this.loadBooks();
  }

  private async loadBooks() {
    try {
      const res = await this.bookService.getExpenseBooks();
      if (!res.success || !res.data?.length) {
        this.status.set('error');
        this.errorMessage.set('No expense books found. Please create one first.');
        return;
      }
      this.books.set(res.data);
      if (res.data.length === 1) {
        await this.selectBook(res.data[0]);
      } else {
        this.status.set('book-select');
      }
    } catch {
      this.status.set('error');
      this.errorMessage.set('Failed to load your expense books.');
    }
  }

  async onBookSelected(bookId: string) {
    if (!bookId) return;
    const book = this.books().find(b => b.id === bookId);
    if (book) await this.selectBook(book);
  }

  private async selectBook(book: ExpenseBook) {
    this.selectedBook.set(book);
    this.status.set('parsing');
    await this.loadFilters(book.id);
    await this.parseAndFill(book);
  }

  private async loadFilters(bookId: string) {
    const [catsRes, methodsRes] = await Promise.allSettled([
      this.memberService.getAccessibleCategories(bookId),
      this.settingsService.getPaymentMethods(bookId),
    ]);
    if (catsRes.status === 'fulfilled' && catsRes.value.success)
      this.categories.set(catsRes.value.data || []);
    if (methodsRes.status === 'fulfilled' && methodsRes.value.success)
      this.paymentMethods.set(methodsRes.value.data || []);
  }

  private async parseAndFill(book: ExpenseBook) {
    const today = new Date().toISOString().split('T')[0];
    const parsed = await this.aiChat.parseMessage(book.id, this.sharedText());

    if (!parsed || parsed.confidence === 0) {
      this.aiUnavailable.set(true);
      this.form.patchValue({ description: this.sharedText().slice(0, 100), date: today });
      this.status.set('review');
      return;
    }

    this.parseConfidence.set(parsed.confidence);

    const resolvedCat = this.categories().find(
      c => c.name?.toLowerCase() === parsed.category?.toLowerCase()
    );
    const resolvedPm = this.paymentMethods().find(
      p => p.name?.toLowerCase() === parsed.paymentMethod?.toLowerCase()
    );

    this.form.patchValue({
      type:          parsed.type === 'income' ? 'income' : 'expense',
      description:   parsed.description || parsed.merchant || '',
      amount:        parsed.amount ?? parsed.total ?? null,
      date:          parsed.date || today,
      category:      resolvedCat?.id || '',
      paymentMethod: resolvedPm != null ? String(resolvedPm.id) : '',
      notes:         parsed.notes || '',
    });

    this.status.set('review');
  }

  async save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const book = this.selectedBook();
    if (!book) return;

    this.status.set('saving');
    const v = this.form.value;
    const payload = {
      type:          v.type,
      description:   v.description,
      amount:        Number(v.amount),
      date:          v.date,
      category:      v.category || null,
      paymentMethod: v.paymentMethod || null,
      currency:      book.currency || 'USD',
      notes:         v.notes || null,
    };

    try {
      const res = await this.expenseService.createExpense(book.id, payload);
      if (res.success) {
        this.toast.success('Expense added');
        this.router.navigate([`/${book.id}/expenses`]);
      } else {
        this.status.set('review');
        this.toast.error(res.error || 'Failed to save expense');
      }
    } catch {
      this.status.set('review');
      this.toast.error('Failed to save expense');
    }
  }

  discard() {
    this.router.navigate(['/app']);
  }
}
