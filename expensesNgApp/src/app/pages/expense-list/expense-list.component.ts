import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExpenseService } from '../../services/expense.service';
import { ToastService } from '../../services/toast.service';
import { SettingsService } from '../../services/settings.service';
import { BookAccessService } from '../../services/book-access.service';
import { Expense } from '../../models/expense.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent } from '../../components/input/input.component';
import { LoadingComponent, EmptyStateComponent } from '../../components/loading/loading.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { DateRangePickerComponent } from '../../components/date-range-picker/date-range-picker.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { formatCurrency, formatDate } from '../../utils/helpers';

const SESSION_KEY = (bookId: string) => `expense-filters-${bookId}`;

interface FilterState {
  searchQuery: string;
  filterType: string;
  filterCategory: string;
  filterPayment: string;
  dateStart: string;
  dateEnd: string;
}

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, CardComponent, CardContentComponent, ButtonComponent, InputComponent, SelectComponent, LoadingComponent, EmptyStateComponent, ConfirmDialogComponent, DateRangePickerComponent, ModalComponent],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.css'
})
export class ExpenseListComponent implements OnInit {
  expenses = signal<Expense[]>([]);
  totalCount = signal(0);
  nextCursor = signal<string | null>(null);
  loading = signal(true);
  currentPage = signal(1);
  readonly pageSize = 50;
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  categories = signal<any[]>([]);
  paymentMethods = signal<any[]>([]);

  categoryOptions = computed(() => [
    { value: '', label: 'All Categories' },
    ...this.categories().map(c => ({ value: c.id, label: c.name }))
  ]);
  paymentMethodOptions = computed(() => [
    { value: '', label: 'All Payment Methods' },
    ...this.paymentMethods().map(p => ({ value: String(p.id), label: p.name }))
  ]);
  addCategoryOptions = computed(() => [
    { value: '', label: 'Select category' },
    ...this.categories().map(c => ({ value: c.id, label: c.name }))
  ]);
  addPaymentMethodOptions = computed(() => [
    { value: '', label: 'Select payment method' },
    ...this.paymentMethods().map(p => ({ value: String(p.id), label: p.name }))
  ]);

  searchQuery = signal('');
  filterType = signal('');
  filterCategory = signal('');
  filterPayment = signal('');
  dateStart = signal('');
  dateEnd = signal('');

  // Sort state
  sortField = signal<'date' | 'amount'>('date');
  sortDir = signal<'asc' | 'desc'>('desc');

  // Cursor stack: index 0 = first page cursor (undefined), each push = next page cursor
  private cursorStack: (string | undefined)[] = [undefined];

  showDeleteConfirm = signal(false);
  expenseToDelete = signal<Expense | null>(null);
  deleteLoading = false;
  bookId = '';

  // Add Expense Modal
  showAddModal = signal(false);
  addActiveTab = signal<'manual' | 'upload'>('manual');
  addLoading = signal(false);
  addError = signal('');
  addIsDragging = signal(false);
  addImportFile = signal<File | null>(null);
  addImportData = signal<any[]>([]);
  addImportErrors = signal<string[]>([]);
  addImportLoading = signal(false);

  // Add Category Sub-modal
  showAddCategoryModal = signal(false);
  newCatName = signal('');
  newCatIcon = signal('fa fa-tag');
  newCatColor = signal('#6366f1');
  addCatLoading = signal(false);
  addCatError = signal('');

  private searchTimeout: any;

