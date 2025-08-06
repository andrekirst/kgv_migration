import { chromium, FullConfig } from '@playwright/test'

/**
 * Global setup for KGV E2E tests
 * 
 * Initializes test environment, checks service health,
 * and prepares test data before running test suites
 */
async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting KGV E2E Test Global Setup...')

  const baseURL = config.projects[0]?.use?.baseURL || 'http://localhost:3001'
  const apiURL = process.env.PLAYWRIGHT_API_BASE_URL || 'http://localhost:8080'
  
  console.log(`Frontend URL: ${baseURL}`)
  console.log(`API URL: ${apiURL}`)

  // Launch a browser to perform setup tasks
  const browser = await chromium.launch()
  const context = await browser.newContext()
  const page = await context.newPage()

  try {
    // Check if frontend is accessible
    console.log('🔍 Checking frontend health...')
    
    let frontendReady = false
    let attempts = 0
    const maxAttempts = 30 // 30 seconds timeout
    
    while (!frontendReady && attempts < maxAttempts) {
      try {
        await page.goto(`${baseURL}/api/health`, { 
          timeout: 5000,
          waitUntil: 'networkidle' 
        })
        
        const response = await page.waitForResponse(
          (response) => response.url().includes('/api/health') && response.status() === 200,
          { timeout: 2000 }
        )
        
        if (response.ok()) {
          frontendReady = true
          console.log('✅ Frontend is ready')
        }
      } catch (error) {
        attempts++
        console.log(`⏳ Frontend not ready yet, attempt ${attempts}/${maxAttempts}`)
        await page.waitForTimeout(1000)
      }
    }

    if (!frontendReady) {
      throw new Error('Frontend server failed to start within 30 seconds')
    }

    // Check backend API health
    console.log('🔍 Checking backend API health...')
    
    let apiReady = false
    attempts = 0
    
    while (!apiReady && attempts < maxAttempts) {
      try {
        await page.goto(`${apiURL}/health`, { 
          timeout: 5000,
          waitUntil: 'networkidle' 
        })
        
        const response = await page.waitForResponse(
          (response) => response.url().includes('/health') && response.status() === 200,
          { timeout: 2000 }
        )
        
        if (response.ok()) {
          apiReady = true
          console.log('✅ Backend API is ready')
        }
      } catch (error) {
        attempts++
        console.log(`⏳ Backend API not ready yet, attempt ${attempts}/${maxAttempts}`)
        await page.waitForTimeout(1000)
      }
    }

    if (!apiReady) {
      console.warn('⚠️ Backend API is not accessible - tests may fail')
    }

    // Set up test environment variables
    process.env.E2E_FRONTEND_URL = baseURL
    process.env.E2E_API_URL = apiURL
    process.env.E2E_TEST_TIMESTAMP = new Date().toISOString()

    console.log('🧪 Preparing test data...')
    
    // You can add test data preparation here
    // For example, creating test users, clearing test database, etc.
    
    console.log('✅ Global setup completed successfully')

  } catch (error) {
    console.error('❌ Global setup failed:', error)
    throw error
  } finally {
    await context.close()
    await browser.close()
  }
}

export default globalSetup