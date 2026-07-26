import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ApiResponse } from '../../shared/models/admin.models';

interface UserSummary {
  id: string; email: string; name: string; plan: string;
  isActive: boolean; createdAt: string; bookCount: number;
}
interface UserList { users: UserSummary[]; total: number; page: number; pageSize: number; }

interface UserDetail {
  id: string; email: string; name: string; currency: string;
  plan: string; isActive: boolean; createdAt: string;
  books: { id: string; name: string; aiChatEnabled: boolean; expenseCount: number }[];
  subscription?: { status: string; plan: string; cancelAtPeriodEnd: boolean; currentPeriodEnd?: string };
  credits?: { totalFreeLeft: number; totalPaidLeft: number };
}

@Component({
  selector: 'admin-users',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css',
})
export class UsersComponent implements OnInit {
  private api = inject(AdminApiService);

  users        = signal<UserSummary[]>([]);
  total        = signal(0);
  page         = signal(1);
  pageSize     = 20;
  search       = signal('');
  loading      = signal(false);

  selectedUser = signal<UserDetail | null>(null);
  detailLoading = signal(false);

  planChanging = signal(false);
  newPlan      = signal('');

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    const params = new URLSearchParams({ page: String(this.page()), pageSize: String(this.pageSize) });
    if (this.search()) params.set('search', this.search());

    this.api.get<ApiResponse<UserList>>(`/admin/users?${params}`).subscribe({
      next: r => {
        this.users.set(r.data?.users ?? []);
        this.total.set(r.data?.total ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onSearch() { this.page.set(1); this.load(); }

  nextPage() { this.page.update(p => p + 1); this.load(); }
  prevPage() { this.page.update(p => Math.max(1, p - 1)); this.load(); }

  get totalPages(): number { return Math.ceil(this.total() / this.pageSize); }

  openDetail(userId: string) {
    this.detailLoading.set(true);
    this.api.get<ApiResponse<UserDetail>>(`/admin/users/${userId}`).subscribe({
      next: r => { this.selectedUser.set(r.data ?? null); this.newPlan.set(r.data?.plan ?? ''); this.detailLoading.set(false); },
      error: () => this.detailLoading.set(false),
    });
  }

  closeDetail() { this.selectedUser.set(null); }

  changePlan() {
    const user = this.selectedUser();
    if (!user || !this.newPlan()) return;
    this.planChanging.set(true);
    this.api.patch<ApiResponse<UserDetail>>(`/admin/users/${user.id}/plan`, { plan: this.newPlan() }).subscribe({
      next: r => { this.selectedUser.set(r.data ?? null); this.planChanging.set(false); this.load(); },
      error: () => this.planChanging.set(false),
    });
  }

  setActive(isActive: boolean) {
    const user = this.selectedUser();
    if (!user) return;
    this.api.patch<ApiResponse<UserDetail>>(`/admin/users/${user.id}/active`, { isActive }).subscribe({
      next: r => { this.selectedUser.set(r.data ?? null); this.load(); },
    });
  }
}
