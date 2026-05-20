import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

export interface CreateSubscriptionResponse {
  subscriptionId: string;
  razorpayKeyId: string;
  planName: string;
  amount: number;
  currency: string;
  description: string;
}

export interface SubscriptionStatus {
  plan: string;
  status: string;
  currentPeriodEnd: string | null;
  cancelAtPeriodEnd: boolean;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  constructor(private api: ApiService) {}

  createSubscription(plan: string): Promise<ApiResponse<CreateSubscriptionResponse>> {
    return firstValueFrom(
      this.api.post<ApiResponse<CreateSubscriptionResponse>>('/payment/create-subscription', { plan })
    );
  }

  verifyPayment(
    razorpayPaymentId: string,
    razorpaySubscriptionId: string,
    razorpaySignature: string
  ): Promise<ApiResponse<SubscriptionStatus>> {
    return firstValueFrom(
      this.api.post<ApiResponse<SubscriptionStatus>>('/payment/verify', {
        razorpayPaymentId,
        razorpaySubscriptionId,
        razorpaySignature,
      })
    );
  }

  getStatus(): Promise<ApiResponse<SubscriptionStatus | null>> {
    return firstValueFrom(this.api.get<ApiResponse<SubscriptionStatus | null>>('/payment/status'));
  }

  cancelSubscription(immediately = false): Promise<ApiResponse<void>> {
    return firstValueFrom(
      this.api.post<ApiResponse<void>>('/payment/cancel', { immediately })
    );
  }

  async openRazorpayCheckout(
    options: CreateSubscriptionResponse,
    prefill: { name: string; email: string },
    onSuccess: (response: { razorpay_payment_id: string; razorpay_subscription_id: string; razorpay_signature: string }) => void,
    onDismiss: () => void
  ): Promise<void> {
    await this.loadRazorpayScript();

    const rzpOptions = {
      key:             options.razorpayKeyId,
      subscription_id: options.subscriptionId,
      name:            'NidhiWise',
      description:     options.description,
      image:           '/favicon.ico',
      prefill:         { name: prefill.name, email: prefill.email },
      theme:           { color: '#6366f1' },
      modal:           { ondismiss: onDismiss },
      handler:         onSuccess,
    };

    const rzp = new (window as any).Razorpay(rzpOptions);
    rzp.open();
  }

  private loadRazorpayScript(): Promise<void> {
    if ((window as any).Razorpay) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load Razorpay'));
      document.head.appendChild(script);
    });
  }
}
