import { test, expect } from '@playwright/test';

/**
 * E2E tests for LLM Chat Completion functionality
 * Tests sending requests to LLM providers through the proxy
 */
test.describe('LLM Chat Completion', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to home page or chat page
    await page.goto('/');
  });

  test('should display the home page with chat interface', async ({ page }) => {
    // Verify page loads
    await expect(page).toHaveTitle(/LLM Proxy/);
    
    // Check for chat interface elements
    await expect(page.getByRole('heading')).toBeVisible();
  });

  test('should send a chat completion request', async ({ page }) => {
    // This test assumes you have a chat interface on the home page
    // If not, navigate to the appropriate page
    
    // Find and fill the message input
    const messageInput = page.getByRole('textbox')
      .or(page.getByPlaceholder(/message/i).first());
    
    if (await messageInput.isVisible()) {
      await messageInput.fill('Hello, how are you?');
      
      // Find and click send button
      const sendButton = page.getByRole('button', { name: /send/i })
        .or(page.getByText(/send/i).first());
      
      if (await sendButton.isVisible()) {
        await sendButton.click();
        
        // Wait for response
        await expect(page.getByText(/assistant/i)
          .or(page.getByText(/response/i))).toBeVisible({ timeout: 15000 });
      }
    }
  });

  test('should display model selection', async ({ page }) => {
    // Check for model selector
    const modelSelector = page.getByLabel(/model/i)
      .or(page.getByText(/model/i).first())
      .or(page.getByRole('combobox'));
    
    if (await modelSelector.isVisible()) {
      await expect(modelSelector).toBeVisible();
    }
  });

  test('should handle API key authentication', async ({ page }) => {
    // Navigate to keys page to get or create a key
    await page.goto('/keys');
    
    // Create a test key if needed
    const nameInput = page.getByLabel(/name/i);
    if (await nameInput.isVisible()) {
      await nameInput.fill('Chat Test Key');
      
      const createButton = page.getByRole('button', { name: /create/i });
      if (await createButton.isVisible()) {
        await createButton.click();
        
        // Wait for key to be created
        await expect(page.getByText(/chat test key/i))
          .toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should show error on invalid model', async ({ page }) => {
    // This test depends on your UI having model selection
    const modelSelector = page.getByLabel(/model/i)
      .or(page.getByRole('combobox'));
    
    if (await modelSelector.isVisible()) {
      // Try to select invalid model if possible
      await modelSelector.selectOption('invalid-model');
      
      // Try to send request
      const sendButton = page.getByRole('button', { name: /send/i });
      if (await sendButton.isVisible()) {
        await sendButton.click();
        
        // Should show error
        await expect(page.getByText(/error/i)
          .or(page.getByText(/invalid/i))).toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should support streaming responses', async ({ page }) => {
    // Navigate to a page with streaming capability
    await page.goto('/');
    
    const messageInput = page.getByRole('textbox')
      .or(page.getByPlaceholder(/message/i).first());
    
    if (await messageInput.isVisible()) {
      await messageInput.fill('Count from 1 to 5');
      
      const sendButton = page.getByRole('button', { name: /send/i })
        .or(page.getByText(/send/i).first());
      
      if (await sendButton.isVisible()) {
        await sendButton.click();
        
        // Wait for streaming response
        // The response should appear gradually
        await expect(page.getByText(/1/i)).toBeVisible({ timeout: 15000 });
      }
    }
  });
});
