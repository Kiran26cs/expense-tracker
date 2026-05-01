import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { ExpenseService } from '../../services/expense.service';
import { DashboardService } from '../../services/dashboard.service';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';
import { Expense } from '../../models/expense.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { LoadingComponent, EmptyStateComponent } from '../../components/loading/loading.component';
import { DateRangePickerComponent } from '../../components/date-range-picker/date-range-picker.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { formatCurrency, formatDate } from '../../utils/helpers';

@Component({
  selector: 'app-insights',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, BaseChartDirective, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, LoadingComponent, EmptyStateComponent, DateRangePickerComponent, ModalComponent, ButtonComponent, ConfirmDialogComponent],
  templateUrl: './insights.component.html',
  styleUrl: './insights.component.css'
})
export class InsightsComponent implements OnInit {
  loading = signal(true);
  expenses = signal<Expense[]>([]);
  recurring = signal<any[]>([]);
  categories = signal<any[]>([]);
  currency = signal('USD');
  dateStart = signal(new Date(new Date().getFullYear(), new Date().getMonth() - 5, 1).toISOString().split('T')[0]);
  dateEnd = signal(new Date().toISOString().split('T')[0]);
  bookId = '';

  // Add Recurring Modal
  showAddRecurringModal = signal(false);
  recurringLoading = signal(false);
  recurringError = signal('');
  recurringCategories = signal<any[]>([]);
  recurringPaymentMethods = signal<any[]>([]);

  // Delete Recurring
  showDeleteRecurringConfirm = signal(false);
  recurringToDelete = signal<any>(null);
  deleteRecurringLoading = false;

  // Loan Calculator
  loanTotalValue = signal(0);
  loanROI = signal(18);
  loanDownPaymentPct = signal(10);
  loanYears = signal(2);

  loanDownPaymentAmt  = computed(() => this.loanTotalValue() * (this.loanDownPaymentPct() / 100));
  loanPrincipal       = computed(() => Math.max(0, this.loanTotalValue() - this.loanDownPaymentAmt()));
  loanMonthlyRate     = computed(() => this.loanROI() / 100 / 12);
  loanMonths          = computed(() => this.loanYears() * 12);
  loanEMI = computed(() => {
    const P = this.loanPrincipal(), r = this.loanMonthlyRate(), n = this.loanMonths();
    if (!P || !n) return 0;
    if (r === 0) return P / n;
    return (P * r * Math.pow(1 + r, n)) / (Math.pow(1 + r, n) - 1);
  });
  loanTotalPayment  = computed(() => this.loanEMI() * this.loanMonths());
  loanTotalInterest = computed(() => this.loanTotalPayment() - this.loanPrincipal());
  loanInterestPct   = computed(() => {
    const p = this.loanPrincipal();
    return p > 0 ? Math.round((this.loanTotalInterest() / p) * 100) : 0;
  });

  // EMI Affordability indicator (expense+EMI / income * 100)
  loanAffordabilityRatio = computed(() => {
    const inc = this.avgMonthlyIncome();
    if (!inc || !this.loanEMI()) return null;
    return ((this.avgMonthlyExpenses() + this.loanEMI()) / inc) * 100;
  });
  loanAffordability = computed(() => {
    const r = this.loanAffordabilityRatio();
    if (r === null) return null;
    if (r >= 50) return { level: 'high',   icon: 'fa-solid fa-face-sad-tear',    color: '#ef4444', label: 'High Risk',    tooltip: `Your expense-to-income ratio would be ${r.toFixed(1)}% — this EMI is likely unaffordable. Consider a longer tenure or a higher down payment.` };
    if (r >= 40) return { level: 'medium', icon: 'fa-solid fa-face-meh',         color: '#f59e0b', label: 'Medium Risk',  tooltip: `Your expense-to-income ratio would be ${r.toFixed(1)}% — this EMI is manageable but stretches your budget.` };
    return             { level: 'low',    icon: 'fa-solid fa-face-smile-beam',   color: '#10b981', label: 'Comfortable', tooltip: `Your expense-to-income ratio would be ${r.toFixed(1)}% — this EMI fits comfortably within your income.` };
  });

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDate = formatDate;

