import { Component, OnInit, OnDestroy, inject, signal, computed, effect, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { DashboardService } from '../../services/dashboard.service';
import { AiChatService } from '../../services/ai-chat.service';
import { SettingsService } from '../../services/settings.service';
import { MemberService } from '../../services/member.service';
import { ToastService } from '../../services/toast.service';
import { DashboardSummary, DailyTransactionGroup, UpcomingPayment } from '../../models/dashboard.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { LoadingComponent, EmptyStateComponent } from '../../components/loading/loading.component';
import { DateRangePickerComponent } from '../../components/date-range-picker/date-range-picker.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { formatCurrency, formatCalendarDate, localDateString } from '../../utils/helpers';

const SESSION_KEY = (bookId: string) => `dashboard-filters-${bookId}`;

interface DashboardFilterState {
  dateStart: string;
  dateEnd:   string;
}

function defaultLast30Start(): string {
  const d = new Date();
  d.setDate(d.getDate() - 29);
  return localDateString(d) + 'T00:00:00Z';
}

const GRADIENT_COLORS = ['#6366f1', '#8b5cf6', '#ec4899', '#14b8a6', '#f59e0b', '#10b981', '#3b82f6', '#f97316', '#ef4444', '#84cc16'];

const BUCKET_COLORS: Record<string, string> = {
  need:          '#10b981',
  want:          '#f59e0b',
  debt:          '#ef4444',
  unclassified:  '#94a3b8',
};
const BUCKET_LABELS: Record<string, string> = {
  need: 'Needs', want: 'Wants', debt: 'Debt', unclassified: 'Unclassified',
};
const BUCKET_ORDER = ['need', 'want', 'debt', 'unclassified'] as const;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective, CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent, ButtonComponent, LoadingComponent, EmptyStateComponent, DateRangePickerComponent, ModalComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  summary = signal<DashboardSummary | null>(null);
  transactions = signal<DailyTransactionGroup[]>([]);
  upcomingPayments = signal<UpcomingPayment[]>([]);
  upcomingTotal = signal(0);
  upcomingPage = signal(1);
  loading = signal(true);
  upcomingLoading = signal(false);
  showMarkPaidModal = signal(false);
  selectedPayment = signal<UpcomingPayment | null>(null);
  markPaidLoading = false;

  dateStart = signal(defaultLast30Start());
  dateEnd = signal(localDateString() + 'T23:59:59Z');

  bookId = '';

  private dashboardService = inject(DashboardService);
  private settingsService = inject(SettingsService);
  private memberService = inject(MemberService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private aiChat = inject(AiChatService);
  private destroyRef = inject(DestroyRef);

  categories = signal<any[]>([]);

  protected readonly GRADIENT_COLORS = GRADIENT_COLORS;
  protected readonly BUCKET_COLORS = BUCKET_COLORS;
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDate = formatCalendarDate;
  protected readonly Math = Math;

  getCategoryColor(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.color || '#6366f1';
  }
  getCategoryIcon(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.icon || 'fa-solid fa-receipt';
  }
  getCategoryName(idOrName: string): string {
    const cat = this.categories().find(c => c.id === idOrName || c.name === idOrName);
    return cat?.name || idOrName || '—';
  }

  // ── Two-ring doughnut: outer = categories, inner = Needs/Wants/Debt buckets ──

  financialBuckets = computed(() => {
    const cats = this.summary()?.categoryBreakdown || [];
    const totals: Record<string, number> = { need: 0, want: 0, debt: 0, unclassified: 0 };
    for (const c of cats) {
      const key = (c as any).financialClass ?? 'unclassified';
      totals[key in totals ? key : 'unclassified'] += Number((c as any).amount ?? 0);
    }
    return BUCKET_ORDER.map(k => ({ key: k, label: BUCKET_LABELS[k], amount: totals[k], color: BUCKET_COLORS[k] }));
  });

  pieData = computed<ChartData<'doughnut'>>(() => {
    const cats  = this.summary()?.categoryBreakdown || [];
    const buckets = this.financialBuckets();
    return {
      labels: cats.map((c: any) => c.category || 'Uncategorized'),
      datasets: [
        // Outer ring — individual categories
        {
          data: cats.map((c: any) => Number(c.amount)),
          backgroundColor: GRADIENT_COLORS.slice(0, cats.length),
          borderWidth: 2, borderColor: 'transparent',
        },
        // Inner ring — financial buckets
        {
          data: buckets.map(b => b.amount),
          backgroundColor: buckets.map(b => b.color),
          borderWidth: 2, borderColor: 'transparent',
        },
      ],
    };
  });

  pieOptions: ChartOptions<'doughnut'> = {
    responsive: true, maintainAspectRatio: false,
    cutout: '40%',
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          // Inner ring (dataset 1) uses bucket labels; outer ring uses category labels.
          title: (items) => {
            const item = items[0];
            if (!item) return '';
            if (item.datasetIndex === 1) {
              const b = this.financialBuckets()[item.dataIndex];
              return b ? b.label : '';
            }
            return item.label || '';
          },
          label: (ctx) => {
            const currency = this.summary()?.currency;
            if (ctx.datasetIndex === 1) {
              // Title already shows the bucket name — just show the amount here.
              const b = this.financialBuckets()[ctx.dataIndex];
              return b ? ` ${formatCurrency(b.amount, currency)}` : '';
            }
            return ` ${ctx.label}: ${formatCurrency(ctx.parsed, currency)}`;
          },
        },
      },
    },
  };

  // ── Session filter persistence ──────────────────────────────────────────────

  private saveFilters() {
    const state: DashboardFilterState = { dateStart: this.dateStart(), dateEnd: this.dateEnd() };
    sessionStorage.setItem(SESSION_KEY(this.bookId), JSON.stringify(state));
  }

  private restoreFilters() {
    try {
      const raw = sessionStorage.getItem(SESSION_KEY(this.bookId));
      if (!raw) return;
      const state: DashboardFilterState = JSON.parse(raw);
      if (state.dateStart) this.dateStart.set(state.dateStart);
      if (state.dateEnd)   this.dateEnd.set(state.dateEnd);
    } catch { /* ignore malformed */ }
  }

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.restoreFilters();
      this.loadAll();
    });
    this.aiChat.dataChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadAll());
  }

  async loadAll() {
    this.loading.set(true);
    await Promise.all([this.loadCategories(), this.loadSummary(), this.loadTransactions(), this.loadUpcoming()]);
    this.loading.set(false);
  }

  async loadCategories() {
    try {
      const res = await this.memberService.getAccessibleCategories(this.bookId);
      if (res.success) this.categories.set(res.data || []);
    } catch {}
  }

  async loadSummary() {
    try {
      const res = await this.dashboardService.getSummary(this.bookId, this.dateStart(), this.dateEnd());
      if (res.success) this.summary.set(res.data || null);
    } catch {}
  }

  async loadTransactions() {
    try {
      const res = await this.dashboardService.getGroupedTransactions(this.bookId, this.dateStart(), this.dateEnd(), 1, 30);
      if (res.success) this.transactions.set((res.data as any) || []);
    } catch {}
  }

  async loadUpcoming() {
    this.upcomingLoading.set(true);
    try {
      const res = await this.dashboardService.getUpcomingPayments(this.bookId, 1, 10);
      if (res.success && res.data) {
        this.upcomingPayments.set(res.data.items || []);
        this.upcomingTotal.set(res.data.total || 0);
      }
    } catch {}
    finally { this.upcomingLoading.set(false); }
  }

  async loadMoreUpcoming() {
    const nextPage = this.upcomingPage() + 1;
    const res = await this.dashboardService.getUpcomingPayments(this.bookId, nextPage, 10);
    if (res.success && res.data) {
      this.upcomingPayments.update(p => [...p, ...(res.data!.items || [])]);
      this.upcomingPage.set(nextPage);
    }
  }

  onDateRangeChange(range: { start: string; end: string }) {
    this.dateStart.set(range.start);
    this.dateEnd.set(range.end);
    this.saveFilters();
    this.loadAll();
  }

  isOverdue(dueDate: string) { return new Date(dueDate) < new Date(); }

  openMarkPaidModal(p: UpcomingPayment) { this.selectedPayment.set(p); this.showMarkPaidModal.set(true); }
  closeMarkPaidModal() { this.showMarkPaidModal.set(false); this.selectedPayment.set(null); }

  async confirmMarkPaid() {
    const p = this.selectedPayment();
    if (!p) return;
    this.markPaidLoading = true;
    try {
      const res = await this.dashboardService.markUpcomingPaymentAsPaid(this.bookId, p.id);
      if (res.success) {
        this.upcomingPayments.update(list => list.filter(x => x.id !== p.id));
        this.toast.success('Payment marked as paid');
        this.closeMarkPaidModal();
      } else { this.toast.error(res.error || 'Failed'); }
    } catch (e: any) { this.toast.error(e.message); }
    finally { this.markPaidLoading = false; }
  }

  async generateUpcoming() {
    try {
      await this.dashboardService.generateUpcomingPayments(this.bookId);
      this.toast.success('Upcoming payments generated');
      this.loadUpcoming();
    } catch { this.toast.error('Failed to generate'); }
  }
}
