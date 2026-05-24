import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/user.model';

export const SUPPORTED_CURRENCIES = [
  'AUD','BGN','BRL','CAD','CHF','CNY','CZK','DKK',
  'EUR','GBP','HKD','HUF','IDR','ILS','INR','ISK',
  'JPY','KRW','MXN','MYR','NOK','NZD','PHP','PLN',
  'RON','SEK','SGD','THB','TRY','USD','ZAR'
];

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private cache = new Map<string, Promise<number | null>>();

  constructor(private api: ApiService) {}

  getRate(from: string, to: string, date: string): Promise<number | null> {
    if (from.toUpperCase() === to.toUpperCase()) return Promise.resolve(1);
    const key = `${from.toUpperCase()}:${to.toUpperCase()}:${date}`;
    if (!this.cache.has(key)) {
      this.cache.set(key, this.fetchRate(from, to, date));
    }
    return this.cache.get(key)!;
  }

  private async fetchRate(from: string, to: string, date: string): Promise<number | null> {
    try {
      const res = await firstValueFrom(
        this.api.get<ApiResponse<{ rate: number | null }>>(
          `/currency/rate?from=${from}&to=${to}&date=${date}`
        )
      );
      return res.success ? (res.data?.rate ?? null) : null;
    } catch {
      return null;
    }
  }
}
