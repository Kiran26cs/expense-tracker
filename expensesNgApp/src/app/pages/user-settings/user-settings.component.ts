import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ThemeService } from '../../services/theme.service';
import { ToastService } from '../../services/toast.service';
import { UpgradeModalService } from '../../services/upgrade-modal.service';
import { ApiService } from '../../services/api.service';
import { TopbarComponent } from '../../components/topbar/topbar.component';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { SelectComponent } from '../../components/input/input.component';
import { InputComponent } from '../../components/input/input.component';
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

@Component({
  selector: 'app-user-settings',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, RouterModule,
    TopbarComponent, CardComponent, CardHeaderComponent, CardTitleComponent,
    CardContentComponent, ButtonComponent, SelectComponent, InputComponent,
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

  saving      = signal(false);
  usageLoading = signal(false);
  usage       = signal<UsageDto | null>(null);

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