  private expenseService = inject(ExpenseService);
  private settingsService = inject(SettingsService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  protected bookAccess = inject(BookAccessService);

  addForm: FormGroup = this.fb.group({
    type: ['expense'],
    description: ['', Validators.required],
    amount: [null, [Validators.required, Validators.min(0.01)]],
    date: [new Date().toISOString().split('T')[0], Validators.required],
    category: [''],
    paymentMethod: [''],
    currency: ['USD'],
    notes: [''],
    isRecurring: [false],
    recurringFrequency: ['monthly'],
  });

  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDate = formatDate;

  // ── Sort helpers ────────────────────────────────────────────────────────────

  isSortedBy(field: 'date' | 'amount'): boolean { return this.sortField() === field; }

  sortIcon(field: 'date' | 'amount'): string {
    if (this.sortField() !== field) return 'fa-solid fa-sort';
    return this.sortDir() === 'desc' ? 'fa-solid fa-sort-down' : 'fa-solid fa-sort-up';
  }

  toggleSort(field: 'date' | 'amount') {
    if (this.sortField() === field) {
      this.sortDir.set(this.sortDir() === 'desc' ? 'asc' : 'desc');
    } else {
      this.sortField.set(field);
      this.sortDir.set('desc');
    }
    this.resetCursors();
    this.loadExpenses();
  }

  // ── Session filter persistence ──────────────────────────────────────────────

  private saveFilters() {
    const state: FilterState = {
      searchQuery:    this.searchQuery(),
      filterType:     this.filterType(),
      filterCategory: this.filterCategory(),
      filterPayment:  this.filterPayment(),
      dateStart:      this.dateStart(),
      dateEnd:        this.dateEnd(),
    };
    sessionStorage.setItem(SESSION_KEY(this.bookId), JSON.stringify(state));
  }

  private restoreFilters() {
    try {
      const raw = sessionStorage.getItem(SESSION_KEY(this.bookId));
      if (!raw) return;
      const state: FilterState = JSON.parse(raw);
      this.searchQuery.set(state.searchQuery || '');
      this.filterType.set(state.filterType || '');
      this.filterCategory.set(state.filterCategory || '');
      this.filterPayment.set(state.filterPayment || '');
      this.dateStart.set(state.dateStart || '');
      this.dateEnd.set(state.dateEnd || '');
    } catch { /* ignore malformed */ }
  }

  // ── Cursor navigation ────────────────────────────────────────────────────────

  private resetCursors() {
    this.cursorStack = [undefined];
    this.currentPage.set(1);
  }

  private currentCursor(): string | undefined {
    return this.cursorStack[this.cursorStack.length - 1];
  }

  // ── Data loading ─────────────────────────────────────────────────────────────

  getCategoryName(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.name || idOrName || '—';
  }
  getCategoryColor(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.color || '#6366f1';
  }
  getCategoryIcon(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.icon || 'fa-solid fa-receipt';
  }
  getPaymentMethodName(idOrName: string): string {
    const pm = this.paymentMethods().find(p => String(p.id) === idOrName || p.name === idOrName);
    return pm?.name || idOrName || '—';
  }
  getStr(val: any): string { return typeof val === 'string' ? val : val?.id || val?.name || ''; }

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.restoreFilters();
      this.loadExpenses();
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

  async loadExpenses() {
    this.loading.set(true);
    try {
      const res = await this.expenseService.getExpenses(this.bookId, {
        search:        this.searchQuery() || undefined,
        type:          this.filterType() || undefined,
        category:      this.filterCategory() || undefined,
        paymentMethod: this.filterPayment() || undefined,
        startDate:     this.dateStart() || undefined,
        endDate:       this.dateEnd() || undefined,
        sortField:     this.sortField(),
        sortDir:       this.sortDir(),
        cursor:        this.currentCursor(),
        pageSize:      this.pageSize,
      });
      if (res.success && res.data) {
        this.expenses.set(res.data.items || []);
        this.totalCount.set(res.data.total ?? 0);
        this.nextCursor.set(res.data.nextCursor ?? null);
      }
    } catch (e: any) { this.toast.error(e.message || 'Failed to load expenses'); }
    finally { this.loading.set(false); }
  }

  onSearch(val: string) {
    this.searchQuery.set(val);
    this.saveFilters();
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => { this.resetCursors(); this.loadExpenses(); }, 400);
  }

  onFilterChange() {
    this.saveFilters();
    this.resetCursors();
    this.loadExpenses();
  }

