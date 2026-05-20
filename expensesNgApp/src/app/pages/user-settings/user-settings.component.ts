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

  saving = signal(false);

  readonly planLimits: Record<string, { books: string; expenses: string; categories: string; credits: string }> = {
    Free:    { books: '3',         expenses: '150 / month',   categories: '20',        credits: '40 (one-time trial)' },
    Starter: { books: 'Unlimited', expenses: '1,000 / month', categories: '50',        credits: '100 / month' },
    Pro:     { books: 'Unlimited', expenses: 'Unlimited',     categories: 'Unlimited', credits: '300 / month' },
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
