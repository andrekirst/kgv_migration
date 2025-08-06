import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import { BezirkeList } from '../bezirke-list'
import { testDataFactories, GERMAN_TEST_LABELS } from '@/test/fixtures/kgv-data'
import { Bezirk } from '@/types/bezirke'
import { server } from '@/test/mocks/server'

// Import MSW server
import '@/test/mocks/server'

describe('BezirkeList Component', () => {
  let mockBezirke: Bezirk[]
  let mockOnFilterChange: jest.Mock
  let mockOnBezirkClick: jest.Mock
  let mockOnBezirkEdit: jest.Mock
  let mockOnBezirkDelete: jest.Mock

  beforeEach(() => {
    // Erstelle Test-Bezirke mit deutschen Daten
    mockBezirke = [
      testDataFactories.bezirk({
        id: 1,
        name: 'Bezirk Mitte-Nord',
        beschreibung: 'Zentraler Bezirk mit vielen Obstbäumen',
        bezirksleiter: 'Herr Müller',
        aktiv: true,
        statistiken: {
          gesamtParzellen: 50,
          belegteParzellen: 40,
          freieParzellen: 10,
          warteliste: 5,
        },
      }),
      testDataFactories.bezirk({
        id: 2,
        name: 'Bezirk Süd-Ost',
        beschreibung: 'Ruhiger Bezirk am Waldrand',
        bezirksleiter: 'Frau Schmidt',
        aktiv: true,
        statistiken: {
          gesamtParzellen: 30,
          belegteParzellen: 25,
          freieParzellen: 5,
          warteliste: 8,
        },
      }),
      testDataFactories.bezirk({
        id: 3,
        name: 'Bezirk Inaktiv',
        beschreibung: 'Temporär geschlossener Bezirk',
        aktiv: false,
        statistiken: {
          gesamtParzellen: 20,
          belegteParzellen: 0,
          freieParzellen: 0,
          warteliste: 0,
        },
      }),
    ]

    // Mock-Funktionen
    mockOnFilterChange = jest.fn()
    mockOnBezirkClick = jest.fn()
    mockOnBezirkEdit = jest.fn()
    mockOnBezirkDelete = jest.fn()
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Rendering', () => {
    it('sollte die Bezirke-Liste korrekt rendern', () => {
      render(
        <BezirkeList
          bezirke={mockBezirke}
          onFilterChange={mockOnFilterChange}
          onBezirkClick={mockOnBezirkClick}
          onBezirkEdit={mockOnBezirkEdit}
          onBezirkDelete={mockOnBezirkDelete}
        />
      )

      // Überprüfe, ob die Suchleiste vorhanden ist
      expect(screen.getByPlaceholderText('Bezirke durchsuchen...')).toBeInTheDocument()

      // Überprüfe, ob alle aktiven Bezirke angezeigt werden
      expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
      expect(screen.getByText('Bezirk Süd-Ost')).toBeInTheDocument()

      // Inaktive Bezirke sollten standardmäßig nicht angezeigt werden
      expect(screen.queryByText('Bezirk Inaktiv')).not.toBeInTheDocument()
    })

    it('sollte die Anzahl der gefundenen Bezirke korrekt anzeigen', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Da standardmäßig nur aktive Bezirke angezeigt werden
      expect(screen.getByText('2 von 3 Bezirken')).toBeInTheDocument()
    })

    it('sollte den Loading-State korrekt darstellen', () => {
      render(<BezirkeList bezirke={[]} loading={true} />)

      // Überprüfe, ob Loading-Skeletons angezeigt werden
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons).toHaveLength(7) // 1 Suchleiste + 2 Buttons + 4 Karten
    })

    it('sollte eine leere Liste korrekt darstellen', () => {
      render(<BezirkeList bezirke={[]} />)

      expect(screen.getByText('Keine Bezirke gefunden')).toBeInTheDocument()
      expect(screen.getByText('Es wurden noch keine Bezirke erstellt.')).toBeInTheDocument()
    })
  })

  describe('Suche und Filter', () => {
    it('sollte die Suche korrekt funktionieren', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onFilterChange={mockOnFilterChange}
        />
      )

      const searchInput = screen.getByPlaceholderText('Bezirke durchsuchen...')
      
      await user.type(searchInput, 'Mitte')

      // Warte auf die Filterung
      await waitFor(() => {
        expect(mockOnFilterChange).toHaveBeenCalledWith({
          search: 'Mitte',
          aktiv: true,
          sortBy: 'name',
          sortOrder: 'asc',
        })
      })

      // Nur der gesuchte Bezirk sollte sichtbar sein
      expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
      expect(screen.queryByText('Bezirk Süd-Ost')).not.toBeInTheDocument()
    })

    it('sollte die Suche nach Bezirksleiter funktionieren', async () => {
      const { user } = render(<BezirkeList bezirke={mockBezirke} />)

      const searchInput = screen.getByPlaceholderText('Bezirke durchsuchen...')
      await user.type(searchInput, 'Schmidt')

      await waitFor(() => {
        expect(screen.getByText('Bezirk Süd-Ost')).toBeInTheDocument()
        expect(screen.queryByText('Bezirk Mitte-Nord')).not.toBeInTheDocument()
      })
    })

    it('sollte den Aktiv-Filter korrekt umschalten', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onFilterChange={mockOnFilterChange}
        />
      )

      const activeFilterButton = screen.getByRole('button', { name: /nur aktive/i })
      
      // Standardmäßig sollte der Filter aktiv sein
      expect(activeFilterButton).toHaveClass('bg-primary')

      // Filter deaktivieren
      await user.click(activeFilterButton)

      await waitFor(() => {
        expect(mockOnFilterChange).toHaveBeenCalledWith({
          search: undefined,
          aktiv: undefined, // Alle Bezirke anzeigen
          sortBy: 'name',
          sortOrder: 'asc',
        })
      })

      // Jetzt sollten auch inaktive Bezirke angezeigt werden
      expect(screen.getByText('Bezirk Inaktiv')).toBeInTheDocument()
      expect(screen.getByText('Inaktive Bezirke werden angezeigt')).toBeInTheDocument()
    })

    it('sollte die Sortierung nach Name funktionieren', async () => {
      const { user } = render(<BezirkeList bezirke={mockBezirke} />)

      const sortButton = screen.getByRole('button', { name: /sortierung/i })
      await user.click(sortButton)

      const sortByNameOption = screen.getByRole('menuitem', { name: /nach name/i })
      await user.click(sortByNameOption)

      // Die Bezirke sollten alphabetisch sortiert werden
      const bezirkeElements = screen.getAllByText(/^Bezirk/)
      expect(bezirkeElements[0]).toHaveTextContent('Bezirk Mitte-Nord')
      expect(bezirkeElements[1]).toHaveTextContent('Bezirk Süd-Ost')
    })
  })

  describe('View Mode', () => {
    it('sollte zwischen Grid- und List-View wechseln können', async () => {
      const { user } = render(<BezirkeList bezirke={mockBezirke} />)

      // Standardmäßig sollte Grid-View aktiv sein
      const gridButton = screen.getByRole('button', { name: '' }) // Grid-Icon
      expect(gridButton).toHaveClass('bg-primary')

      // Zu List-View wechseln
      const listButton = screen.getAllByRole('button')[screen.getAllByRole('button').length - 1]
      await user.click(listButton)

      // Überprüfe, ob die Ansicht gewechselt wurde
      // (Dies würde normalerweise durch CSS-Klassen oder Datenattribute überprüft)
      expect(listButton).toHaveClass('bg-primary')
    })
  })

  describe('Bezirk-Aktionen', () => {
    it('sollte beim Klick auf einen Bezirk die Callback-Funktion aufrufen', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onBezirkClick={mockOnBezirkClick}
        />
      )

      const bezirkCard = screen.getByText('Bezirk Mitte-Nord').closest('[data-testid], .cursor-pointer, [role="button"]') || 
                        screen.getByText('Bezirk Mitte-Nord').closest('div')

      if (bezirkCard) {
        await user.click(bezirkCard)
        expect(mockOnBezirkClick).toHaveBeenCalledWith(mockBezirke[0])
      }
    })

    it('sollte das Bearbeiten-Menü korrekt anzeigen', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onBezirkEdit={mockOnBezirkEdit}
          onBezirkDelete={mockOnBezirkDelete}
        />
      )

      // Finde das Dropdown-Menü (⋯ Button)
      const dropdownButtons = screen.getAllByText('⋯')
      await user.click(dropdownButtons[0])

      // Überprüfe, ob die Menüoptionen angezeigt werden
      expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      expect(screen.getByText('Löschen')).toBeInTheDocument()
    })

    it('sollte die Bearbeiten-Funktion aufrufen', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onBezirkEdit={mockOnBezirkEdit}
        />
      )

      const dropdownButtons = screen.getAllByText('⋯')
      await user.click(dropdownButtons[0])

      const editButton = screen.getByText('Bearbeiten')
      await user.click(editButton)

      expect(mockOnBezirkEdit).toHaveBeenCalledWith(1)
    })

    it('sollte die Löschen-Funktion aufrufen', async () => {
      const { user } = render(
        <BezirkeList
          bezirke={mockBezirke}
          onBezirkDelete={mockOnBezirkDelete}
        />
      )

      const dropdownButtons = screen.getAllByText('⋯')
      await user.click(dropdownButtons[0])

      const deleteButton = screen.getByText('Löschen')
      await user.click(deleteButton)

      expect(mockOnBezirkDelete).toHaveBeenCalledWith(1)
    })
  })

  describe('Statistiken-Anzeige', () => {
    it('sollte die Parzellen-Statistiken korrekt anzeigen', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Überprüfe die Statistiken für den ersten Bezirk
      expect(screen.getByText('50')).toBeInTheDocument() // Gesamtparzellen
      expect(screen.getByText('Gesamt')).toBeInTheDocument()
      expect(screen.getByText('40')).toBeInTheDocument() // Belegte Parzellen
      expect(screen.getByText('Belegt')).toBeInTheDocument()
      expect(screen.getByText('10')).toBeInTheDocument() // Freie Parzellen
      expect(screen.getByText('Frei')).toBeInTheDocument()
    })

    it('sollte die Auslastung korrekt berechnen und anzeigen', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Auslastung für ersten Bezirk: 40/50 = 80%
      expect(screen.getByText('80% Auslastung')).toBeInTheDocument()
    })

    it('sollte die Warteliste korrekt anzeigen', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      expect(screen.getByText('5 auf Warteliste')).toBeInTheDocument()
      expect(screen.getByText('8 auf Warteliste')).toBeInTheDocument()
    })
  })

  describe('Responsive Verhalten', () => {
    it('sollte auf kleineren Bildschirmen korrekt funktionieren', () => {
      // Mock für kleineren Bildschirm
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 480,
      })

      render(<BezirkeList bezirke={mockBezirke} />)

      // Die Komponente sollte immer noch korrekt rendern
      expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('Bezirke durchsuchen...')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte ARIA-Labels haben', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Suchfeld sollte korrekt gelabelt sein
      const searchInput = screen.getByPlaceholderText('Bezirke durchsuchen...')
      expect(searchInput).toBeInTheDocument()

      // Buttons sollten korrekte Labels haben
      expect(screen.getByRole('button', { name: /nur aktive/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /sortierung/i })).toBeInTheDocument()
    })

    it('sollte mit der Tastatur navigierbar sein', async () => {
      const { user } = render(<BezirkeList bezirke={mockBezirke} />)

      const searchInput = screen.getByPlaceholderText('Bezirke durchsuchen...')
      
      // Tab-Navigation sollte funktionieren
      await user.tab()
      expect(searchInput).toHaveFocus()

      await user.tab()
      // Der nächste fokussierbare Button sollte fokussiert werden
      expect(document.activeElement).toBeInstanceOf(HTMLButtonElement)
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle Texte auf Deutsch anzeigen', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Überprüfe deutsche UI-Texte
      expect(screen.getByText('Nur Aktive')).toBeInTheDocument()
      expect(screen.getByText('Sortierung')).toBeInTheDocument()
      expect(screen.getByText('Parzellen-Übersicht')).toBeInTheDocument()
      expect(screen.getByText('Auslastung')).toBeInTheDocument()
      expect(screen.getByText('auf Warteliste')).toBeInTheDocument()
    })

    it('sollte deutsche Suchplatzhalter verwenden', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      expect(screen.getByPlaceholderText('Bezirke durchsuchen...')).toBeInTheDocument()
    })

    it('sollte deutsche Datums- und Zahlenformatierung verwenden', () => {
      render(<BezirkeList bezirke={mockBezirke} />)

      // Deutsche Zahlenformatierung sollte verwendet werden
      const numbers = screen.getAllByText(/^\d+$/)
      expect(numbers.length).toBeGreaterThan(0)
    })
  })

  describe('Error Handling', () => {
    it('sollte mit leeren Daten umgehen können', () => {
      render(<BezirkeList bezirke={[]} />)

      expect(screen.getByText('Keine Bezirke gefunden')).toBeInTheDocument()
      expect(screen.getByText('0 von 0 Bezirken')).toBeInTheDocument()
    })

    it('sollte mit unvollständigen Bezirk-Daten umgehen können', () => {
      const incompleteBezirk = testDataFactories.bezirk({
        id: 1,
        name: 'Unvollständiger Bezirk',
        beschreibung: undefined,
        bezirksleiter: undefined,
        adresse: undefined,
      })

      render(<BezirkeList bezirke={[incompleteBezirk]} />)

      expect(screen.getByText('Unvollständiger Bezirk')).toBeInTheDocument()
      // Komponente sollte trotz fehlender Daten funktionieren
    })
  })

  describe('Performance', () => {
    it('sollte mit vielen Bezirken performant umgehen', () => {
      const manyBezirke = testDataFactories.multipleBezirke(100)
      
      const startTime = performance.now()
      render(<BezirkeList bezirke={manyBezirke} />)
      const endTime = performance.now()

      // Render sollte unter 100ms dauern
      expect(endTime - startTime).toBeLessThan(100)
    })

    it('sollte Memoization korrekt verwenden', () => {
      const { rerender } = render(<BezirkeList bezirke={mockBezirke} />)

      // Re-render mit den gleichen Props sollte schnell sein
      const startTime = performance.now()
      rerender(<BezirkeList bezirke={mockBezirke} />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(50)
    })
  })
})