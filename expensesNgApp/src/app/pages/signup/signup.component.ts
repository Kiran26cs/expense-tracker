import { Component, inject, signal, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent } from '../../components/input/input.component';
import { isValidEmail, isValidPhone } from '../../utils/helpers';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonComponent, InputComponent],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css'
})
export class SignupComponent implements AfterViewInit {
  name = signal('');
  emailOrPhone = signal('');
  otp = '';
  step = signal<'input' | 'otp'>('input');
  error = signal('');
  loading = signal(false);
  resendTimer = signal(0);
  otpDigits = ['', '', '', '', '', ''];

  private auth = inject(AuthStateService);
  private router = inject(Router);

  ngAfterViewInit() {
    this.setupGoogleButton();
  }

  private setupGoogleButton() {
    const google = (window as any)['google'];
    if (!google?.accounts?.id) return;
    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (res: { credential: string }) => this.onGoogleCredential(res)
    });
    const container = document.getElementById('g-btn-signup');
    if (container) {
      google.accounts.id.renderButton(container, { theme: 'outline', size: 'large' });
    }
  }

  private async onGoogleCredential(res: { credential: string }) {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.googleLogin(res.credential);
      const pendingToken = sessionStorage.getItem('pendingInviteToken');
      if (pendingToken) {
        this.router.navigate(['/accept-invite'], { queryParams: { token: pendingToken } });
      } else {
        this.router.navigate(['/']);
      }
    } catch (e: any) {
      this.error.set(e.message || 'Google sign-up failed');
    } finally {
      this.loading.set(false);
    }
  }

  onOtpInput(event: Event, idx: number) {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '');
    input.value = val.slice(-1);
    this.otpDigits[idx] = input.value;
    this.otp = this.otpDigits.join('');
    if (val && idx < 5) {
      (document.getElementById(`sotp-${idx + 1}`) as HTMLInputElement)?.focus();
    } else if (val && idx === 5) {
      this.handleSignup();
    }
  }

  onOtpKeydown(event: KeyboardEvent, idx: number) {
    if (event.key === 'Backspace' && !(event.target as HTMLInputElement).value && idx > 0) {
      (document.getElementById(`sotp-${idx - 1}`) as HTMLInputElement)?.focus();
    }
  }

  async handleRequestOTP() {
    this.error.set('');
    if (!this.name().trim()) { this.error.set('Please enter your name'); return; }
    if (!this.emailOrPhone().trim()) { this.error.set('Please enter email or phone'); return; }
    if (!isValidEmail(this.emailOrPhone()) && !isValidPhone(this.emailOrPhone())) {
      this.error.set('Please enter a valid email or phone'); return;
    }
    this.loading.set(true);
    try {
      const res = await this.auth.requestOTP(this.emailOrPhone());
      if (res.success) { this.step.set('otp'); this.startResendTimer(); }
      else this.error.set(res.error || 'Failed to send OTP');
    } catch (e: any) { this.error.set(e.message || 'Failed to send OTP'); }
    finally { this.loading.set(false); }
  }

  async handleSignup() {
    this.error.set('');
    if (this.otp.length < 6) { this.error.set('Please enter the 6-digit OTP'); return; }
    this.loading.set(true);
    try {
      const verifyRes = await this.auth.verifyOTP(this.emailOrPhone(), this.otp);
      if (!verifyRes.success) { this.error.set('Invalid verification code'); return; }
      await this.auth.signup(this.name(), this.emailOrPhone(), this.otp);
      const pendingToken = sessionStorage.getItem('pendingInviteToken');
      if (pendingToken) {
        this.router.navigate(['/accept-invite'], { queryParams: { token: pendingToken } });
      } else {
        this.router.navigate(['/']);
      }
    } catch (e: any) { this.error.set(e.message || 'Signup failed'); }
    finally { this.loading.set(false); }
  }

  async handleResendOTP() {
    this.loading.set(true);
    try { await this.auth.requestOTP(this.emailOrPhone()); this.startResendTimer(); }
    catch { this.error.set('Failed to resend OTP'); }
    finally { this.loading.set(false); }
  }

  handleGoogleLogin() {
    const google = (window as any)['google'];
    if (!google?.accounts?.id) {
      this.error.set('Google Sign-In is unavailable. Please refresh the page and try again.');
      return;
    }
    this.error.set('');
    const container = document.getElementById('g-btn-signup');
    // Render on first click if script loaded after component init
    if (container && !container.hasChildNodes()) {
      this.setupGoogleButton();
    }
    const btn = container?.querySelector<HTMLElement>('[role="button"]');
    if (btn) {
      btn.click();
    } else {
      this.error.set('Google Sign-In is unavailable. Please refresh the page and try again.');
    }
  }

  startResendTimer() {
    this.resendTimer.set(60);
    const t = setInterval(() => {
      this.resendTimer.update(v => v - 1);
      if (this.resendTimer() <= 0) clearInterval(t);
    }, 1000);
  }
}
