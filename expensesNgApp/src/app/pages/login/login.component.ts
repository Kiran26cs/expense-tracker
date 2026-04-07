import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent } from '../../components/input/input.component';
import { isValidEmail, isValidPhone } from '../../utils/helpers';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonComponent, InputComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  emailOrPhone = signal('');
  otp = '';
  step = signal<'input' | 'otp'>('input');
  error = signal('');
  loading = signal(false);
  resendTimer = signal(0);
  otpDigits = ['', '', '', '', '', ''];

  private auth = inject(AuthStateService);
  private router = inject(Router);

  onOtpInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '');
    input.value = val.slice(-1);
    this.otpDigits[idx] = input.value;
    this.otp = this.otpDigits.join('');
    if (val && idx < 5) {
      const next = document.getElementById(`otp-${idx + 1}`) as HTMLInputElement;
      next?.focus();
    } else if (val && idx === 5) {
      this.handleLogin();
    }
  }

  onOtpKeydown(event: KeyboardEvent, idx: number) {
    if (event.key === 'Backspace' && !(event.target as HTMLInputElement).value && idx > 0) {
      const prev = document.getElementById(`otp-${idx - 1}`) as HTMLInputElement;
      prev?.focus();
    }
  }

  async handleRequestOTP() {
    this.error.set('');
    if (!this.emailOrPhone().trim()) { this.error.set('Please enter your email or phone'); return; }
    if (!isValidEmail(this.emailOrPhone()) && !isValidPhone(this.emailOrPhone())) {
      this.error.set('Please enter a valid email or phone number'); return;
    }
    this.loading.set(true);
    try {
      const res = await this.auth.requestOTP(this.emailOrPhone());
      if (res.success) {
        this.step.set('otp');
        this.startResendTimer();
      } else {
        this.error.set(res.error || 'Failed to send OTP');
      }
    } catch (e: any) {
      this.error.set(e.message || 'Failed to send OTP');
    } finally {
      this.loading.set(false);
    }
  }

  async handleLogin() {
    this.error.set('');
    if (this.otp.length < 6) { this.error.set('Please enter the 6-digit OTP'); return; }
    this.loading.set(true);
    try {
      const verify = await this.auth.verifyOTP(this.emailOrPhone(), this.otp);
      if (!verify.success) { this.error.set('Invalid verification code'); return; }
      await this.auth.login(this.emailOrPhone(), this.otp);
      this.router.navigate(['/']);
    } catch (e: any) {
      this.error.set(e.message || 'Login failed');
    } finally {
      this.loading.set(false);
    }
  }

  async handleResendOTP() {
    this.loading.set(true);
    try {
      await this.auth.requestOTP(this.emailOrPhone());
      this.startResendTimer();
    } catch { this.error.set('Failed to resend OTP'); }
    finally { this.loading.set(false); }
  }

  handleGoogleLogin() {
    // Google OAuth – requires backend integration with credential
    this.error.set('Google login requires the Google Client ID to be configured in environment.ts');
  }

  startResendTimer() {
    this.resendTimer.set(60);
    const t = setInterval(() => {
      this.resendTimer.update(v => v - 1);
      if (this.resendTimer() <= 0) clearInterval(t);
    }, 1000);
  }
}
