import { Page, Locator, expect } from '@playwright/test'
import { BasePage } from './base-page'

/**
 * Dashboard Page Object Model
 * 
 * Handles interactions with the main dashboard and navigation
 */
export class DashboardPage extends BasePage {
  constructor(page: Page) {
    super(page)
  }

  // Page elements
  get pageTitle(): Locator {
    return this.page.locator('h1, h2').filter({ hasText: /dashboard|übersicht/i })
  }

  get welcomeMessage(): Locator {
    return this.page.locator('text=/willkommen|guten tag|hallo/i')
  }

  get statisticsCards(): Locator {
    return this.page.locator('[data-testid="dashboard-stats"], .dashboard-stats, .statistics-card')
  }

  get quickActions(): Locator {
    return this.page.locator('[data-testid="quick-actions"], .quick-actions')
  }

  get recentActivity(): Locator {
    return this.page.locator('[data-testid="recent-activity"], .recent-activity')
  }

  // Navigation elements in sidebar
  get bezirkeNavLink(): Locator {
    return this.sidebar.locator('a[href*="/bezirke"], text=bezirke', { hasText: /bezirke/i })
  }

  get antraegeNavLink(): Locator {
    return this.sidebar.locator('a[href*="/antraege"], text=anträge', { hasText: /anträge/i })
  }

  get parzellenNavLink(): Locator {
    return this.sidebar.locator('a[href*="/parzellen"], text=parzellen', { hasText: /parzellen/i })
  }

  get dashboardNavLink(): Locator {
    return this.sidebar.locator('a[href="/"], text=dashboard', { hasText: /dashboard|übersicht/i })
  }

  // Quick action buttons
  get neuerBezirkButton(): Locator {
    return this.quickActions.locator('button, a').filter({ hasText: /neuer bezirk|bezirk erstellen/i })
  }

  get neueParzelleButton(): Locator {
    return this.quickActions.locator('button, a').filter({ hasText: /neue parzelle|parzelle erstellen/i })
  }

  get neuerAntragButton(): Locator {
    return this.quickActions.locator('button, a').filter({ hasText: /neuer antrag|antrag erstellen/i })
  }

  // Methods
  async navigateTo(): Promise<void> {
    await this.navigateToUrl('/')
    await this.waitForDashboardToLoad()
  }

  async waitForDashboardToLoad(): Promise<void> {
    await this.waitForPageLoad()
    await this.assertElementVisible(this.pageTitle)
    await this.waitForLoadingToComplete()
  }

  async navigateToBezirke(): Promise<void> {
    await this.bezirkeNavLink.click()
    await this.waitForPageLoad()
  }

  async navigateToAntraege(): Promise<void> {
    await this.antraegeNavLink.click()
    await this.waitForPageLoad()
  }

  async navigateToParzellen(): Promise<void> {
    await this.parzellenNavLink.click()
    await this.waitForPageLoad()
  }

  async navigateToNeuerBezirk(): Promise<void> {
    if (await this.isElementVisible(this.neuerBezirkButton)) {
      await this.neuerBezirkButton.click()
    } else {
      // Fallback: navigate via menu then to create form
      await this.navigateToBezirke()
      await this.page.locator('a[href*="/bezirke/neu"], button').filter({ hasText: /neuer bezirk|erstellen/i }).click()
    }
    await this.waitForPageLoad()
  }

  // Assertion methods
  async assertDashboardLoaded(): Promise<void> {
    await this.assertElementVisible(this.pageTitle)
    await this.assertElementVisible(this.sidebar)
  }

  async assertNavigationVisible(): Promise<void> {
    await this.assertElementVisible(this.bezirkeNavLink)
    await this.assertElementVisible(this.antraegeNavLink)
    await this.assertElementVisible(this.parzellenNavLink)
  }

  async assertStatisticsVisible(): Promise<void> {
    if (await this.isElementVisible(this.statisticsCards)) {
      await this.assertElementVisible(this.statisticsCards)
    }
  }

  async assertQuickActionsVisible(): Promise<void> {
    if (await this.isElementVisible(this.quickActions)) {
      await this.assertElementVisible(this.quickActions)
    }
  }

  // Statistics methods
  async getStatisticValue(statName: string): Promise<string> {
    const statCard = this.statisticsCards.filter({ hasText: new RegExp(statName, 'i') })
    const valueElement = statCard.locator('.stat-value, .statistic-value, .number, .count').first()
    return await this.getElementText(valueElement)
  }

  async getAllStatistics(): Promise<Record<string, string>> {
    const stats: Record<string, string> = {}
    const statCards = await this.statisticsCards.all()
    
    for (const card of statCards) {
      const label = await card.locator('.stat-label, .label, h3, h4').first().textContent()
      const value = await card.locator('.stat-value, .value, .number, .count').first().textContent()
      
      if (label && value) {
        stats[label.trim()] = value.trim()
      }
    }
    
    return stats
  }

  // Theme and settings
  async toggleTheme(): Promise<void> {
    const themeToggle = this.page.locator('[data-testid="theme-toggle"], .theme-toggle, button[aria-label*="theme"]')
    if (await this.isElementVisible(themeToggle)) {
      await themeToggle.click()
    }
  }

  async openUserMenu(): Promise<void> {
    const userMenu = this.page.locator('[data-testid="user-menu"], .user-menu, button[aria-label*="user"]')
    if (await this.isElementVisible(userMenu)) {
      await userMenu.click()
    }
  }

  // Breadcrumb navigation
  get breadcrumb(): Locator {
    return this.page.locator('nav[aria-label="Breadcrumb"], .breadcrumb')
  }

  async assertBreadcrumb(items: string[]): Promise<void> {
    for (const item of items) {
      await expect(this.breadcrumb.locator(`text=${item}`)).toBeVisible()
    }
  }
}