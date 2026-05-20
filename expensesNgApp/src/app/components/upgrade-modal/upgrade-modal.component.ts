import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UpgradeModalService } from '../../services/upgrade-modal.service';
import { PaymentService } from '../../services/payment.service';
import { AuthStateService } from '../../services/auth-state.service';
import { ToastService } from '../../services/toast.service';

type PlanKey = 'Starter' | 'Pro';

const PLAN_FEATURES: Record<PlanKey, string[]> = {
  Starter: [
    'Unlimited expense books',
    '1,000 expenses / month',
    '50 custom categories',
    '100 AI credits / month (resets monthly)',
    'Up to 5 team members',
    'All core features',
  ],
  Pro: [
    'Unlimited expense books',
    'Unlimited expenses',
    'Unlimited categories',
    '300 AI credits / month (resets monthly)',
    'Up to 51 team members',
    'Priority support',
    'Everything in Starter',
  ],
};

const PLAN_PRICES: Record<PlanKey, string> = { Starter: '$3.99', Pro: '$7.99' };
const PLAN_PRICES_INR: Record<PlanKey, string> = { Starter: '₹335', Pro: '₹670' };

@Component({
  selector: 'app-upgrade-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './upgrade-modal.component.html',
  styleUrl: './upgrade-modal.component.css',
})
export class UpgradeModalComponent {
  modal   = inject(UpgradeModalService);
  payment = inject(PaymentService);
  auth    = inject(AuthStateService);
  toast   = inject(ToastService);

  selectedPlan = signal<PlanKey>('Starter');
  loading      = signal(false);

  // Plans available to the user — Starter users can only upgrade to Pro
  readonly availablePlans = computed<PlanKey[]>(() => {
    const userPlan = this.auth.user()?.plan ?? 'Free';
    return userPlan === 'Starter' ? ['Pro'] : ['Starter', 'Pro'];
  });
  readonly planFeatures  = PLAN_FEATURES;
  readonly planPrices    = PLAN_PRICES;
  readonly planPricesInr = PLAN_PRICES_INR;

  constructor() {
    // When modal opens, honour the requested plan or default to first available
    effect(() => {
      if (this.modal.isOpen()) {
        const requested = this.modal.initialPlan();
        const available = this.availablePlans();
        this.selectedPlan.set(
          requested && available.includes(requested) ? requested : available[0]
        );
      }
    });
  }

  selectPlan(plan: PlanKey) { this.selectedPlan.set(plan); }

  async subscribe() {
    if (this.loading()) return;
    this.loading.set(true);

    try {
      const res = await this.payment.createSubscription(this.selectedPlan());
      if (!res.success || !res.data) throw new Error(res.error ?? 'Failed to create subscription');

      const user = this.auth.user();
      await this.payment.openRazorpayCheckout(
        res.data,
        { name: user?.name ?? '', email: user?.email ?? '' },
        async (rzpResponse) => {
          try {
            const verify = await this.payment.verifyPayment(
              rzpResponse.razorpay_payment_id,
              rzpResponse.razorpay_subscription_id,
              rzpResponse.razorpay_signature
            );
            if (verify.success) {
              // Refresh user to pick up new plan
              await this.auth.checkAuth();
              this.toast.success(`🎉 You're now on the ${this.selectedPlan()} plan!`);
              this.modal.close();
            } else {
              this.toast.error(verify.error ?? 'Payment verification failed');
            }
          } catch {
            this.toast.error('Payment verification failed. Contact support.');
          } finally {
            this.loading.set(false);
          }
        },
        () => { this.loading.set(false); }
      );
    } catch (e: any) {
      this.toast.error(e.message ?? 'Something went wrong');
      this.loading.set(false);
    }
  }
}