  onDateRangeChange(range: { start: string; end: string }) {
    this.dateStart.set(range.start);
    this.dateEnd.set(range.end);
    this.saveFilters();
    this.resetCursors();
    this.loadExpenses();
  }

  goNext(nextCursor: string) {
    this.cursorStack.push(nextCursor);
    this.currentPage.update(p => p + 1);
    this.loadExpenses();
  }

  goPrev() {
    if (this.cursorStack.length > 1) this.cursorStack.pop();
    this.currentPage.update(p => Math.max(1, p - 1));
    this.loadExpenses();
  }

  // kept for template compatibility (delete/add reload resets to page 1)
  goToPage(_page: number) { this.resetCursors(); this.loadExpenses(); }

  openAddModal() {
    this.addForm.reset({
      type: 'expense',
      date: new Date().toISOString().split('T')[0],
      currency: 'USD',
      isRecurring: false,
      recurringFrequency: 'monthly',
    });
    this.addError.set('');
    this.addActiveTab.set('manual');
    this.addImportFile.set(null);
    this.addImportData.set([]);
    this.addImportErrors.set([]);
    this.showAddModal.set(true);
  }

  closeAddModal() { this.showAddModal.set(false); }

  openAddCategoryModal() {
    this.newCatName.set('');
    this.newCatIcon.set('fa fa-tag');
    this.newCatColor.set('#6366f1');
    this.addCatError.set('');
    this.showAddCategoryModal.set(true);
  }

  closeAddCategoryModal() { this.showAddCategoryModal.set(false); }

  async handleAddCategory() {
    if (!this.newCatName().trim()) { this.addCatError.set('Category name is required'); return; }
    this.addCatLoading.set(true);
    this.addCatError.set('');
    try {
      const res = await this.settingsService.createCategory(this.bookId, {
        name: this.newCatName().trim(),
        icon: this.newCatIcon() || 'fa fa-tag',
        color: this.newCatColor() || '#6366f1',
      });
      if (res.success && res.data) {
        this.categories.update(cats => [...cats, res.data]);
        this.addForm.get('category')?.setValue(res.data.id);
        this.toast.success('Category added');
        this.closeAddCategoryModal();
      } else {
        this.addCatError.set((res as any).error || 'Failed to create category');
      }
    } catch (e: any) {
      this.addCatError.set(e.message || 'Failed to create category');
    } finally {
      this.addCatLoading.set(false);
    }
  }

