import { Page } from '@playwright/test';
import { ResolvedPermissions } from '../../src/app/models/member.model';

const API_BASE = 'https://localhost:7250/api';

export const TEST_BOOK_ID = 'e2e-test-book';

export const OWNER_PERMISSIONS: ResolvedPermissions = {
  role: 'owner',
  dashboard: 'write',
  expenses: 'write',
  budgets: 'write',
  settings: 'write',
  insights: 'write',
  canDeleteExpenses: true,
  canManageMembers: true,
  canModifyBook: true,
  isOwner: true,
  allowedCategoryIds: [],
};

/** A member with no Settings access and no member-management rights */
export const LIMITED_PERMISSIONS: ResolvedPermissions = {
  role: 'member',
  dashboard: 'view',
  expenses: 'view',
  budgets: 'view',
  settings: 'none',
  insights: 'view',
  canDeleteExpenses: false,
  canManageMembers: false,
  canModifyBook: false,
  isOwner: false,
  allowedCategoryIds: [],
};

const MOCK_USER = {
  id: 'e2e-test-user',
  name: 'E2E Test User',
  email: 'e2e@example.com',
  currency: 'INR',
  monthlyIncome: 0,
  plan: 'Pro',
};

/**
 * Seeds a fake auth session and mocks all backend calls so tests run against
 * no live API. Must be called before `page.goto`.
 */
export async function loginAs(page: Page, permissions: ResolvedPermissions = OWNER_PERMISSIONS): Promise<void> {
  await page.addInitScript((user) => {
    localStorage.setItem('authToken', 'e2e-fake-token');
    localStorage.setItem('authUser', JSON.stringify(user));
  }, MOCK_USER);

  // Catch-all: anything not explicitly mocked below returns an empty success envelope
  // so dashboard widgets render "no data" instead of erroring. Registered first so
  // the more specific routes added after it take priority.
  await page.route(`${API_BASE}/**`, route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: [] }) })
  );

  await page.route(`${API_BASE}/Auth/me`, route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: MOCK_USER }) })
  );

  await page.route(`${API_BASE}/expensebooks/*/members/me`, route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: permissions }) })
  );
}
