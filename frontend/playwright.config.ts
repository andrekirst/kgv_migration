import { defineConfig, devices } from '@playwright/test'

/**
 * KGV (Kleingartenverein) E2E Test Configuration
 * 
 * Comprehensive Playwright configuration for testing the KGV management system
 * Supports cross-browser testing, error handling, and CI/CD integration
 */
export default defineConfig({
  // Test directory
  testDir: './e2e',
  
  // Run tests in files in parallel
  fullyParallel: true,
  
  // Fail the build on CI if you accidentally left test.only in the source code.
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests on CI.
  workers: process.env.CI ? 1 : undefined,
  
  // Reporter to use. See https://playwright.dev/docs/test-reporters
  reporter: [
    ['html', { outputFolder: 'e2e-test-results/html-report' }],
    ['json', { outputFile: 'e2e-test-results/test-results.json' }],
    ['junit', { outputFile: 'e2e-test-results/results.xml' }],
    // Add line reporter for better CI output
    ['line']
  ],
  
  // Shared settings for all tests
  use: {
    // Base URL for all tests
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3001',
    
    // API URL for backend calls
    extraHTTPHeaders: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    
    // Collect trace when retrying the failed test.
    trace: 'on-first-retry',
    
    // Screenshots on failure
    screenshot: 'only-on-failure',
    
    // Videos on failure
    video: 'retain-on-failure',
    
    // Browser timeout
    actionTimeout: 10000,
    navigationTimeout: 30000,
    
    // Locale for German UI
    locale: 'de-DE',
    timezoneId: 'Europe/Berlin',
    
    // Ignore HTTPS errors (for development)
    ignoreHTTPSErrors: true
  },
  
  // Configure projects for major browsers
  projects: [
    {
      name: 'setup',
      testMatch: '**/setup.ts',
      teardown: 'cleanup'
    },
    {
      name: 'cleanup', 
      testMatch: '**/cleanup.ts'
    },
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        // Enable additional chromium features
        launchOptions: {
          args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-dev-shm-usage',
            '--disable-web-security'
          ]
        }
      },
      dependencies: ['setup']
    },
    {
      name: 'firefox',
      use: { 
        ...devices['Desktop Firefox'] 
      },
      dependencies: ['setup']
    },
    {
      name: 'webkit',
      use: { 
        ...devices['Desktop Safari'] 
      },
      dependencies: ['setup']
    },
    // Mobile testing
    {
      name: 'mobile-chrome',
      use: { 
        ...devices['Pixel 5'] 
      },
      dependencies: ['setup']
    },
    {
      name: 'mobile-safari',
      use: { 
        ...devices['iPhone 12'] 
      },
      dependencies: ['setup']
    },
    // API testing project
    {
      name: 'api',
      testMatch: '**/api/*.spec.ts',
      use: {
        baseURL: process.env.PLAYWRIGHT_API_BASE_URL || 'http://localhost:8080'
      }
    }
  ],
  
  // Global setup and teardown
  globalSetup: require.resolve('./e2e/global-setup.ts'),
  globalTeardown: require.resolve('./e2e/global-teardown.ts'),
  
  // Test output directory
  outputDir: 'e2e-test-results/',
  
  // Directory for test artifacts
  testIgnore: ['**/node_modules/**'],
  
  // Web Server - starts the dev server before running tests
  webServer: [
    {
      // Frontend server
      command: 'npm run dev',
      port: 3001,
      reuseExistingServer: !process.env.CI,
      timeout: 120000,
      env: {
        NODE_ENV: 'test',
        NEXT_PUBLIC_API_URL: 'http://localhost:8080'
      }
    },
    // API server would be started separately via docker-compose
  ],
  
  // Test timeout
  timeout: 30000,
  
  // Global test settings
  expect: {
    // Maximum time expect() should wait for the condition to be met
    timeout: 10000,
    // Threshold for screenshot comparison
    threshold: 0.3
  },
  
  // Configure test artifacts
  metadata: {
    'kgv-version': '1.0.0',
    'test-environment': process.env.NODE_ENV || 'development',
    'api-url': process.env.PLAYWRIGHT_API_BASE_URL || 'http://localhost:8080',
    'frontend-url': process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3001'
  }
})