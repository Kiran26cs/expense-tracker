import { Routes } from '@angular/router';
import { adminAuthGuard, adminPublicGuard } from './core/guards/admin-auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    canActivate: [adminPublicGuard],
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [adminAuthGuard],
    loadComponent: () => import('./layout/admin-shell/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'users',
        loadComponent: () => import('./pages/users/users.component').then(m => m.UsersComponent),
      },
      {
        path: 'credits',
        loadComponent: () => import('./pages/credits/credits.component').then(m => m.CreditsComponent),
      },
      {
        path: 'books',
        loadComponent: () => import('./pages/books/books.component').then(m => m.BooksComponent),
      },
      {
        path: 'cache',
        loadComponent: () => import('./pages/cache/cache.component').then(m => m.CacheComponent),
      },
      {
        path: 'jobs',
        loadComponent: () => import('./pages/jobs/jobs.component').then(m => m.JobsComponent),
      },
      {
        path: 'platform-admins',
        loadComponent: () => import('./pages/platform-admins/platform-admins.component').then(m => m.PlatformAdminsComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
