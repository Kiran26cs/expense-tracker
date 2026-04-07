import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent, TextareaComponent } from '../../components/input/input.component';
import { LoadingComponent } from '../../components/loading/loading.component';

@Component({
  selector: 'app-edit-expense',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, ButtonComponent, InputComponent, SelectComponent, TextareaComponent, LoadingComponent],
  templateUrl: './edit-expense.component.html',
  styleUrl: './edit-expense.component.css'
})
export class EditExpenseComponent implements OnInit {
  categories = signal<any[]>([]);
  paymentMethods = signal<any[]>([]);
  categoryOptions = computed(() => [
    { value: '', label: 'Select category' },
    ...this.categories().map(c => ({ value: c.id, label: c.name }))
  ]);
  paymentMethodOptions = computed(() => [
    { value: '', label: 'Select payment method' },
    ...this.paymentMethods().map(p => ({ value: String(p.id), label: p.name }))
  ]);
  loading = signal(false);
  pageLoading = signal(true);
  error = signal('');
  bookId = '';
  expenseId = '';

  private fb = inject(FormBuilder);
  private expenseService = inject(ExpenseService);
  private settingsService = inject(SettingsService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  form: FormGroup = this.fb.group({
    type: ['expense'],
    description: ['', Validators.required],
    amount: [null, [Validators.required, Validators.min(0.01)]],
    date: ['', Validators.required],
    category: [''],
    paymentMethod: [''],
    currency: ['USD'],
    notes: [''],
    isRecurring: [false],
    recurringFrequency: ['monthly'],
    recurringEndDate: [''],
  });

  ngOnInit() {
    this.route.parent?.params.subscribe(p => { this.bookId = p['bookId'] || ''; });
    this.route.params.subscribe(async p => {
      this.expenseId = p['id'] || '';
      await this.loadFilters();
      this.loadExpense();
    });
  }

  async loadFilters() {
    const [catsResult, methodsResult] = await Promise.allSettled([
      this.settingsService.getCategories(this.bookId),
      this.settingsService.getPaymentMethods(this.bookId)
    ]);
    if (catsResult.status === 'fulfilled' && catsResult.value.success)
      this.categories.set(catsResult.value.data || []);
    if (methodsResult.status === 'fulfilled' && methodsResult.value.success)
      this.paymentMethods.set(methodsResult.value.data || []);
  }

  async loadExpense() {
    this.pageLoading.set(true);
    try {
      const res = await this.expenseService.getExpense(this.bookId, this.expenseId);
      if (res.success && res.data) {
        const e = res.data;
        const catVal = typeof e.category === 'object' ? (e.category as any).id : (e.category || '');
        const pmVal = typeof e.paymentMethod === 'object' ? (e.paymentMethod as any).id : (e.paymentMethod || '');
        // Resolve name-based values to IDs for backward compatibility
        const resolvedCat = this.categories().find(c => c.id === catVal || c.name === catVal);
        const resolvedPm = this.paymentMethods().find(p => p.id === pmVal || p.name === pmVal);
        this.form.patchValue({
          type: e.type, description: e.description, amount: e.amount,
          date: e.date?.split('T')[0] || '',
          category: resolvedCat?.id || catVal,
          paymentMethod: resolvedPm?.id || pmVal,
          currency: e.currency || 'USD', notes: e.notes || '',
          isRecurring: !!(e as any).recurringConfig,
          recurringFrequency: (e as any).recurringConfig?.frequency || 'monthly',
          recurringEndDate: (e as any).recurringConfig?.endDate?.split('T')[0] || '',
        });
      } else { this.error.set('Failed to load expense'); }
    } catch (e: any) { this.error.set(e.message || 'Failed to load'); }
    finally { this.pageLoading.set(false); }
  }

  async handleSubmit() {
    this.error.set('');
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    const v = this.form.value;
    const payload: any = {
      type: v.type, description: v.description, amount: Number(v.amount),
      date: v.date, category: v.category, paymentMethod: v.paymentMethod,
      currency: v.currency, notes: v.notes,
    };
    if (v.isRecurring) {
      payload.recurringConfig = { frequency: v.recurringFrequency, endDate: v.recurringEndDate || undefined };
    } else { payload.recurringConfig = null; }
    try {
      const res = await this.expenseService.updateExpense(this.bookId, this.expenseId, payload);
      if (res.success) { this.toast.success(v.type === 'income' ? 'Income updated' : 'Expense updated'); this.goBack(); }
      else this.error.set(res.error || 'Failed to update');
    } catch (e: any) { this.error.set(e.message || 'Error'); }
    finally { this.loading.set(false); }
  }

  goBack() { this.router.navigate([`/${this.bookId}/expenses`]); }
}
