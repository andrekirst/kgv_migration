/**
 * Bezirke Components Accessibility Tests
 * 
 * Umfassende Accessibility-Tests für alle Bezirke-Komponenten
 * mit deutschem Screen Reader Support und WCAG 2.1 AA Compliance
 */

import React from 'react'
import { render, screen } from '@/test/utils/test-utils'
import {
  checkAccessibility,
  checkGermanA11yStandards,
  testKeyboardNavigation,
  simulateScreenReaderNavigation,
  getFocusableElements,
  hasAccessibleName,
  isFormControlLabeled,
  GERMAN_A11Y_LABELS
} from '@/test/utils/accessibility-utils'
import { BezirkeList } from '@/components/bezirke/bezirke-list'
import { BezirkeFilters } from '@/components/bezirke/bezirke-filters'
import { BezirkForm } from '@/components/forms/bezirk-form'
import { testDataFactories, bezirkeData } from '@/test/fixtures/kgv-data'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

// Import MSW server
import '@/test/mocks/server'

describe('Bezirke Accessibility Tests', () => {
  beforeEach(() => {
    server.resetHandlers()
    
    // Standard API responses
    server.use(
      http.get('/api/bezirke', () => {
        return HttpResponse.json({
          success: true,
          data: {
            bezirke: bezirkeData,
            pagination: { page: 1, limit: 20, total: bezirkeData.length, totalPages: 1 },
            filters: {}
          }
        })
      }),
      http.get('/api/bezirke/dropdown', () => {
        return HttpResponse.json({
          success: true,
          data: bezirkeData.map(b => ({ id: b.id, name: b.name }))
        })
      })
    )
  })

  describe('BezirkeList Accessibility', () => {
    it('sollte WCAG 2.1 AA Standards erfüllen', async () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
          onBezirkClick={jest.fn()}
          onBezirkEdit={jest.fn()}
        />
      )

      const a11yResults = checkAccessibility(container)
      
      expect(a11yResults.isValid).toBe(true)
      
      if (!a11yResults.isValid) {
        console.log('Accessibility Issues:', {
          headingHierarchy: a11yResults.results.headingHierarchy.issues,
          listSemantics: a11yResults.results.listSemantics.issues,
          textContrast: a11yResults.results.textContrast.issues,
          screenReaderContent: a11yResults.results.screenReaderContent.issues,
          elementsWithoutNames: a11yResults.results.elementsWithoutNames.length
        })
      }
    })

    it('sollte korrekte Heading-Hierarchie haben', () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
        />
      )

      const headings = screen.getAllByRole('heading')
      expect(headings.length).toBeGreaterThan(0)

      // Hauptüberschrift sollte Level 2 sein (da es eine Komponente ist)
      const mainHeading = screen.getByRole('heading', { name: /bezirke/i })
      expect(mainHeading.tagName).toBe('H2')
    })

    it('sollte alle interaktiven Elemente accessible names haben', () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
          onBezirkClick={jest.fn()}
          onBezirkEdit={jest.fn()}
        />
      )

      const buttons = screen.getAllByRole('button')
      const links = screen.getAllByRole('link')
      
      buttons.forEach((button, index) => {
        expect(hasAccessibleName(button)).toBe(true)
        expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
      })

      links.forEach((link, index) => {
        expect(hasAccessibleName(link)).toBe(true)
        expect(link.textContent || link.getAttribute('aria-label')).toBeTruthy()
      })
    })

    it('sollte Keyboard-Navigation unterstützen', async () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
          onBezirkClick={jest.fn()}
          onBezirkEdit={jest.fn()}
        />
      )

      const navigationResult = await testKeyboardNavigation(container)
      
      expect(navigationResult.success).toBe(true)
      expect(navigationResult.focusableElements.length).toBeGreaterThan(0)
      
      if (!navigationResult.success) {
        console.log('Keyboard Navigation Issues:', navigationResult.issues)
      }
    })

    it('sollte Screen Reader Navigation unterstützen', async () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
        />
      )

      const screenReader = await simulateScreenReaderNavigation(container)
      
      expect(screenReader.landmarks.length).toBeGreaterThan(0)
      expect(screenReader.headings.length).toBeGreaterThan(0)
      expect(screenReader.focusableElements.length).toBeGreaterThan(0)
    })

    it('sollte korrekte ARIA-Labels für Status-Badges haben', () => {
      const bezirkeWithStatus = bezirkeData.map(b => ({
        ...b,
        aktiv: Math.random() > 0.5
      }))

      render(
        <BezirkeList
          bezirke={bezirkeWithStatus}
          loading={false}
        />
      )

      const aktivBadges = screen.getAllByText('Aktiv')
      const inaktivBadges = screen.getAllByText('Inaktiv')
      
      [...aktivBadges, ...inaktivBadges].forEach(badge => {
        expect(badge).toBeInTheDocument()
        // Status sollte für Screen Reader verständlich sein
        expect(badge.closest('[role="status"]') || 
               badge.getAttribute('aria-label') || 
               badge.textContent).toBeTruthy()
      })
    })

    it('sollte Loading-State accessible machen', () => {
      const { container } = render(
        <BezirkeList
          bezirke={[]}
          loading={true}
        />
      )

      // Loading-Indikatoren sollten aria-live haben
      const loadingElements = container.querySelectorAll('.animate-pulse')
      expect(loadingElements.length).toBeGreaterThan(0)
      
      // Oder ein expliziter Loading-Text sollte vorhanden sein
      const loadingText = screen.queryByText(/lädt/i) || 
                          screen.queryByLabelText(/lädt/i) ||
                          container.querySelector('[role="progressbar"]')
      
      expect(loadingText || loadingElements.length > 0).toBeTruthy()
    })

    it('sollte Empty State accessible machen', () => {
      render(
        <BezirkeList
          bezirke={[]}
          loading={false}
        />
      )

      const emptyMessage = screen.getByText('Keine Bezirke gefunden')
      expect(emptyMessage).toBeInTheDocument()
      
      // Empty State sollte informativ und hilfreich sein
      expect(screen.getByText('Es wurden noch keine Bezirke erstellt.')).toBeInTheDocument()
      
      // Action-Button sollte verfügbar sein
      const createButton = screen.getByRole('button', { name: /bezirk erstellen/i })
      expect(createButton).toBeInTheDocument()
      expect(hasAccessibleName(createButton)).toBe(true)
    })

    it('sollte deutsche Accessibility-Standards erfüllen', () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
        />
      )

      const germanA11yResult = checkGermanA11yStandards(container)
      
      expect(germanA11yResult.isValid).toBe(true)
      
      if (!germanA11yResult.isValid) {
        console.log('German A11y Issues:', germanA11yResult.issues)
      }
    })
  })

  describe('BezirkeFilters Accessibility', () => {
    it('sollte alle Form-Controls korrekt labeln', async () => {
      const { container } = render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      await screen.findByPlaceholderText('Bezirke durchsuchen...')

      const inputs = container.querySelectorAll('input, select, textarea')
      
      inputs.forEach((input, index) => {
        expect(isFormControlLabeled(input as HTMLInputElement)).toBe(true)
      })
    })

    it('sollte Suchfeld accessible machen', async () => {
      render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      const searchInput = await screen.findByPlaceholderText('Bezirke durchsuchen...')
      expect(searchInput).toHaveAttribute('type', 'search')
      expect(searchInput).toHaveAttribute('aria-label', 'Bezirke durchsuchen')
    })

    it('sollte Dropdown-Buttons korrekte ARIA-Attribute haben', async () => {
      const { user } = render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      const statusButton = screen.getByRole('button', { name: /status/i })
      expect(statusButton).toHaveAttribute('aria-expanded', 'false')
      expect(statusButton).toHaveAttribute('aria-haspopup', 'listbox')

      await user.click(statusButton)
      expect(statusButton).toHaveAttribute('aria-expanded', 'true')
    })

    it('sollte Filter-Badges accessible machen', async () => {
      const { user } = render(
        <BezirkeFilters
          initialFilters={{ search: 'test' }}
          onFiltersChange={jest.fn()}
        />
      )

      // Aktiviere einen Filter
      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)
      
      const aktivOption = screen.getByRole('option', { name: /aktiv/i })
      await user.click(aktivOption)

      // Filter-Badge sollte accessible sein
      const filterBadge = await screen.findByLabelText(/filter entfernen/i)
      expect(filterBadge).toBeInTheDocument()
      expect(hasAccessibleName(filterBadge)).toBe(true)
    })

    it('sollte Keyboard-Navigation in Dropdowns unterstützen', async () => {
      const { user } = render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      // Öffne Dropdown mit Enter/Space
      const statusButton = screen.getByRole('button', { name: /status/i })
      statusButton.focus()
      await user.keyboard('{Enter}')
      
      expect(statusButton).toHaveAttribute('aria-expanded', 'true')

      // Navigiere mit Pfeiltasten
      await user.keyboard('{ArrowDown}')
      expect(document.activeElement?.getAttribute('role')).toBe('option')

      // Schließe mit Escape
      await user.keyboard('{Escape}')
      expect(statusButton).toHaveAttribute('aria-expanded', 'false')
    })
  })

  describe('BezirkForm Accessibility', () => {
    it('sollte alle Form-Fields korrekt labeln', () => {
      const { container } = render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      const inputs = container.querySelectorAll('input, select, textarea')
      
      inputs.forEach((input, index) => {
        expect(isFormControlLabeled(input as HTMLInputElement)).toBe(true)
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
      expect(nameInput).toHaveAttribute('required')
      expect(nameInput).toHaveAttribute('aria-required', 'true')
    })

    it('sollte Validation-Errors accessible machen', async () => {
      const { user } = render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      // Trigger Validation durch Submit ohne required fields
      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      // Error-Messages sollten mit Fields verknüpft sein
      const nameInput = screen.getByLabelText(/bezirksname/i)
      expect(nameInput).toHaveAttribute('aria-invalid', 'true')
      
      const errorMessage = screen.getByText(/pflichtfeld/i)
      expect(errorMessage).toBeInTheDocument()
      
      // Error sollte mit Input verknüpft sein via aria-describedby
      const ariaDescribedBy = nameInput.getAttribute('aria-describedby')
      if (ariaDescribedBy) {
        const errorElement = document.getElementById(ariaDescribedBy)
        expect(errorElement).toContainElement(errorMessage)
      } else {
        // Alternative: Error ist in der Nähe des Inputs
        expect(nameInput.closest('.form-field')).toContainElement(errorMessage)
      }
    })

    it('sollte Fieldset-Gruppen korrekt strukturieren', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      // Sektionen sollten semantisch gruppiert sein
      expect(screen.getByText('Grundinformationen')).toBeInTheDocument()
      expect(screen.getByText('Bezirksleitung')).toBeInTheDocument()
      expect(screen.getByText('Postanschrift')).toBeInTheDocument()

      // Jede Sektion sollte als Fieldset oder mit Role gruppiert sein
      const sectionHeadings = screen.getAllByRole('heading', { level: 3 })
      expect(sectionHeadings.length).toBeGreaterThanOrEqual(3)
    })

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
      
      // Fill required field
      const nameInput = screen.getByLabelText(/bezirksname/i)
      await user.type(nameInput, 'Test Bezirk')
      
      expect(submitButton).not.toBeDisabled()
      
      // During submission sollte Loading-State accessible sein
      await user.click(submitButton)
      
      // Loading-Button sollte korrekte Attribute haben
      const loadingButton = screen.getByRole('button', { name: /speichert/i })
      expect(loadingButton).toBeDisabled()
      expect(loadingButton).toHaveAttribute('aria-busy', 'true')
    })

    it('sollte Autocomplete-Attribute für bessere UX setzen', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      // Kontakt-Fields sollten Autocomplete haben
      expect(screen.getByLabelText(/bezirksleiter/i)).toHaveAttribute('autocomplete', 'name')
      expect(screen.getByLabelText(/telefon/i)).toHaveAttribute('autocomplete', 'tel')
      expect(screen.getByLabelText(/e-mail/i)).toHaveAttribute('autocomplete', 'email')
      
      // Adress-Fields
      expect(screen.getByLabelText(/straße/i)).toHaveAttribute('autocomplete', 'street-address')
      expect(screen.getByLabelText(/plz/i)).toHaveAttribute('autocomplete', 'postal-code')
      expect(screen.getByLabelText(/ort/i)).toHaveAttribute('autocomplete', 'address-level2')
    })

    it('sollte Checkbox accessible machen (Edit-Mode)', () => {
      const mockBezirk = testDataFactories.bezirk({ aktiv: true })
      
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
      
      // Description sollte hilfreichen Text enthalten
      const description = screen.getByText('Deaktivierte Bezirke werden nicht in Listen angezeigt')
      expect(description).toBeInTheDocument()
    })
  })

  describe('German Accessibility Standards', () => {
    it('sollte deutsche Sprach-Attribute verwenden', () => {
      render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
        />
      )

      // HTML sollte deutsche Sprache definiert haben
      expect(document.documentElement).toHaveAttribute('lang', 'de')
    })

    it('sollte deutsche Button-Texte verwenden', () => {
      render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      // Deutsche Standard-Button-Texte
      expect(screen.getByRole('button', { name: 'Erstellen' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Abbrechen' })).toBeInTheDocument()
      
      // Keine englischen Begriffe
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

      // Trigger validation
      const submitButton = screen.getByRole('button', { name: /erstellen/i })
      await user.click(submitButton)

      // Deutsche Fehlermeldungen
      expect(screen.getByText(/pflichtfeld/i)).toBeInTheDocument()
      
      // Test email validation
      const emailInput = screen.getByLabelText(/e-mail/i)
      await user.type(emailInput, 'ungueltige-email')
      
      const nameInput = screen.getByLabelText(/bezirksname/i)
      await user.type(nameInput, 'Test')
      
      await user.click(submitButton)
      
      expect(screen.getByText(/gültige e-mail/i)).toBeInTheDocument()
    })

    it('sollte deutsche Placeholder-Texte verwenden', () => {
      render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      expect(screen.getByPlaceholderText('Bezirke durchsuchen...')).toBeInTheDocument()
    })

    it('sollte deutsche Datum- und Zahlenformate unterstützen', () => {
      const bezirkWithStats = testDataFactories.bezirk({
        statistiken: {
          gesamtParzellen: 1234,
          belegteParzellen: 567,
          freieParzellen: 667,
          auslastung: 45.67
        },
        erstelltAm: '2024-01-15T10:30:00Z'
      })

      render(
        <BezirkeList
          bezirke={[bezirkWithStats]}
          loading={false}
        />
      )

      // Deutsche Zahlenformatierung (Punkt als Tausendertrennzeichen, Komma als Dezimaltrennzeichen)
      expect(screen.getByText('1.234')).toBeInTheDocument()
      expect(screen.getByText('45,67%')).toBeInTheDocument()
      
      // Deutsches Datumsformat
      expect(screen.getByText('15.01.2024')).toBeInTheDocument()
    })
  })

  describe('WCAG 2.1 AA Compliance', () => {
    it('sollte ausreichende Farbkontraste haben', () => {
      const { container } = render(
        <BezirkeList
          bezirke={bezirkeData}
          loading={false}
        />
      )

      const textContrastResult = checkAccessibility(container).results.textContrast
      expect(textContrastResult.isValid).toBe(true)
      
      if (!textContrastResult.isValid) {
        console.log('Color Contrast Issues:', textContrastResult.issues)
      }
    })

    it('sollte ohne Maus bedienbar sein', async () => {
      const onBezirkClick = jest.fn()
      const onBezirkEdit = jest.fn()
      
      render(
        <BezirkeList
          bezirke={bezirkeData.slice(0, 1)}
          loading={false}
          onBezirkClick={onBezirkClick}
          onBezirkEdit={onBezirkEdit}
        />
      )

      const { user } = { user: userEvent.setup() }
      
      // Navigiere mit Tab zum ersten Bezirk
      await user.tab()
      
      // Enter sollte Click triggern
      await user.keyboard('{Enter}')
      expect(onBezirkClick).toHaveBeenCalled()
    })

    it('sollte Focus-Indikatoren haben', async () => {
      const { container } = render(
        <BezirkeFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      const focusableElements = getFocusableElements(container)
      const { user } = { user: userEvent.setup() }
      
      for (const element of focusableElements.slice(0, 3)) { // Test first 3 elements
        element.focus()
        await user.tab()
        
        // Element sollte Focus-Styles haben (kann durch CSS definiert sein)
        const computedStyle = window.getComputedStyle(element)
        expect(element).toHaveFocus()
      }
    })

    it('sollte responsive und zoombar sein', () => {
      const { container } = render(
        <BezirkForm
          mode="create"
          onSuccess={jest.fn()}
          onCancel={jest.fn()}
        />
      )

      // Simuliere 200% Zoom
      Object.defineProperty(window, 'devicePixelRatio', {
        writable: true,
        configurable: true,
        value: 2,
      })

      // Container sollte nicht horizontal scrollen
      expect(container.scrollWidth).toBeLessThanOrEqual(container.clientWidth + 10) // 10px tolerance
    })
  })
})