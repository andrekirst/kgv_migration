import { Page, Locator, expect } from '@playwright/test'
import { BasePage } from './base-page'

/**
 * Bezirke Overview Page Object Model
 * 
 * Handles interactions with the Bezirke list/overview page
 */
export class BezirkeOverviewPage extends BasePage {
  constructor(page: Page) {
    super(page)
  }

  // URL and navigation
  get url(): string {
    return '/bezirke'
  }

  // Page elements
  get pageTitle(): Locator {
    return this.page.locator('h1, h2').filter({ hasText: /bezirke verwalten|bezirke übersicht/i })
  }

  get pageDescription(): Locator {
    return this.page.locator('p').filter({ hasText: /übersicht aller kleingartenverein bezirke/i })
  }

  // Header actions
  get neuerBezirkButton(): Locator {
    return this.page.locator('a[href*="/bezirke/neu"], button').filter({ hasText: /neuer bezirk/i })
  }

  get zuParzellenButton(): Locator {
    return this.page.locator('a[href*="/parzellen"], button').filter({ hasText: /zu parzellen/i })
  }

  // Statistics section
  get statisticsSection(): Locator {
    return this.page.locator('[data-testid="bezirke-stats"], .statistics, .stats-grid')
  }

  get statisticsCards(): Locator {
    return this.statisticsSection.locator('.card, [data-testid*="stat"]')
  }

  // Filter section
  get filtersSection(): Locator {
    return this.page.locator('[data-testid="bezirke-filters"], .filters, .search-filters')
  }

  get searchInput(): Locator {
    return this.filtersSection.locator('input[placeholder*="suchen"], input[type="search"]')
  }

  get statusFilter(): Locator {
    return this.filtersSection.locator('select, [role="combobox"]').filter({ hasText: /status|aktiv/i })
  }

  get sortBySelect(): Locator {
    return this.filtersSection.locator('select, [role="combobox"]').filter({ hasText: /sortieren/i })
  }

  // Table section
  get bezirkeTable(): Locator {
    return this.page.locator('table, [data-testid="bezirke-table"]')
  }

  get tableHeader(): Locator {
    return this.bezirkeTable.locator('thead, .table-header')
  }

  get tableBody(): Locator {
    return this.bezirkeTable.locator('tbody, .table-body')
  }

  get tableRows(): Locator {
    return this.tableBody.locator('tr, .table-row')
  }

  get noDataMessage(): Locator {
    return this.page.locator('.no-data, .empty-state').filter({ hasText: /keine bezirke gefunden/i })
  }

  get erstenBezirkErstellenButton(): Locator {
    return this.noDataMessage.locator('button, a').filter({ hasText: /ersten bezirk erstellen/i })
  }

  // Pagination
  get paginationSection(): Locator {
    return this.page.locator('.pagination, [data-testid="pagination"]')
  }

  get vorherigePage(): Locator {
    return this.paginationSection.locator('button, a').filter({ hasText: /vorherige/i })
  }

  get naechstePage(): Locator {
    return this.paginationSection.locator('button, a').filter({ hasText: /nächste/i })
  }

  get pageInfo(): Locator {
    return this.paginationSection.locator('text=/zeige .* von .* bezirken/i')
  }

  // Methods
  async navigateTo(): Promise<void> {
    await this.navigateToUrl(this.url)
    await this.waitForBezirkeOverviewToLoad()
  }

  async waitForBezirkeOverviewToLoad(): Promise<void> {
    await this.waitForPageLoad()
    await this.assertElementVisible(this.pageTitle)
    await this.waitForLoadingToComplete()
    
    // Wait for either table or no-data message
    try {
      await Promise.race([
        this.bezirkeTable.waitFor({ state: 'visible', timeout: 5000 }),
        this.noDataMessage.waitFor({ state: 'visible', timeout: 5000 })
      ])
    } catch {
      // One of them should be visible
    }
  }

  async clickNeuerBezirk(): Promise<void> {
    await this.neuerBezirkButton.click()
    await this.waitForPageLoad()
  }

  async clickErstenBezirkErstellen(): Promise<void> {
    if (await this.isElementVisible(this.erstenBezirkErstellenButton)) {
      await this.erstenBezirkErstellenButton.click()
      await this.waitForPageLoad()
    }
  }

  // Search and filter methods
  async searchBezirke(searchTerm: string): Promise<void> {
    await this.fillInput(this.searchInput, searchTerm)
    await this.page.keyboard.press('Enter')
    await this.waitForApiResponse('/bezirke')
    await this.waitForLoadingToComplete()
  }

  async clearSearch(): Promise<void> {
    await this.searchInput.clear()
    await this.page.keyboard.press('Enter')
    await this.waitForApiResponse('/bezirke')
    await this.waitForLoadingToComplete()
  }

  async filterByStatus(status: 'aktiv' | 'inaktiv' | 'alle'): Promise<void> {
    if (await this.isElementVisible(this.statusFilter)) {
      await this.statusFilter.selectOption({ label: status === 'alle' ? 'Alle' : status === 'aktiv' ? 'Aktiv' : 'Inaktiv' })
      await this.waitForApiResponse('/bezirke')
      await this.waitForLoadingToComplete()
    }
  }

  async sortBy(sortOption: string): Promise<void> {
    if (await this.isElementVisible(this.sortBySelect)) {
      await this.sortBySelect.selectOption({ label: sortOption })
      await this.waitForApiResponse('/bezirke')
      await this.waitForLoadingToComplete()
    }
  }

