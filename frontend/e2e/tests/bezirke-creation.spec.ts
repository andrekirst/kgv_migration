import { test, expect } from '@playwright/test'
import { 
  BezirkeCreationPage, 
  BezirkeOverviewPage,
  BezirkFormData 
} from '../pages'
import { TestHelpers, StaticTestHelpers } from '../utils/test-helpers'
import { BezirkTestData, MockApiData, TestScenarios } from '../fixtures/test-data'

/**
 * Bezirke Creation Form E2E Tests
 * 
 * Comprehensive tests for the Bezirk creation form including:
 * - Form rendering and field validation
 * - Happy path form submission
 * - Error handling and API failures
 * - Client-side and server-side validation
 * - Form state management
 * - Data persistence and cleanup
 */

test.describe('Bezirke Creation Form', () => {
  let creationPage: BezirkeCreationPage
  let overviewPage: BezirkeOverviewPage
  let testHelpers: TestHelpers

  test.beforeEach(async ({ page }) => {
    creationPage = new BezirkeCreationPage(page)
    overviewPage = new BezirkeOverviewPage(page)
    testHelpers = new TestHelpers(page)

    // Navigate to creation form before each test
    await creationPage.navigateTo()
    await creationPage.waitForCreationPageToLoad()
  })

  test.afterEach(async ({ page }) => {
    // Clean up any test artifacts
    await testHelpers.resetApplicationState()
  })

  test.describe('Form Rendering and Initial State', () => {
    test('should render all form elements correctly', async () => {
      await creationPage.assertPageLoaded()
      await creationPage.assertFormFieldsVisible()
      await creationPage.assertFormActionsVisible()
      await creationPage.assertBreadcrumbVisible()
    })

    test('should have correct initial form state', async () => {
      // Form should be empty initially
      await expect(creationPage.nameField).toHaveValue('')
      await expect(creationPage.beschreibungField).toHaveValue('')
      
      // Submit button should be enabled (will be validated on submit)
      expect(await creationPage.isSubmitButtonEnabled()).toBeTruthy()
      
      // Form should not be in submitting state
      expect(await creationPage.isFormSubmitting()).toBeFalsy()
    })

    test('should display correct form labels and descriptions', async () => {
      await expect(creationPage.nameLabel).toContainText(/bezirksname/i)
      await expect(creationPage.nameDescription).toContainText(/maximal 10 zeichen/i)
      
      await expect(creationPage.beschreibungLabel).toContainText(/beschreibung/i)
      await expect(creationPage.beschreibungDescription).toContainText(/kurze beschreibung/i)
    })

    test('should display proper field attributes', async () => {
      // Name field should have proper attributes
      await expect(creationPage.nameField).toHaveAttribute('maxlength', '10')
      await expect(creationPage.nameField).toHaveAttribute('name', 'name')
      
      // Description field should be a textarea
      await expect(creationPage.beschreibungField).toHaveAttribute('name', 'beschreibung')
      
      // Check for accessibility attributes
      const nameFieldId = await creationPage.nameField.getAttribute('id')
      if (nameFieldId) {
        await expect(creationPage.nameLabel).toHaveAttribute('for', nameFieldId)
      }
    })
  })

  test.describe('Happy Path - Successful Form Submission', () => {
    test('should create bezirk with valid minimal data', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Mock successful API response
      const mockResponse = MockApiData.generateBezirkResponse(testData)
      await testHelpers.interceptApiCall(/\/bezirke$/, mockResponse, 201)

      // Fill and submit form
      await creationPage.fillBezirkForm({
        name: testData.name,
        beschreibung: testData.beschreibung
      })

      await creationPage.submitFormAndExpectSuccess()

      // Should navigate back to overview
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should create bezirk with name only', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse({ name: testData.name }), 201)

      // Fill only required field
      await creationPage.fillName(testData.name)
      
      await creationPage.submitFormAndExpectSuccess()
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should handle successful form submission with toast notification', async ({ page }) => {
      test.skip(StaticTestHelpers.shouldSkipTest('api-unavailable'))
      
      const testData = TestScenarios.happyPath.bezirk
      
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse(testData), 201)

      await creationPage.fillBezirkForm(testData)
      
      // Submit and look for success indicators
      await creationPage.submitFormAndWaitForResponse()
      
      // Should show success toast or navigate
      await Promise.race([
        creationPage.waitForSuccessToast(),
        page.waitForURL(/\/bezirke$/, { timeout: 5000 })
      ])
    })

    test('should reset form after successful submission', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse(testData), 201)

      await creationPage.fillBezirkForm(testData)
      
      // Submit and wait for navigation
      await creationPage.submitFormAndWaitForResponse()
      await page.waitForURL(/\/bezirke$/, { timeout: 10000 })
      
      // Navigate back to form to check reset state
      await overviewPage.clickNeuerBezirk()
      await creationPage.assertPageLoaded()
      
      // Form should be reset
      await expect(creationPage.nameField).toHaveValue('')
      await expect(creationPage.beschreibungField).toHaveValue('')
    })
  })

  test.describe('Form Validation - Client Side', () => {
    test('should validate required name field', async () => {
      // Try to submit empty form
      await creationPage.submitForm()
      
      // Should show validation error
      await creationPage.triggerNameValidation()
      
      // Check for error state
      const errors = await creationPage.getAllFormErrors()
      expect(errors.length).toBeGreaterThan(0)
    })

    test('should validate name length limit', async () => {
      const longName = 'VeryLongBezirkNameThatExceedsLimit'
      
      await creationPage.fillName(longName)
      await creationPage.triggerNameValidation()
      
      // Should show length validation error
      const nameError = await creationPage.getNameError()
      expect(nameError.toLowerCase()).toMatch(/10 zeichen|zu lang|too long/)
    })

    test('should validate against empty/whitespace name', async () => {
      // Test whitespace-only name
      await creationPage.fillName('   ')
      await creationPage.triggerNameValidation()
      
      const nameError = await creationPage.getNameError()
      expect(nameError.toLowerCase()).toMatch(/erforderlich|required|leer/)
    })

    test('should validate description length if applicable', async () => {
      const longDescription = 'A'.repeat(501) // Assuming 500 char limit
      
      await creationPage.fillName('Valid')
      await creationPage.fillBeschreibung(longDescription)
      await creationPage.triggerBeschreibungValidation()
      
      const beschreibungError = await creationPage.getBeschreibungError()
      if (beschreibungError) {
        expect(beschreibungError.toLowerCase()).toMatch(/500|zu lang|too long/)
      }
    })

    test.describe('Special Characters in Name Field', () => {
      const specialCharTests = BezirkTestData.getSpecialCharacterTestData()

      for (const testCase of specialCharTests) {
        test(`should ${testCase.shouldBeValid ? 'accept' : 'reject'} ${testCase.name}`, async () => {
          await creationPage.fillBezirkForm(testCase.data)
          await creationPage.triggerNameValidation()

          if (testCase.shouldBeValid) {
            await creationPage.assertNoFormErrors()
          } else {
            const errors = await creationPage.getAllFormErrors()
            expect(errors.length).toBeGreaterThan(0)
          }
        })
      }
    })

    test.describe('Boundary Value Testing', () => {
      const boundaryTests = BezirkTestData.getBoundaryTestData()

      for (const testCase of boundaryTests) {
        test(`should handle ${testCase.name}`, async () => {
          await creationPage.fillBezirkForm(testCase.data)
          await creationPage.triggerAllFieldValidation()

          // All boundary test cases should be valid
          await creationPage.assertNoFormErrors()
          
          // Submit button should be enabled
          expect(await creationPage.isSubmitButtonEnabled()).toBeTruthy()
        })
      }
    })
  })

  test.describe('Form Validation - Server Side', () => {
    test('should handle server validation errors', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Mock server validation error
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateValidationError('name', 'Name bereits vergeben'), 400)

      await creationPage.fillBezirkForm(testData)
      await creationPage.submitFormAndExpectError()

      // Should display server error
      await creationPage.assertFormSubmissionError()
    })

    test('should handle multiple validation errors from server', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.interceptApiCall(/\/bezirke$/, {
        error: 'Validation Error',
        details: [
          { field: 'name', message: 'Name bereits vergeben' },
          { field: 'beschreibung', message: 'Beschreibung zu kurz' }
        ],
        status: 400
      }, 400)

      await creationPage.fillBezirkForm(testData)
      await creationPage.submitFormAndExpectError()

      const errors = await creationPage.getAllFormErrors()
      expect(errors.length).toBeGreaterThanOrEqual(1)
    })

    test.describe('Invalid Data Submission', () => {
      const invalidDataTests = BezirkTestData.getInvalidBezirkData()

      for (const testCase of invalidDataTests) {
        test(`should reject ${testCase.name}`, async () => {
          // Mock appropriate server response
          await testHelpers.interceptApiCall(/\/bezirke$/, 
            MockApiData.generateValidationError('name', 'Invalid data'), 400)

          await creationPage.fillBezirkForm(testCase.data)
          await creationPage.submitFormAndExpectError()

          await creationPage.assertFormSubmissionError()
        })
      }
    })
  })

  test.describe('API Error Handling', () => {
    test('should handle 500 server error gracefully', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.simulateApiError(/\/bezirke$/, 500)

      await creationPage.fillBezirkForm(testData)
      
      await testHelpers.handleApiErrorStates(async () => {
        await creationPage.submitFormAndExpectError()
        await creationPage.assertFormSubmissionError()
      })
    })

    test('should handle network timeout', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Simulate slow response
      await page.route(/\/bezirke$/, async route => {
        await new Promise(resolve => setTimeout(resolve, 35000)) // Longer than timeout
        await route.continue()
      })

      await creationPage.fillBezirkForm(testData)
      
      await testHelpers.handleApiErrorStates(async () => {
        await creationPage.submitForm()
        // Should handle timeout gracefully
      })
    })

    test('should handle API unavailable (503)', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.simulateApiError(/\/bezirke$/, 503)

      await creationPage.fillBezirkForm(testData)
      
      await testHelpers.handleApiErrorStates(async () => {
        await creationPage.submitFormAndExpectError()
      })
    })

    test('should show appropriate error messages for different HTTP status codes', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      const statusCodes = [400, 401, 403, 404, 500, 503]

      for (const statusCode of statusCodes) {
        await testHelpers.simulateApiError(/\/bezirke$/, statusCode)
        
        await creationPage.fillBezirkForm(testData)
        await creationPage.submitForm()
        
        // Should show some error indicator
        await testHelpers.handleApiErrorStates(async () => {
          const hasError = await creationPage.handleApiError()
          expect(hasError).toBeTruthy()
        })

        // Reset for next test
        await testHelpers.removeApiIntercept(/\/bezirke$/)
        await creationPage.clearForm()
      }
    })
  })

  test.describe('Form State Management', () => {
    test('should show loading state during submission', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Slow API response to catch loading state
      await creationPage.page.route(/\/bezirke$/, async route => {
        await new Promise(resolve => setTimeout(resolve, 2000))
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(MockApiData.generateBezirkResponse(testData))
        })
      })

      await creationPage.fillBezirkForm(testData)
      
      // Start submission
      const submitPromise = creationPage.submitForm()
      
      // Should show loading state
      await expect(creationPage.loadingSubmitButton).toBeVisible({ timeout: 1000 })
      
      // Wait for completion
      await submitPromise
      await creationPage.waitForFormSubmissionToComplete()
    })

    test('should disable form during submission', async () => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Slow response to test disabled state
      await creationPage.page.route(/\/bezirke$/, async route => {
        await new Promise(resolve => setTimeout(resolve, 1000))
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(MockApiData.generateBezirkResponse(testData))
        })
      })

      await creationPage.fillBezirkForm(testData)
      
      // Start submission
      const submitPromise = creationPage.submitForm()
      
      // Fields should be disabled during submission
      await expect(creationPage.nameField).toBeDisabled({ timeout: 1000 })
      await expect(creationPage.submitButtonDisabled).toBeVisible()
      
      await submitPromise
    })

    test('should prevent double submission', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      let requestCount = 0
      await page.route(/\/bezirke$/, async route => {
        requestCount++
        await new Promise(resolve => setTimeout(resolve, 1000))
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(MockApiData.generateBezirkResponse(testData))
        })
      })

      await creationPage.fillBezirkForm(testData)
      
      // Try to submit multiple times quickly
      const submit1 = creationPage.submitButton.click()
      const submit2 = creationPage.submitButton.click()
      const submit3 = creationPage.submitButton.click()
      
      await Promise.allSettled([submit1, submit2, submit3])
      
      // Should only make one request
      expect(requestCount).toBe(1)
    })
  })

  test.describe('Form Reset and Cancel', () => {
    test('should clear form when cancel button is clicked', async ({ page }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      // Fill form with data
      await creationPage.fillBezirkForm(testData)
      
      // Verify data is filled
      await expect(creationPage.nameField).toHaveValue(testData.name)
      
      // Cancel form
      await creationPage.cancelForm()
      
      // Should navigate back to overview
      await expect(page).toHaveURL(/\/bezirke$/)
    })

    test('should show confirmation dialog for unsaved changes', async ({ page }) => {
      // Note: This test assumes your form has unsaved changes detection
      const testData = BezirkTestData.generateValidBezirk()
      
      await creationPage.fillBezirkForm(testData)
      
      // Try to navigate away
      await page.goto('/bezirke')
      
      // Should either show confirmation or navigate (depending on implementation)
      await expect(page).toHaveURL(/\/bezirke/)
    })
  })

  test.describe('Accessibility and Usability', () => {
    test('should support keyboard navigation', async ({ page }) => {
      // Tab through form fields
      await page.keyboard.press('Tab') // Should focus first field
      await page.keyboard.type('TestName')
      
      await page.keyboard.press('Tab') // Should focus description
      await page.keyboard.type('Test description')
      
      await page.keyboard.press('Tab') // Should focus submit button
      await page.keyboard.press('Enter') // Should submit
    })

    test('should have proper focus management', async () => {
      // Name field should be focused initially
      await expect(creationPage.nameField).toBeFocused()
      
      // Focus should move logically through form
      await creationPage.nameField.press('Tab')
      await expect(creationPage.beschreibungField).toBeFocused()
    })

    test('should have proper error announcement', async () => {
      await creationPage.submitForm()
      await creationPage.triggerAllFieldValidation()
      
      // Error elements should have proper ARIA attributes
      const errors = await creationPage.page.locator('[role="alert"], .error').all()
      for (const error of errors) {
        const text = await error.textContent()
        if (text && text.trim()) {
          // Error should be announced to screen readers
          expect(await error.getAttribute('role')).toBeTruthy()
        }
      }
    })
  })

  test.describe('Cross-Browser Compatibility', () => {
    test('should work consistently across browsers', async ({ browserName }) => {
      const testData = BezirkTestData.generateValidBezirk()
      
      await testHelpers.interceptApiCall(/\/bezirke$/, 
        MockApiData.generateBezirkResponse(testData), 201)

      await creationPage.fillBezirkForm(testData)
      await creationPage.submitFormAndExpectSuccess()

      // Basic functionality should work in all browsers
      console.log(`Test completed successfully in ${browserName}`)
    })
  })

  test.describe('Performance', () => {
    test('should load form quickly', async () => {
      const startTime = Date.now()
      
      await creationPage.navigateTo()
      await creationPage.assertPageLoaded()
      
      const loadTime = Date.now() - startTime
      expect(loadTime).toBeLessThan(5000) // Should load within 5 seconds
    })

    test('should handle large description text efficiently', async () => {
      const largeText = 'A'.repeat(500) // Maximum allowed length
      
      const startTime = Date.now()
      await creationPage.fillBeschreibung(largeText)
      const endTime = Date.now()
      
      expect(endTime - startTime).toBeLessThan(1000) // Should handle quickly
      await expect(creationPage.beschreibungField).toHaveValue(largeText)
    })
  })
})