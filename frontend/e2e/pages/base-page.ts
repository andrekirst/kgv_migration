import { Page, Locator, expect } from '@playwright/test'

/**
 * Base Page Object Model for KGV application
 * 
 * Contains common functionality shared across all pages
 */
export abstract class BasePage {
  readonly page: Page

  constructor(page: Page) {
    this.page = page
  }

  // Common elements across all pages
  get header(): Locator {
    return this.page.locator('header')
  }

  get navigation(): Locator {
    return this.page.locator('[role="navigation"], nav')
  }

  get sidebar(): Locator {
    return this.page.locator('[role="navigation"], .sidebar, nav[aria-label*="Navigation"]')
  }

  get mainContent(): Locator {
    return this.page.locator('main, [role="main"]')
  }

  get loadingIndicator(): Locator {
    return this.page.locator('[data-testid="loading"], .loading, .animate-spin')
  }

  get toast(): Locator {
    return this.page.locator('[data-testid="toast"], .toast, [role="alert"]')
  }

  get errorMessage(): Locator {
    return this.page.locator('[data-testid="error"], .error, [role="alert"][aria-live="assertive"]')
  }

  // Navigation methods
  async navigateToUrl(path: string): Promise<void> {
    await this.page.goto(path)
    await this.waitForPageLoad()
  }

  async navigateToPage(linkText: string): Promise<void> {
    await this.page.click(`text=${linkText}`)
    await this.waitForPageLoad()
  }

  async navigateViaSidebar(itemText: string): Promise<void> {
    await this.sidebar.getByText(itemText).click()
    await this.waitForPageLoad()
  }

  // Wait methods
  async waitForPageLoad(): Promise<void> {
    await this.page.waitForLoadState('networkidle')
    await this.page.waitForLoadState('domcontentloaded')
  }

  async waitForElement(locator: Locator, timeout = 10000): Promise<void> {
    await expect(locator).toBeVisible({ timeout })
  }

  async waitForElementToDisappear(locator: Locator, timeout = 10000): Promise<void> {
    await expect(locator).toBeHidden({ timeout })
  }

  async waitForText(text: string, timeout = 10000): Promise<void> {
    await expect(this.page.locator(`text=${text}`)).toBeVisible({ timeout })
  }

  async waitForApiResponse(urlPattern: string | RegExp, timeout = 30000): Promise<void> {
    await this.page.waitForResponse(
      response => {
        const url = response.url()
        if (typeof urlPattern === 'string') {
          return url.includes(urlPattern)
        }
        return urlPattern.test(url)
      },
      { timeout }
    )
  }

  // Assertion methods
  async assertPageTitle(expectedTitle: string | RegExp): Promise<void> {
    await expect(this.page).toHaveTitle(expectedTitle)
  }

  async assertUrl(expectedUrl: string | RegExp): Promise<void> {
    await expect(this.page).toHaveURL(expectedUrl)
  }

  async assertElementVisible(locator: Locator): Promise<void> {
    await expect(locator).toBeVisible()
  }

  async assertElementHidden(locator: Locator): Promise<void> {
    await expect(locator).toBeHidden()
  }

  async assertTextVisible(text: string): Promise<void> {
    await expect(this.page.locator(`text=${text}`)).toBeVisible()
  }

  async assertTextNotVisible(text: string): Promise<void> {
    await expect(this.page.locator(`text=${text}`)).toBeHidden()
  }

  // Loading state methods
  async waitForLoadingToComplete(): Promise<void> {
    try {
      await this.loadingIndicator.waitFor({ state: 'visible', timeout: 2000 })
      await this.loadingIndicator.waitFor({ state: 'hidden', timeout: 30000 })
    } catch {
      // Loading indicator might not appear for fast operations
    }
  }

  // Toast methods
  async waitForSuccessToast(timeout = 10000): Promise<void> {
    const successToast = this.page.locator('.toast, [role="alert"]').filter({ hasText: /erfolg|erfolgreich|gespeichert|erstellt/i })
    await expect(successToast).toBeVisible({ timeout })
  }

  async waitForErrorToast(timeout = 10000): Promise<void> {
    const errorToast = this.page.locator('.toast, [role="alert"]').filter({ hasText: /fehler|error|fehlgeschlagen/i })
    await expect(errorToast).toBeVisible({ timeout })
  }

  async dismissToast(): Promise<void> {
    const dismissButton = this.toast.locator('button, [role="button"]')
    if (await dismissButton.isVisible()) {
      await dismissButton.click()
    }
  }

  // Error handling methods
  async handleApiError(): Promise<boolean> {
    try {
      const errorElement = this.errorMessage.first()
      if (await errorElement.isVisible({ timeout: 3000 })) {
        const errorText = await errorElement.textContent()
        console.warn(`API Error detected: ${errorText}`)
        return true
      }
      return false
    } catch {
      return false
    }
  }

  // Screenshot methods
  async takeScreenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `e2e-test-results/screenshots/${name}-${Date.now()}.png`,
      fullPage: true 
    })
  }

  // Scroll methods
  async scrollToElement(locator: Locator): Promise<void> {
    await locator.scrollIntoViewIfNeeded()
  }

  async scrollToTop(): Promise<void> {
    await this.page.evaluate(() => window.scrollTo(0, 0))
  }

  async scrollToBottom(): Promise<void> {
    await this.page.evaluate(() => window.scrollTo(0, document.body.scrollHeight))
  }

  // Form methods
  async fillInput(locator: Locator, value: string): Promise<void> {
    await locator.click()
    await locator.fill(value)
  }

  async clearAndFillInput(locator: Locator, value: string): Promise<void> {
    await locator.click()
    await locator.selectAll()
    await locator.fill(value)
  }

  // Utility methods
  async getElementText(locator: Locator): Promise<string> {
    return await locator.textContent() || ''
  }

  async getElementAttribute(locator: Locator, attribute: string): Promise<string | null> {
    return await locator.getAttribute(attribute)
  }

  async isElementVisible(locator: Locator): Promise<boolean> {
    try {
      await expect(locator).toBeVisible({ timeout: 3000 })
      return true
    } catch {
      return false
    }
  }

  async isElementEnabled(locator: Locator): Promise<boolean> {
    return await locator.isEnabled()
  }
}