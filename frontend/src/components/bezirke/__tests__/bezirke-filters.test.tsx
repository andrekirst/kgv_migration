import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import { BezirkeFilters } from '../bezirke-filters'
import { BezirkeFilter } from '@/types/bezirke'

describe('BezirkeFilters Component', () => {
  let mockOnFiltersChange: jest.Mock
  let mockFilters: BezirkeFilter
  let mockAvailableOrte: string[]
  let mockAvailableBezirksleiter: string[]

  beforeEach(() => {
    mockOnFiltersChange = jest.fn()
    mockFilters = {
      search: '',
      aktiv: true,
      sortBy: 'name',
      sortOrder: 'asc',
      page: 1,
      limit: 20,
    }
    mockAvailableOrte = ['Berlin', 'Hamburg', 'München', 'Köln']
    mockAvailableBezirksleiter = ['Herr Müller', 'Frau Schmidt', 'Dr. Weber', 'Frau Klein']
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Rendering - Vollständige Ansicht', () => {
    it('sollte alle Filter-Optionen in der vollständigen Ansicht anzeigen', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableOrte={mockAvailableOrte}
          availableBezirksleiter={mockAvailableBezirksleiter}
        />
      )

      // Haupttitel und Icons
      expect(screen.getByText('Filter & Suche')).toBeInTheDocument()

      // Suchfeld
      expect(screen.getByLabelText('Suche')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('Name, Beschreibung oder Bezirksleiter...')).toBeInTheDocument()

      // Status-Filter
      expect(screen.getByLabelText('Status')).toBeInTheDocument()

      // Ort-Filter
      expect(screen.getByLabelText('Ort')).toBeInTheDocument()

      // Bezirksleiter-Filter
      expect(screen.getByLabelText('Bezirksleiter')).toBeInTheDocument()

      // Sortierung
      expect(screen.getByLabelText('Sortierung')).toBeInTheDocument()
      expect(screen.getByLabelText('Sortieren nach')).toBeInTheDocument()
      expect(screen.getByLabelText('Reihenfolge')).toBeInTheDocument()

      // Pagination
      expect(screen.getByLabelText('Seite')).toBeInTheDocument()
      expect(screen.getByLabelText('Anzahl pro Seite')).toBeInTheDocument()
    })

    it('sollte ohne optionale Daten korrekt rendern', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      // Grundlegende Filter sollten immer vorhanden sein
      expect(screen.getByText('Filter & Suche')).toBeInTheDocument()
      expect(screen.getByLabelText('Suche')).toBeInTheDocument()
      expect(screen.getByLabelText('Status')).toBeInTheDocument()

      // Optionale Filter sollten nicht angezeigt werden
      expect(screen.queryByLabelText('Ort')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Bezirksleiter')).not.toBeInTheDocument()
    })

    it('sollte den "Alle löschen" Button anzeigen wenn Filter aktiv sind', () => {
      const filtersWithValues: BezirkeFilter = {
        ...mockFilters,
        search: 'Testsuche',
        aktiv: false,
      }

      render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      expect(screen.getByText(/Alle löschen \(\d+\)/)).toBeInTheDocument()
    })
  })

  describe('Rendering - Kompakte Ansicht', () => {
    it('sollte die kompakte Ansicht korrekt rendern', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          compact={true}
        />
      )

      // Suchfeld sollte sichtbar sein
      expect(screen.getByPlaceholderText('Bezirke durchsuchen...')).toBeInTheDocument()

      // Filter-Button sollte sichtbar sein
      expect(screen.getByRole('button', { name: /filter/i })).toBeInTheDocument()

      // Vollständige Filter sollten nicht direkt sichtbar sein
      expect(screen.queryByText('Filter & Suche')).not.toBeInTheDocument()
    })

    it('sollte die Anzahl aktiver Filter im kompakten Modus anzeigen', () => {
      const filtersWithValues: BezirkeFilter = {
        search: 'Test',
        aktiv: false,
        sortBy: 'erstelltAm',
      }

      render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
          compact={true}
        />
      )

      // Badge mit Anzahl aktiver Filter
      expect(screen.getByText('3')).toBeInTheDocument() // search, aktiv, sortBy
    })

    it('sollte das Filter-Popover öffnen und schließen', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          compact={true}
        />
      )

      const filterButton = screen.getByRole('button', { name: /filter/i })
      await user.click(filterButton)

      // Popover-Inhalt sollte sichtbar sein
      expect(screen.getByText('Status auswählen')).toBeInTheDocument()
      expect(screen.getByText('Sortierung')).toBeInTheDocument()
    })
  })

  describe('Suchfunktionalität', () => {
    it('sollte Suchbegriff korrekt verarbeiten', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const searchInput = screen.getByLabelText('Suche')
      await user.type(searchInput, 'Mitte-Nord')

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          search: 'Mitte-Nord',
        })
      })
    })

    it('sollte leere Suche korrekt behandeln', async () => {
      const filtersWithSearch: BezirkeFilter = {
        ...mockFilters,
        search: 'existierender Begriff',
      }

      const { user } = render(
        <BezirkeFilters
          filters={filtersWithSearch}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const searchInput = screen.getByLabelText('Suche')
      await user.clear(searchInput)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...filtersWithSearch,
          search: undefined,
        })
      })
    })
  })

  describe('Status-Filter', () => {
    it('sollte Status-Filter korrekt umschalten', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      // Status-Select öffnen
      const statusSelect = screen.getByDisplayValue('Nur Aktive')
      await user.click(statusSelect)

      // "Nur Inaktive" auswählen
      const inactiveOption = screen.getByText('Nur Inaktive')
      await user.click(inactiveOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          aktiv: false,
        })
      })
    })

    it('sollte "Alle" Status korrekt setzen', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={{ ...mockFilters, aktiv: false }}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const statusSelect = screen.getByDisplayValue('Nur Inaktive')
      await user.click(statusSelect)

      const allOption = screen.getByText('Alle Bezirke')
      await user.click(allOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          aktiv: undefined,
        })
      })
    })
  })

  describe('Sortierung', () => {
    it('sollte Sortierungsoptionen korrekt ändern', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      // Sortieren nach "Erstellungsdatum"
      const sortBySelect = screen.getByDisplayValue('Name')
      await user.click(sortBySelect)

      const dateOption = screen.getByText('Erstellungsdatum')
      await user.click(dateOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          sortBy: 'erstelltAm',
        })
      })
    })

    it('sollte Sortierreihenfolge ändern', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const sortOrderSelect = screen.getByDisplayValue('Aufsteigend')
      await user.click(sortOrderSelect)

      const descOption = screen.getByText('Absteigend')
      await user.click(descOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          sortOrder: 'desc',
        })
      })
    })
  })

  describe('Ort-Filter', () => {
    it('sollte Ort-Filter korrekt anwenden', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableOrte={mockAvailableOrte}
        />
      )

      const ortSelect = screen.getByText('Ort auswählen')
      await user.click(ortSelect)

      const berlinOption = screen.getByText('Berlin')
      await user.click(berlinOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          search: 'Berlin',
        })
      })
    })
  })

  describe('Bezirksleiter-Filter', () => {
    it('sollte Bezirksleiter-Filter korrekt anwenden', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableBezirksleiter={mockAvailableBezirksleiter}
        />
      )

      const leiterSelect = screen.getByText('Bezirksleiter auswählen')
      await user.click(leiterSelect)

      const muellerOption = screen.getByText('Herr Müller')
      await user.click(muellerOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          search: 'Herr Müller',
        })
      })
    })
  })

  describe('Pagination', () => {
    it('sollte Seitenzahl korrekt ändern', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const pageInput = screen.getByLabelText('Seite')
      await user.clear(pageInput)
      await user.type(pageInput, '3')

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          page: 3,
        })
      })
    })

    it('sollte Anzahl pro Seite ändern', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const limitSelect = screen.getByDisplayValue('20')
      await user.click(limitSelect)

      const fiftyOption = screen.getByText('50')
      await user.click(fiftyOption)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({
          ...mockFilters,
          limit: 50,
        })
      })
    })
  })

  describe('Aktive Filter-Badges', () => {
    it('sollte aktive Filter als Badges anzeigen', () => {
      const filtersWithValues: BezirkeFilter = {
        search: 'Testsuche',
        aktiv: false,
        sortBy: 'erstelltAm',
        sortOrder: 'desc',
      }

      render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      expect(screen.getByText('Aktive Filter')).toBeInTheDocument()
      expect(screen.getByText('Suche: Testsuche')).toBeInTheDocument()
      expect(screen.getByText('Status: Inaktiv')).toBeInTheDocument()
      expect(screen.getByText(/Sortierung.*erstelltAm.*absteigend/)).toBeInTheDocument()
    })

    it('sollte Filter-Badges korrekt entfernen', async () => {
      const filtersWithValues: BezirkeFilter = {
        search: 'Testsuche',
        aktiv: false,
      }

      const { user } = render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      // X-Button beim Such-Badge finden und klicken
      const searchBadge = screen.getByText('Suche: Testsuche').closest('[class*="gap-1"]')
      const removeButton = searchBadge?.querySelector('button')

      if (removeButton) {
        await user.click(removeButton)

        await waitFor(() => {
          expect(mockOnFiltersChange).toHaveBeenCalledWith({
            aktiv: false,
          })
        })
      }
    })
  })

  describe('Alle Filter löschen', () => {
    it('sollte alle Filter zurücksetzen', async () => {
      const filtersWithValues: BezirkeFilter = {
        search: 'Test',
        aktiv: false,
        sortBy: 'erstelltAm',
        sortOrder: 'desc',
        page: 2,
        limit: 50,
      }

      const { user } = render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const clearAllButton = screen.getByText(/Alle löschen \(\d+\)/)
      await user.click(clearAllButton)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({})
      })
    })

    it('sollte "Alle löschen" Button im kompakten Modus funktionieren', async () => {
      const filtersWithValues: BezirkeFilter = {
        search: 'Test',
        aktiv: false,
      }

      const { user } = render(
        <BezirkeFilters
          filters={filtersWithValues}
          onFiltersChange={mockOnFiltersChange}
          compact={true}
        />
      )

      // Filter-Popover öffnen
      const filterButton = screen.getByRole('button', { name: /filter/i })
      await user.click(filterButton)

      const clearAllButton = screen.getByText('Alle löschen')
      await user.click(clearAllButton)

      await waitFor(() => {
        expect(mockOnFiltersChange).toHaveBeenCalledWith({})
      })
    })
  })

  describe('Filter-Synchronisation', () => {
    it('sollte externe Filter-Änderungen korrekt übernehmen', () => {
      const initialFilters: BezirkeFilter = { search: '', aktiv: true }
      
      const { rerender } = render(
        <BezirkeFilters
          filters={initialFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const updatedFilters: BezirkeFilter = { search: 'Neuer Text', aktiv: false }
      
      rerender(
        <BezirkeFilters
          filters={updatedFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      // Die neuen Werte sollten in den Inputs angezeigt werden
      expect(screen.getByDisplayValue('Neuer Text')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Nur Inaktive')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte ARIA-Labels haben', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableOrte={mockAvailableOrte}
          availableBezirksleiter={mockAvailableBezirksleiter}
        />
      )

      // Alle wichtigen Felder sollten Labels haben
      expect(screen.getByLabelText('Suche')).toBeInTheDocument()
      expect(screen.getByLabelText('Status')).toBeInTheDocument()
      expect(screen.getByLabelText('Ort')).toBeInTheDocument()
      expect(screen.getByLabelText('Bezirksleiter')).toBeInTheDocument()
      expect(screen.getByLabelText('Sortierung')).toBeInTheDocument()
    })

    it('sollte mit Tastatur navigierbar sein', async () => {
      const { user } = render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      const searchInput = screen.getByLabelText('Suche')
      
      // Tab-Navigation
      await user.tab()
      expect(searchInput).toHaveFocus()

      // Weitere Tab-Navigation sollte zu anderen Elementen führen
      await user.tab()
      expect(document.activeElement).not.toBe(searchInput)
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle deutschen UI-Texte anzeigen', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableOrte={mockAvailableOrte}
          availableBezirksleiter={mockAvailableBezirksleiter}
        />
      )

      // Deutsche Labels und Platzhalter
      expect(screen.getByText('Filter & Suche')).toBeInTheDocument()
      expect(screen.getByText('Suche')).toBeInTheDocument()
      expect(screen.getByText('Status')).toBeInTheDocument()
      expect(screen.getByText('Ort')).toBeInTheDocument()
      expect(screen.getByText('Bezirksleiter')).toBeInTheDocument()
      expect(screen.getByText('Sortierung')).toBeInTheDocument()
      expect(screen.getByText('Sortieren nach')).toBeInTheDocument()
      expect(screen.getByText('Reihenfolge')).toBeInTheDocument()
      expect(screen.getByText('Aufsteigend')).toBeInTheDocument()
      expect(screen.getByText('Absteigend')).toBeInTheDocument()
    })

    it('sollte deutsche Platzhalter verwenden', () => {
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )

      expect(screen.getByPlaceholderText('Name, Beschreibung oder Bezirksleiter...')).toBeInTheDocument()
    })
  })

  describe('Performance', () => {
    it('sollte schnell rendern', () => {
      const startTime = performance.now()
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
        />
      )
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(100)
    })

    it('sollte bei vielen Optionen performant bleiben', () => {
      const manyOrte = Array.from({ length: 100 }, (_, i) => `Stadt ${i}`)
      const manyBezirksleiter = Array.from({ length: 50 }, (_, i) => `Person ${i}`)

      const startTime = performance.now()
      render(
        <BezirkeFilters
          filters={mockFilters}
          onFiltersChange={mockOnFiltersChange}
          availableOrte={manyOrte}
          availableBezirksleiter={manyBezirksleiter}
        />
      )
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(200)
    })
  })

  describe('Error Handling', () => {
    it('sollte mit fehlenden Callback-Funktionen umgehen', () => {
      expect(() => {
        render(
          <BezirkeFilters
            filters={mockFilters}
            onFiltersChange={() => {}} // Leere Funktion
          />
        )
      }).not.toThrow()
    })

    it('sollte mit ungültigen Filter-Werten umgehen', () => {
      const invalidFilters = {
        page: -1,
        limit: 0,
        sortBy: 'invalid' as any,
        sortOrder: 'invalid' as any,
      }

      expect(() => {
        render(
          <BezirkeFilters
            filters={invalidFilters}
            onFiltersChange={mockOnFiltersChange}
          />
        )
      }).not.toThrow()
    })
  })
})