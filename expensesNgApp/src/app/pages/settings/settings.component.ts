import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SettingsService } from '../../services/settings.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';
import { ExpenseBookService } from '../../services/expense-book.service';
import { CurrentBookService } from '../../services/current-book.service';
import { AuthStateService } from '../../services/auth-state.service';
import { UpgradeModalService } from '../../services/upgrade-modal.service';
import { ApiService } from '../../services/api.service';
import { ApiResponse } from '../../models/user.model';
import { firstValueFrom } from 'rxjs';

interface UsageDto {
  categoriesUsed: number;
  categoriesLimit: number;
}
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent } from '../../components/input/input.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { LoadingComponent } from '../../components/loading/loading.component';
import { CATEGORY_FA_ICONS, CATEGORY_COLOR_OPTIONS } from '../../utils/helpers';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, ButtonComponent, InputComponent, SelectComponent, ModalComponent, LoadingComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  authState    = inject(AuthStateService);
  upgradeModal = inject(UpgradeModalService);

  get userPlan(): string { return this.authState.user()?.plan ?? 'Free'; }

  // Per-book count from loaded list (Starter only — 50/book limit)
  get nonDefaultCategoryCount(): number {
    return this.categories().filter((c: any) => !c.isDefault).length;
  }

  get atCategoryLimit(): boolean {
    if (this.userPlan === 'Starter') return this.nonDefaultCategoryCount >= 50;
    if (this.userPlan === 'Free') {
      const u = this.usage();
      return !!u && u.categoriesLimit > 0 && u.categoriesUsed >= u.categoriesLimit;
    }
    return false; // Pro: unlimited
  }

  get categoryUsagePct(): number {
    if (this.userPlan === 'Starter')
      return Math.min(100, Math.round((this.nonDefaultCategoryCount / 50) * 100));
    return 0;
  }

  get categoryUsageBarClass(): string {
    const pct = this.categoryUsagePct;
    if (pct >= 90) return 'cat-bar-danger';
    if (pct >= 70) return 'cat-bar-warning';
    return 'cat-bar-ok';
  }

  readonly planLimits: Record<string, { books: string; expenses: string; categories: string; credits: string }> = {
    Free:    { books: '3',         expenses: '150 / month', categories: '20',        credits: '15 (one-time trial)' },
    Starter: { books: 'Unlimited', expenses: '1,000 / month', categories: '50',      credits: '50 / month' },
    Pro:     { books: 'Unlimited', expenses: 'Unlimited',    categories: 'Unlimited', credits: '150 / month' },
  };

  usage = signal<UsageDto | null>(null);
  categories = signal<any[]>([]);
  paymentMethods = signal<any[]>([]);
  categoriesLoading = signal(false);
  paymentLoading = signal(false);
  generalLoading = false;
  bookId = '';

  // Payment Method Modal
  showPaymentModal = signal(false);
  addPaymentLoading = false;
  newPaymentName = '';
  newPaymentIcon = '';

  // Expense CSV Import
  csvFile = signal<File | null>(null);
  csvFileName = signal('');
  importResult = signal<any>(null);
  importLoading = false;

  // ── Category Management Modal ──
  showCategoryModal = signal(false);
  catView = signal<'main' | 'add'>('main');
  catActiveTab = signal<'manual' | 'upload'>('manual');
  catError = signal('');
  catSuccess = signal('');
  catModalLoading = signal(false);
  classifyLoading = signal(false);
  // Add form
  newCatName = signal('');
  newCatType = signal<'expense' | 'income' | 'both'>('expense');
  newCatIcon = signal('fa-solid fa-tag');
  newCatColor = signal('#6366f1');
  // Inline edit
  editingCatId = signal<string | null>(null);
  editCatName  = signal('');
  editCatIcon  = signal('');
  editCatColor = signal('#6366f1');
  editCatType  = signal<'expense' | 'income'>('expense');
  // Import
  importCatFile = signal<File | null>(null);
  importCatData = signal<Array<{ name: string; icon: string; color: string }>>([]);
  isDraggingCat = signal(false);

  readonly FA_ICONS = CATEGORY_FA_ICONS;
  readonly COLOR_OPTIONS = CATEGORY_COLOR_OPTIONS;

  private settingsService = inject(SettingsService);
  readonly themeService = inject(ThemeService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private bookService = inject(ExpenseBookService);
  readonly currentBook = inject(CurrentBookService);
  private api = inject(ApiService);

  aiChatEnabled = signal(false);
  aiChatLoading = signal(false);

  generalForm: FormGroup = this.fb.group({
    defaultCurrency: ['USD'],
    monthlySavingsGoal: [null],
  });

  get catModalTitle() {
    if (this.catView() !== 'add') return 'Manage Categories';
    const t = this.newCatType();
    if (t === 'income') return 'Add Income Category';
    if (t === 'both')   return 'Add Income & Expense Category';
    return 'Add Expense Category';
  }

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadAll();
    });
  }

  async loadAll() {
    this.aiChatEnabled.set(this.currentBook.book()?.aiChatEnabled ?? false);
    await Promise.all([
      this.loadCategories(),
      this.loadPaymentMethods(),
      this.loadGeneralSettings(),
      this.loadUsage(),
    ]);
  }

  async loadUsage() {
    try {
      const res = await firstValueFrom(this.api.get<ApiResponse<UsageDto>>('/usage'));
      if (res.success && res.data) this.usage.set(res.data);
    } catch {}
  }

  get canModifyBook(): boolean {
    const role = this.currentBook.book()?.memberRole;
    // null = owner; 'admin' also has CanModifyBook
    return role === null || role === undefined || role === 'admin';
  }

  async toggleAiChat(enabled: boolean): Promise<void> {
    if (!this.canModifyBook || this.aiChatLoading()) return;
    this.aiChatLoading.set(true);
    try {
      const res = await this.bookService.updateAiChat(this.bookId, enabled);
      if (res.success && res.data) {
        this.aiChatEnabled.set(res.data.aiChatEnabled ?? enabled);
        const current = this.currentBook.book();
        if (current) this.currentBook.setBook({ ...current, aiChatEnabled: res.data.aiChatEnabled ?? enabled });
        this.toast.success(`AI Chat ${enabled ? 'enabled' : 'disabled'}`);
      } else {
        this.toast.error(res.error || 'Failed to update AI Chat setting');
      }
    } catch (e: any) {
      this.toast.error(e?.error?.error ?? 'Failed to update AI Chat setting');
    } finally {
      this.aiChatLoading.set(false);
    }
  }

  async loadGeneralSettings() {
    try {
      const res = await this.settingsService.getSettings(this.bookId);
      if (res.success && res.data) {
        this.generalForm.patchValue({ defaultCurrency: res.data.defaultCurrency ?? 'USD', monthlySavingsGoal: res.data.monthlySavingsGoal ?? null });
      }
    } catch {}
  }

  async loadCategories() {
    this.categoriesLoading.set(true);
    try {
      const res = await this.settingsService.getCategories(this.bookId);
      if (res.success) this.categories.set(res.data || []);
    } catch {}
    finally { this.categoriesLoading.set(false); }
  }

  async loadPaymentMethods() {
    this.paymentLoading.set(true);
    try {
      const res = await this.settingsService.getPaymentMethods(this.bookId);
      if (res.success) this.paymentMethods.set(res.data || []);
    } catch {}
    finally { this.paymentLoading.set(false); }
  }

  async saveGeneral() {
    this.generalLoading = true;
    try {
      const res = await this.settingsService.updateSettings(this.bookId, this.generalForm.value);
      if (res.success) {
        this.toast.success('Settings saved');
        // Reflect currency change immediately in the current book signal
        const current = this.currentBook.book();
        if (current && res.data?.defaultCurrency) {
          this.currentBook.setBook({ ...current, currency: res.data.defaultCurrency });
        }
      } else {
        this.toast.error(res.error || 'Failed to save');
      }
    } catch (e: any) { this.toast.error(e.message); }
    finally { this.generalLoading = false; }
  }

  // ── Category Management ──
  openCategoryModal() {
    this.newCatName.set('');
    this.newCatType.set('expense');
    this.newCatIcon.set('fa-solid fa-tag');
    this.newCatColor.set('#6366f1');
    this.catView.set('main');
    this.catActiveTab.set('manual');
    this.catError.set('');
    this.catSuccess.set('');
    this.editingCatId.set(null);
    this.importCatFile.set(null);
    this.importCatData.set([]);
    this.showCategoryModal.set(true);
  }

  closeCategoryModal() { this.showCategoryModal.set(false); }

  openCatAddView(tab: 'manual' | 'upload') {
    this.catError.set('');
    this.newCatName.set('');
    this.newCatType.set('expense');
    this.newCatIcon.set('fa-solid fa-tag');
    this.newCatColor.set('#6366f1');
    this.catActiveTab.set(tab);
    this.catView.set('add');
  }

  backToMainView() { this.catView.set('main'); this.catError.set(''); }

  startEditCat(cat: any) {
    this.editingCatId.set(cat.id);
    this.editCatName.set(cat.name);
    this.editCatIcon.set(cat.icon  || 'fa-solid fa-tag');
    this.editCatColor.set(cat.color || '#6366f1');
    this.editCatType.set(cat.type === 'income' ? 'income' : 'expense');
  }
  cancelEditCat() { this.editingCatId.set(null); }

  async updateFinancialClass(catId: string, value: string) {
    const payload = value
      ? { financialClass: value }
      : { financialClass: '', clearFinancialClass: true };
    try {
      const res = await this.settingsService.updateCategory(this.bookId, catId, payload);
      if (res.success) {
        this.categories.update(cats => cats.map(c => c.id === catId ? { ...c, financialClass: value || null } : c));
      }
    } catch { /* silent */ }
  }

  async handleBulkClassify() {
    this.classifyLoading.set(true);
    try {
      const res = await this.settingsService.bulkClassifyCategories(this.bookId);
      if (res.success && res.data) {
        const { classified, usedCredit, freeUsed, freeQuota } = res.data;
        const creditNote = usedCredit
          ? '(1 credit used)'
          : `(${freeUsed}/${freeQuota} free uses)`;
        const msg = classified > 0
          ? `${classified} categor${classified === 1 ? 'y' : 'ies'} classified ${creditNote}`
          : `All categories already classified ${creditNote}`;
        this.toast.success(msg);
        if (classified > 0) await this.loadCategories();
      } else {
        this.toast.error(res.error || 'Classification failed');
      }
    } catch (e: any) {
      const status = e?.status;
      if (status === 402) {
        this.toast.error(e?.error?.error ?? 'No credits remaining. Purchase credits to continue using Auto-classify.');
      } else {
        this.toast.error(e?.error?.error ?? e?.message ?? 'Classification failed');
      }
    } finally {
      this.classifyLoading.set(false);
    }
  }

  async confirmUpdateCategory() {
    const id = this.editingCatId();
    if (!id) return;
    const cat = this.categories().find(c => c.id === id);
    if (!cat) return;
    this.catModalLoading.set(true);
    try {
      const res = await this.settingsService.updateCategory(this.bookId, id, { name: this.editCatName(), icon: this.editCatIcon() || cat.icon, color: this.editCatColor(), type: this.editCatType() });
      if (res.success) { this.editingCatId.set(null); this.showCatSuccess('Category updated!'); await this.loadCategories(); }
      else this.catError.set(res.error || 'Failed to update');
    } catch (e: any) { this.catError.set(e.message); }
    finally { this.catModalLoading.set(false); }
  }

  async handleAddCategory() {
    if (!this.newCatName().trim()) { this.catError.set('Category name is required'); return; }
    this.catModalLoading.set(true);
    this.catError.set('');
    try {
      const res = await this.settingsService.createCategory(this.bookId, { name: this.newCatName(), type: this.newCatType(), icon: this.newCatIcon(), color: this.newCatColor() });
      if (res.success) {
        this.showCatSuccess('Category added successfully!');
        this.catView.set('main');
        await Promise.all([this.loadCategories(), this.loadUsage()]);
      } else { this.catError.set(res.error || 'Failed to create category'); }
    } catch (e: any) { this.catError.set(e?.error?.error ?? e?.error?.message ?? e?.message ?? 'Failed to create category'); }
    finally { this.catModalLoading.set(false); }
  }

  async handleDeleteCategory(cat: any) {
    if (!confirm(`Delete category "${cat.name}"?`)) return;
    this.catModalLoading.set(true);
    this.catError.set('');
    try {
      const res = await this.settingsService.deleteCategory(this.bookId, cat.id);
      if (res.success) { this.showCatSuccess('Category deleted'); await Promise.all([this.loadCategories(), this.loadUsage()]); }
      else this.catError.set(res.error || 'Failed to delete');
    } catch (e: any) { this.catError.set(e.message || 'Cannot delete this category'); }
    finally { this.catModalLoading.set(false); }
  }

  private showCatSuccess(msg: string) {
    this.catSuccess.set(msg);
    setTimeout(() => this.catSuccess.set(''), 3000);
  }

  // ── Category CSV Import ──
  handleCatFileInput(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.processCatFile(file);
  }

  processCatFile(file: File) {
    if (!file.name.endsWith('.csv')) { this.catError.set('Please upload a CSV file'); return; }
    this.importCatFile.set(file);
    const reader = new FileReader();
    reader.onload = (e) => this.parseCatCSV(e.target?.result as string);
    reader.readAsText(file);
  }

  parseCatCSV(text: string) {
    const lines = text.trim().split('\n');
    if (lines.length < 2) { this.catError.set('CSV must have a header and at least one data row'); return; }
    if (!lines[0].toLowerCase().includes('name')) { this.catError.set('CSV must have a "name" column'); return; }
    const parsed: Array<{ name: string; icon: string; color: string }> = [];
    for (const line of lines.slice(1)) {
      const parts = line.split(',').map(s => s.trim().replace(/^"|"$/g, ''));
      const [name, icon, color] = parts;
      if (name) parsed.push({ name, icon: icon || 'fa-solid fa-tag', color: color || '#6366f1' });
    }
    if (!parsed.length) { this.catError.set('No valid data found in CSV'); return; }
    this.importCatData.set(parsed);
    this.catError.set('');
  }

  onCatDragOver(event: DragEvent) { event.preventDefault(); this.isDraggingCat.set(true); }
  onCatDragLeave(event: DragEvent) { event.preventDefault(); this.isDraggingCat.set(false); }
  onCatDrop(event: DragEvent) {
    event.preventDefault();
    this.isDraggingCat.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.processCatFile(file);
  }

  removeImportFile() { this.importCatFile.set(null); this.importCatData.set([]); }

  downloadCatTemplate() {
    const csv = 'name,icon,color\nFood & Dining,fa-solid fa-utensils,#ef4444\nTransport,fa-solid fa-car,#3b82f6\nShopping,fa-solid fa-bag-shopping,#8b5cf6';
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'categories_template.csv'; a.click();
    URL.revokeObjectURL(url);
  }

  async handleImportCategories() {
    const data = this.importCatData();
    if (!data.length) { this.catError.set('No data to import'); return; }
    this.catModalLoading.set(true);
    this.catError.set('');
    try {
      const res = await this.settingsService.importCategories(this.bookId, data);
      if (res.success && res.data) {
        const msg = `Imported ${res.data.imported} categories` + (res.data.failed > 0 ? `, ${res.data.failed} failed` : '');
        this.showCatSuccess(msg);
        this.importCatFile.set(null);
        this.importCatData.set([]);
        this.catView.set('main');
        await this.loadCategories();
      } else { this.catError.set(res.error || 'Import failed'); }
    } catch (e: any) { this.catError.set(e.message || 'Import failed'); }
    finally { this.catModalLoading.set(false); }
  }

  // ── Payment Methods ──
  openPaymentModal() { this.newPaymentName = ''; this.newPaymentIcon = ''; this.showPaymentModal.set(true); }

  async handleAddPayment() {
    if (!this.newPaymentName.trim()) return;
    this.addPaymentLoading = true;
    try {
      const res = await this.settingsService.createPaymentMethod(this.bookId, { name: this.newPaymentName, icon: this.newPaymentIcon || 'fa fa-credit-card' });
      if (res.success) { this.toast.success('Payment method added'); this.loadPaymentMethods(); this.showPaymentModal.set(false); }
      else this.toast.error(res.error || 'Failed');
    } catch (e: any) { this.toast.error(e.message); }
    finally { this.addPaymentLoading = false; }
  }

  async deletePaymentMethod(id: string) {
    try {
      const res = await this.settingsService.deletePaymentMethod(this.bookId, id);
      if (res.success) { this.paymentMethods.update(p => p.filter(x => (x.id || x.name) !== id)); this.toast.success('Payment method removed'); }
      else this.toast.error(res.error || 'Failed');
    } catch (e: any) { this.toast.error(e.message); }
  }

  // ── Expense CSV Import ──
  onCSVSelect(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) { this.csvFile.set(file); this.csvFileName.set(file.name); this.importResult.set(null); }
  }

  async handleImport() {
    const file = this.csvFile();
    if (!file) return;
    this.importLoading = true;
    try {
      const formData = new FormData();
      formData.append('file', file);
      const preview = await this.settingsService.importCSV(this.bookId, formData);
      if (preview.success && preview.data) {
        const confirmImport = await this.settingsService.confirmImport(this.bookId, preview.data);
        if (confirmImport.success) { this.importResult.set(confirmImport.data); this.toast.success('Import complete'); }
        else this.toast.error(confirmImport.error || 'Import failed');
      } else this.toast.error(preview.error || 'Failed to parse CSV');
    } catch (e: any) { this.toast.error(e.message || 'Import error'); }
    finally { this.importLoading = false; }
  }
}

