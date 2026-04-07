import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';
import { CurrentBookService } from '../../services/current-book.service';
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

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadFilters();
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
