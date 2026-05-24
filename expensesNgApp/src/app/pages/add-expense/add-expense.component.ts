import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { debounceTime } from 'rxjs';
import { ExpenseService } from '../../services/expense.service';
import { SettingsService } from '../../services/settings.service';
import { MemberService } from '../../services/member.service';
import { ToastService } from '../../services/toast.service';
import { CurrentBookService } from '../../services/current-book.service';
import { CurrencyService, SUPPORTED_CURRENCIES } from '../../services/currency.service';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent, TextareaComponent } from '../../components/input/input.component';

@Component({
  selector: 'app-add-expense',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, ButtonComponent, InputComponent, SelectComponent, TextareaComponent],
  templateUrl: './add-expense.component.html',
  styleUrl: './add-expense.component.css'
})
export class AddExpenseComponent implements OnInit {
  categories = signal<any[]>([]);
  paymentMethods = signal<any[]>([]);
  selectedCurrency = signal('');
  currentRate = signal<number | null>(null);
  rateLoading = signal(false);

  categoryOptions = computed(() => [
    { value: '', label: 'Select category' },
    ...this.categories().map(c => ({ value: c.id, label: c.name }))
  ]);
  paymentMethodOptions = computed(() => [
    { value: '', label: 'Select payment method' },
    ...this.paymentMethods().map(p => ({ value: String(p.id), label: p.name }))
  ]);
  loading = signal(false);
  error = signal('');
  bookId = '';

  private fb = inject(FormBuilder);
  private expenseService = inject(ExpenseService);
  private settingsService = inject(SettingsService);
  private memberService = inject(MemberService);
  private currencyService = inject(CurrencyService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  currentBook = inject(CurrentBookService);

  form: FormGroup = this.fb.group({
    type: ['expense'],
    description: ['', Validators.required],
    amount: [null, [Validators.required, Validators.min(0.01)]],
    date: [new Date().toISOString().split('T')[0], Validators.required],
    category: [''],
    paymentMethod: [''],
    notes: [''],
    isRecurring: [false],
    recurringFrequency: ['monthly'],
    recurringEndDate: [''],
  });

  private formValues = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  currencyOptions = computed(() => {
    const book = this.currentBook.currency();
    const others = SUPPORTED_CURRENCIES.filter(c => c !== book);
    return [book, ...others];
  });

  isForeignCurrency = computed(() => {
    const sel = this.selectedCurrency();
    return sel !== '' && sel !== this.currentBook.currency();
  });

  conversionPreview = computed(() => {
    if (!this.isForeignCurrency() || this.currentRate() === null) return null;
    const vals = this.formValues();
    const amount = Number(vals?.amount);
    if (!amount || isNaN(amount) || amount <= 0) return null;
    const rate = this.currentRate()!;
    const converted = Math.round(amount * rate * 100) / 100;
    return { converted, rate, bookCurrency: this.currentBook.currency() };
  });

  noRateWarning = computed(() =>
    this.isForeignCurrency() && !this.rateLoading() && this.currentRate() === null
  );

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.selectedCurrency.set(this.currentBook.currency());
      this.loadFilters();
    });

    this.form.get('date')?.valueChanges.pipe(debounceTime(400)).subscribe(() => {
      if (this.isForeignCurrency()) this.loadRate();
    });
  }

  async loadFilters() {
    const [catsResult, methodsResult] = await Promise.allSettled([
      this.memberService.getAccessibleCategories(this.bookId),
      this.settingsService.getPaymentMethods(this.bookId)
    ]);
    if (catsResult.status === 'fulfilled' && catsResult.value.success)
      this.categories.set(catsResult.value.data || []);
    if (methodsResult.status === 'fulfilled' && methodsResult.value.success)
      this.paymentMethods.set(methodsResult.value.data || []);
  }

  async onCurrencyChange(currency: string) {
    this.selectedCurrency.set(currency);
    await this.loadRate();
  }

  private async loadRate() {
    const sel = this.selectedCurrency();
    const book = this.currentBook.currency();
    if (sel === '' || sel === book) { this.currentRate.set(null); return; }
    const date: string = this.form.get('date')?.value ?? new Date().toISOString().split('T')[0];
    this.rateLoading.set(true);
    const rate = await this.currencyService.getRate(sel, book, date);
    this.currentRate.set(rate);
    this.rateLoading.set(false);
  }

  async handleSubmit() {
    this.error.set('');
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    const v = this.form.value;
    const payload: any = {
      type: v.type, description: v.description, amount: Number(v.amount),
      date: v.date, category: v.category, paymentMethod: v.paymentMethod,
      notes: v.notes,
    };
    if (this.isForeignCurrency()) {
      payload.originalAmount = Number(v.amount);
      payload.originalCurrency = this.selectedCurrency();
    }
    if (v.isRecurring) {
      payload.recurringConfig = { frequency: v.recurringFrequency, endDate: v.recurringEndDate || undefined };
    }
    try {
      const res = await this.expenseService.createExpense(this.bookId, payload);
      if (res.success) { this.toast.success(v.type === 'income' ? 'Income added' : 'Expense added'); this.goBack(); }
      else this.error.set(res.error || 'Failed to save');
    } catch (e: any) { this.error.set(e.message || 'Error'); }
    finally { this.loading.set(false); }
  }

  goBack() { this.router.navigate([`/${this.bookId}/expenses`]); }
}
