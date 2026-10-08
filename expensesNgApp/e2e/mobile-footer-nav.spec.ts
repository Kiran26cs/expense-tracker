import { test, expect } from '@playwright/test';
import { loginAs, LIMITED_PERMISSIONS, TEST_BOOK_ID } from './fixtures/auth';

test.describe('Mobile footer nav — "More" overflow menu', () => {
  test.beforeEach(async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== 'mobile', 'mobile-only footer behavior');
  });

  test('shows exactly the 6 primary tabs, with Settings/Members tucked away', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    const nav = page.locator('nav.sidebar-nav');
    const tabs = nav.locator(':scope > a.nav-item, :scope > .nav-item-more-wrap > .nav-item-more');
    await expect(tabs).toHaveCount(6);
    await expect(tabs).toHaveText([
      'Dashboard', 'Expenses', 'Budget', 'Finance Tools', 'Bank Sync', 'More',
    ]);

    await expect(nav.getByRole('link', { name: 'Settings' })).toHaveCount(0);
    await expect(nav.getByRole('link', { name: 'Members' })).toHaveCount(0);
  });

  test('tapping More opens a visible popover with Settings and Members', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    const popover = page.locator('.more-menu-popover');
    await expect(popover).toBeHidden();

    await page.locator('.nav-item-more').click();

    await expect(popover).toBeVisible();
    const box = await popover.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThan(0);
    expect(box!.height).toBeGreaterThan(0);

    const items = popover.locator('.more-menu-item');
    await expect(items).toHaveCount(2);
    await expect(items).toHaveText(['Settings', 'Members']);
  });

  test('tapping the backdrop closes the popover', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    await page.locator('.nav-item-more').click();
    await expect(page.locator('.more-menu-popover')).toBeVisible();

    // Top-left corner is far from the bottom-right anchored popover
    await page.mouse.click(10, 10);

    await expect(page.locator('.more-menu-popover')).toBeHidden();
  });

  test('tapping Settings in the popover navigates and closes the menu', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    await page.locator('.nav-item-more').click();
    await page.locator('.more-menu-item', { hasText: 'Settings' }).click();

    await expect(page).toHaveURL(new RegExp(`/${TEST_BOOK_ID}/settings$`));
    await expect(page.locator('.more-menu-popover')).toBeHidden();
  });

  test('tapping Members in the popover navigates and closes the menu', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    await page.locator('.nav-item-more').click();
    await page.locator('.more-menu-item', { hasText: 'Members' }).click();

    await expect(page).toHaveURL(new RegExp(`/${TEST_BOOK_ID}/members$`));
    await expect(page.locator('.more-menu-popover')).toBeHidden();
  });

  test('More tab shows active styling while on the Settings route', async ({ page }) => {
    await loginAs(page);
    await page.goto(`/${TEST_BOOK_ID}/settings`);

    await expect(page.locator('.nav-item-more')).toHaveClass(/active/);
  });

  test('member without Settings/Members access sees no More tab at all', async ({ page }) => {
    await loginAs(page, LIMITED_PERMISSIONS);
    await page.goto(`/${TEST_BOOK_ID}/dashboard`);

    const nav = page.locator('nav.sidebar-nav');
    await expect(nav.locator('.nav-item-more-wrap')).toHaveCount(0);
    await expect(nav.locator(':scope > a.nav-item')).toHaveText([
      'Dashboard', 'Expenses', 'Budget', 'Finance Tools',
    ]);
  });
});
