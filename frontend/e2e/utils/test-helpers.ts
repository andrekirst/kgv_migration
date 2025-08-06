import { Page, expect, Locator } from '@playwright/test'

/**
 * Test Helper Utilities for KGV E2E Tests
 * 
 * Common utilities and helper functions for test operations
 */

export class TestHelpers {
  constructor(private page: Page) {}

  // URL and navigation helpers
  static getBaseUrl(): string {
    return process.env.E2E_FRONTEND_URL || process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3001'
  }

  static getApiUrl(): string {
    return process.env.E2E_API_URL || process.env.PLAYWRIGHT_API_BASE_URL || 'http://localhost:8080'
  }

  // Wait helpers with retry logic
  async waitWithRetry<T>(
    operation: () => Promise<T>,
    options: {
      retries?: number
      delay?: number
      timeout?: number
      errorMessage?: string
    } = {}
  ): Promise<T> {
    const { retries = 3, delay = 1000, timeout = 10000, errorMessage = 'Operation failed' } = options
    
    for (let attempt = 1; attempt <= retries; attempt++) {
      try {
        return await Promise.race([
          operation(),
          new Promise<never>((_, reject) => 
            setTimeout(() => reject(new Error(`Timeout after ${timeout}ms`)), timeout)
          )
        ])
      } catch (error) {
        if (attempt === retries) {
          throw new Error(`${errorMessage} after ${retries} attempts: ${error}`)
        }
        
        console.log(`Attempt ${attempt} failed, retrying in ${delay}ms...`)
        await this.page.waitForTimeout(delay)
      }
    }
    
    throw new Error(`${errorMessage} - should not reach here`)
  }

  // API interaction helpers
  async waitForApiCall(urlPattern: string | RegExp, method = 'GET'): Promise<void> {
    await this.page.waitForResponse(
      response => {
        const url = response.url()
        const matchesUrl = typeof urlPattern === 'string' 
          ? url.includes(urlPattern)
          : urlPattern.test(url)
        const matchesMethod = response.request().method() === method
        return matchesUrl && matchesMethod
      }
    )
  }

  async interceptApiCall(
    urlPattern: string | RegExp, 
    mockResponse: any,
    statusCode = 200
  ): Promise<void> {
    await this.page.route(urlPattern, async route => {
      await route.fulfill({
        status: statusCode,
        contentType: 'application/json',
        body: JSON.stringify(mockResponse)
      })
    })
  }

