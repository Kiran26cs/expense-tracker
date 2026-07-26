import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';
import {
  UserStatsDto, SubscriptionStatsDto, CreditStatsDto,
  BookStatsDto, ImportStatsDto, RecentActionsDto
} from '../../shared/models/dashboard.models';
import { DatePipe, CurrencyPipe, DecimalPipe } from '@angular/common';

@Component({
  selector: 'admin-dashboard',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  private api = inject(AdminApiService);

  userStats        = signal<UserStatsDto | null>(null);
  subscriptionStats = signal<SubscriptionStatsDto | null>(null);
  creditStats      = signal<CreditStatsDto | null>(null);
  bookStats        = signal<BookStatsDto | null>(null);
  importStats      = signal<ImportStatsDto | null>(null);
  recentActions    = signal<RecentActionsDto | null>(null);

  loadingUsers        = signal(true);
  loadingSubscriptions = signal(true);
  loadingCredits      = signal(true);
  loadingBooks        = signal(true);
  loadingImports      = signal(true);
  loadingActions      = signal(true);

  ngOnInit() {
    this.api.get<ApiResponse<UserStatsDto>>('/admin/dashboard/user-stats').subscribe({
      next: r => { this.userStats.set(r.data ?? null); this.loadingUsers.set(false); },
      error: () => this.loadingUsers.set(false),
    });

    this.api.get<ApiResponse<SubscriptionStatsDto>>('/admin/dashboard/subscription-stats').subscribe({
      next: r => { this.subscriptionStats.set(r.data ?? null); this.loadingSubscriptions.set(false); },
      error: () => this.loadingSubscriptions.set(false),
    });

    this.api.get<ApiResponse<CreditStatsDto>>('/admin/dashboard/credit-stats').subscribe({
      next: r => { this.creditStats.set(r.data ?? null); this.loadingCredits.set(false); },
      error: () => this.loadingCredits.set(false),
    });

    this.api.get<ApiResponse<BookStatsDto>>('/admin/dashboard/book-stats').subscribe({
      next: r => { this.bookStats.set(r.data ?? null); this.loadingBooks.set(false); },
      error: () => this.loadingBooks.set(false),
    });

    this.api.get<ApiResponse<ImportStatsDto>>('/admin/dashboard/import-stats').subscribe({
      next: r => { this.importStats.set(r.data ?? null); this.loadingImports.set(false); },
      error: () => this.loadingImports.set(false),
    });

    this.api.get<ApiResponse<RecentActionsDto>>('/admin/dashboard/recent-actions').subscribe({
      next: r => { this.recentActions.set(r.data ?? null); this.loadingActions.set(false); },
      error: () => this.loadingActions.set(false),
    });
  }

  planPercent(count: number, total: number): string {
    if (!total) return '0%';
    return `${Math.round((count / total) * 100)}%`;
  }

  formatAction(action: string): string {
    return action.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
  }
}
