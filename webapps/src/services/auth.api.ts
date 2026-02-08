// Authentication API endpoints
import { apiService } from './api.service';
import type { User, AuthCredentials, SignupData, ApiResponse } from '@/types';

export const authApi = {
  // Request OTP for login
  requestOTP: (email?: string, phone?: string) => {
    return apiService.post<ApiResponse<boolean>>('/Auth/send-otp', {
      email,
      phone,
    });
  },

  // Verify OTP during signup/login
  verifyOTP: (email: string | undefined, phone: string | undefined, otp: string) => {
    return apiService.post<ApiResponse<boolean>>('/Auth/verify-otp', {
      email,
      phone,
      otp,
    });
  },

  // Signup with OTP as query parameter
  signup: (data: SignupData, otp: string) => {
    return apiService.post<ApiResponse<{ token: string; refreshToken: string; user: User }>>(`/Auth/signup?otp=${otp}`, {
      email: data.email,
      phone: data.phone,
      name: data.name,
      currency: data.currency || 'USD',
      monthlyIncome: data.monthlyIncome || 0,
    });
  },

  // Login with OTP as query parameter
  login: (credentials: AuthCredentials, otp: string) => {
    return apiService.post<ApiResponse<{ token: string; refreshToken: string; user: User }>>(`/Auth/login?otp=${otp}`, {
      email: credentials.email,
      phone: credentials.phone,
    });
  },

  // Get current user
  getCurrentUser: () => {
    return apiService.get<ApiResponse<User>>('/Auth/me');
  },

  // Google SSO login
  googleLogin: (credential: string) => {
    return apiService.post<ApiResponse<{ token: string; refreshToken: string; user: User }>>('/Auth/google', {
      credential,
    });
  },
};