  async simulateApiError(urlPattern: string | RegExp, statusCode = 500): Promise<void> {
    await this.page.route(urlPattern, async route => {
      await route.fulfill({
        status: statusCode,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Simulated API error for testing',
          status: statusCode,
          timestamp: new Date().toISOString()
        })
      })
    })
  }

  async removeApiIntercept(urlPattern: string | RegExp): Promise<void> {
    await this.page.unroute(urlPattern)
  }

  // Form helpers
  async fillFormField(
    fieldLocator: Locator, 
    value: string, 
    options: { clear?: boolean; blur?: boolean } = {}
  ): Promise<void> {
    const { clear = true, blur = true } = options
    
    await fieldLocator.click()
    
    if (clear) {
      await fieldLocator.selectAll()
    }
    
    await fieldLocator.fill(value)
    
    if (blur) {
      await fieldLocator.blur()
    }
  }

  async submitFormAndWait(
    submitButton: Locator, 
    expectationOrUrl: string | RegExp | (() => Promise<void>)
  ): Promise<void> {
    const submitPromise = submitButton.click()
    
    if (typeof expectationOrUrl === 'function') {
      await Promise.all([expectationOrUrl(), submitPromise])
    } else {
      await Promise.all([
        this.page.waitForURL(expectationOrUrl),
        submitPromise
      ])
    }
  }

  // Error handling helpers
  async capturePageState(testName: string): Promise<void> {
    const timestamp = Date.now()
    const fileName = `${testName}-${timestamp}`
    
    // Take screenshot
    await this.page.screenshot({ 
      path: `e2e-test-results/debug/${fileName}.png`,
      fullPage: true 
    })
    
    // Save HTML
    const html = await this.page.content()
    require('fs').writeFileSync(
      `e2e-test-results/debug/${fileName}.html`, 
      html
    )
    
    // Log console messages
    const logs = await this.page.evaluate(() => {
      const logs: string[] = []
      // Try to capture any console logs if available
      return logs
    })
    
    console.log(`Debug state captured for ${testName}:`)
    console.log(`- Screenshot: e2e-test-results/debug/${fileName}.png`)
    console.log(`- HTML: e2e-test-results/debug/${fileName}.html`)
  }

  async handleApiErrorStates(testOperation: () => Promise<void>): Promise<void> {
    try {
      await testOperation()
    } catch (error) {
      // Check for common API error indicators
      const errorToast = this.page.locator('.toast, [role="alert"]').filter({ 
        hasText: /fehler|error|500|server/i 
      })
      
      const errorMessage = this.page.locator('.error-message, .alert-error')
      const whiteScreen = this.page.locator('body:empty, body:has(.error-boundary)')
      
      if (await errorToast.isVisible({ timeout: 2000 })) {
        console.log('API Error detected via toast notification')
        const toastText = await errorToast.textContent()
        console.log(`Toast message: ${toastText}`)
      }
      
      if (await errorMessage.isVisible({ timeout: 2000 })) {
        console.log('API Error detected via error message')
        const errorText = await errorMessage.textContent()
        console.log(`Error message: ${errorText}`)
      }
      
      if (await whiteScreen.isVisible({ timeout: 2000 })) {
        console.log('White screen or error boundary detected')
      }
      
      // Re-throw the original error for proper test reporting
      throw error
    }
  }

  // Data generation helpers
  generateTestBezirk(suffix?: string): { name: string; beschreibung: string } {
    const timestamp = Date.now()
    const suffixStr = suffix ? `-${suffix}` : ''
    
    return {
      name: `Test${timestamp.toString().slice(-4)}${suffixStr}`,
      beschreibung: `Test Bezirk erstellt für E2E Tests am ${new Date().toLocaleString('de-DE')}`
    }
  }

  generateInvalidBezirkData(): Array<{ 
    name: string
    data: { name: string; beschreibung?: string }
    expectedError: string 
  }> {
    return [
      {
        name: 'empty name',
        data: { name: '', beschreibung: 'Valid description' },
        expectedError: 'Name ist erforderlich'
      },
      {
        name: 'name too long',
        data: { name: 'ThisNameIsTooLongForTheField', beschreibung: 'Valid description' },
        expectedError: 'Name darf maximal 10 Zeichen haben'
      },
      {
        name: 'invalid characters in name',
        data: { name: 'Test@#$', beschreibung: 'Valid description' },
        expectedError: 'Name enthält ungültige Zeichen'
      },
      {
        name: 'description too long',
        data: { 
          name: 'Valid', 
          beschreibung: 'A'.repeat(501) // Assuming 500 char limit
        },
        expectedError: 'Beschreibung darf maximal 500 Zeichen haben'
      }
    ]
  }

  // Assertion helpers
  async assertNoJavaScriptErrors(): Promise<void> {
    const jsErrors: string[] = []
    
    this.page.on('pageerror', error => {
      jsErrors.push(error.message)
    })
    
    this.page.on('console', msg => {
      if (msg.type() === 'error') {
        jsErrors.push(msg.text())
      }
    })
    
    // Small delay to catch any immediate errors
    await this.page.waitForTimeout(1000)
    
    if (jsErrors.length > 0) {
      throw new Error(`JavaScript errors detected: ${jsErrors.join(', ')}`)
    }
  }

  async assertNetworkRequests(expectedRequests: string[]): Promise<void> {
    const requests: string[] = []
    
    this.page.on('request', request => {
      requests.push(request.url())
    })
    
    await this.page.waitForTimeout(2000) // Allow time for requests
    
    for (const expectedRequest of expectedRequests) {
      const found = requests.some(url => url.includes(expectedRequest))
      if (!found) {
        throw new Error(`Expected network request not found: ${expectedRequest}`)
      }
    }
  }

  async assertPagePerformance(maxLoadTime: number = 5000): Promise<void> {
    const startTime = Date.now()
    await this.page.waitForLoadState('networkidle')
    const loadTime = Date.now() - startTime
    
    if (loadTime > maxLoadTime) {
      throw new Error(`Page load time ${loadTime}ms exceeded maximum ${maxLoadTime}ms`)
    }
  }

  // Accessibility helpers
  async checkBasicAccessibility(): Promise<void> {
    // Check for basic accessibility issues
    const missingAltImages = await this.page.locator('img:not([alt])').count()
    const missingFormLabels = await this.page.locator('input:not([aria-label]):not([aria-labelledby])').count()
    const invalidHeadingOrder = await this.checkHeadingOrder()
    
    const issues: string[] = []
    
    if (missingAltImages > 0) {
      issues.push(`${missingAltImages} images without alt text`)
    }
    
    if (missingFormLabels > 0) {
      issues.push(`${missingFormLabels} form inputs without labels`)
    }
    
    if (invalidHeadingOrder) {
      issues.push('Invalid heading order detected')
    }
    
    if (issues.length > 0) {
      console.warn(`Accessibility issues found: ${issues.join(', ')}`)
    }
  }

  private async checkHeadingOrder(): Promise<boolean> {
    const headings = await this.page.locator('h1, h2, h3, h4, h5, h6').allTextContents()
    // Simple check - should start with h1 and not skip levels
    // This is a basic implementation
    return false // Placeholder - implement proper heading order check
  }

  // Cleanup helpers
  async cleanupTestData(testDataIdentifiers: string[]): Promise<void> {
    // This would typically make API calls to clean up test data
    console.log(`Cleaning up test data: ${testDataIdentifiers.join(', ')}`)
    
    for (const identifier of testDataIdentifiers) {
      try {
        // Example: await this.page.request.delete(`/api/bezirke/${identifier}`)
        console.log(`Cleaned up: ${identifier}`)
      } catch (error) {
        console.warn(`Failed to clean up ${identifier}:`, error)
      }
    }
  }

  async resetApplicationState(): Promise<void> {
    // Clear browser storage
    await this.page.context().clearCookies()
    await this.page.evaluate(() => {
      localStorage.clear()
      sessionStorage.clear()
    })
    
    // Navigate to clean state
    await this.page.goto('/')
  }
}

// Static utility functions that don't need page instance
export class StaticTestHelpers {
  static formatTimestamp(): string {
    return new Date().toISOString().replace(/[:.]/g, '-')
  }

  static generateUniqueId(): string {
    return Math.random().toString(36).substring(2) + Date.now().toString(36)
  }

  static sanitizeTestName(name: string): string {
    return name.toLowerCase().replace(/[^a-z0-9]/g, '-')
  }

  static createTestTimeout(baseTimeout: number, factor = 1): number {
    const isCi = process.env.CI === 'true'
    const multiplier = isCi ? 2 : 1 // Longer timeouts in CI
    return baseTimeout * factor * multiplier
  }

  static shouldSkipTest(reason: string): boolean {
    const skipReasons = {
      'api-unavailable': process.env.SKIP_API_TESTS === 'true',
      'slow-tests': process.env.SKIP_SLOW_TESTS === 'true',
      'visual-tests': process.env.SKIP_VISUAL_TESTS === 'true'
    }
    
    return skipReasons[reason as keyof typeof skipReasons] || false
  }
}