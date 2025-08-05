/**
 * Forms Accessibility Tests
 * 
 * Comprehensive accessibility tests for all form components
 * with WCAG 2.1 AA compliance, German localization, and complex validation
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import {
  checkAccessibility,
  checkGermanA11yStandards,
  testKeyboardNavigation,
  getFocusableElements,
  hasAccessibleName,
  isFormControlLabeled,
  GERMAN_A11Y_LABELS
} from '@/test/utils/accessibility-utils'
import { BezirkForm } from '@/components/forms/bezirk-form'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

// Import MSW server
import '@/test/mocks/server'

describe('Forms Accessibility Tests', () => {
  beforeEach(() => {
    server.resetHandlers()
    
    server.use(
      http.post('/api/bezirke', () => {
        return HttpResponse.json({
          success: true,
          data: testDataFactories.bezirk({ id: Date.now() })
        })
      }),
      http.put('/api/bezirke/:id', () => {
        return HttpResponse.json({
          success: true,
          data: testDataFactories.bezirk({ id: 1 })
        })
      })
    )
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('BezirkForm Accessibility', () => {
    it('sollte WCAG 2.1 AA Standards für Formulare erfüllen', () => {
      const { container } = render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      const a11yResults = checkAccessibility(container)
      
      expect(a11yResults.isValid).toBe(true)
      
      if (!a11yResults.isValid) {
        console.log('Form Accessibility Issues:', {
          headingHierarchy: a11yResults.results.headingHierarchy.issues,
          screenReaderContent: a11yResults.results.screenReaderContent.issues,
          elementsWithoutNames: a11yResults.results.elementsWithoutNames.map(el => ({
            tag: el.tagName,
            type: el.getAttribute('type'),
            text: el.textContent?.substring(0, 50)
          }))
        })
      }
    })

    describe('Form Structure and Semantics', () => {
      it('sollte korrektes Formular-Element verwenden', () => {
        const { container } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const form = container.querySelector('form')
        expect(form).toBeInTheDocument()
        expect(form).toHaveAttribute('noValidate') // Client-side validation
      })

      it('sollte Fieldset-Gruppierungen korrekt verwenden', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Sektions-Überschriften sollten vorhanden sein
        expect(screen.getByText('Grundinformationen')).toBeInTheDocument()
        expect(screen.getByText('Bezirksleitung')).toBeInTheDocument()
        expect(screen.getByText('Postanschrift')).toBeInTheDocument()

        // Sektionen sollten semantisch gruppiert sein
        const sectionHeadings = screen.getAllByRole('heading', { level: 3 })
        expect(sectionHeadings.length).toBeGreaterThanOrEqual(3)
      })

      it('sollte alle Form-Controls korrekt labeln', () => {
        const { container } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const formControls = container.querySelectorAll('input, select, textarea')
        
        formControls.forEach((control, index) => {
          expect(isFormControlLabeled(control as HTMLInputElement)).toBe(true)
        })
      })

      it('sollte Required-Fields korrekt kennzeichnen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const nameInput = screen.getByLabelText(/bezirksname/i)
        
        // Required-Attribut
        expect(nameInput).toHaveAttribute('required')
        expect(nameInput).toHaveAttribute('aria-required', 'true')
        
        // Visueller Indikator
        const label = nameInput.closest('.form-field')?.querySelector('label')
        expect(label?.textContent).toMatch(/\*|pflicht|required/i)
      })
    })

    describe('Labels and Descriptions', () => {
      it('sollte explizite Labels verwenden', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const nameInput = screen.getByLabelText('Bezirksname')
        expect(nameInput.id).toBeTruthy()
        
        const label = document.querySelector(`label[for="${nameInput.id}"]`)
        expect(label).toBeInTheDocument()
        expect(label).toHaveTextContent('Bezirksname')
      })

      it('sollte hilfreiche Beschreibungen bereitstellen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const beschreibungField = screen.getByLabelText(/beschreibung/i)
        expect(screen.getByText('Kurze Beschreibung des Bezirks (optional)')).toBeInTheDocument()
        
        const bezirksleiterField = screen.getByLabelText(/bezirksleiter/i)
        expect(screen.getByText('Name der verantwortlichen Person')).toBeInTheDocument()
        
        const telefonField = screen.getByLabelText(/telefon/i)
        expect(screen.getByText('Telefonnummer der Bezirksleitung')).toBeInTheDocument()
      })

      it('sollte aria-describedby für komplexe Felder verwenden', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const emailInput = screen.getByLabelText(/e-mail/i)
        const ariaDescribedBy = emailInput.getAttribute('aria-describedby')
        
        if (ariaDescribedBy) {
          const descriptionElement = document.getElementById(ariaDescribedBy)
          expect(descriptionElement).toBeInTheDocument()
          expect(descriptionElement?.textContent).toContain('E-Mail-Adresse')
        }
      })

      it('sollte Placeholder-Texte informativ gestalten', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Placeholder sollten Beispiele enthalten, nicht Label ersetzen
        expect(screen.getByPlaceholderText('z.B. Bezirk Nord, Zentrum, etc.')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Max Mustermann')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('bezirksleiter@kgv-beispiel.de')).toBeInTheDocument()
      })
    })

    describe('Validation and Error Handling', () => {
      it('sollte Validation-Errors accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Trigger validation durch Submit ohne required fields
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          const nameInput = screen.getByLabelText(/bezirksname/i)
          
          // Field sollte als invalid markiert sein
          expect(nameInput).toHaveAttribute('aria-invalid', 'true')
          
          // Error-Message sollte vorhanden sein
          const errorMessage = screen.getByText(/pflichtfeld|erforderlich/i)
          expect(errorMessage).toBeInTheDocument()
          
          // Error sollte mit Field verknüpft sein
          const ariaDescribedBy = nameInput.getAttribute('aria-describedby')
          if (ariaDescribedBy) {
            const errorElement = document.getElementById(ariaDescribedBy)
            expect(errorElement).toContainElement(errorMessage)
          }
        })
      })

      it('sollte Email-Validation accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const emailInput = screen.getByLabelText(/e-mail/i)
        const nameInput = screen.getByLabelText(/bezirksname/i)
        
        await user.type(nameInput, 'Test Bezirk')
        await user.type(emailInput, 'ungueltige-email')
        
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          expect(emailInput).toHaveAttribute('aria-invalid', 'true')
          expect(screen.getByText(/gültige e-mail/i)).toBeInTheDocument()
        })
      })

      it('sollte Telefon-Validation accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const telefonInput = screen.getByLabelText(/telefon/i)
        const nameInput = screen.getByLabelText(/bezirksname/i)
        
        await user.type(nameInput, 'Test Bezirk')
        await user.type(telefonInput, 'abc123')
        
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          expect(telefonInput).toHaveAttribute('aria-invalid', 'true')
          expect(screen.getByText(/gültige telefonnummer/i)).toBeInTheDocument()
        })
      })

      it('sollte PLZ-Validation accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const plzInput = screen.getByLabelText(/plz/i)
        const nameInput = screen.getByLabelText(/bezirksname/i)
        
        await user.type(nameInput, 'Test Bezirk')
        await user.type(plzInput, '123') // Zu kurz
        
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          expect(plzInput).toHaveAttribute('aria-invalid', 'true')
          expect(screen.getByText(/gültige postleitzahl/i)).toBeInTheDocument()
        })
      })

      it('sollte Erfolgs-Meldungen accessible machen', async () => {
        const onSuccess = jest.fn()
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={onSuccess}
            onCancel={jest.fn()}
          />
        )

        // Fülle Mindestdaten aus
        await user.type(screen.getByLabelText(/bezirksname/i), 'Test Bezirk')
        
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          expect(onSuccess).toHaveBeenCalled()
        })

        // Success-Toast sollte für Screen Reader ankündigt werden
        const successElements = document.querySelectorAll('[role="status"], [aria-live="polite"], [aria-live="assertive"]')
        expect(successElements.length).toBeGreaterThan(0)
      })
    })

    describe('Keyboard Navigation and Interaction', () => {
      it('sollte Tab-Reihenfolge logisch sein', async () => {
        const { container } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const navigationResult = await testKeyboardNavigation(container)
        
        expect(navigationResult.success).toBe(true)
        expect(navigationResult.focusableElements.length).toBeGreaterThan(0)
        
        // Tab-Reihenfolge sollte dem visuellen Layout folgen
        const expectedOrder = [
          'name',
          'beschreibung', 
          'bezirksleiter',
          'telefon',
          'email',
          'adresse.strasse',
          'adresse.hausnummer',
          'adresse.plz',
          'adresse.ort'
        ]
        
        const focusableFields = navigationResult.focusableElements
          .filter(el => el.tagName === 'INPUT' || el.tagName === 'TEXTAREA')
          .map(el => el.getAttribute('name') || el.getAttribute('id'))
        
        expectedOrder.forEach((fieldName, index) => {
          if (index < focusableFields.length) {
            expect(focusableFields[index]).toContain(fieldName.split('.').pop())
          }
        })
      })

      it('sollte Form-Submission mit Enter unterstützen', async () => {
        const onSuccess = jest.fn()
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={onSuccess}
            onCancel={jest.fn()}
          />
        )

        const nameInput = screen.getByLabelText(/bezirksname/i)
        await user.type(nameInput, 'Test Bezirk')
        
        // Enter im letzten Feld sollte Form submitten
        await user.keyboard('{Enter}')

        await waitFor(() => {
          expect(onSuccess).toHaveBeenCalled()
        })
      })

      it('sollte Escape für Cancel unterstützen', async () => {
        const onCancel = jest.fn()
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={onCancel}
          />
        )

        // Focus ein Feld
        const nameInput = screen.getByLabelText(/bezirksname/i)
        nameInput.focus()
        
        await user.keyboard('{Escape}')
        
        expect(onCancel).toHaveBeenCalled()
      })

      it('sollte Field-Navigation mit Pfeiltasten in TextArea unterstützen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const beschreibungTextarea = screen.getByLabelText(/beschreibung/i)
        await user.type(beschreibungTextarea, 'Erste Zeile\nZweite Zeile')
        
        // Cursor sollte navigierbar sein
        await user.keyboard('{ArrowUp}')
        await user.keyboard('{Home}')
        await user.keyboard('{End}')
        
        // Diese Aktionen sollten ohne Errors funktionieren
        expect(beschreibungTextarea).toHaveFocus()
      })
    })

    describe('Button States and Feedback', () => {
      it('sollte Submit-Button States accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        
        // Initial disabled wegen fehlendem required field
        expect(submitButton).toBeDisabled()
        expect(submitButton).toHaveAttribute('aria-describedby')
        
        // Enable durch Eingabe required field
        const nameInput = screen.getByLabelText(/bezirksname/i)
        await user.type(nameInput, 'Test Bezirk')
        
        await waitFor(() => {
          expect(submitButton).not.toBeDisabled()
        })
        
        // Loading-State während Submission
        await user.click(submitButton)
        
        const loadingButton = screen.getByRole('button', { name: /speichert/i })
        expect(loadingButton).toBeDisabled()
        expect(loadingButton).toHaveAttribute('aria-busy', 'true')
      })

      it('sollte Cancel-Button immer verfügbar machen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const cancelButton = screen.getByRole('button', { name: /abbrechen/i })
        expect(cancelButton).not.toBeDisabled()
        expect(hasAccessibleName(cancelButton)).toBe(true)
      })

      it('sollte Fehler-Button accessible machen', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Trigger Validationsfehler
        const emailInput = screen.getByLabelText(/e-mail/i)
        await user.type(emailInput, 'ungueltig')
        
        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          const errorButton = screen.getByRole('button', { name: /fehler anzeigen/i })
          expect(errorButton).toBeInTheDocument()
          expect(errorButton).toHaveAttribute('aria-describedby')
        })
      })
    })

    describe('Autocomplete and Input Assistance', () => {
      it('sollte Autocomplete-Attribute korrekt setzen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Kontakt-Felder
        expect(screen.getByLabelText(/bezirksleiter/i)).toHaveAttribute('autocomplete', 'name')
        expect(screen.getByLabelText(/telefon/i)).toHaveAttribute('autocomplete', 'tel')
        expect(screen.getByLabelText(/e-mail/i)).toHaveAttribute('autocomplete', 'email')
        
        // Adress-Felder
        expect(screen.getByLabelText(/straße/i)).toHaveAttribute('autocomplete', 'street-address')
        expect(screen.getByLabelText(/plz/i)).toHaveAttribute('autocomplete', 'postal-code')
        expect(screen.getByLabelText(/ort/i)).toHaveAttribute('autocomplete', 'address-level2')
      })

      it('sollte Input-Types korrekt setzen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        expect(screen.getByLabelText(/telefon/i)).toHaveAttribute('type', 'tel')
        expect(screen.getByLabelText(/e-mail/i)).toHaveAttribute('type', 'email')
      })

      it('sollte Input-Constraints accessibility-konform setzen', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const plzInput = screen.getByLabelText(/plz/i)
        expect(plzInput).toHaveAttribute('maxLength', '5')
        expect(plzInput).toHaveAttribute('pattern') // Regex für deutsche PLZ
        
        const nameInput = screen.getByLabelText(/bezirksname/i)
        expect(nameInput).toHaveAttribute('minLength')
        expect(nameInput).toHaveAttribute('maxLength')
      })
    })

    describe('Edit Mode Accessibility', () => {
      it('sollte Edit-Mode korrekt ankündigen', () => {
        const mockBezirk = testDataFactories.bezirk({
          name: 'Test Bezirk',
          aktiv: true
        })

        render(
          <BezirkForm
            mode="edit"
            initialData={mockBezirk}
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        expect(screen.getByRole('heading', { name: /bezirk bearbeiten/i })).toBeInTheDocument()
        expect(screen.getByText('Bearbeiten Sie die Bezirksdaten')).toBeInTheDocument()
      })

      it('sollte Status-Checkbox accessible machen', () => {
        const mockBezirk = testDataFactories.bezirk({
          aktiv: true
        })

        render(
          <BezirkForm
            mode="edit"
            initialData={mockBezirk}
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const aktivCheckbox = screen.getByRole('checkbox', { name: /bezirk ist aktiv/i })
        expect(aktivCheckbox).toBeChecked()
        expect(aktivCheckbox).toHaveAttribute('aria-describedby')
        
        const description = screen.getByText('Deaktivierte Bezirke werden nicht in Listen angezeigt')
        expect(description).toBeInTheDocument()
      })

      it('sollte vorausgefüllte Werte accessible machen', () => {
        const mockBezirk = testDataFactories.bezirk({
          name: 'Test Bezirk',
          beschreibung: 'Test Beschreibung',
          bezirksleiter: 'Max Mustermann'
        })

        render(
          <BezirkForm
            mode="edit"
            initialData={mockBezirk}
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Werte sollten in Feldern stehen und accessible sein
        expect(screen.getByDisplayValue('Test Bezirk')).toBeInTheDocument()
        expect(screen.getByDisplayValue('Test Beschreibung')).toBeInTheDocument()
        expect(screen.getByDisplayValue('Max Mustermann')).toBeInTheDocument()
      })
    })

    describe('German Form Standards', () => {
      it('sollte deutsche Field-Labels verwenden', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Deutsche Standard-Labels
        expect(screen.getByText('Bezirksname')).toBeInTheDocument()
        expect(screen.getByText('Beschreibung')).toBeInTheDocument()
        expect(screen.getByText('Bezirksleiter/in')).toBeInTheDocument()
        expect(screen.getByText('Telefon')).toBeInTheDocument()
        expect(screen.getByText('E-Mail')).toBeInTheDocument()
        expect(screen.getByText('Straße')).toBeInTheDocument()
        expect(screen.getByText('Hausnummer')).toBeInTheDocument()
        expect(screen.getByText('PLZ')).toBeInTheDocument()
        expect(screen.getByText('Ort')).toBeInTheDocument()

        // Keine englischen Labels
        expect(screen.queryByText(/name|description|phone|email|street|zip/i)).not.toBeInTheDocument()
      })

      it('sollte deutsche Button-Texte verwenden', () => {
        render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        expect(screen.getByRole('button', { name: 'Erstellen' })).toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Abbrechen' })).toBeInTheDocument()
        
        // Keine englischen Button-Texte
        expect(screen.queryByRole('button', { name: /create|save|cancel/i })).not.toBeInTheDocument()
      })

      it('sollte deutsche Error-Messages verwenden', async () => {
        const { user } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        const submitButton = screen.getByRole('button', { name: /erstellen/i })
        await user.click(submitButton)

        await waitFor(() => {
          expect(screen.getByText(/pflichtfeld|erforderlich/i)).toBeInTheDocument()
        })

        // Email-Validation
        const emailInput = screen.getByLabelText(/e-mail/i)
        const nameInput = screen.getByLabelText(/bezirksname/i)
        
        await user.type(nameInput, 'Test')
        await user.type(emailInput, 'ungueltig')
        await user.click(submitButton)

        await waitFor(() => {
          expect(screen.getByText(/gültige e-mail/i)).toBeInTheDocument()
        })
      })

      it('sollte deutsches Datumsformat in Validierung unterstützen', () => {
        // Falls Datumsfelder vorhanden wären
        const { container } = render(
          <BezirkForm
            mode="create"
            onSuccess={jest.fn()}
            onCancel={jest.fn()}
          />
        )

        // Deutsche Locale sollte für Datums-Inputs verwendet werden
        const dateInputs = container.querySelectorAll('input[type="date"]')
        dateInputs.forEach(input => {
          expect(input).toHaveAttribute('lang', 'de')
        })
      })
    })
  })
})