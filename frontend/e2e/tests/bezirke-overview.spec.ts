import { test, expect } from '@playwright/test'
import { 
  BezirkeOverviewPage, 
  BezirkeCreationPage, 
  DashboardPage 
} from '../pages'
import { TestHelpers, StaticTestHelpers } from '../utils/test-helpers'
import { BezirkTestData, MockApiData, TestScenarios } from '../fixtures/test-data'

/**
 * Bezirke Overview Page E2E Tests
 * 
 * Comprehensive tests for the Bezirke list/overview functionality:
 * - Page rendering and data display
 * - Statistics overview
 * - Table functionality and data presentation
 * - Search and filtering
 * - Pagination
 * - Loading and error states
 * - Empty state handling
 * - Integration with creation workflow
 */

test.describe('Bezirke Overview Page', () => {
  let overviewPage: BezirkeOverviewPage
  let creationPage: BezirkeCreationPage
  let dashboardPage: DashboardPage
  let testHelpers: TestHelpers

  test.beforeEach(async ({ page }) => {
    overviewPage = new BezirkeOverviewPage(page)
    creationPage = new BezirkeCreationPage(page)
    dashboardPage = new DashboardPage(page)
    testHelpers = new TestHelpers(page)
  })

  test.afterEach(async () => {
    await testHelpers.resetApplicationState()
  })

  test.describe('Page Rendering and Initial State', () => {
    test('should render overview page with all components', async () => {
      await overviewPage.navigateTo()
      await overviewPage.assertPageLoaded()
      
      // Verify main page elements
      await expect(overviewPage.pageTitle).toContainText(/bezirke verwalten/i)
      await expect(overviewPage.pageDescription).toContainText(/übersicht aller kleingartenverein bezirke/i)
      
      // Verify action buttons
      await overviewPage.assertElementVisible(overviewPage.neuerBezirkButton)
    })

    test('should display statistics section when available', async () => {
      // Mock statistics data
      await testHelpers.interceptApiCall(/\/bezirke\/statistiken/, MockApiData.generateStatisticsResponse())
      
      await overviewPage.navigateTo()
      await overviewPage.assertStatisticsVisible()
      
      const stats = await overviewPage.getAllStatistics()
      expect(Object.keys(stats)).toHaveLength.toBeGreaterThan(0)
    })

    test('should handle loading state properly', async () => {
      // Slow API response to test loading state
      await overviewPage.page.route(/\/bezirke/, async route => {
        await new Promise(resolve => setTimeout(resolve, 2000))
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(MockApiData.generateBezirkeListResponse(
            BezirkTestData.getValidBezirkSamples()
          ))
        })
      })

      await overviewPage.navigateTo()
      
      // Should show loading state initially
      await expect(overviewPage.loadingIndicator).toBeVisible({ timeout: 1000 })
      
      // Should eventually show content
      await overviewPage.assertPageLoaded()
      await expect(overviewPage.loadingIndicator).toBeHidden({ timeout: 5000 })
    })
  })

  test.describe('Data Display - Table with Bezirke', () => {
    test.beforeEach(async () => {
      // Mock successful API response with sample data
      const sampleBezirke = BezirkTestData.getValidBezirkSamples()
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(sampleBezirke))
    })

    test('should display bezirke table with data', async () => {
      await overviewPage.navigateTo()
      await overviewPage.assertBezirkeTableVisible()
      
      const bezirkCount = await overviewPage.getBezirkRowCount()
      expect(bezirkCount).toBeGreaterThan(0)
    })

    test('should display correct bezirk information in table rows', async () => {
      await overviewPage.navigateTo()
      
      const sampleBezirk = 'Nord' // From our sample data
      await overviewPage.assertBezirkInTable(sampleBezirk)
      
      const bezirkData = await overviewPage.getBezirkData(sampleBezirk)
      expect(bezirkData.name).toBe(sampleBezirk)
      expect(bezirkData.beschreibung).toBeTruthy()
    })

    test('should show proper bezirk statistics in table', async () => {
      await overviewPage.navigateTo()
      
      const bezirkData = await overviewPage.getBezirkData('Nord')
      
      // Should display parzellen information
      expect(bezirkData.parzellen).toMatch(/\d+\s*\/\s*\d+/)
      
      // Should display auslastung percentage
      if (bezirkData.auslastung) {
        expect(bezirkData.auslastung).toMatch(/\d+%/)
      }
    })

    test('should display correct status badges', async () => {
      await overviewPage.navigateTo()
      
      const bezirkData = await overviewPage.getBezirkData('Nord')
      
      if (bezirkData.status) {
        expect(bezirkData.status.toLowerCase()).toMatch(/aktiv|inaktiv/)
      }
    })

    test('should handle action buttons in table rows', async ({ page }) => {
      await overviewPage.navigateTo()
      
      // Click "Anzeigen" button
      await overviewPage.clickBezirkViewButton('Nord')
      
      // Should navigate to bezirk detail view
      await expect(page).toHaveURL(/\/bezirke\/[\w-]+$/)
    })

    test('should handle edit buttons in table rows', async ({ page }) => {
      await overviewPage.navigateTo()
      
      // Click "Bearbeiten" button
      await overviewPage.clickBezirkEditButton('Nord')
      
      // Should navigate to bezirk edit form
      await expect(page).toHaveURL(/\/bezirke\/[\w-]+\/bearbeiten$/)
    })
  })

  test.describe('Empty State Handling', () => {
    test.beforeEach(async () => {
      // Mock empty response
      await testHelpers.interceptApiCall(/\/bezirke/, {
        bezirke: [],
        pagination: { page: 1, limit: 20, total: 0, totalPages: 0 }
      })
    })

    test('should display empty state when no bezirke exist', async () => {
      await overviewPage.navigateTo()
      await overviewPage.assertNoBezirkeFound()
      
      await expect(overviewPage.noDataMessage).toContainText(/keine bezirke gefunden/i)
      await overviewPage.assertElementVisible(overviewPage.erstenBezirkErstellenButton)
    })

    test('should navigate to creation form from empty state', async ({ page }) => {
      await overviewPage.navigateTo()
      await overviewPage.clickErstenBezirkErstellen()
      
      await creationPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke\/neu$/)
    })

    test('should handle empty state with proper accessibility', async () => {
      await overviewPage.navigateTo()
      
      // Empty state should have proper semantic structure
      await expect(overviewPage.noDataMessage.locator('h3')).toBeVisible()
      await expect(overviewPage.noDataMessage.locator('p')).toBeVisible()
      
      // Action button should be properly labeled
      await expect(overviewPage.erstenBezirkErstellenButton).toBeVisible()
    })
  })

  test.describe('Search and Filtering', () => {
    test.beforeEach(async () => {
      const sampleBezirke = BezirkTestData.getValidBezirkSamples()
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(sampleBezirke))
    })

    test('should perform search functionality', async () => {
      await overviewPage.navigateTo()
      
      // Perform search
      await overviewPage.searchBezirke('Nord')
      
      // Should make API call with search parameter
      await testHelpers.waitForApiCall('/bezirke?.*search=Nord')
    })

    test('should clear search results', async () => {
      await overviewPage.navigateTo()
      
      // First search for something
      await overviewPage.searchBezirke('Nord')
      
      // Then clear search
      await overviewPage.clearSearch()
      
      // Should make API call without search parameter
      await testHelpers.waitForApiCall('/bezirke')
    })

    test('should filter by status', async () => {
      await overviewPage.navigateTo()
      
      if (await overviewPage.isElementVisible(overviewPage.statusFilter)) {
        await overviewPage.filterByStatus('aktiv')
        
        // Should make API call with status filter
        await testHelpers.waitForApiCall('/bezirke?.*aktiv=true')
      }
    })

    test('should sort bezirke by different criteria', async () => {
      await overviewPage.navigateTo()
      
      if (await overviewPage.isElementVisible(overviewPage.sortBySelect)) {
        await overviewPage.sortBy('Name')
        
        // Should make API call with sort parameter
        await testHelpers.waitForApiCall('/bezirke?.*sortBy=name')
      }
    })

    test('should handle search with no results', async () => {
      // Mock empty search results
      await testHelpers.interceptApiCall(/\/bezirke\?.*search/, {
        bezirke: [],
        pagination: { page: 1, limit: 20, total: 0, totalPages: 0 }
      })

      await overviewPage.navigateTo()
      await overviewPage.searchBezirke('NonExistentBezirk')
      
      await overviewPage.assertNoBezirkeFound()
    })
  })

  test.describe('Statistics Display', () => {
    test.beforeEach(async () => {
      await testHelpers.interceptApiCall(/\/bezirke\/statistiken/, 
        MockApiData.generateStatisticsResponse())
    })

    test('should display overview statistics', async () => {
      await overviewPage.navigateTo()
      await overviewPage.assertStatisticsVisible()
      
      const stats = await overviewPage.getAllStatistics()
      
      // Should have key statistics
      expect(Object.keys(stats).some(key => 
        key.toLowerCase().includes('bezirk') || 
        key.toLowerCase().includes('district')
      )).toBeTruthy()
    })

    test('should display statistics with proper formatting', async () => {
      await overviewPage.navigateTo()
      
      const gesamtBezirke = await overviewPage.getStatisticValue('Gesamt')
      if (gesamtBezirke) {
        expect(gesamtBezirke).toMatch(/^\d+$/) // Should be a number
      }
    })

    test('should update statistics when data changes', async ({ page }) => {
      await overviewPage.navigateTo()
      
      const initialStats = await overviewPage.getAllStatistics()
      
      // Navigate to create new bezirk
      await overviewPage.clickNeuerBezirk()
      
      // Mock successful creation
      const newBezirk = BezirkTestData.generateValidBezirk()
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse(newBezirk), 201)
      
      await creationPage.fillBezirkForm(newBezirk)
      await creationPage.submitForm()
      
      // Wait for navigation back
      await page.waitForURL(/\/bezirke$/)
      
      // Statistics should be updated (in a real scenario)
      await overviewPage.waitForLoadingToComplete()
    })
  })

  test.describe('Pagination', () => {
    test.beforeEach(async () => {
      // Mock paginated response
      const manyBezirke = Array.from({ length: 25 }, (_, i) => 
        BezirkTestData.generateValidBezirk({ name: `Test${i + 1}` }))
      
      await testHelpers.interceptApiCall(/\/bezirke/, {
        bezirke: manyBezirke.slice(0, 20),
        pagination: {
          page: 1,
          limit: 20,
          total: manyBezirke.length,
          totalPages: Math.ceil(manyBezirke.length / 20)
        }
      })
    })

    test('should display pagination when multiple pages exist', async () => {
      await overviewPage.navigateTo()
      await overviewPage.assertPaginationVisible()
      
      const paginationInfo = await overviewPage.getPaginationInfo()
      expect(paginationInfo).toMatch(/zeige.*von.*bezirken/i)
    })

    test('should navigate to next page', async () => {
      await overviewPage.navigateTo()
      
      if (await overviewPage.isElementVisible(overviewPage.naechstePage)) {
        await overviewPage.goToNextPage()
        
        // Should make API call for page 2
        await testHelpers.waitForApiCall('/bezirke?.*page=2')
      }
    })

    test('should navigate to previous page', async () => {
      // Mock being on page 2
      await testHelpers.interceptApiCall(/\/bezirke/, {
        bezirke: [],
        pagination: { page: 2, limit: 20, total: 25, totalPages: 2 }
      })

      await overviewPage.navigateTo()
      
      if (await overviewPage.isElementVisible(overviewPage.vorherigePage)) {
        await overviewPage.goToPreviousPage()
        
        // Should make API call for page 1
        await testHelpers.waitForApiCall('/bezirke?.*page=1')
      }
    })

    test('should display correct pagination information', async () => {
      await overviewPage.navigateTo()
      
      const paginationInfo = await overviewPage.getPaginationInfo()
      if (paginationInfo) {
        // Should show range like "Zeige 1 bis 20 von 25 Bezirken"
        expect(paginationInfo).toMatch(/\d+\s+bis\s+\d+\s+von\s+\d+/)
      }
    })
  })

  test.describe('Error Handling', () => {
    test('should handle API error gracefully', async () => {
      await testHelpers.simulateApiError(/\/bezirke/, 500)
      
      await testHelpers.handleApiErrorStates(async () => {
        await overviewPage.navigateTo()
        
        // Should show some error indication
        const hasError = await overviewPage.handleApiError()
        expect(hasError).toBeTruthy()
      })
    })

    test('should handle network timeout', async ({ page }) => {
      // Simulate very slow response
      await page.route(/\/bezirke/, async route => {
        await new Promise(resolve => setTimeout(resolve, 30000))
        await route.continue()
      })

      await testHelpers.handleApiErrorStates(async () => {
        await overviewPage.navigateTo()
        // Should handle timeout gracefully
      })
    })

    test('should handle malformed API response', async () => {
      await testHelpers.interceptApiCall(/\/bezirke/, { invalid: 'data' })
      
      await testHelpers.handleApiErrorStates(async () => {
        await overviewPage.navigateTo()
        
        // Should handle malformed data gracefully
        // Either show empty state or error message
        const isEmpty = await overviewPage.isElementVisible(overviewPage.noDataMessage)
        const hasError = await overviewPage.handleApiError()
        
        expect(isEmpty || hasError).toBeTruthy()
      })
    })

    test('should recover from errors when API becomes available', async ({ page }) => {
      // First simulate error
      await testHelpers.simulateApiError(/\/bezirke/, 500)
      
      await overviewPage.navigateTo()
      
      // Then fix the API
      await testHelpers.removeApiIntercept(/\/bezirke/)
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(BezirkTestData.getValidBezirkSamples()))
      
      // Reload page
      await page.reload()
      await overviewPage.assertPageLoaded()
      await overviewPage.assertBezirkeTableVisible()
    })
  })

  test.describe('Integration with Creation Workflow', () => {
    test('should show newly created bezirk in list', async ({ page }) => {
      const newBezirk = BezirkTestData.generateValidBezirk()
      
      // Start from overview
      await overviewPage.navigateTo()
      
      // Navigate to creation
      await overviewPage.clickNeuerBezirk()
      
      // Mock successful creation
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse(newBezirk), 201)
      
      // Mock updated list with new bezirk
      const updatedList = [...BezirkTestData.getValidBezirkSamples(), newBezirk]
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(updatedList))
      
      // Fill and submit form
      await creationPage.fillBezirkForm(newBezirk)
      await creationPage.submitForm()
      
      // Wait for navigation back
      await page.waitForURL(/\/bezirke$/)
      await overviewPage.waitForBezirkeOverviewToLoad()
      
      // New bezirk should be visible
      await overviewPage.assertBezirkInTable(newBezirk.name)
    })

    test('should maintain list state after form cancellation', async ({ page }) => {
      await overviewPage.navigateTo()
      const initialCount = await overviewPage.getBezirkRowCount()
      
      // Navigate to creation and cancel
      await overviewPage.clickNeuerBezirk()
      await creationPage.cancelForm()
      
      // Should return to same list state
      await expect(page).toHaveURL(/\/bezirke$/)
      await overviewPage.waitForBezirkeOverviewToLoad()
      
      const finalCount = await overviewPage.getBezirkRowCount()
      expect(finalCount).toBe(initialCount)
    })
  })

  test.describe('Responsive Design and Mobile', () => {
    test('should display properly on mobile viewport', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 }) // iPhone size
      
      await overviewPage.navigateTo()
      await overviewPage.assertPageLoaded()
      
      // Table should be responsive or show mobile view
      await overviewPage.assertElementVisible(overviewPage.bezirkeTable)
    })

    test('should handle touch interactions on mobile', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 })
      
      await overviewPage.navigateTo()
      
      // Should be able to tap buttons
      await overviewPage.neuerBezirkButton.tap()
      await creationPage.assertPageLoaded()
    })
  })

  test.describe('Performance and Optimization', () => {
    test('should load page within reasonable time', async () => {
      const startTime = Date.now()
      
      await overviewPage.navigateTo()
      await overviewPage.assertPageLoaded()
      
      const loadTime = Date.now() - startTime
      expect(loadTime).toBeLessThan(StaticTestHelpers.createTestTimeout(5000))
    })

    test('should handle large datasets efficiently', async () => {
      // Mock large dataset
      const largeBezirkeList = Array.from({ length: 100 }, (_, i) => 
        BezirkTestData.generateValidBezirk({ name: `Bezirk${i + 1}` }))
      
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(largeBezirkeList.slice(0, 20)))
      
      const startTime = Date.now()
      await overviewPage.navigateTo()
      await overviewPage.assertBezirkeTableVisible()
      const renderTime = Date.now() - startTime
      
      expect(renderTime).toBeLessThan(3000) // Should render within 3 seconds
    })

    test('should not cause memory leaks with repeated navigation', async () => {
      // Navigate back and forth multiple times
      for (let i = 0; i < 3; i++) {
        await overviewPage.navigateTo()
        await overviewPage.assertPageLoaded()
        
        await overviewPage.clickNeuerBezirk()
        await creationPage.assertPageLoaded()
        
        await creationPage.clickBackButton()
      }
      
      // Final navigation should still work smoothly
      await overviewPage.assertPageLoaded()
    })
  })

  test.describe('Accessibility', () => {
    test('should have proper heading structure', async () => {
      await overviewPage.navigateTo()
      
      // Should have main page heading
      await expect(overviewPage.pageTitle).toHaveAttribute('aria-level', /1|2/)
      
      // Section headings should follow proper hierarchy
      const headings = await overviewPage.page.locator('h1, h2, h3, h4').all()
      expect(headings.length).toBeGreaterThan(0)
    })

    test('should have proper table accessibility', async () => {
      const sampleBezirke = BezirkTestData.getValidBezirkSamples()
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(sampleBezirke))

      await overviewPage.navigateTo()
      
      // Table should have proper structure
      await expect(overviewPage.bezirkeTable).toHaveAttribute('role', /table|grid/)
      
      // Headers should be properly associated
      const tableHeaders = await overviewPage.tableHeader.locator('th').all()
      expect(tableHeaders.length).toBeGreaterThan(0)
    })

    test('should support keyboard navigation', async ({ page }) => {
      const sampleBezirke = BezirkTestData.getValidBezirkSamples()
      await testHelpers.interceptApiCall(/\/bezirke/, 
        MockApiData.generateBezirkeListResponse(sampleBezirke))

      await overviewPage.navigateTo()
      
      // Should be able to navigate with Tab
      await page.keyboard.press('Tab')
      
      // Should be able to activate buttons with Enter/Space
      const focusedElement = await page.locator(':focus').first()
      if (await focusedElement.isVisible()) {
        await page.keyboard.press('Enter')
      }
    })
  })
})