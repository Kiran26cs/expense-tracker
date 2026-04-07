import { Routes } from '@angular/router';
import { authGuard, publicGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [publicGuard]
  },
  {
    path: 'signup',
    loadComponent: () => import('./pages/signup/signup.component').then(m => m.SignupComponent),
    canActivate: [publicGuard]
  },
  {
    path: '',
    loadComponent: () => import('./pages/expense-book-dashboard/expense-book-dashboard.component').then(m => m.ExpenseBookDashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: ':bookId',
    loadComponent: () => import('./layouts/app-layout/app-layout.component').then(m => m.AppLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'expenses', loadComponent: () => import('./pages/expense-list/expense-list.component').then(m => m.ExpenseListComponent) },
      { path: 'expenses/add', loadComponent: () => import('./pages/add-expense/add-expense.component').then(m => m.AddExpenseComponent) },
      { path: 'expenses/:id/edit', loadComponent: () => import('./pages/edit-expense/edit-expense.component').then(m => m.EditExpenseComponent) },
      { path: 'budget', loadComponent: () => import('./pages/budget/budget.component').then(m => m.BudgetComponent) },
      { path: 'insights', loadComponent: () => import('./pages/insights/insights.component').then(m => m.InsightsComponent) },
      { path: 'settings', loadComponent: () => import('./pages/settings/settings.component').then(m => m.SettingsComponent) },
    ]
  },
  { path: '**', redirectTo: '' }
];
