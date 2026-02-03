// Custom hook for authentication
import { useState, useEffect, createContext, useContext, ReactNode } from 'react';
import { authApi } from '@/services/auth.api';
import { apiService } from '@/services/api.service';
import type { User } from '@/types';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (emailOrPhone: string, otp: string) => Promise<void>;
  signup: (name: string, emailOrPhone: string, otp: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const token = localStorage.getItem('authToken');
      if (token) {
        const response = await authApi.getCurrentUser();
        if (response.success && response.data) {
          setUser(response.data);
        }
      }
    } catch (error) {
      console.error('Auth check failed:', error);
      apiService.clearToken();
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (emailOrPhone: string, otp: string) => {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    
    const response = await authApi.login({ email, phone, otp }, otp);
    if (response.success && response.data) {
      apiService.setToken(response.data.token);
      setUser(response.data.user);
    } else {
      throw new Error(response.error || 'Login failed');
    }
  };

  const signup = async (name: string, emailOrPhone: string, otp: string) => {
    const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
    const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
    
    const response = await authApi.signup({ name, email, phone, otp }, otp);
    if (response.success && response.data) {
      apiService.setToken(response.data.token);
      setUser(response.data.user);
    } else {
      throw new Error(response.error || 'Signup failed');
    }
  };

  const logout = async () => {
    try {
      // Logout endpoint doesn't exist in backend, just clear local state
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      apiService.clearToken();
      setUser(null);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        signup,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
