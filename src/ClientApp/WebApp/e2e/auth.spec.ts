import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display login page for unauthenticated users', async ({ page }) => {
    await expect(page).toHaveURL(/.*login/);
    await expect(page.locator('h1, h2').filter({ hasText: /登录|Login/i })).toBeVisible();
  });

  test('should show validation errors for empty form submission', async ({ page }) => {
    await page.goto('/login');
    
    // Try to submit empty form
    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();
    
    // Check for validation messages
    await expect(page.locator('mat-error, .mat-mdc-form-field-error')).toBeVisible();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await page.goto('/login');
    
    // Fill in invalid credentials
    await page.fill('input[name="username"], input[formControlName="username"]', 'invalid_user');
    await page.fill('input[name="password"], input[formControlName="password"]', 'wrong_password');
    
    // Submit form
    await page.click('button[type="submit"]');
    
    // Wait for error message or response
    await page.waitForTimeout(2000);
    
    // Should still be on login page or show error
    const currentUrl = page.url();
    expect(currentUrl).toContain('/login');
  });

  test('should navigate to login page when accessing protected route', async ({ page }) => {
    await page.goto('/system-role');
    
    // Should redirect to login
    await expect(page).toHaveURL(/.*login/);
  });

  test('login form should have required fields', async ({ page }) => {
    await page.goto('/login');
    
    // Check username field exists
    const usernameField = page.locator('input[name="username"], input[formControlName="username"]');
    await expect(usernameField).toBeVisible();
    
    // Check password field exists
    const passwordField = page.locator('input[name="password"], input[formControlName="password"]');
    await expect(passwordField).toBeVisible();
    
    // Check submit button exists
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeVisible();
  });

  test('password field should be of type password', async ({ page }) => {
    await page.goto('/login');
    
    const passwordField = page.locator('input[name="password"], input[formControlName="password"]');
    await expect(passwordField).toHaveAttribute('type', 'password');
  });
});
