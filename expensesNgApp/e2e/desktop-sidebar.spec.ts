import { test, expect } from '@playwright/test';
import { loginAs, TEST_BOOK_ID } from './fixtures/auth';

test.describe('Desktop sidebar — unaffected by the mobile "More" overflow', () => {
  test.beforeEach(async ({}, testInfo) => {
    test.skip(testInfo.project.name !== 'desktop', 'desktop-only regression guard');
  });

  test('renders the full flat nav list, including Settings and Members inline', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    const nav = page.locator('nav.sidebar-nav');
    await expect(nav.locator('.nav-item-more-wrap')).toHaveCount(0);
    await expect(nav.locator('a.nav-item')).toHaveText([
      'Dashboard', 'Expenses', 'Budget', 'Finance Tools', 'Settings', 'Bank Sync', 'Members',
    ]);
  });
});
