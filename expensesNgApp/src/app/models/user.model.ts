export type PlanType = 'Free' | 'Starter' | 'Pro';

export interface User {
  id: string;
  name: string;
  email?: string;
  currency: string;
  monthlyIncome: number;
  monthlySavingsGoal?: number;
  createdAt?: string;
  plan?: PlanType;
}

export interface AuthCredentials {
  email?: string;
  otp?: string;
}

export interface SignupData {
  name: string;
  email?: string;
  otp?: string;
  currency?: string;
  monthlyIncome?: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export type Theme = 'light' | 'dark';
