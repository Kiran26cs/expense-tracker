import { Component, inject, signal, computed, OnInit, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent } from '../../components/input/input.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { isValidEmail } from '../../utils/helpers';
import { environment } from '../../../environments/environment';
import { AccountLinkPreview } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonComponent, InputComponent, ConfirmDialogComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {
  emailOrPhone = signal('');
  otp = '';
  step = signal<'input' | 'otp'>('input');
  error = signal('');
  loading = signal(false);
  resendTimer = signal(0);
  otpDigits = ['', '', '', '', '', ''];

  linkPreview = signal<AccountLinkPreview | null>(null);
  linkKind = signal<'google' | 'otp' | null>(null);
  linking = signal(false);
  private pendingGoogleCredential = '';
  private pendingOtp = '';

  linkDialogTitle = computed(() =>
    this.linkKind() === 'google' ? 'Link your Google account?' : 'Link your email sign-in?');

  linkDialogMessage = computed(() => {
    const preview = this.linkPreview();
    if (!preview) return '';
    const created = new Date(preview.createdAt).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    return this.linkKind() === 'google'
      ? `An account already exists for ${preview.email}, created ${created} with email sign-in. Signing in with Google will link to this existing account and its data.`
      : `The account for ${preview.email} was created ${created} with Google sign-in. Signing in with your email code will link to this existing account and its data.`;
  });

  private auth = inject(AuthStateService);
  private router = inject(Router);
  private savedTheme: string | null = null;

  ngOnInit(): void {
    this.savedTheme = document.documentElement.getAttribute('data-theme');
    document.documentElement.setAttribute('data-theme', 'light');
    // Handle redirect back from Google OAuth (PWA standalone flow)
    const hash = window.location.hash.slice(1);
    if (hash) {
      const params = new URLSearchParams(hash);
      const idToken = params.get('id_token');
      const oauthError = params.get('error');
      history.replaceState(null, '', window.location.pathname + window.location.search);
      if (idToken) {
        this.onGoogleCredential({ credential: idToken });
      } else if (oauthError) {
        this.error.set(`Google Sign-In failed: ${oauthError}`);
      }
    }
  }

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
    const container = document.getElementById('g-btn-login');
    if (container) {
      google.accounts.id.renderButton(container, { theme: 'outline', size: 'large' });
    }
  }

  private async onGoogleCredential(res: { credential: string }) {
    this.loading.set(true);
    this.error.set('');
    try {
      const outcome = await this.auth.googleLogin(res.credential);
      if (outcome.requiresLinking) {
        this.pendingGoogleCredential = res.credential;
        this.linkKind.set('google');
        this.linkPreview.set(outcome.preview);
        return;
      }
      this.navigateAfterAuth();
    } catch (e: any) {
      this.error.set(e.message || 'Google login failed');
    } finally {
      this.loading.set(false);
    }
  }

  cancelLink() {
    this.linkPreview.set(null);
    this.linkKind.set(null);
    this.pendingGoogleCredential = '';
    this.pendingOtp = '';
  }

  async confirmLink() {
    this.linking.set(true);
    try {
      if (this.linkKind() === 'google') {
        await this.auth.confirmGoogleLink(this.pendingGoogleCredential);
      } else {
        await this.auth.confirmOtpLink(this.emailOrPhone(), this.pendingOtp);
      }
      this.linkPreview.set(null);
      this.linkKind.set(null);
      this.navigateAfterAuth();
    } catch (e: any) {
      this.linkPreview.set(null);
      this.linkKind.set(null);
      this.error.set(e.message || 'Account linking failed');
    } finally {
      this.linking.set(false);
    }
  }

  private navigateAfterAuth() {
    const pendingToken = sessionStorage.getItem('pendingInviteToken');
    if (pendingToken) {
      this.router.navigate(['/accept-invite'], { queryParams: { token: pendingToken } });
    } else {
      this.router.navigate(['/app']);
    }
  }

  handleGoogleLogin() {
    if (this.isPwaStandalone()) {
      this.initiateGooglePwaRedirect();
      return;
    }
    const google = (window as any)['google'];
    if (!google?.accounts?.id) {
      this.error.set('Google Sign-In is unavailable. Please refresh the page and try again.');
      return;
    }
    this.error.set('');
    const container = document.getElementById('g-btn-login');
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

  private isPwaStandalone(): boolean {
    return window.matchMedia('(display-mode: standalone)').matches ||
           !!(window.navigator as any)['standalone'];
  }

  private initiateGooglePwaRedirect(): void {
    const nonce = (crypto as any).randomUUID?.() ?? Math.random().toString(36).slice(2);
    sessionStorage.setItem('gauth_nonce', nonce);
    const params = new URLSearchParams({
      client_id: environment.googleClientId,
      redirect_uri: `${environment.appUrl}/login`,
      response_type: 'id_token',
      scope: 'openid email profile',
      nonce
    });
    window.location.href = `https://accounts.google.com/o/oauth2/v2/auth?${params}`;
  }

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
    if (!this.emailOrPhone().trim()) { this.error.set('Please enter your email'); return; }
    if (!isValidEmail(this.emailOrPhone())) {
      this.error.set('Please enter a valid email address'); return;
    }
    this.loading.set(true);
    try {
      const res = await this.auth.requestOTP(this.emailOrPhone(), true);
      if (res.success) {
        this.step.set('otp');
        this.startResendTimer();
        setTimeout(() => (document.getElementById('otp-0') as HTMLInputElement)?.focus(), 0);
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
      const outcome = await this.auth.login(this.emailOrPhone(), this.otp);
      if (outcome.requiresLinking) {
        this.pendingOtp = this.otp;
        this.linkKind.set('otp');
        this.linkPreview.set(outcome.preview);
        return;
      }
      this.navigateAfterAuth();
    } catch (e: any) {
      this.error.set(e.message || 'Login failed');
    } finally {
      this.loading.set(false);
    }
  }

  async handleResendOTP() {
    this.loading.set(true);
    try {
      await this.auth.requestOTP(this.emailOrPhone(), true);
      this.startResendTimer();
    } catch { this.error.set('Failed to resend OTP'); }
    finally { this.loading.set(false); }
  }

  ngOnDestroy(): void {
    if (this.savedTheme) {
      document.documentElement.setAttribute('data-theme', this.savedTheme);
    } else {
      document.documentElement.removeAttribute('data-theme');
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
