export interface AdminDto {
  id: string;
  email: string;
  name: string;
  permissions: string[];
  lastLoginAt?: string;
}

export interface AdminAuthResponse {
  token: string;
  admin: AdminDto;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

export const AdminPermissions = {
  Credits:   'credits',
  Users:     'users',
  Books:     'books',
  Cache:     'cache',
  Jobs:      'jobs',
  Analytics: 'analytics',
  Admins:    'admins',
} as const;
