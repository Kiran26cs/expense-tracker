import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrivacyService } from '../../services/privacy.service';
import { AuthStateService } from '../../services/auth-state.service';
import { ToastService } from '../../services/toast.service';

type ModalMode = 'setup' | 'setup-confirm' | 'unlock';

@Component({
  selector: 'app-privacy-unlock-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './privacy-unlock-modal.component.html',
  styleUrl: './privacy-unlock-modal.component.css',
})
export class PrivacyUnlockModalComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  /** true = user has no PIN yet and needs to create one */
  @Input() setupMode = false;
  @Output() verified = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  private privacy = inject(PrivacyService);
  private auth = inject(AuthStateService);
  private toast = inject(ToastService);

  mode = signal<ModalMode>('unlock');
  pin = signal('');
  confirmPin = signal('');
  loading = signal(false);
  error = signal('');
  biometricAvailable = signal(false);
  biometricRegistered = signal(false);

  readonly PIN_LENGTH = 4;
  readonly digits = [1, 2, 3, 4, 5, 6, 7, 8, 9, null, 0, 'del'] as const;

  async ngOnInit() {
    this.biometricRegistered.set(this.privacy.hasBiometricRegistered());
    const available = await this.privacy.isBiometricAvailable();
    this.biometricAvailable.set(available);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']?.currentValue === true) {
      this.mode.set(this.setupMode ? 'setup' : 'unlock');
      this.pin.set('');
      this.confirmPin.set('');
      this.error.set('');
    }
  }

  get activePin(): string {
    return this.mode() === 'setup-confirm' ? this.confirmPin() : this.pin();
  }

  get dots(): boolean[] {
    return Array.from({ length: this.PIN_LENGTH }, (_, i) => i < this.activePin.length);
  }

  get title(): string {
    if (this.mode() === 'setup') return 'Create Privacy PIN';
    if (this.mode() === 'setup-confirm') return 'Confirm Your PIN';
    return 'Enter PIN';
  }

  get subtitle(): string {
    if (this.mode() === 'setup') return 'Choose a 4-digit PIN to protect financial values';
    if (this.mode() === 'setup-confirm') return 'Enter your PIN again to confirm';
    return 'Enter your PIN to view financial data';
  }

  onDigit(key: typeof this.digits[number]): void {
    if (key === 'del') {
      this.onDelete();
      return;
    }
    if (key === null) return;
    const current = this.activePin;
    if (current.length >= this.PIN_LENGTH) return;
    const next = current + String(key);
    this.setActivePin(next);
    this.error.set('');
    if (next.length === this.PIN_LENGTH) {
      // Auto-submit after last digit
      setTimeout(() => this.submit(), 120);
    }
  }

  onDelete(): void {
    const current = this.activePin;
    this.setActivePin(current.slice(0, -1));
    this.error.set('');
  }

  private setActivePin(value: string): void {
    if (this.mode() === 'setup-confirm') {
      this.confirmPin.set(value);
    } else {
      this.pin.set(value);
    }
  }

  async submit(): Promise<void> {
    if (this.loading()) return;
    const current = this.activePin;
    if (current.length < this.PIN_LENGTH) return;

    this.loading.set(true);
    this.error.set('');

    try {
      if (this.mode() === 'setup') {
        this.mode.set('setup-confirm');
        this.loading.set(false);
        return;
      }

      if (this.mode() === 'setup-confirm') {
        if (this.confirmPin() !== this.pin()) {
          this.error.set('PINs do not match. Try again.');
          this.confirmPin.set('');
          this.loading.set(false);
          return;
        }
        const hash = await this.privacy.hashPin(this.pin());
        await this.privacy.setPin(hash);
        this.privacy.markVerified();
        this.verified.emit();
        return;
      }

      // unlock mode
      const hash = await this.privacy.hashPin(current);
      const valid = await this.privacy.verifyPin(hash);
      if (valid) {
        this.privacy.markVerified();
        this.verified.emit();
      } else {
        this.error.set('Incorrect PIN. Try again.');
        this.pin.set('');
      }
    } catch {
      this.error.set('Something went wrong. Please try again.');
      this.pin.set('');
      this.confirmPin.set('');
    } finally {
      this.loading.set(false);
    }
  }

  async useBiometric(): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    try {
      let success = false;
      if (!this.privacy.hasBiometricRegistered()) {
        const userId = this.auth.user()?.id ?? '';
        success = await this.privacy.registerBiometric(userId);
        if (success) this.biometricRegistered.set(true);
      } else {
        success = await this.privacy.unlockWithBiometric();
      }
      if (success) {
        this.privacy.markVerified();
        this.verified.emit();
      } else {
        this.error.set('Biometric verification failed. Use your PIN instead.');
      }
    } catch {
      this.error.set('Biometric not available. Use your PIN.');
    } finally {
      this.loading.set(false);
    }
  }

  close(): void {
    this.pin.set('');
    this.confirmPin.set('');
    this.error.set('');
    this.closed.emit();
  }
}
