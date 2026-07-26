import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminAuthService } from '../../core/services/admin-auth.service';

type Step = 'email' | 'otp';

@Component({
  selector: 'admin-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private auth   = inject(AdminAuthService);
  private router = inject(Router);

  step    = signal<Step>('email');
  email   = signal('');
  otp     = signal('');
  loading = signal(false);
  error   = signal('');

  async onSendOtp() {
    if (!this.email().trim()) return;
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.sendOtp(this.email().trim());
      this.step.set('otp');
    } catch (e: any) {
      this.error.set(e?.message ?? 'Failed to send OTP. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  async onVerifyOtp() {
    if (!this.otp().trim()) return;
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.verifyOtp(this.email().trim(), this.otp().trim());
      this.router.navigate(['/dashboard']);
    } catch (e: any) {
      this.error.set(e?.message ?? 'Invalid OTP. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  backToEmail() {
    this.step.set('email');
    this.otp.set('');
    this.error.set('');
  }
}
