import { test, expect } from '@playwright/test';

/**
 * E2E tests for API Key management functionality
 * Tests the complete flow of creating, viewing, and managing API keys
 */
test.describe('API Key Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the keys page
    await page.goto('/keys');
  });

  test('should display the API keys page with form', async ({ page }) => {
    // Verify page title
    await expect(page).toHaveTitle(/LLM Proxy/);
    
    // Verify form elements exist
    await expect(page.getByRole('heading', { name: /api keys/i })).toBeVisible();
    await expect(page.getByLabel(/name/i)).toBeVisible();
    await expect(page.getByLabel(/permissions/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /create/i })).toBeVisible();
  });

  test('should create a new API key with valid data', async ({ page }) => {
    // Fill out the form
    await page.getByLabel(/name/i).fill('Test E2E Key');
    
    // Select permissions (assuming checkbox or select)
    const permissionsCheckbox = page.getByLabel(/permissions/i);
    if (await permissionsCheckbox.isChecked()) {
      await permissionsCheckbox.uncheck();
    }
    await permissionsCheckbox.check();
    
    // Click create button
    await page.getByRole('button', { name: /create/i }).click();
    
    // Wait for navigation or success message
    await expect(page.getByText(/test e2e key/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show validation error when name is empty', async ({ page }) => {
    // Try to create key without name
    await page.getByRole('button', { name: /create/i }).click();
    
    // Should show validation error
    await expect(page.getByText(/required/i)).toBeVisible()
      .or(page.getByText(/name/i).first());
  });

  test('should display existing API keys', async ({ page }) => {
    // Assuming there are existing keys or we create one first
    await page.getByLabel(/name/i).fill('Display Test Key');
    await page.getByRole('button', { name: /create/i }).click();
    
    // Wait for the key to appear in the list
    await expect(page.getByText(/display test key/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show key hash after creation', async ({ page }) => {
    // Create a new key
    await page.getByLabel(/name/i).fill('Hash Test Key');
    await page.getByRole('button', { name: /create/i }).click();
    
    // Should display key information including hash
    await expect(page.getByText(/sk_/i)).toBeVisible({ timeout: 10000 });
  });
});