  // Table interaction methods
  async getBezirkRows(): Promise<Locator[]> {
    return await this.tableRows.all()
  }

  async getBezirkRowCount(): Promise<number> {
    if (await this.isElementVisible(this.noDataMessage)) {
      return 0
    }
    return await this.tableRows.count()
  }

  async getBezirkByName(name: string): Promise<Locator> {
    return this.tableRows.filter({ hasText: name })
  }

  async clickBezirkViewButton(bezirkName: string): Promise<void> {
    const row = await this.getBezirkByName(bezirkName)
    await row.locator('button, a').filter({ hasText: /anzeigen|details/i }).click()
    await this.waitForPageLoad()
  }

  async clickBezirkEditButton(bezirkName: string): Promise<void> {
    const row = await this.getBezirkByName(bezirkName)
    await row.locator('button, a').filter({ hasText: /bearbeiten|edit/i }).click()
    await this.waitForPageLoad()
  }

  async getBezirkData(bezirkName: string): Promise<{
    name: string
    beschreibung?: string
    bezirksleiter?: string
    parzellen?: string
    auslastung?: string
    status?: string
  }> {
    const row = await this.getBezirkByName(bezirkName)
    const cells = await row.locator('td').all()
    
    const data: any = {}
    
    if (cells.length >= 1) {
      const nameCell = cells[0]
      data.name = await this.getElementText(nameCell.locator('.font-medium, strong').first())
      data.beschreibung = await this.getElementText(nameCell.locator('.text-secondary-500, .description').first())
    }
    
    if (cells.length >= 2) {
      data.bezirksleiter = await this.getElementText(cells[1])
    }
    
    if (cells.length >= 3) {
      data.parzellen = await this.getElementText(cells[2])
    }
    
    if (cells.length >= 4) {
      data.auslastung = await this.getElementText(cells[3])
    }
    
    if (cells.length >= 5) {
      const statusCell = cells[4]
      data.status = await this.getElementText(statusCell.locator('.badge, .status').first())
    }
    
    return data
  }

  // Statistics methods
  async getStatisticValue(statName: string): Promise<string> {
    const statCard = this.statisticsCards.filter({ hasText: new RegExp(statName, 'i') })
    const valueElement = statCard.locator('.stat-value, .statistic-value, .text-2xl, .text-3xl, h2, h3').first()
    return await this.getElementText(valueElement)
  }

  async getAllStatistics(): Promise<Record<string, string>> {
    const stats: Record<string, string> = {}
    
    if (await this.isElementVisible(this.statisticsCards)) {
      const statCards = await this.statisticsCards.all()
      
      for (const card of statCards) {
        const label = await card.locator('.stat-label, .label, h3, h4, .text-sm, .text-gray-600').first().textContent()
        const value = await card.locator('.stat-value, .value, .text-2xl, .text-3xl, h2').first().textContent()
        
        if (label && value) {
          stats[label.trim()] = value.trim()
        }
      }
    }
    
    return stats
  }

  // Pagination methods
  async goToNextPage(): Promise<void> {
    if (await this.isElementVisible(this.naechstePage) && await this.naechstePage.isEnabled()) {
      await this.naechstePage.click()
      await this.waitForApiResponse('/bezirke')
      await this.waitForLoadingToComplete()
    }
  }

  async goToPreviousPage(): Promise<void> {
    if (await this.isElementVisible(this.vorherigePage) && await this.vorherigePage.isEnabled()) {
      await this.vorherigePage.click()
      await this.waitForApiResponse('/bezirke')
      await this.waitForLoadingToComplete()
    }
  }

  async getPaginationInfo(): Promise<string> {
    if (await this.isElementVisible(this.pageInfo)) {
      return await this.getElementText(this.pageInfo)
    }
    return ''
  }

  // Assertion methods
  async assertPageLoaded(): Promise<void> {
    await this.assertElementVisible(this.pageTitle)
    await this.assertElementVisible(this.neuerBezirkButton)
  }

  async assertNoBezirkeFound(): Promise<void> {
    await this.assertElementVisible(this.noDataMessage)
    await this.assertElementVisible(this.erstenBezirkErstellenButton)
  }

  async assertBezirkeTableVisible(): Promise<void> {
    await this.assertElementVisible(this.bezirkeTable)
    await this.assertElementVisible(this.tableHeader)
  }

  async assertBezirkInTable(bezirkName: string): Promise<void> {
    const row = await this.getBezirkByName(bezirkName)
    await this.assertElementVisible(row)
  }

  async assertBezirkNotInTable(bezirkName: string): Promise<void> {
    const row = this.tableRows.filter({ hasText: bezirkName })
    await this.assertElementHidden(row)
  }

  async assertStatisticsVisible(): Promise<void> {
    if (await this.isElementVisible(this.statisticsSection)) {
      await this.assertElementVisible(this.statisticsCards.first())
    }
  }

  async assertPaginationVisible(): Promise<void> {
    if (await this.isElementVisible(this.paginationSection)) {
      await this.assertElementVisible(this.pageInfo)
    }
  }

  // Breadcrumb methods
  async assertBreadcrumb(): Promise<void> {
    await expect(this.breadcrumb.locator('text=Bezirke')).toBeVisible()
  }
}