import { Page, Locator, expect } from '@playwright/test'
import { BasePage } from './base-page'

export interface BezirkFormData {
  name: string
  beschreibung?: string
}

/**
 * Bezirke Creation Page Object Model
 * 
 * Handles interactions with the Bezirk creation form
 */
export class BezirkeCreationPage extends BasePage {
  constructor(page: Page) {
    super(page)
  }

  // URL and navigation
  get url(): string {
    return '/bezirke/neu'
  }

  // Page elements
  get pageTitle(): Locator {
    return this.page.locator('h1, h2').filter({ hasText: /neuen bezirk erstellen/i })
  }

  get pageDescription(): Locator {
    return this.page.locator('p').filter({ hasText: /erfassen sie die grunddaten/i })
  }

  // Breadcrumb
  get breadcrumbBezirke(): Locator {
    return this.breadcrumb.locator('a[href="/bezirke"], text=bezirke')
  }

  get breadcrumbCurrent(): Locator {
    return this.breadcrumb.locator('text=neuer bezirk')
  }

  // Header actions
  get backButton(): Locator {
    return this.page.locator('a[href="/bezirke"], button').filter({ hasText: /zurück zur übersicht/i })
  }

  // Form elements
  get form(): Locator {
    return this.page.locator('form, .bezirk-form')
  }

  get formTitle(): Locator {
    return this.form.locator('h2').filter({ hasText: /neuen bezirk erstellen/i })
  }

  get formDescription(): Locator {
    return this.form.locator('p').filter({ hasText: /erfassen sie die grunddaten/i })
  }

  // Form fields
  get nameField(): Locator {
    return this.form.locator('input[name="name"], input#name')
  }

  get nameLabel(): Locator {
    return this.form.locator('label[for="name"]').filter({ hasText: /bezirksname/i })
  }

  get nameDescription(): Locator {
    return this.form.locator('text=maximal 10 zeichen')
  }

  get nameError(): Locator {
    return this.form.locator('.error, .text-red-600').filter({ hasText: /name/i })
  }

  get beschreibungField(): Locator {
    return this.form.locator('textarea[name="beschreibung"], textarea#beschreibung')
  }

  get beschreibungLabel(): Locator {
    return this.form.locator('label[for="beschreibung"]').filter({ hasText: /beschreibung/i })
  }

  get beschreibungDescription(): Locator {
    return this.form.locator('text=kurze beschreibung des bezirks')
  }

  get beschreibungError(): Locator {
    return this.form.locator('.error, .text-red-600').filter({ hasText: /beschreibung/i })
  }

  // Form actions
  get submitButton(): Locator {
    return this.form.locator('button[type="submit"]').filter({ hasText: /erstellen/i })
  }

  get cancelButton(): Locator {
    return this.form.locator('button[type="button"]').filter({ hasText: /abbrechen/i })
  }

  get showErrorsButton(): Locator {
    return this.form.locator('button').filter({ hasText: /fehler anzeigen/i })
  }

  // Form state indicators
  get loadingSubmitButton(): Locator {
    return this.form.locator('button[type="submit"]:disabled').filter({ hasText: /speichert/i })
  }

  get submitButtonDisabled(): Locator {
    return this.form.locator('button[type="submit"]:disabled')
  }

  // Methods
  async navigateTo(): Promise<void> {
    await this.navigateToUrl(this.url)
    await this.waitForCreationPageToLoad()
  }

  async waitForCreationPageToLoad(): Promise<void> {
    await this.waitForPageLoad()
    await this.assertElementVisible(this.pageTitle)
    await this.assertElementVisible(this.form)
    await this.waitForLoadingToComplete()
  }

  // Navigation methods
  async clickBackButton(): Promise<void> {
    await this.backButton.click()
    await this.waitForPageLoad()
  }

  async navigateToBezirkeViaBackButton(): Promise<void> {
    await this.clickBackButton()
  }

