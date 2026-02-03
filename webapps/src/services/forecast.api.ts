// Forecast and insights API endpoints
import { apiService } from './api.service';
import type { CashForecast, PurchaseSimulation, ApiResponse } from '@/types';

export const forecastApi = {
  // Get cash forecast
  getForecast: (months = 6) => {
    return apiService.get<ApiResponse<CashForecast>>(`/forecast?months=${months}`);
  },

  // Simulate purchase impact
  simulatePurchase: (data: Omit<PurchaseSimulation, 'result'>) => {
    return apiService.post<ApiResponse<PurchaseSimulation>>('/forecast/simulate', data);
  },
};
