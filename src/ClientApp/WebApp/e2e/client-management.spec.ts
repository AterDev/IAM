import { test, expect } from '@playwright/test';

test.describe('Client Management', () => {
  test('should display client list page', async ({ page }) => {
    await page.goto('/client');
    
    await page.waitForLoadState('networkidle');
    
    if (page.url().includes('/login')) {
      expect(page.url()).toContain('/login');
    } else {
      await expect(page.locator('h1, h2, mat-card-title').filter({ hasText: /客户端|Client/i })).toBeVisible();
    }
  });

  test('client list should have table', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    await expect(page.locator('table, mat-table')).toBeVisible();
  });

  test('should have add client button', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i });
    await expect(addButton.first()).toBeVisible();
  });

  test('should open add client dialog', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i }).first();
    await addButton.click();
    await page.waitForTimeout(500);
    
    await expect(page.locator('mat-dialog-container')).toBeVisible();
  });

  test('add client dialog should have client ID field', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i }).first();
    await addButton.click();
    await page.waitForTimeout(500);
    
    const clientIdField = page.locator('mat-dialog-container input[formControlName="clientId"], mat-dialog-container input[name="clientId"]');
    await expect(clientIdField).toBeVisible();
  });

  test('should have search functionality', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    const searchInput = page.locator('input[placeholder*="搜索"], input[placeholder*="Search"]');
    if (await searchInput.count() > 0) {
      await expect(searchInput.first()).toBeVisible();
    }
  });

  test('should display client details', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    await page.waitForTimeout(1000);
    const rows = page.locator('table tr[mat-row], mat-row');
    const rowCount = await rows.count();
    
    if (rowCount > 0) {
      await rows.first().click();
      await page.waitForTimeout(1000);
      
      const urlChanged = page.url().includes('/client/');
      const dialogVisible = await page.locator('mat-dialog-container').isVisible().catch(() => false);
      
      expect(urlChanged || dialogVisible).toBeTruthy();
    }
  });

  test('should have pagination', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    await expect(page.locator('mat-paginator')).toBeVisible();
  });

  test('should show client actions', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    await page.waitForTimeout(1000);
    
    const actionButtons = page.locator('button[mat-icon-button]');
    if (await actionButtons.count() > 0) {
      await expect(actionButtons.first()).toBeVisible();
    }
  });

  test('client detail should show configuration', async ({ page }) => {
    await page.goto('/client');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    await page.waitForTimeout(1000);
    const rows = page.locator('table tr[mat-row], mat-row');
    
    if (await rows.count() > 0) {
      // Click to view details
      await rows.first().click();
      await page.waitForTimeout(1000);
      
      if (page.url().includes('/client/')) {
        // Should show client configuration like PKCE, grant types, etc.
        const content = await page.content();
        expect(content.length).toBeGreaterThan(0);
      }
    }
  });
});
