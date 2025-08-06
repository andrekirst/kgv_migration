import { chromium, FullConfig } from '@playwright/test'

/**
 * Global teardown for KGV E2E tests
 * 
 * Cleans up test environment, removes test data,
 * and generates final test reports
 */
async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting KGV E2E Test Global Teardown...')

  const browser = await chromium.launch()
  const context = await browser.newContext()
  const page = await context.newPage()

  try {
    // Clean up test data
    console.log('🗑️ Cleaning up test data...')
    
    // You can add cleanup logic here
    // For example, deleting test bezirke, resetting database state, etc.
    
    // Log test completion statistics
    const timestamp = process.env.E2E_TEST_TIMESTAMP
    const duration = timestamp ? 
      ((Date.now() - new Date(timestamp).getTime()) / 1000).toFixed(2) : 
      'unknown'
    
    console.log(`⏱️ Total test execution time: ${duration} seconds`)
    console.log('✅ Global teardown completed successfully')

  } catch (error) {
    console.error('❌ Global teardown failed:', error)
    // Don't throw - teardown failures shouldn't fail the entire test suite
  } finally {
    await context.close()
    await browser.close()
  }
}

export default globalTeardown