  async navigateToBezirkeViaBreadcrumb(): Promise<void> {
    await this.breadcrumbBezirke.click()
    await this.waitForPageLoad()
  }

  // Form interaction methods
  async fillName(name: string): Promise<void> {
    await this.clearAndFillInput(this.nameField, name)
  }

  async fillBeschreibung(beschreibung: string): Promise<void> {
    await this.clearAndFillInput(this.beschreibungField, beschreibung)
  }

  async fillBezirkForm(data: BezirkFormData): Promise<void> {
    await this.fillName(data.name)
    
    if (data.beschreibung) {
      await this.fillBeschreibung(data.beschreibung)
    }
  }

  async clearForm(): Promise<void> {
    await this.nameField.clear()
    await this.beschreibungField.clear()
  }

  // Form submission methods
  async submitForm(): Promise<void> {
    await this.submitButton.click()
  }

  async submitFormAndWaitForResponse(): Promise<void> {
    await Promise.all([
      this.waitForApiResponse('/bezirke'),
      this.submitButton.click()
    ])
  }

  async submitFormAndExpectSuccess(): Promise<void> {
    await this.submitFormAndWaitForResponse()
    
    // Wait for either success navigation or success toast
    await Promise.race([
      this.page.waitForURL(/\/bezirke$/, { timeout: 10000 }),
      this.waitForSuccessToast()
    ])
  }

  async submitFormAndExpectError(): Promise<void> {
    await this.submitFormAndWaitForResponse()
    
    // Wait for error state - either toast or form errors
    await Promise.race([
      this.waitForErrorToast(),
      this.showErrorsButton.waitFor({ state: 'visible', timeout: 5000 }),
      this.nameError.waitFor({ state: 'visible', timeout: 5000 })
    ])
  }

  async cancelForm(): Promise<void> {
    await this.cancelButton.click()
    await this.waitForPageLoad()
  }

  // Form validation methods
  async showValidationErrors(): Promise<void> {
    if (await this.isElementVisible(this.showErrorsButton)) {
      await this.showErrorsButton.click()
    }
  }

  async getNameError(): Promise<string> {
    if (await this.isElementVisible(this.nameError)) {
      return await this.getElementText(this.nameError)
    }
    return ''
  }

  async getBeschreibungError(): Promise<string> {
    if (await this.isElementVisible(this.beschreibungError)) {
      return await this.getElementText(this.beschreibungError)
    }
    return ''
  }

  async getAllFormErrors(): Promise<string[]> {
    const errors: string[] = []
    const errorElements = await this.form.locator('.error, .text-red-600, [role="alert"]').all()
    
    for (const element of errorElements) {
      const text = await this.getElementText(element)
      if (text.trim()) {
        errors.push(text.trim())
      }
    }
    
    return errors
  }

  // Form state methods
  async isSubmitButtonEnabled(): Promise<boolean> {
    return await this.submitButton.isEnabled()
  }

  async isFormSubmitting(): Promise<boolean> {
    return await this.isElementVisible(this.loadingSubmitButton)
  }

  async waitForFormSubmissionToComplete(): Promise<void> {
    // Wait for loading state to appear and disappear
    try {
      await this.loadingSubmitButton.waitFor({ state: 'visible', timeout: 2000 })
      await this.loadingSubmitButton.waitFor({ state: 'hidden', timeout: 30000 })
    } catch {
      // Loading state might not appear for very fast operations
    }
  }

  // Field validation methods
  async triggerNameValidation(): Promise<void> {
    await this.nameField.click()
    await this.nameField.blur()
  }

  async triggerBeschreibungValidation(): Promise<void> {
    await this.beschreibungField.click()
    await this.beschreibungField.blur()
  }

  async triggerAllFieldValidation(): Promise<void> {
    await this.triggerNameValidation()
    await this.triggerBeschreibungValidation()
  }

