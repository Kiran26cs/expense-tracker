import { Component, OnInit, OnDestroy, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions } from 'chart.js';
import { DashboardService } from '../../services/dashboard.service';
import { SettingsService } from '../../services/settings.service';
import { MemberService } from '../../services/member.service';
import { ToastService } from '../../services/toast.service';
import { DashboardSummary, DailyTransactionGroup, UpcomingPayment } from '../../models/dashboard.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { LoadingComponent, EmptyStateComponent } from '../../components/loading/loading.component';
import { DateRangePickerComponent } from '../../components/date-range-picker/date-range-picker.component';
import { ModalComponent } from '../../components/modal/modal.component';
import { formatCurrency, formatDate } from '../../utils/helpers';

const GRADIENT_COLORS = ['#6366f1', '#8b5cf6', '#ec4899', '#14b8a6', '#f59e0b', '#10b981', '#3b82f6', '#f97316', '#ef4444', '#84cc16'];

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

  dateStart = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0] + 'T00:00:00');
  dateEnd = signal(new Date().toISOString().split('T')[0] + 'T23:59:59');

  bookId = '';

  private dashboardService = inject(DashboardService);
  private settingsService = inject(SettingsService);
  private memberService = inject(MemberService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  categories = signal<any[]>([]);

  protected readonly GRADIENT_COLORS = GRADIENT_COLORS;
  protected readonly formatCurrency = formatCurrency;
  protected readonly formatDate = formatDate;

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

  pieData = computed<ChartData<'doughnut'>>(() => {
    const cats = this.summary()?.categoryBreakdown || [];
    return {
      labels: cats.map((c: any) => c.category || 'Uncategorized'),
      datasets: [{ data: cats.map((c: any) => Number(c.amount)), backgroundColor: GRADIENT_COLORS.slice(0, cats.length), borderWidth: 2, borderColor: 'transparent' }]
    };
  });

  barData = computed<ChartData<'bar'>>(() => {
    const groups = this.transactions();
    return {
      labels: groups.map(g => formatDate(g.date, 'short')),
      datasets: [
        { label: 'Expenses', data: groups.map(g => (g.transactions || []).filter((t: any) => t.type !== 'income').reduce((sum: number, t: any) => sum + t.amount, 0)), backgroundColor: 'rgba(239,68,68,0.7)', borderRadius: 4 },
        { label: 'Income', data: groups.map(g => (g.transactions || []).filter((t: any) => t.type === 'income').reduce((sum: number, t: any) => sum + t.amount, 0)), backgroundColor: 'rgba(16,185,129,0.7)', borderRadius: 4 }
      ]
    };
  });

  pieOptions: ChartOptions<'doughnut'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false }, tooltip: { callbacks: { label: (ctx) => ` ${ctx.label}: ${formatCurrency(ctx.parsed)}` } } },
    cutout: '65%'
  };

  barOptions: ChartOptions<'bar'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { position: 'top' } },
    scales: { x: { grid: { display: false } }, y: { beginAtZero: true } }
  };

  ngOnInit() {
    this.route.parent?.params.subscribe(p => { this.bookId = p['bookId'] || ''; this.loadAll(); });
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
      console.log('load summary details --->', res)
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
