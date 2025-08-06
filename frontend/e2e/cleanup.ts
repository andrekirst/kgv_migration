import { test as cleanup } from '@playwright/test'

/**
 * Cleanup file for individual test runs
 * 
 * This runs after each test project and can be used for
 * cleaning up test data, logging out, etc.
 */
cleanup('cleanup test environment', async ({ page }) => {
  console.log('🧹 Cleaning up test environment...')

  // Clean up any test-specific data
  // For example, delete created bezirke, clear browser storage, etc.

  // Clear browser storage
  await page.context().clearCookies()
  await page.evaluate(() => {
    localStorage.clear()
    sessionStorage.clear()
  })

  console.log('✅ Test environment cleanup complete')
})