import '@/styles/global.css';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { AuthProvider, useAuth } from '@/hooks/useAuth';
import { AppLayout } from '@/layouts/AppLayout/AppLayout';
import { LoginPage } from '@/pages/Auth/LoginPage';
import { SignupPage } from '@/pages/Auth/SignupPage';
import { DashboardPage } from '@/pages/Dashboard/DashboardPage';
import { ExpenseListPage } from '@/pages/Expenses/ExpenseListPage';
import { AddExpensePage } from '@/pages/Expenses/AddExpensePage';
import { EditExpensePage } from '@/pages/Expenses/EditExpensePage';
import { BudgetPage } from '@/pages/Budget/BudgetPage';
import { InsightsPage } from '@/pages/Insights/InsightsPage';
import { SettingsPage } from '@/pages/Settings/SettingsPage';
import { Loading } from '@/components/Loading/Loading';

// Protected Route Wrapper
const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <Loading />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
};

// Public Route Wrapper (redirect to dashboard if already authenticated)
const PublicRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <Loading />;
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
};

function AppRoutes() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        }
      />
      <Route
        path="/signup"
        element={
          <PublicRoute>
            <SignupPage />
          </PublicRoute>
        }
      />

      {/* Protected Routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout>
              <DashboardPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/expenses"
        element={
          <ProtectedRoute>
            <AppLayout>
              <ExpenseListPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/expenses/add"
        element={
          <ProtectedRoute>
            <AppLayout>
              <AddExpensePage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/expenses/:id/edit"
        element={
          <ProtectedRoute>
            <AppLayout>
              <EditExpensePage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/budget"
        element={
          <ProtectedRoute>
            <AppLayout>
              <BudgetPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/insights"
        element={
          <ProtectedRoute>
            <AppLayout>
              <InsightsPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/settings"
        element={
          <ProtectedRoute>
            <AppLayout>
              <SettingsPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function App() {
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || '';
  
  return (
    <GoogleOAuthProvider clientId={googleClientId}>
      <BrowserRouter>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </BrowserRouter>
    </GoogleOAuthProvider>
  );
}

export default App;
