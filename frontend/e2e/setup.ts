import { test as setup, expect } from '@playwright/test'

/**
 * Setup file for individual test runs
 * 
 * This runs before each test project and can be used for
 * authentication, data preparation, etc.
 */
setup('setup test environment', async ({ page }) => {
  console.log('🔧 Setting up test environment...')

  // Navigate to the application
  await page.goto('/')

  // Wait for the application to load
  await expect(page).toHaveTitle(/KGV/)

  // You can add authentication setup here if needed
  // For example, login as a test user, set tokens, etc.

  console.log('✅ Test environment setup complete')
})