  private expenseService = inject(ExpenseService);
  private settingsService = inject(SettingsService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  addRecurringForm: FormGroup = this.fb.group({
    description: ['', Validators.required],
    amount: [null, [Validators.required, Validators.min(0.01)]],
    frequency: ['monthly', Validators.required],
    startDate: [new Date().toISOString().split('T')[0], Validators.required],
    category: [''],
    paymentMethod: [''],
    endDate: [''],
  });

  avgMonthlyExpenses = computed(() => {
    const total = this.expenses().filter(e => e.type === 'expense').reduce((s, e) => s + e.amount, 0);
    return total / 6;
  });
  avgMonthlyIncome = computed(() => {
    const total = this.expenses().filter(e => e.type === 'income').reduce((s, e) => s + e.amount, 0);
    return total / 6;
  });
  savingsRate = computed(() => {
    const exp = this.avgMonthlyExpenses(); const inc = this.avgMonthlyIncome();
    if (!inc) return 0;
    return Math.round(((inc - exp) / inc) * 100);
  });
  topCategories = computed(() => {
    const map = new Map<string, number>();
    this.expenses().filter(e => e.type === 'expense').forEach(e => {
      const raw = typeof e.category === 'string' ? e.category : (e.category as any)?.name || '';
      const name = this.getCatName(raw);
      map.set(name, (map.get(name) || 0) + e.amount);
    });
    const total = Array.from(map.values()).reduce((s, v) => s + v, 0);
    return Array.from(map.entries()).sort((a, b) => b[1] - a[1]).slice(0, 8).map(([name, t]) => ({ name, total: t, percent: total ? Math.round((t / total) * 100) : 0 }));
  });
  recurringTotal = computed(() => this.recurring().reduce((s, r) => s + r.amount, 0));

  trendData = computed<ChartData<'line'>>(() => {
    const byMonth = new Map<string, { income: number; expense: number }>();
    this.expenses().forEach(e => {
      const m = e.date?.substring(0, 7) || '';
      if (!byMonth.has(m)) byMonth.set(m, { income: 0, expense: 0 });
      const v = byMonth.get(m)!;
      if (e.type === 'income') v.income += e.amount; else v.expense += e.amount;
    });
    const sorted = Array.from(byMonth.entries()).sort((a, b) => a[0].localeCompare(b[0]));
    return {
      labels: sorted.map(([m]) => { try { return new Date(m + '-01').toLocaleString('default', { month: 'short', year: '2-digit' }); } catch { return m; } }),
      datasets: [
        { label: 'Expenses', data: sorted.map(([, v]) => v.expense), borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,0.1)', fill: true, tension: 0.4 },
        { label: 'Income', data: sorted.map(([, v]) => v.income), borderColor: '#10b981', backgroundColor: 'rgba(16,185,129,0.1)', fill: true, tension: 0.4 }
      ]
    };
  });

  lineOptions: ChartOptions<'line'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { position: 'top' } },
    scales: { x: { grid: { display: false } }, y: { beginAtZero: true } }
  };

  ngOnInit() {
    this.route.parent?.params.subscribe(p => { this.bookId = p['bookId'] || ''; this.loadAll(); });
  }

  async loadAll() {
    this.loading.set(true);
    await Promise.all([this.loadExpenses(), this.loadRecurring(), this.loadCategories(), this.loadSettings()]);
    this.loading.set(false);
  }

  async loadSettings() {
    try {
      const res = await this.settingsService.getSettings(this.bookId);
      if (res.success && res.data?.defaultCurrency) {
        this.currency.set(res.data.defaultCurrency);
      }
    } catch {}
  }

  async loadCategories() {
    try {
      const res = await this.settingsService.getCategories(this.bookId);
      if (res.success) this.categories.set(res.data || []);
    } catch {}
  }

  async loadExpenses() {
    try {
      const res = await this.expenseService.getExpenses(this.bookId, { pageSize: 500, startDate: this.dateStart(), endDate: this.dateEnd() });
      if (res.success && res.data) {
        const items = (res.data as any).items || (Array.isArray(res.data) ? res.data : []);
        this.expenses.set(items);
        // currency is loaded from settings, not expense records
      }
    } catch {}
  }

  async loadRecurring() {
    try {
      const res = await this.expenseService.getRecurringExpenses(this.bookId, { page: 1, limit: 50 });
      if (res.success && res.data) {
        const items = (res.data as any).items || (Array.isArray(res.data) ? res.data : []);
        this.recurring.set(items);
      }
    } catch {}
  }

  async openAddRecurringModal() {
    this.addRecurringForm.reset({
      frequency: 'monthly',
      startDate: new Date().toISOString().split('T')[0],
    });
    this.recurringError.set('');
    const [catsRes, methodsRes] = await Promise.allSettled([
      this.settingsService.getCategories(this.bookId),
      this.settingsService.getPaymentMethods(this.bookId),
    ]);
    if (catsRes.status === 'fulfilled' && catsRes.value.success)
      this.recurringCategories.set(catsRes.value.data || []);
    if (methodsRes.status === 'fulfilled' && methodsRes.value.success)
      this.recurringPaymentMethods.set(methodsRes.value.data || []);
    this.showAddRecurringModal.set(true);
  }

  async handleAddRecurring() {
    if (this.addRecurringForm.invalid) { this.addRecurringForm.markAllAsTouched(); return; }
    this.recurringLoading.set(true);
    this.recurringError.set('');
    const v = this.addRecurringForm.value;
    try {
      const res = await this.expenseService.createExpense(this.bookId, {
        description: v.description,
        amount: Number(v.amount),
        date: v.startDate,
        category: v.category || '',
        paymentMethod: v.paymentMethod || '',
        isRecurring: true,
        recurringConfig: {
          frequency: v.frequency,
          startDate: v.startDate,
          endDate: v.endDate || null,
        },
      });
      if (res.success) {
        this.toast.success('Recurring expense added');
        this.showAddRecurringModal.set(false);
        await this.loadRecurring();
      } else {
        this.recurringError.set((res as any).error || 'Failed to save');
      }
    } catch (e: any) {
      this.recurringError.set(e.message || 'Error saving recurring expense');
    } finally {
      this.recurringLoading.set(false);
    }
  }

  openDeleteRecurringConfirm(r: any) {
    this.recurringToDelete.set(r);
    this.showDeleteRecurringConfirm.set(true);
  }

  async handleDeleteRecurring() {
    const r = this.recurringToDelete();
    if (!r) return;
    this.deleteRecurringLoading = true;
    try {
      const res = await this.expenseService.deleteRecurringExpense(r.id);
      if (res.success) {
        this.toast.success('Recurring expense deleted');
        this.showDeleteRecurringConfirm.set(false);
        this.recurring.update(list => list.filter(x => x.id !== r.id));
      } else {
        this.toast.error((res as any).error || 'Failed to delete');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Error');
    } finally {
      this.deleteRecurringLoading = false;
    }
  }

  getFrequency(r: any): string {
    return r?.recurringConfig?.frequency || r?.frequency || 'monthly';
  }

  getCatName(idOrName: string): string {
    if (!idOrName) return 'Other';
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.name || idOrName;
  }

  getNextOccurrence(r: any): string {
    if (!r?.nextOccurrence) return '';
    return formatDate(r.nextOccurrence);
  }

  getDueBadge(r: any): { label: string; cls: string } {
    if (!r?.nextOccurrence) return { label: '', cls: '' };
    const days = Math.floor((new Date(r.nextOccurrence).getTime() - Date.now()) / 86400000);
    if (days < 0) return { label: 'Overdue', cls: 'badge-overdue' };
    if (days <= 3) return { label: 'Due Soon', cls: 'badge-due-soon' };
    return { label: `In ${days}d`, cls: 'badge-upcoming' };
  }

  onDateRangeChange(range: { start: string; end: string }) {
    this.dateStart.set(range.start);
    this.dateEnd.set(range.end);
    this.loadAll();
  }
}
