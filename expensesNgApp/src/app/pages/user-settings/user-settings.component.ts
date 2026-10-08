import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';
import { UpgradeModalService } from '../../services/upgrade-modal.service';
import { ApiService } from '../../services/api.service';
import { PushNotificationService, NotifStatus } from '../../services/push-notification.service';
import { TopbarComponent } from '../../components/topbar/topbar.component';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { SelectComponent } from '../../components/input/input.component';
import { InputComponent } from '../../components/input/input.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { ApiResponse } from '../../models/user.model';
import { firstValueFrom } from 'rxjs';

interface UsageDto {
  booksOwned: number;
  booksLimit: number;
  expensesThisMonth: number;
  expensesLimit: number;
  categoriesUsed: number;
  categoriesLimit: number;  // -1 = not applicable for this plan
}

interface SessionItem {
  id: string;
  deviceLabel: string;
  createdAt: string;
  lastUsedAt: string;
  isCurrent: boolean;
}

@Component({
  selector: 'app-user-settings',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, RouterModule,
    TopbarComponent, CardComponent, CardHeaderComponent, CardTitleComponent,
    CardContentComponent, ButtonComponent, SelectComponent, InputComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './user-settings.component.html',
  styleUrl: './user-settings.component.css',
})
export class UserSettingsComponent implements OnInit {
  authState    = inject(AuthStateService);
  themeService = inject(ThemeService);
  upgradeModal = inject(UpgradeModalService);
  private toast = inject(ToastService);
  private api   = inject(ApiService);
  private fb    = inject(FormBuilder);
  private push  = inject(PushNotificationService);

  saving       = signal(false);
  usageLoading = signal(false);
  usage        = signal<UsageDto | null>(null);
  notifStatus  = signal<NotifStatus>('loading');
  notifBusy    = signal(false);

  sessions             = signal<SessionItem[]>([]);
  sessionsLoading      = signal(false);
  sessionsBusy         = signal(false);
  showLogoutAllConfirm = signal(false);

  readonly planLimits: Record<string, { books: string; expenses: string; categories: string; credits: string; autoClassify: string }> = {
    Free:    { books: '3',         expenses: '150 / month',   categories: '20',        credits: '15 (one-time trial)', autoClassify: '5 (lifetime)'    },
    Starter: { books: 'Unlimited', expenses: '1,000 / month', categories: '50',        credits: '50 / month',          autoClassify: '15 / month'       },
    Pro:     { books: 'Unlimited', expenses: 'Unlimited',     categories: 'Unlimited', credits: '150 / month',         autoClassify: '15 / month'       },
  };

  get userPlan(): string { return this.authState.user()?.plan ?? 'Free'; }

  prefsForm: FormGroup = this.fb.group({
    currency:           ['USD'],
    monthlySavingsGoal: [null],
  });

  ngOnInit() {
    const u = this.authState.user();
    if (u) {
      this.prefsForm.patchValue({
        currency:           u.currency || 'USD',
        monthlySavingsGoal: u.monthlySavingsGoal ?? null,
      });
    }
    this.loadUsage();
    this.loadSessions();
    this.push.getStatus().then(s => this.notifStatus.set(s));
  }

  async loadSessions() {
    this.sessionsLoading.set(true);
    try {
      const currentSessionId = this.authState.getSessionId() ?? '';
      const res = await firstValueFrom(
        this.api.get<ApiResponse<SessionItem[]>>(`/Auth/sessions?currentSessionId=${encodeURIComponent(currentSessionId)}`)
      );
      if (res.success && res.data) this.sessions.set(res.data);
    } catch {}
    finally { this.sessionsLoading.set(false); }
  }

  async revokeSession(sessionId: string) {
    if (this.sessionsBusy()) return;
    this.sessionsBusy.set(true);
    try {
      await firstValueFrom(this.api.post<ApiResponse<boolean>>('/Auth/logout', { sessionId }));
      await this.loadSessions();
    } catch {
      this.toast.error('Could not revoke that session');
    } finally {
      this.sessionsBusy.set(false);
    }
  }

  openLogoutAllConfirm()  { this.showLogoutAllConfirm.set(true); }
  cancelLogoutAll()       { this.showLogoutAllConfirm.set(false); }

  async confirmLogoutAll() {
    this.sessionsBusy.set(true);
    await this.authState.logoutAll();
  }

  async loadUsage() {
    this.usageLoading.set(true);
    try {
      const res = await firstValueFrom(this.api.get<ApiResponse<UsageDto>>('/usage'));
      if (res.success && res.data) this.usage.set(res.data);
    } catch {}
    finally { this.usageLoading.set(false); }
  }

  usagePct(used: number, limit: number): number {
    if (limit <= 0) return 0;
    return Math.min(100, Math.round((used / limit) * 100));
  }

  usageBarClass(pct: number): string {
    if (pct >= 90) return 'bar-danger';
    if (pct >= 70) return 'bar-warning';
    return 'bar-ok';
  }

  async toggleNotifications() {
    if (this.notifBusy()) return;
    this.notifBusy.set(true);
    try {
      if (this.notifStatus() === 'subscribed') {
        await this.push.unsubscribe();
        this.notifStatus.set('unsubscribed');
        this.toast.success('Notifications disabled');
      } else {
        await this.push.subscribe();
        this.notifStatus.set('subscribed');
        this.toast.success('Notifications enabled');
      }
    } catch {
      if (typeof Notification !== 'undefined' && Notification.permission === 'denied') {
        this.notifStatus.set('blocked');
        this.toast.error('Notifications blocked — allow them in your browser settings');
      } else {
        this.toast.error('Could not update notification settings');
      }
    } finally {
      this.notifBusy.set(false);
    }
  }

  async savePreferences() {
    if (this.saving()) return;
    this.saving.set(true);
    try {
      const { currency, monthlySavingsGoal } = this.prefsForm.value;
      const res = await firstValueFrom(
        this.api.patch<ApiResponse<any>>('/Auth/profile', { currency, monthlySavingsGoal })
      );
      if (res.success) {
        await this.authState.checkAuth();
        this.toast.success('Preferences saved');
      } else {
        this.toast.error(res.error || 'Failed to save');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to save');
    } finally {
      this.saving.set(false);
    }
  }
}
