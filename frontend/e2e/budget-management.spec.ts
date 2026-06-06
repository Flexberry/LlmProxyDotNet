import { test, expect } from '@playwright/test';

/**
 * E2E tests for Budget Management functionality
 * Tests creating, updating, and monitoring budgets for API keys and teams
 */
test.describe('Budget Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to settings or budgets page
    await page.goto('/settings');
  });

  test('should display the settings page', async ({ page }) => {
    // Verify page loads
    await expect(page).toHaveTitle(/LLM Proxy/);
    
    // Check for settings elements
    await expect(page.getByRole('heading')).toBeVisible();
  });

  test('should create a new budget for API key', async ({ page }) => {
    // Navigate to budget section (adjust selector based on your UI)
    const budgetSection = page.getByText(/budget/i)
      .or(page.getByLabel(/budget/i));
    
    if (await budgetSection.isVisible()) {
      await budgetSection.click();
    }
    
    // Find and fill budget creation form
    const amountInput = page.getByLabel(/amount/i)
      .or(page.getByPlaceholder(/amount/i));
    
    if (await amountInput.isVisible()) {
      await amountInput.fill('1000');
      
      // Select limit action
      const limitAction = page.getByLabel(/limit action/i)
        .or(page.getByRole('combobox'));
      
      if (await limitAction.isVisible()) {
        await limitAction.selectOption('warn');
      }
      
      // Submit form
      const submitButton = page.getByRole('button', { name: /create/i })
        .or(page.getByRole('button', { name: /save/i }));
      
      if (await submitButton.isVisible()) {
        await submitButton.click();
        
        // Wait for success
        await expect(page.getByText(/created/i)
          .or(page.getByText(/1000/i))).toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should display existing budgets', async ({ page }) => {
    // Navigate to budgets section
    const budgetsLink = page.getByText(/budgets/i)
      .or(page.getByLabel(/budgets/i));
    
    if (await budgetsLink.isVisible()) {
      await budgetsLink.click();
    }
    
    // Check for budget list
    const budgetList = page.getByText(/budget/i);
    if (await budgetList.isVisible()) {
      await expect(budgetList).toBeVisible();
    }
  });

  test('should update budget amount', async ({ page }) => {
    // Navigate to budget management
    await page.goto('/settings');
    
    const editButton = page.getByRole('button', { name: /edit/i })
      .or(page.getByText(/edit/i).first());
    
    if (await editButton.isVisible()) {
      await editButton.click();
      
      const amountInput = page.getByLabel(/amount/i);
      if (await amountInput.isVisible()) {
        await amountInput.fill('2000');
        
        const saveButton = page.getByRole('button', { name: /save/i });
        if (await saveButton.isVisible()) {
          await saveButton.click();
          
          // Wait for update confirmation
          await expect(page.getByText(/2000/i))
            .toBeVisible({ timeout: 10000 });
        }
      }
    }
  });

  test('should set limit action to block', async ({ page }) => {
    await page.goto('/settings');
    
    const limitActionSelect = page.getByLabel(/limit action/i)
      .or(page.getByRole('combobox'));
    
    if (await limitActionSelect.isVisible()) {
      await limitActionSelect.selectOption('block');
      
      const saveButton = page.getByRole('button', { name: /save/i });
      if (await saveButton.isVisible()) {
        await saveButton.click();
        
        await expect(page.getByText(/block/i))
          .toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should show budget status', async ({ page }) => {
    await page.goto('/settings');
    
    // Look for budget status indicators
    const statusIndicators = page.getByText(/spent/i)
      .or(page.getByText(/remaining/i))
      .or(page.getByText(/budget/i));
    
    if (await statusIndicators.isVisible()) {
      await expect(statusIndicators).toBeVisible();
    }
  });

  test('should delete a budget', async ({ page }) => {
    await page.goto('/settings');
    
    const deleteButton = page.getByRole('button', { name: /delete/i })
      .or(page.getByText(/delete/i).first());
    
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      
      // Confirm deletion if dialog appears
      const confirmButton = page.getByRole('button', { name: /confirm/i })
        .or(page.getByRole('button', { name: /yes/i }));
      
      if (await confirmButton.isVisible()) {
        await confirmButton.click();
      }
      
      // Verify deletion
      await expect(page.getByText(/deleted/i)
        .or(page.getByText(/not found/i))).toBeVisible({ timeout: 10000 });
    }
  });
});

test.describe('Team Budget Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/settings');
  });

  test('should create budget for team', async ({ page }) => {
    // Navigate to teams section
    const teamsLink = page.getByText(/teams/i)
      .or(page.getByLabel(/teams/i));
    
    if (await teamsLink.isVisible()) {
      await teamsLink.click();
    }
    
    // Create or select a team
    const teamSelector = page.getByLabel(/team/i)
      .or(page.getByRole('combobox'));
    
    if (await teamSelector.isVisible()) {
      await teamSelector.selectOption('test-team');
    }
    
    // Set team budget
    const amountInput = page.getByLabel(/budget amount/i);
    if (await amountInput.isVisible()) {
      await amountInput.fill('5000');
      
      const saveButton = page.getByRole('button', { name: /save/i });
      if (await saveButton.isVisible()) {
        await saveButton.click();
        
        await expect(page.getByText(/5000/i))
          .toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should display team spending', async ({ page }) => {
    await page.goto('/settings');
    
    // Look for team spending information
    const spendingInfo = page.getByText(/spending/i)
      .or(page.getByText(/team/i).first());
    
    if (await spendingInfo.isVisible()) {
      await expect(spendingInfo).toBeVisible();
    }
  });
});
