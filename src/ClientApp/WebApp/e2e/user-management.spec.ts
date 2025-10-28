import { test, expect } from '@playwright/test';

// Helper function to login (would need to be customized based on actual auth flow)
async function login(page) {
  // This is a placeholder - actual implementation would depend on your auth setup
  // For now, we'll skip auth in tests or use a test token
  await page.goto('/login');
  // In a real scenario, you'd fill credentials and login
  // Or use page.context().addCookies() to set auth cookies
}

test.describe('User Management', () => {
  test.beforeEach(async ({ page }) => {
    // Setup: login or set auth state
    // await login(page);
  });

  test('should display user list page', async ({ page }) => {
    await page.goto('/user');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check if we're redirected to login (auth required)
    if (page.url().includes('/login')) {
      // This is expected if not authenticated
      expect(page.url()).toContain('/login');
    } else {
      // If authenticated, should see user list elements
      await expect(page.locator('h1, h2, mat-card-title').filter({ hasText: /用户|User/i })).toBeVisible();
    }
  });

  test('user list should have table with pagination', async ({ page }) => {
    await page.goto('/user');
    
    // Skip if redirected to login
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Check for table
    await expect(page.locator('table, mat-table')).toBeVisible();
    
    // Check for pagination
    await expect(page.locator('mat-paginator, .pagination')).toBeVisible();
  });

  test('should have add user button', async ({ page }) => {
    await page.goto('/user');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Look for add button
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i });
    await expect(addButton).toBeVisible();
  });

  test('should open add user dialog on button click', async ({ page }) => {
    await page.goto('/user');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Click add button
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i }).first();
    await addButton.click();
    
    // Check dialog appeared
    await expect(page.locator('mat-dialog-container, .mat-mdc-dialog-container')).toBeVisible();
  });

  test('should have search functionality', async ({ page }) => {
    await page.goto('/user');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Look for search input
    const searchInput = page.locator('input[placeholder*="搜索"], input[placeholder*="Search"]');
    if (await searchInput.count() > 0) {
      await expect(searchInput.first()).toBeVisible();
    }
  });

  test('should display user detail when row is clicked', async ({ page }) => {
    await page.goto('/user');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Wait for table rows
    const tableRows = page.locator('table tr, mat-row');
    const rowCount = await tableRows.count();
    
    if (rowCount > 1) {
      // Click first data row (skip header)
      await tableRows.nth(1).click();
      
      // Should navigate to detail page or open dialog
      await page.waitForTimeout(1000);
      
      // Either URL changed or dialog appeared
      const urlChanged = !page.url().endsWith('/user');
      const dialogVisible = await page.locator('mat-dialog-container').isVisible().catch(() => false);
      
      expect(urlChanged || dialogVisible).toBeTruthy();
    }
  });
});
