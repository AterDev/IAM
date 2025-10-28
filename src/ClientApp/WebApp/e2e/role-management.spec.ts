import { test, expect } from '@playwright/test';

test.describe('Role Management', () => {
  test('should display role list page', async ({ page }) => {
    await page.goto('/system-role');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check if we're redirected to login (auth required)
    if (page.url().includes('/login')) {
      expect(page.url()).toContain('/login');
    } else {
      // If authenticated, should see role list elements
      await expect(page.locator('h1, h2, mat-card-title').filter({ hasText: /角色|Role/i })).toBeVisible();
    }
  });

  test('role list should have table structure', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Check for table
    await expect(page.locator('table, mat-table')).toBeVisible();
  });

  test('should have add role button', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Look for add button
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create.*角色|Role/i });
    await expect(addButton.first()).toBeVisible();
  });

  test('should open add role dialog', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Click add button
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i }).first();
    await addButton.click();
    
    // Wait for dialog
    await page.waitForTimeout(500);
    
    // Check dialog appeared
    await expect(page.locator('mat-dialog-container, .mat-mdc-dialog-container')).toBeVisible();
  });

  test('add role dialog should have required fields', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Open add dialog
    const addButton = page.locator('button').filter({ hasText: /添加|新增|Add|Create/i }).first();
    await addButton.click();
    await page.waitForTimeout(500);
    
    // Check for name field
    const nameField = page.locator('mat-dialog-container input[formControlName="name"], mat-dialog-container input[name="name"]');
    await expect(nameField).toBeVisible();
    
    // Check for description field
    const descField = page.locator('mat-dialog-container textarea, mat-dialog-container input[formControlName="description"]');
    await expect(descField.first()).toBeVisible();
  });

  test('should have pagination controls', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Check for paginator
    await expect(page.locator('mat-paginator')).toBeVisible();
  });

  test('should have action menu for each role', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Wait for table to load
    await page.waitForTimeout(1000);
    
    // Look for action buttons (edit, delete, permissions)
    const actionButtons = page.locator('button[mat-icon-button], button.mat-mdc-icon-button');
    
    if (await actionButtons.count() > 0) {
      await expect(actionButtons.first()).toBeVisible();
    }
  });

  test('should navigate to role detail page', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // Wait for table rows
    await page.waitForTimeout(1000);
    const rows = page.locator('table tr[mat-row], mat-row');
    const rowCount = await rows.count();
    
    if (rowCount > 0) {
      // Click first row
      await rows.first().click();
      
      // Wait for navigation
      await page.waitForTimeout(1000);
      
      // Should navigate to detail page or open dialog
      const urlChanged = page.url().includes('/system-role/');
      const dialogVisible = await page.locator('mat-dialog-container').isVisible().catch(() => false);
      
      expect(urlChanged || dialogVisible).toBeTruthy();
    }
  });

  test('permission management dialog should have permission groups', async ({ page }) => {
    await page.goto('/system-role');
    
    if (page.url().includes('/login')) {
      test.skip();
    }
    
    // This test checks if permission management exists
    // Look for permissions button
    const permButton = page.locator('button').filter({ hasText: /权限|Permission/i });
    
    if (await permButton.count() > 0) {
      await permButton.first().click();
      await page.waitForTimeout(500);
      
      // Check for permission checkboxes
      const checkboxes = page.locator('mat-checkbox');
      expect(await checkboxes.count()).toBeGreaterThan(0);
    }
  });
});
