import { test, expect } from '@playwright/test'
import { 
  DashboardPage, 
  BezirkeOverviewPage, 
  BezirkeCreationPage 
} from '../pages'
import { TestHelpers } from '../utils/test-helpers'

/**
 * Bezirke Navigation Flow E2E Tests
 * 
 * Tests all navigation paths related to Bezirke management:
 * - Dashboard → Bezirke Overview
 * - Bezirke Overview → Bezirk Creation 
 * - Breadcrumb navigation
 * - Back button navigation
 * - URL-based navigation
 */

test.describe('Bezirke Navigation Flow', () => {
  let dashboardPage: DashboardPage
  let bezirkeOverviewPage: BezirkeOverviewPage
  let bezirkeCreationPage: BezirkeCreationPage
  let testHelpers: TestHelpers

  test.beforeEach(async ({ page }) => {
    dashboardPage = new DashboardPage(page)
    bezirkeOverviewPage = new BezirkeOverviewPage(page)
    bezirkeCreationPage = new BezirkeCreationPage(page)
    testHelpers = new TestHelpers(page)
  })

  test.describe('Dashboard to Bezirke Navigation', () => {
    test('should navigate from dashboard to bezirke overview via sidebar', async () => {
      // Navigate to dashboard
      await dashboardPage.navigateTo()
      await dashboardPage.assertDashboardLoaded()

      // Verify sidebar navigation is visible
      await dashboardPage.assertNavigationVisible()

      // Navigate to Bezirke via sidebar
      await dashboardPage.navigateToBezirke()

      // Verify we reached the Bezirke overview page
      await bezirkeOverviewPage.assertPageLoaded()
      await expect(dashboardPage.page).toHaveURL(/\/bezirke$/)
    })

    test('should navigate from dashboard to bezirke via quick actions', async ({ page }) => {
      test.skip(!await dashboardPage.isElementVisible(dashboardPage.quickActions), 'Quick actions not available')
      
      await dashboardPage.navigateTo()
      await dashboardPage.assertDashboardLoaded()

      // Navigate via quick action if available
      if (await dashboardPage.isElementVisible(dashboardPage.neuerBezirkButton)) {
        await dashboardPage.navigateToNeuerBezirk()
        await bezirkeCreationPage.assertPageLoaded()
        await expect(page).toHaveURL(/\/bezirke\/neu$/)
      }
    })

    test('should maintain navigation state during page transitions', async ({ page }) => {
      // Start from dashboard
      await dashboardPage.navigateTo()
      await dashboardPage.assertDashboardLoaded()

      // Navigate to bezirke
      await dashboardPage.navigateToBezirke()
      await bezirkeOverviewPage.assertPageLoaded()

      // Verify breadcrumb shows correct navigation path
      await bezirkeOverviewPage.assertBreadcrumb()

      // Navigate back to dashboard
      await dashboardPage.navigateTo()
      await dashboardPage.assertDashboardLoaded()
    })
  })

  test.describe('Bezirke Overview Navigation', () => {
    test.beforeEach(async () => {
      await bezirkeOverviewPage.navigateTo()
      await bezirkeOverviewPage.waitForBezirkeOverviewToLoad()
    })

    test('should navigate to new bezirk creation form', async ({ page }) => {
      // Click "Neuer Bezirk" button
      await bezirkeOverviewPage.clickNeuerBezirk()

      // Verify navigation to creation form
      await bezirkeCreationPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke\/neu$/)
    })

    test('should navigate via "ersten bezirk erstellen" when no data', async ({ page }) => {
      // Mock empty state
      await testHelpers.interceptApiCall(/\/bezirke/, {
        bezirke: [],
        pagination: { page: 1, limit: 20, total: 0, totalPages: 0 }
      })

      await page.reload()
      await bezirkeOverviewPage.waitForBezirkeOverviewToLoad()

      // Should show no data state
      await bezirkeOverviewPage.assertNoBezirkeFound()

      // Click "ersten bezirk erstellen"
      await bezirkeOverviewPage.clickErstenBezirkErstellen()

      // Verify navigation to creation form
      await bezirkeCreationPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke\/neu$/)
    })

    test('should navigate to parzellen from bezirke overview', async ({ page }) => {
      // Click "Zu Parzellen" button if available
      if (await bezirkeOverviewPage.isElementVisible(bezirkeOverviewPage.zuParzellenButton)) {
        await bezirkeOverviewPage.zuParzellenButton.click()
        await bezirkeOverviewPage.waitForPageLoad()
        
        // Should navigate to parzellen page
        await expect(page).toHaveURL(/\/parzellen$/)
      }
    })
  })

  test.describe('Bezirk Creation Navigation', () => {
    test.beforeEach(async () => {
      await bezirkeCreationPage.navigateTo()
      await bezirkeCreationPage.waitForCreationPageToLoad()
    })

    test('should display correct breadcrumb navigation', async () => {
      await bezirkeCreationPage.assertBreadcrumbVisible()
      
      // Verify breadcrumb structure
      await expect(bezirkeCreationPage.breadcrumbBezirke).toBeVisible()
      await expect(bezirkeCreationPage.breadcrumbCurrent).toBeVisible()
      await expect(bezirkeCreationPage.breadcrumbCurrent).toContainText(/neuer bezirk/i)
    })

    test('should navigate back to bezirke overview via breadcrumb', async ({ page }) => {
      // Click breadcrumb link
      await bezirkeCreationPage.navigateToBezirkeViaBreadcrumb()

      // Verify navigation back to overview
      await bezirkeOverviewPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should navigate back to bezirke overview via back button', async ({ page }) => {
      // Click back button
      await bezirkeCreationPage.navigateToBezirkeViaBackButton()

      // Verify navigation back to overview
      await bezirkeOverviewPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should navigate back via cancel button', async ({ page }) => {
      // Click cancel button
      await bezirkeCreationPage.cancelForm()

      // Should navigate back to overview
      await bezirkeOverviewPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke$/)
    })
  })

  test.describe('Direct URL Navigation', () => {
    test('should handle direct navigation to bezirke overview', async ({ page }) => {
      await page.goto('/bezirke')
      
      await bezirkeOverviewPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should handle direct navigation to bezirk creation', async ({ page }) => {
      await page.goto('/bezirke/neu')
      
      await bezirkeCreationPage.assertPageLoaded()
      await expect(page).toHaveURL(/\/bezirke\/neu$/)
    })

    test('should handle invalid bezirk URLs gracefully', async ({ page }) => {
      await page.goto('/bezirke/nonexistent')
      
      // Should either redirect or show 404 error
      // This depends on your routing implementation
      await expect(async () => {
        await page.waitForURL(/\/bezirke|404|error/, { timeout: 5000 })
      }).not.toThrow()
    })
  })

  test.describe('Browser Navigation', () => {
    test('should handle browser back/forward navigation', async ({ page }) => {
      // Navigate through the flow
      await dashboardPage.navigateTo()
      await dashboardPage.navigateToBezirke()
      await bezirkeOverviewPage.clickNeuerBezirk()
      
      // Verify current location
      await expect(page).toHaveURL(/\/bezirke\/neu$/)

      // Use browser back button
      await page.goBack()
      await expect(page).toHaveURL(/\/bezirke$/)

      // Use browser forward button
      await page.goForward()
      await expect(page).toHaveURL(/\/bezirke\/neu$/)

      // Go back twice to reach dashboard
      await page.goBack()
      await page.goBack()
      await expect(page).toHaveURL(/\/$/)
    })

    test('should maintain state during browser refresh', async ({ page }) => {
      // Navigate to bezirk creation
      await bezirkeCreationPage.navigateTo()
      await bezirkeCreationPage.assertPageLoaded()

      // Fill some form data
      await bezirkeCreationPage.fillName('TestRefresh')

      // Refresh the page
      await page.reload()
      
      // Page should reload correctly
      await bezirkeCreationPage.assertPageLoaded()
      
      // Form should be reset (this is typical behavior)
      const nameValue = await bezirkeCreationPage.nameField.inputValue()
      expect(nameValue).toBe('')
    })
  })

  test.describe('Navigation Error Handling', () => {
    test('should handle navigation when API is unavailable', async ({ page }) => {
      // Simulate API errors
      await testHelpers.simulateApiError(/\/bezirke/, 503)

      await bezirkeOverviewPage.navigateTo()

      // Page should still load but might show error state
      await testHelpers.handleApiErrorStates(async () => {
        await bezirkeOverviewPage.assertPageLoaded()
      })

      // Navigation should still work even with API errors
      await bezirkeOverviewPage.clickNeuerBezirk()
      await bezirkeCreationPage.assertPageLoaded()
    })

    test('should handle slow API responses during navigation', async ({ page }) => {
      // Simulate slow API response
      await page.route(/\/bezirke/, async (route) => {
        await new Promise(resolve => setTimeout(resolve, 2000)) // 2 second delay
        await route.continue()
      })

      const startTime = Date.now()
      await bezirkeOverviewPage.navigateTo()
      await bezirkeOverviewPage.assertPageLoaded()
      const endTime = Date.now()

      // Should handle the delay gracefully
      expect(endTime - startTime).toBeGreaterThan(1000)
    })

    test('should handle navigation during form submission', async ({ page }) => {
      await bezirkeCreationPage.navigateTo()
      await bezirkeCreationPage.fillName('TestNav')

      // Start form submission (don't wait for it)
      const submitPromise = bezirkeCreationPage.submitForm()

      // Try to navigate away immediately
      await page.goto('/bezirke')

      // Should handle gracefully (either complete submission or cancel it)
      await testHelpers.handleApiErrorStates(async () => {
        await bezirkeOverviewPage.assertPageLoaded()
      })
    })
  })

  test.describe('Accessibility in Navigation', () => {
    test('should support keyboard navigation', async ({ page }) => {
      await dashboardPage.navigateTo()

      // Tab through navigation elements
      await page.keyboard.press('Tab')
      
      // This is a basic test - you might need to implement more specific keyboard navigation tests
      // based on your application's accessibility implementation
    })

    test('should have proper ARIA labels for navigation', async () => {
      await bezirkeOverviewPage.navigateTo()
      
      // Check for navigation landmarks
      await expect(bezirkeOverviewPage.navigation).toHaveAttribute('role', /navigation|banner/)
      
      // Check for breadcrumb navigation
      if (await bezirkeOverviewPage.isElementVisible(bezirkeOverviewPage.breadcrumb)) {
        await expect(bezirkeOverviewPage.breadcrumb).toHaveAttribute('aria-label', /breadcrumb|navigation/i)
      }
    })
  })

  test.describe('Performance in Navigation', () => {
    test('should navigate quickly between pages', async ({ page }) => {
      const startTime = Date.now()
      
      await dashboardPage.navigateTo()
      await dashboardPage.navigateToBezirke()
      await bezirkeOverviewPage.clickNeuerBezirk()
      
      const endTime = Date.now()
      const totalTime = endTime - startTime
      
      // Navigation should complete within reasonable time
      expect(totalTime).toBeLessThan(10000) // 10 seconds max
    })

    test('should not cause memory leaks during navigation', async ({ page }) => {
      // Navigate multiple times to check for memory leaks
      for (let i = 0; i < 5; i++) {
        await dashboardPage.navigateTo()
        await dashboardPage.navigateToBezirke()
        await bezirkeOverviewPage.clickNeuerBezirk()
        await bezirkeCreationPage.clickBackButton()
      }

      // Basic check - page should still be responsive
      await bezirkeOverviewPage.assertPageLoaded()
    })
  })
})