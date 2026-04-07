import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { SettingsService } from '../../services/settings.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent } from '../../components/input/input.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { LoadingComponent } from '../../components/loading/loading.component';

const FA_ICONS = [
  { icon: 'fa-solid fa-utensils', label: 'Food' },
  { icon: 'fa-solid fa-car', label: 'Transport' },
  { icon: 'fa-solid fa-bag-shopping', label: 'Shopping' },
  { icon: 'fa-solid fa-bolt', label: 'Utilities' },
  { icon: 'fa-solid fa-film', label: 'Entertainment' },
  { icon: 'fa-solid fa-heart-pulse', label: 'Health' },
  { icon: 'fa-solid fa-graduation-cap', label: 'Education' },
  { icon: 'fa-solid fa-house', label: 'Rent' },
  { icon: 'fa-solid fa-cart-shopping', label: 'Groceries' },
  { icon: 'fa-solid fa-plane', label: 'Travel' },
  { icon: 'fa-solid fa-gamepad', label: 'Gaming' },
  { icon: 'fa-solid fa-shirt', label: 'Clothing' },
  { icon: 'fa-solid fa-mobile-screen', label: 'Phone' },
  { icon: 'fa-solid fa-dumbbell', label: 'Fitness' },
  { icon: 'fa-solid fa-gift', label: 'Gifts' },
  { icon: 'fa-solid fa-baby', label: 'Kids' },
  { icon: 'fa-solid fa-paw', label: 'Pets' },
  { icon: 'fa-solid fa-briefcase', label: 'Work' },
  { icon: 'fa-solid fa-piggy-bank', label: 'Savings' },
  { icon: 'fa-solid fa-ellipsis', label: 'Other' },
];

const COLOR_OPTIONS = [
  '#ef4444', '#f97316', '#f59e0b', '#22c55e',
  '#10b981', '#14b8a6', '#3b82f6', '#6366f1',
  '#8b5cf6', '#ec4899', '#64748b', '#0ea5e9',
];

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, ButtonComponent, InputComponent, SelectComponent, ModalComponent, LoadingComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
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
  // Add form
  newCatName = signal('');
  newCatIcon = signal('fa-solid fa-tag');
  newCatColor = signal('#6366f1');
  // Inline edit
  editingCatId = signal<string | null>(null);
  editCatName = signal('');
  // Import
  importCatFile = signal<File | null>(null);
  importCatData = signal<Array<{ name: string; icon: string; color: string }>>([]);
  isDraggingCat = signal(false);

  readonly FA_ICONS = FA_ICONS;
  readonly COLOR_OPTIONS = COLOR_OPTIONS;

  private settingsService = inject(SettingsService);
  readonly themeService = inject(ThemeService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  generalForm: FormGroup = this.fb.group({
    defaultCurrency: ['USD'],
    monthlySavingsGoal: [null],
  });

  get catModalTitle() {
    return this.catView() === 'add' ? 'Add Expense Category' : 'Manage Categories';
  }

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadAll();
    });
  }

  async loadAll() {
    await Promise.all([this.loadCategories(), this.loadPaymentMethods(), this.loadGeneralSettings()]);
  }

  async loadGeneralSettings() {
    try {
      const res = await this.settingsService.getSettings(this.bookId);
      if (res.success && res.data) {
        this.generalForm.patchValue({ defaultCurrency: res.data.defaultCurrency || 'USD', monthlySavingsGoal: res.data.monthlySavingsGoal || null });
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
      if (res.success) this.toast.success('Settings saved');
      else this.toast.error(res.error || 'Failed to save');
    } catch (e: any) { this.toast.error(e.message); }
    finally { this.generalLoading = false; }
  }

  // ── Category Management ──
  openCategoryModal() {
    this.newCatName.set('');
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
    this.newCatIcon.set('fa-solid fa-tag');
    this.newCatColor.set('#6366f1');
    this.catActiveTab.set(tab);
    this.catView.set('add');
  }

  backToMainView() { this.catView.set('main'); this.catError.set(''); }

  startEditCat(cat: any) { this.editingCatId.set(cat.id); this.editCatName.set(cat.name); }
  cancelEditCat() { this.editingCatId.set(null); }

  async confirmUpdateCategory() {
    const id = this.editingCatId();
    if (!id) return;
    const cat = this.categories().find(c => c.id === id);
    if (!cat) return;
    this.catModalLoading.set(true);
    try {
      const res = await this.settingsService.updateCategory(this.bookId, id, { name: this.editCatName(), icon: cat.icon, color: cat.color });
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
      const res = await this.settingsService.createCategory(this.bookId, { name: this.newCatName(), icon: this.newCatIcon(), color: this.newCatColor() });
      if (res.success) {
        this.showCatSuccess('Category added successfully!');
        this.catView.set('main');
        await this.loadCategories();
      } else { this.catError.set(res.error || 'Failed to create category'); }
    } catch (e: any) { this.catError.set(e.message); }
    finally { this.catModalLoading.set(false); }
  }

  async handleDeleteCategory(cat: any) {
    if (!confirm(`Delete category "${cat.name}"?`)) return;
    this.catModalLoading.set(true);
    this.catError.set('');
    try {
      const res = await this.settingsService.deleteCategory(this.bookId, cat.id);
      if (res.success) { this.showCatSuccess('Category deleted'); await this.loadCategories(); }
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
      const res = await this.settingsService.createPaymentMethod(this.bookId, { name: this.newPaymentName, icon: this.newPaymentIcon || '💳' });
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