  // Test data methods
  async createValidBezirk(name?: string): Promise<BezirkFormData> {
    const timestamp = Date.now()
    const data: BezirkFormData = {
      name: name || `Test${timestamp.toString().slice(-4)}`,
      beschreibung: `Test Bezirk erstellt am ${new Date().toLocaleString('de-DE')}`
    }
    
    await this.fillBezirkForm(data)
    await this.submitFormAndExpectSuccess()
    
    return data
  }

  async createInvalidBezirk(invalidData: Partial<BezirkFormData>): Promise<void> {
    const data: BezirkFormData = {
      name: invalidData.name || '',
      beschreibung: invalidData.beschreibung
    }
    
    await this.fillBezirkForm(data)
    await this.submitFormAndExpectError()
  }

  // Assertion methods
  async assertPageLoaded(): Promise<void> {
    await this.assertElementVisible(this.pageTitle)
    await this.assertElementVisible(this.form)
    await this.assertElementVisible(this.nameField)
    await this.assertElementVisible(this.submitButton)
  }

  async assertFormFieldsVisible(): Promise<void> {
    await this.assertElementVisible(this.nameField)
    await this.assertElementVisible(this.nameLabel)
    await this.assertElementVisible(this.beschreibungField)
    await this.assertElementVisible(this.beschreibungLabel)
  }

  async assertFormActionsVisible(): Promise<void> {
    await this.assertElementVisible(this.submitButton)
    await this.assertElementVisible(this.cancelButton)
  }

  async assertBreadcrumbVisible(): Promise<void> {
    await this.assertElementVisible(this.breadcrumb)
    await this.assertElementVisible(this.breadcrumbBezirke)
    await this.assertElementVisible(this.breadcrumbCurrent)
  }

  async assertNameError(expectedError: string): Promise<void> {
    await this.assertElementVisible(this.nameError)
    await expect(this.nameError).toContainText(expectedError)
  }

  async assertBeschreibungError(expectedError: string): Promise<void> {
    await this.assertElementVisible(this.beschreibungError)
    await expect(this.beschreibungError).toContainText(expectedError)
  }

  async assertNoFormErrors(): Promise<void> {
    const errors = await this.getAllFormErrors()
    expect(errors).toHaveLength(0)
  }

  async assertFormSubmissionSuccess(): Promise<void> {
    // Should navigate back to bezirke overview or show success toast
    await Promise.race([
      expect(this.page).toHaveURL(/\/bezirke$/),
      this.waitForSuccessToast()
    ])
  }

  async assertFormSubmissionError(): Promise<void> {
    // Should show error toast or form errors
    const hasErrorToast = await this.isElementVisible(this.toast.filter({ hasText: /fehler|error/i }))
    const hasFormErrors = await this.getAllFormErrors()
    const hasErrorButton = await this.isElementVisible(this.showErrorsButton)
    
    expect(hasErrorToast || hasFormErrors.length > 0 || hasErrorButton).toBeTruthy()
  }

  // Integration test helpers
  async testCompleteWorkflow(data: BezirkFormData): Promise<void> {
    // Navigate to form
    await this.navigateTo()
    await this.assertPageLoaded()
    
    // Fill and submit form
    await this.fillBezirkForm(data)
    await this.assertFormFieldsVisible()
    
    // Submit and verify success
    await this.submitFormAndExpectSuccess()
    await this.assertFormSubmissionSuccess()
  }

  async testFormValidation(): Promise<void> {
    await this.navigateTo()
    await this.assertPageLoaded()
    
    // Test empty form submission
    await this.submitFormAndExpectError()
    await this.assertFormSubmissionError()
    
    // Test individual field validation
    await this.triggerAllFieldValidation()
    
    // Test field length limits
    await this.fillName('ThisNameIsTooLongForTheField')
    await this.triggerNameValidation()
    
    // Test valid data
    await this.fillName('Valid')
    await this.triggerNameValidation()
    await this.assertNoFormErrors()
  }
}