  async handleAddExpense() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.addLoading.set(true);
    this.addError.set('');
    try {
      const v = this.addForm.value;
      const payload: any = {
        type: v.type,
        description: v.description,
        amount: parseFloat(v.amount),
        date: new Date(v.date).toISOString(),
        category: v.category || undefined,
        paymentMethod: v.paymentMethod || undefined,
        currency: v.currency || 'USD',
        notes: v.notes || undefined,
        isRecurring: v.isRecurring,
      };
      if (v.isRecurring && v.recurringFrequency) {
        payload.recurringConfig = { frequency: v.recurringFrequency, startDate: new Date(v.date).toISOString(), endDate: null };
      }
      const res = await this.expenseService.createExpense(this.bookId, payload);
      if (res.success) {
        this.toast.success(v.type === 'income' ? 'Income added' : 'Expense added');
        this.closeAddModal();
        this.loadExpenses();
      } else {
        this.addError.set((res as any).error || `Failed to add ${v.type || 'transaction'}`);
      }
    } catch (e: any) {
      this.addError.set(e.message || 'Failed to add transaction');
    } finally {
      this.addLoading.set(false);
    }
  }

  onAddDragOver(e: DragEvent) { e.preventDefault(); this.addIsDragging.set(true); }
  onAddDragLeave() { this.addIsDragging.set(false); }
  onAddDrop(e: DragEvent) {
    e.preventDefault();
    this.addIsDragging.set(false);
    const file = e.dataTransfer?.files[0];
    if (file) this.processAddFile(file);
  }
  onAddFileInput(e: Event) {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.processAddFile(file);
  }
  processAddFile(file: File) {
    this.addImportFile.set(file);
    this.addImportErrors.set([]);
    const reader = new FileReader();
    reader.onload = (ev) => {
      const text = (ev.target as FileReader).result as string;
      const { data, errors } = this.parseExpenseCSV(text);
      this.addImportData.set(data);
      this.addImportErrors.set(errors);
    };
    reader.readAsText(file);
  }
  parseExpenseCSV(text: string): { data: any[]; errors: string[] } {
    const lines = text.trim().split('\n');
    if (lines.length < 2) return { data: [], errors: ['CSV file is empty or has no data rows'] };
    const headers = lines[0].split(',').map(h => h.trim().toLowerCase().replace(/[\"']/g, ''));
    const data: any[] = [];
    const errors: string[] = [];
    for (let i = 1; i < lines.length; i++) {
      if (!lines[i].trim()) continue;
      const row = lines[i].split(',').map(c => c.trim().replace(/^"|"$/g, ''));
      const obj: any = {};
      headers.forEach((h, j) => { obj[h] = row[j] || ''; });
      if (!obj['description']) { errors.push(`Row ${i}: missing description`); continue; }
      if (!obj['amount'] || isNaN(parseFloat(obj['amount']))) { errors.push(`Row ${i}: invalid amount`); continue; }
      if (!obj['date']) { errors.push(`Row ${i}: missing date`); continue; }
      data.push({
        description: obj['description'],
        amount: parseFloat(obj['amount']),
        date: obj['date'],
        category: obj['category'] || '',
        paymentMethod: obj['paymentmethod'] || obj['payment method'] || obj['payment_method'] || '',
        notes: obj['notes'] || '',
        type: obj['type'] || 'expense',
        currency: obj['currency'] || 'USD',
      });
    }
    return { data, errors };
  }
  async handleImportExpenses() {
    const rows = this.addImportData();
    if (!rows.length) return;
    this.addImportLoading.set(true);
    let success = 0, failed = 0;
    for (const row of rows) {
      try {
        const res = await this.expenseService.createExpense(this.bookId, { ...row, date: new Date(row.date).toISOString() });
        if (res.success) success++; else failed++;
      } catch { failed++; }
    }
    this.addImportLoading.set(false);
    if (success > 0) {
      this.toast.success(`Imported ${success} expense${success !== 1 ? 's' : ''}${failed > 0 ? ` (${failed} failed)` : ''}`);
      this.closeAddModal();
      this.loadExpenses();
    } else {
      this.addError.set(`All ${failed} row${failed !== 1 ? 's' : ''} failed to import`);
    }
  }
  downloadExpenseTemplate() {
    const csv = 'description,amount,date,category,paymentMethod,notes,type,currency\n' +
      'Coffee,4.50,2024-01-15,Food & Dining,Cash,Morning coffee,expense,USD\n' +
      'Salary,3000,2024-01-01,Income,Bank Transfer,Monthly salary,income,USD';
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'expense-template.csv'; a.click();
    URL.revokeObjectURL(url);
  }

  navigateToEdit(id: string) { this.router.navigate([`/${this.bookId}/expenses/${id}/edit`]); }

  openDeleteConfirm(e: Expense) { this.expenseToDelete.set(e); this.showDeleteConfirm.set(true); }

  async handleDelete() {
    const e = this.expenseToDelete();
    if (!e) return;
    this.deleteLoading = true;
    try {
      const res = await this.expenseService.deleteExpense(this.bookId, e.id);
      if (res.success) {
        this.expenses.update(list => list.filter(x => x.id !== e.id));
        this.totalCount.update(n => n - 1);
        this.toast.success('Expense deleted');
        this.showDeleteConfirm.set(false);
      } else { this.toast.error(res.error || 'Failed to delete'); }
    } catch (err: any) { this.toast.error(err.message); }
    finally { this.deleteLoading = false; }
  }
}
