import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BudgetService } from '../../services/budget.service';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';
import { Budget, BudgetVersion } from '../../models/budget.model';
import { CardComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { formatCurrency } from '../../utils/helpers';

interface BudgetRow {
  category: string;
  budget: Budget | null;
}

@Component({
  selector: 'app-budget',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent, CardContentComponent, ButtonComponent, ConfirmDialogComponent],
  templateUrl: './budget.component.html',
  styleUrl: './budget.component.css'
})
export class BudgetComponent implements OnInit {
  budgets = signal<Budget[]>([]);
  categories = signal<any[]>([]);
  loading = signal(true);
  bookId = '';

  filterMonthStr = signal(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}`
  );

  categorySearch = signal('');

  isEditable = computed(() => {
    const parts = this.filterMonthStr().split('-');
    const y = Number(parts[0]), m = Number(parts[1]);
    const now = new Date();
    return y > now.getFullYear() || (y === now.getFullYear() && m >= now.getMonth() + 1);
  });

  tableRows = computed<BudgetRow[]>(() => {
    const budgetMap = new Map(this.budgets().map(b => [b.category, b]));
    const q = this.categorySearch().toLowerCase().trim();
    return this.categories()
      .filter(c => !q || c.name.toLowerCase().includes(q))
      .map(c => ({ category: c.name, budget: budgetMap.get(c.name) ?? null }));
  });

  editingCategory = signal<string | null>(null);
  editAmount = signal<number>(0);
  editDate = signal('');
  saveLoading = signal<string | null>(null);

  expandedIds = signal<Set<string>>(new Set());

  showDeleteConfirm = signal(false);
  budgetToDelete = signal<Budget | null>(null);
  deleteLoading = false;

  readonly skeletonRows = Array.from({ length: 6 });
  protected readonly formatCurrency = formatCurrency;

  private budgetService = inject(BudgetService);
  private settingsService = inject(SettingsService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadData();
    });
  }

  async loadData() {
    this.loading.set(true);
    try {
      const [catRes, budgetRes] = await Promise.all([
        this.settingsService.getCategories(this.bookId),
        this.budgetService.getBudgets(this.bookId, this.filterMonthStr())
      ]);
      if (catRes.success) this.categories.set(catRes.data || []);
      if (budgetRes.success) this.budgets.set(budgetRes.data || []);
    } catch {
      this.toast.error('Failed to load data');
    } finally {
      this.loading.set(false);
    }
  }

  async loadBudgets() {
    try {
      const res = await this.budgetService.getBudgets(this.bookId, this.filterMonthStr());
      if (res.success) this.budgets.set(res.data || []);
    } catch {
      this.toast.error('Failed to load budgets');
    }
  }

  onFilterChange() {
    this.editingCategory.set(null);
    this.expandedIds.set(new Set());
    this.loadBudgets();
  }

  toggleExpand(id: string) {
    const s = new Set(this.expandedIds());
    if (s.has(id)) s.delete(id); else s.add(id);
    this.expandedIds.set(s);
  }

  isExpanded(id: string) { return this.expandedIds().has(id); }

  sortedVersions(b: Budget): BudgetVersion[] {
    if (!b.versions?.length) return [];
    return [...b.versions].sort((a, z) => z.versionNumber - a.versionNumber);
  }

  startEdit(row: BudgetRow) {
    this.editingCategory.set(row.category);
    this.editAmount.set(row.budget?.amount ?? 0);
    this.editDate.set(`${this.filterMonthStr()}-01`);
  }

  cancelEdit() { this.editingCategory.set(null); }

  async saveEdit(row: BudgetRow) {
    const amount = this.editAmount();
    const date = this.editDate();
    if (!amount || amount <= 0) { this.toast.error('Enter a valid amount'); return; }
    if (!date) { this.toast.error('Select an effective date'); return; }
    this.saveLoading.set(row.category);
    try {
      const res = await this.budgetService.upsertVersion(this.bookId, row.category, amount, date, this.filterMonthStr());
      if (res.success) {
        this.toast.success(row.budget ? 'Budget updated — new version created' : 'Budget saved');
        this.editingCategory.set(null);
        await this.loadBudgets();
      } else {
        this.toast.error((res as any).error || 'Failed to save');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to save');
    } finally {
      this.saveLoading.set(null);
    }
  }

  openDeleteConfirm(b: Budget) { this.budgetToDelete.set(b); this.showDeleteConfirm.set(true); }

  async handleDelete() {
    const b = this.budgetToDelete();
    if (!b) return;
    this.deleteLoading = true;
    try {
      const res = await this.budgetService.deleteBudget(this.bookId, b.id);
      if (res.success) {
        this.toast.success('Budget deleted');
        this.showDeleteConfirm.set(false);
        this.budgets.update(list => list.filter(x => x.id !== b.id));
      }
    } catch (e: any) {
      this.toast.error(e.message);
    } finally {
      this.deleteLoading = false;
    }
  }

  getProgressPct(b: Budget): number {
    if (!b.amount) return 0;
    return Math.min(100, Math.round(((b.spent || 0) / b.amount) * 100));
  }

  getProgressColor(pct: number): string {
    if (pct >= 100) return 'var(--color-danger)';
    if (pct >= 80) return 'var(--color-warning)';
    return 'var(--color-success)';
  }

  getRemaining(b: Budget): number { return Math.max(0, b.amount - (b.spent || 0)); }

  /** True when a real budget limit has been saved (has version history). Virtual/spending-only entries have no versions. */
  hasLimit(b: Budget | null): boolean { return !!(b?.versions?.length); }

  getEffectiveDateDisplay(b: Budget): string {
    if (!b.versions?.length) return '—';
    const parts = this.filterMonthStr().split('-');
    const y = Number(parts[0]), m = Number(parts[1]);
    const monthEnd = new Date(Date.UTC(y, m, 0, 23, 59, 59));
    const eff = b.versions
      .filter(v => new Date(v.effectiveDate) <= monthEnd)
      .sort((a, z) => z.versionNumber - a.versionNumber)[0];
    if (!eff) return '—';
    return new Date(eff.effectiveDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatVersionDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
