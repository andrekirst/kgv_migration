/**
 * Parzellen Components Accessibility Tests
 * 
 * Accessibility-Tests für Parzellen-Komponenten mit komplexem Status-Handling,
 * Multi-Filter-Support und deutscher Lokalisierung
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import {
  checkAccessibility,
  checkGermanA11yStandards,
  testKeyboardNavigation,
  simulateScreenReaderNavigation,
  getFocusableElements,
  hasAccessibleName,
  isFormControlLabeled,
  checkHeadingHierarchy,
  GERMAN_A11Y_LABELS
} from '@/test/utils/accessibility-utils'
import { ParzellenList } from '@/components/parzellen/parzellen-list'
import { ParzellenFilters } from '@/components/parzellen/parzellen-filters'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { ParzellenStatus } from '@/types/bezirke'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

// Import MSW server
import '@/test/mocks/server'

describe('Parzellen Accessibility Tests', () => {
  let mockParzellen: any[]

  beforeEach(() => {
    mockParzellen = [
      testDataFactories.parzelle({
        id: 1,
        nummer: 'P-001',
        bezirkId: 1,
        bezirkName: 'Bezirk Mitte-Nord',
        groesse: 400,
        status: ParzellenStatus.FREI,
        monatlichePacht: 25.50,
        beschreibung: 'Sonnige Parzelle mit Obstbäumen',
        ausstattung: ['Laube', 'Wasserhahn', 'Kompost'],
        aktiv: true,
        mieter: undefined,
      }),
      testDataFactories.parzelle({
        id: 2,
        nummer: 'P-002',
        bezirkId: 1,
        bezirkName: 'Bezirk Mitte-Nord',
        groesse: 350,
        status: ParzellenStatus.BELEGT,
        monatlichePacht: 22.00,
        beschreibung: 'Gemütliche Parzelle am Waldrand',
        ausstattung: ['Laube', 'Stromanschluss'],
        aktiv: true,
        mieter: {
          id: 1,
          vorname: 'Max',
          nachname: 'Mustermann',
          email: 'max.mustermann@example.com',
          telefon: '+49 30 12345678',
        },
        mietbeginn: '2023-04-01',
      }),
      testDataFactories.parzelle({
        id: 3,
        nummer: 'P-003',
        bezirkId: 2,
        bezirkName: 'Bezirk Süd-Ost',
        groesse: 600,
        status: ParzellenStatus.RESERVIERT,
        monatlichePacht: 35.00,
        beschreibung: 'Große Parzelle mit viel Platz',
        ausstattung: ['Laube', 'Wasserhahn', 'Stromanschluss', 'Geräteschuppen'],
        aktiv: true,
        mieter: undefined,
      })
    ]

    server.resetHandlers()
    
    server.use(
      http.get('/api/parzellen', () => {
        return HttpResponse.json({
          success: true,
          data: {
            parzellen: mockParzellen,
            pagination: { page: 1, limit: 20, total: mockParzellen.length, totalPages: 1 },
            filters: {}
          }
        })
      }),
      http.get('/api/bezirke/dropdown', () => {
        return HttpResponse.json({
          success: true,
          data: [
            { id: 1, name: 'Bezirk Mitte-Nord' },
            { id: 2, name: 'Bezirk Süd-Ost' }
          ]
        })
      })
    )
  })

  describe('ParzellenList Accessibility', () => {
    it('sollte WCAG 2.1 AA Standards erfüllen', async () => {
      const { container } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          onParzelleClick={jest.fn()}
          onParzelleEdit={jest.fn()}
          onParzelleAssign={jest.fn()}
          onParzelleDelete={jest.fn()}
        />
      )

      const a11yResults = checkAccessibility(container)
      
      expect(a11yResults.isValid).toBe(true)
      
      if (!a11yResults.isValid) {
        console.log('Accessibility Issues:', {
          headingHierarchy: a11yResults.results.headingHierarchy.issues,
          listSemantics: a11yResults.results.listSemantics.issues,
          screenReaderContent: a11yResults.results.screenReaderContent.issues,
          elementsWithoutNames: a11yResults.results.elementsWithoutNames.length
        })
      }
    })

    it('sollte komplexe Parzellen-Status accessible machen', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Status-Badges sollten Screen Reader freundlich sein
      const freiStatus = screen.getByText('Frei')
      const belegtStatus = screen.getByText('Belegt')  
      const reserviertStatus = screen.getByText('Reserviert')

      // Jeder Status sollte semantisch korrekt markiert sein
      expect(freiStatus.closest('[role="status"]') || 
             freiStatus.getAttribute('aria-label') ||
             freiStatus.textContent).toBeTruthy()
             
      expect(belegtStatus.closest('[role="status"]') || 
             belegtStatus.getAttribute('aria-label') ||
             belegtStatus.textContent).toBeTruthy()
             
      expect(reserviertStatus.closest('[role="status"]') || 
             reserviertStatus.getAttribute('aria-label') ||
             reserviertStatus.textContent).toBeTruthy()
    })

    it('sollte Mieter-Informationen accessible strukturieren', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Belegte Parzelle mit Mieter-Info
      expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
      expect(screen.getByText('max.mustermann@example.com')).toBeInTheDocument()
      
      // Mieter-Informationen sollten semantisch gruppiert sein
      const mieterContainer = screen.getByText('Max Mustermann').closest('.mieter-info') ||
                              screen.getByText('Max Mustermann').parentElement
      
      if (mieterContainer) {
        expect(mieterContainer).toContainElement(screen.getByText('Max Mustermann'))
        expect(mieterContainer).toContainElement(screen.getByText('max.mustermann@example.com'))
      }

      // Nicht zugewiesene Parzellen
      const nichtZugewiesen = screen.getAllByText('Nicht zugewiesen')
      expect(nichtZugewiesen.length).toBeGreaterThan(0)
      
      nichtZugewiesen.forEach(element => {
        expect(element).toHaveAttribute('aria-label', 'Keine Mieter zugewiesen')
      })
    })

    it('sollte Ausstattungs-Listen accessible machen', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Ausstattung sollte als Liste markiert sein
      expect(screen.getByText('Ausstattung')).toBeInTheDocument()
      
      // Ausstattungs-Items
      expect(screen.getByText('Laube')).toBeInTheDocument()
      expect(screen.getByText('Wasserhahn')).toBeInTheDocument()
      expect(screen.getByText('Kompost')).toBeInTheDocument()
      
      // "+X weitere" sollte informativ sein
      const weitereElement = screen.queryByText('+1 weitere')
      if (weitereElement) {
        expect(weitereElement).toHaveAttribute('aria-label', '1 weitere Ausstattungsmerkmale')
      }
    })

    it('sollte View-Mode-Umschaltung accessible machen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // View-Mode-Buttons sollten korrekte ARIA-Attribute haben
      const gridButton = screen.getByLabelText('Rasteransicht')
      const listButton = screen.getByLabelText('Listenansicht')
      
      expect(gridButton).toHaveAttribute('aria-pressed', 'true') // Default ist Grid
      expect(listButton).toHaveAttribute('aria-pressed', 'false')
      
      // Umschalten sollte ARIA-States updaten
      await user.click(listButton)
      
      expect(gridButton).toHaveAttribute('aria-pressed', 'false')
      expect(listButton).toHaveAttribute('aria-pressed', 'true')
    })

    it('sollte Aktions-Dropdowns accessible machen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          onParzelleEdit={jest.fn()}
          onParzelleDelete={jest.fn()}
        />
      )

      // Dropdown-Trigger sollte korrekte ARIA-Attribute haben
      const dropdownTrigger = screen.getAllByLabelText('Weitere Aktionen')[0]
      expect(dropdownTrigger).toHaveAttribute('aria-expanded', 'false')
      expect(dropdownTrigger).toHaveAttribute('aria-haspopup', 'menu')
      
      await user.click(dropdownTrigger)
      
      expect(dropdownTrigger).toHaveAttribute('aria-expanded', 'true')
      
      // Menu-Items sollten korrekte Role haben
      const bearbeitenOption = screen.getByRole('menuitem', { name: 'Bearbeiten' })
      const loeschenOption = screen.getByRole('menuitem', { name: 'Löschen' })
      
      expect(bearbeitenOption).toBeInTheDocument()
      expect(loeschenOption).toBeInTheDocument()
    })

    it('sollte Zuweisen-Button für freie Parzellen korrekt beschriften', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          onParzelleAssign={jest.fn()}
        />
      )

      // Freie Parzelle sollte Zuweisen-Button haben
      const zuweisenButton = screen.getByRole('button', { name: 'Zuweisen' })
      expect(zuweisenButton).toBeInTheDocument()
      expect(zuweisenButton).toHaveAttribute('aria-label', 'Parzelle P-001 einem Mieter zuweisen')
    })

    it('sollte Keyboard-Navigation zwischen Parzellen unterstützen', async () => {
      const { container } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          onParzelleClick={jest.fn()}
        />
      )

      const navigationResult = await testKeyboardNavigation(container)
      
      expect(navigationResult.success).toBe(true)
      expect(navigationResult.focusableElements.length).toBeGreaterThan(0)
      
      // Jede Parzelle sollte fokussierbar sein
      const parzelleCards = container.querySelectorAll('[data-testid*="parzelle-card"]')
      parzelleCards.forEach(card => {
        expect(isFocusable(card)).toBe(true)
      })
    })
  })

  describe('ParzellenFilters Accessibility', () => {
    it('sollte komplexe Filter-UI accessible machen', async () => {
      const { container } = render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
          showBezirkFilter={true}
        />
      )

      // Alle Form-Controls sollten gelabelt sein
      const inputs = container.querySelectorAll('input, select, textarea')
      inputs.forEach((input, index) => {
        expect(isFormControlLabeled(input as HTMLInputElement)).toBe(true)
      })
    })

    it('sollte Multi-Status-Filter accessible machen', async () => {
      const { user } = render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)
      
      // Alle Status-Optionen sollten als Checkboxes oder Options verfügbar sein
      expect(screen.getByRole('option', { name: 'Frei' })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: 'Belegt' })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: 'Reserviert' })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: 'Wartung' })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: 'Gesperrt' })).toBeInTheDocument()
      
      // Multi-Selection sollte möglich sein
      const freiOption = screen.getByRole('option', { name: 'Frei' })
      const belegtOption = screen.getByRole('option', { name: 'Belegt' })
      
      await user.click(freiOption)
      await user.click(belegtOption)
      
      // Beide sollten als selected markiert sein
      expect(freiOption).toHaveAttribute('aria-selected', 'true')
      expect(belegtOption).toHaveAttribute('aria-selected', 'true')
    })

    it('sollte Range-Filter accessible machen', async () => {
      render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      // Größe Range-Filter
      const groesseMinInput = screen.getByLabelText('Mindestgröße')
      const groesseMaxInput = screen.getByLabelText('Maximalgröße')
      
      expect(groesseMinInput).toHaveAttribute('type', 'number')
      expect(groesseMaxInput).toHaveAttribute('type', 'number')
      expect(groesseMinInput).toHaveAttribute('min', '50')
      expect(groesseMaxInput).toHaveAttribute('max', '2000')
      
      // Pacht Range-Filter
      const pachtMinInput = screen.getByLabelText('Mindestpacht')
      const pachtMaxInput = screen.getByLabelText('Maximalpacht')
      
      expect(pachtMinInput).toHaveAttribute('type', 'number')
      expect(pachtMaxInput).toHaveAttribute('type', 'number')
      expect(pachtMinInput).toHaveAttribute('step', '0.50')
      expect(pachtMaxInput).toHaveAttribute('step', '0.50')
    })

    it('sollte Bezirk-Filter mit API-Daten accessible machen', async () => {
      const { user } = render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
          showBezirkFilter={true}
        />
      )

      const bezirkButton = screen.getByRole('button', { name: /bezirk/i })
      await user.click(bezirkButton)
      
      await waitFor(() => {
        expect(screen.getByRole('option', { name: 'Bezirk Mitte-Nord' })).toBeInTheDocument()
        expect(screen.getByRole('option', { name: 'Bezirk Süd-Ost' })).toBeInTheDocument()
      })
    })

    it('sollte aktive Filter-Badges accessible machen', async () => {
      const { user } = render(
        <ParzellenFilters
          initialFilters={{ search: 'test', groesseMin: 300 }}
          onFiltersChange={jest.fn()}
        />
      )

      // Aktive Filter sollten angezeigt werden
      expect(screen.getByText('Aktive Filter:')).toBeInTheDocument()
      
      // Filter-Badges sollten entfernbar sein
      const filterBadges = screen.getAllByLabelText(/filter entfernen/i)
      expect(filterBadges.length).toBeGreaterThan(0)
      
      // Jeder Badge sollte korrekt beschriftet sein
      filterBadges.forEach(badge => {
        expect(hasAccessibleName(badge)).toBe(true)
      })
    })

    it('sollte Filter-Reset accessible machen', async () => {
      const onFiltersChange = jest.fn()
      
      render(
        <ParzellenFilters
          initialFilters={{ search: 'test', status: [ParzellenStatus.FREI] }}
          onFiltersChange={onFiltersChange}
        />
      )

      const resetButton = screen.getByRole('button', { name: 'Filter zurücksetzen' })
      expect(resetButton).toBeInTheDocument()
      expect(resetButton).toHaveAttribute('aria-label', 'Alle Filter zurücksetzen')
      
      const { user } = { user: userEvent.setup() }
      await user.click(resetButton)
      
      expect(onFiltersChange).toHaveBeenCalledWith({})
    })
  })

  describe('Complex Interaction Accessibility', () => {
    it('sollte Sortierung mit Screen Reader funktionieren', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      const sortButton = screen.getByRole('button', { name: /sortierung/i })
      expect(sortButton).toHaveAttribute('aria-expanded', 'false')
      
      await user.click(sortButton)
      expect(sortButton).toHaveAttribute('aria-expanded', 'true')
      
      // Sortier-Optionen sollten korrekt beschriftet sein
      const sortOptions = screen.getAllByRole('menuitem')
      sortOptions.forEach(option => {
        expect(hasAccessibleName(option)).toBe(true)
        expect(option.textContent).toMatch(/nach (nummer|größe|pacht|erstellungsdatum)/i)
      })
      
      // Aktuelle Sortierung sollte markiert sein
      const activeSort = sortOptions.find(option => 
        option.getAttribute('aria-current') === 'true' ||
        option.getAttribute('data-selected') === 'true'
      )
      expect(activeSort).toBeTruthy()
    })

    it('sollte Paginierung accessible machen', () => {
      const mockPaginatedParzellen = Array.from({ length: 25 }, (_, i) =>
        testDataFactories.parzelle({ id: i + 1, nummer: `P-${String(i + 1).padStart(3, '0')}` })
      )

      render(
        <ParzellenList
          parzellen={mockPaginatedParzellen.slice(0, 20)}
          loading={false}
          pagination={{
            page: 1,
            limit: 20,
            total: 25,
            totalPages: 2
          }}
        />
      )

      // Pagination sollte als Navigation markiert sein
      const pagination = screen.getByRole('navigation', { name: /seitennavigation/i })
      expect(pagination).toBeInTheDocument()
      
      // Pagination-Info sollte für Screen Reader verfügbar sein
      expect(screen.getByText(/zeige 1 bis 20 von 25/i)).toBeInTheDocument()
      
      // Next-Button sollte korrekt beschriftet sein
      const nextButton = screen.getByRole('button', { name: 'Nächste Seite' })
      expect(nextButton).toBeInTheDocument()
      expect(nextButton).not.toBeDisabled()
      
      // Previous-Button sollte disabled sein auf Seite 1
      const prevButton = screen.queryByRole('button', { name: 'Vorherige Seite' })
      expect(prevButton).toBeNull() // Nicht vorhanden auf Seite 1
    })

    it('sollte Bulk-Aktionen accessible machen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          showBulkActions={true}
          onBulkAction={jest.fn()}
        />
      )

      // Select-All Checkbox
      const selectAllCheckbox = screen.getByRole('checkbox', { name: 'Alle Parzellen auswählen' })
      expect(selectAllCheckbox).toBeInTheDocument()
      
      await user.click(selectAllCheckbox)
      
      // Individual Checkboxes sollten alle checked sein
      const individualCheckboxes = screen.getAllByRole('checkbox')
        .filter(cb => cb !== selectAllCheckbox)
      
      individualCheckboxes.forEach(checkbox => {
        expect(checkbox).toBeChecked()
      })
      
      // Bulk-Action-Buttons sollten aktiviert sein
      const bulkDeleteButton = screen.getByRole('button', { name: /ausgewählte löschen/i })
      expect(bulkDeleteButton).not.toBeDisabled()
      expect(bulkDeleteButton).toHaveAttribute('aria-label', '3 Parzellen löschen')
    })
  })

  describe('German Accessibility Standards', () => {
    it('sollte deutsche Parzellen-Status verwenden', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Deutsche Status-Begriffe
      expect(screen.getByText('Frei')).toBeInTheDocument()
      expect(screen.getByText('Belegt')).toBeInTheDocument()
      expect(screen.getByText('Reserviert')).toBeInTheDocument()
      
      // Keine englischen Status-Begriffe
      expect(screen.queryByText(/free|occupied|reserved|available/i)).not.toBeInTheDocument()
    })

    it('sollte deutsche Maßeinheiten verwenden', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Deutsche Quadratmeter-Angaben
      expect(screen.getByText('400 m²')).toBeInTheDocument()
      expect(screen.getByText('350 m²')).toBeInTheDocument()
      expect(screen.getByText('600 m²')).toBeInTheDocument()
      
      // Deutsche Währungsangaben
      expect(screen.getByText('25,50 €')).toBeInTheDocument()
      expect(screen.getByText('22,00 €')).toBeInTheDocument()
      expect(screen.getByText('35,00 €')).toBeInTheDocument()
    })

    it('sollte deutsche Datumsangaben verwenden', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
        />
      )

      // Deutsches Datumsformat für Mietbeginn
      expect(screen.getByText(/01\.04\.2023/)).toBeInTheDocument()
      expect(screen.getByText(/seit/i)).toBeInTheDocument()
    })

    it('sollte deutsche Accessibility-Labels verwenden', () => {
      render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      // Deutsche Placeholder-Texte
      expect(screen.getByPlaceholderText('Parzellen durchsuchen...')).toBeInTheDocument()
      
      // Deutsche Button-Texte
      expect(screen.getByRole('button', { name: /status/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sortierung/i })).toBeInTheDocument()
      
      // Deutsche ARIA-Labels
      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      expect(searchInput).toHaveAttribute('aria-label', 'Parzellen durchsuchen')
    })
  })

  describe('Mobile Accessibility', () => {
    it('sollte Touch-Navigation unterstützen', () => {
      // Simuliere Mobile Viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })
      
      const { container } = render(
        <ParzellenList
          parzellen={mockParzellen}
          loading={false}
          onParzelleClick={jest.fn()}
        />
      )

      // Touch-Targets sollten mindestens 44px sein
      const buttons = container.querySelectorAll('button')
      buttons.forEach(button => {
        const computedStyle = window.getComputedStyle(button)
        const minSize = Math.min(
          parseInt(computedStyle.minHeight) || parseInt(computedStyle.height) || 0,
          parseInt(computedStyle.minWidth) || parseInt(computedStyle.width) || 0
        )
        expect(minSize).toBeGreaterThanOrEqual(44)
      })
    })

    it('sollte Mobile-Menüs accessible machen', async () => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      const { user } = render(
        <ParzellenFilters
          initialFilters={{}}
          onFiltersChange={jest.fn()}
        />
      )

      // Mobile Filter-Button
      const mobileFilterButton = screen.getByRole('button', { name: /filter/i })
      expect(mobileFilterButton).toHaveAttribute('aria-expanded', 'false')
      expect(mobileFilterButton).toHaveAttribute('aria-controls')
      
      await user.click(mobileFilterButton)
      
      expect(mobileFilterButton).toHaveAttribute('aria-expanded', 'true')
      
      // Filter-Panel sollte mit Escape schließbar sein
      await user.keyboard('{Escape}')
      expect(mobileFilterButton).toHaveAttribute('aria-expanded', 'false')
    })
  })

  describe('Error States Accessibility', () => {
    it('sollte Loading-Errors accessible machen', () => {
      render(
        <ParzellenList
          parzellen={[]}
          loading={false}
          error="Fehler beim Laden der Parzellen"
        />
      )

      const errorMessage = screen.getByRole('alert')
      expect(errorMessage).toHaveTextContent('Fehler beim Laden der Parzellen')
      expect(errorMessage).toHaveAttribute('aria-live', 'assertive')
    })

    it('sollte Empty-State mit Hilfestellung accessible machen', () => {
      render(
        <ParzellenList
          parzellen={[]}
          loading={false}
        />
      )

      // Empty-State sollte informativ sein
      expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      expect(screen.getByText('Es wurden noch keine Parzellen erstellt.')).toBeInTheDocument()
      
      // Call-to-Action sollte verfügbar sein
      const createButton = screen.getByRole('button', { name: /erste parzelle erstellen/i })
      expect(createButton).toBeInTheDocument()
      expect(hasAccessibleName(createButton)).toBe(true)
    })

    it('sollte Filter-No-Results accessible machen', () => {
      render(
        <ParzellenList
          parzellen={[]}
          loading={false}
          appliedFilters={{ search: 'nicht-existent' }}
        />
      )

      expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      expect(screen.getByText('Versuchen Sie andere Suchkriterien oder entfernen Sie Filter.')).toBeInTheDocument()
      
      // Filter-Reset sollte als Lösung angeboten werden
      const resetButton = screen.getByRole('button', { name: /filter zurücksetzen/i })
      expect(resetButton).toBeInTheDocument()
    })
  })
})