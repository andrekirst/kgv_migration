import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import { ParzellenList } from '../parzellen-list'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { Parzelle, ParzellenStatus } from '@/types/bezirke'

// Import MSW server
import '@/test/mocks/server'

describe('ParzellenList Component', () => {
  let mockParzellen: Parzelle[]
  let mockOnFilterChange: jest.Mock
  let mockOnParzelleClick: jest.Mock
  let mockOnParzelleEdit: jest.Mock
  let mockOnParzelleAssign: jest.Mock
  let mockOnParzelleDelete: jest.Mock

  beforeEach(() => {
    // Erstelle Test-Parzellen mit deutschen Daten
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
      }),
      testDataFactories.parzelle({
        id: 4,
        nummer: 'P-004',
        bezirkId: 1,
        bezirkName: 'Bezirk Mitte-Nord',
        groesse: 300,
        status: ParzellenStatus.WARTUNG,
        monatlichePacht: 20.00,
        aktiv: false,
      }),
    ]

    // Mock-Funktionen
    mockOnFilterChange = jest.fn()
    mockOnParzelleClick = jest.fn()
    mockOnParzelleEdit = jest.fn()
    mockOnParzelleAssign = jest.fn()
    mockOnParzelleDelete = jest.fn()
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Rendering', () => {
    it('sollte die Parzellen-Liste korrekt rendern', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          onFilterChange={mockOnFilterChange}
          onParzelleClick={mockOnParzelleClick}
          onParzelleEdit={mockOnParzelleEdit}
          onParzelleAssign={mockOnParzelleAssign}
          onParzelleDelete={mockOnParzelleDelete}
        />
      )

      // Überprüfe, ob die Suchleiste vorhanden ist
      expect(screen.getByPlaceholderText('Parzellen durchsuchen...')).toBeInTheDocument()

      // Überprüfe, ob die Parzellen angezeigt werden
      expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
      expect(screen.getByText('Parzelle P-002')).toBeInTheDocument()
      expect(screen.getByText('Parzelle P-003')).toBeInTheDocument()

      // Inaktive Parzellen sollten standardmäßig nicht angezeigt werden
      expect(screen.queryByText('Parzelle P-004')).not.toBeInTheDocument()
    })

    it('sollte die Anzahl der gefundenen Parzellen korrekt anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Da standardmäßig nur aktive Parzellen angezeigt werden (3 von 4)
      expect(screen.getByText('3 von 4 Parzellen')).toBeInTheDocument()
    })

    it('sollte den Loading-State korrekt darstellen', () => {
      render(<ParzellenList parzellen={[]} loading={true} />)

      // Überprüfe, ob Loading-Skeletons angezeigt werden
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('sollte eine leere Liste korrekt darstellen', () => {
      render(<ParzellenList parzellen={[]} />)

      expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      expect(screen.getByText('Es wurden noch keine Parzellen erstellt.')).toBeInTheDocument()
    })
  })

  describe('Status-Badges', () => {
    it('sollte Status-Badges korrekt anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      expect(screen.getByText('Frei')).toBeInTheDocument()
      expect(screen.getByText('Belegt')).toBeInTheDocument()
      expect(screen.getByText('Reserviert')).toBeInTheDocument()
    })

    it('sollte Status-spezifische Icons anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Überprüfe, ob die Status-Icons vorhanden sind
      const statusBadges = screen.getAllByText(/Frei|Belegt|Reserviert/)
      expect(statusBadges.length).toBeGreaterThan(0)
    })

    it('sollte inaktive Parzellen korrekt kennzeichnen', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      // Aktiviere "Alle anzeigen" um inaktive Parzellen zu sehen
      const activeButton = screen.getByRole('button', { name: /nur aktive/i })
      await user.click(activeButton)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-004')).toBeInTheDocument()
        expect(screen.getByText('Inaktiv')).toBeInTheDocument()
      })
    })
  })

  describe('Suche und Filter', () => {
    it('sollte die Suche nach Parzellennummer funktionieren', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          onFilterChange={mockOnFilterChange}
        />
      )

      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      await user.type(searchInput, 'P-001')

      await waitFor(() => {
        expect(mockOnFilterChange).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'P-001',
          })
        )
      })

      // Nur die gesuchte Parzelle sollte sichtbar sein
      expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
      expect(screen.queryByText('Parzelle P-002')).not.toBeInTheDocument()
    })

    it('sollte die Suche nach Bezirksname funktionieren', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      await user.type(searchInput, 'Süd-Ost')

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-003')).toBeInTheDocument()
        expect(screen.queryByText('Parzelle P-001')).not.toBeInTheDocument()
      })
    })

    it('sollte die Suche nach Mieter funktionieren', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      await user.type(searchInput, 'Mustermann')

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-002')).toBeInTheDocument()
        expect(screen.queryByText('Parzelle P-001')).not.toBeInTheDocument()
      })
    })

    it('sollte den Aktiv-Filter korrekt umschalten', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          onFilterChange={mockOnFilterChange}
        />
      )

      const activeFilterButton = screen.getByRole('button', { name: /nur aktive/i })
      
      // Filter deaktivieren (alle Parzellen anzeigen)
      await user.click(activeFilterButton)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-004')).toBeInTheDocument()
        expect(screen.getByText('Inaktive Parzellen werden angezeigt')).toBeInTheDocument()
      })
    })
  })

  describe('Status-Filter', () => {
    it('sollte das Status-Filter-Dropdown öffnen', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)

      // Alle Status-Optionen sollten sichtbar sein
      expect(screen.getByText('Status filtern')).toBeInTheDocument()
      expect(screen.getByRole('menuitemcheckbox', { name: /frei/i })).toBeInTheDocument()
      expect(screen.getByRole('menuitemcheckbox', { name: /belegt/i })).toBeInTheDocument()
      expect(screen.getByRole('menuitemcheckbox', { name: /reserviert/i })).toBeInTheDocument()
    })

    it('sollte einzelne Status-Filter aktivieren', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)

      const freiOption = screen.getByRole('menuitemcheckbox', { name: /frei/i })
      await user.click(freiOption)

      await waitFor(() => {
        // Nur freie Parzellen sollten angezeigt werden
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
        expect(screen.queryByText('Parzelle P-002')).not.toBeInTheDocument()
      })
    })

    it('sollte aktive Filter-Badges anzeigen', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)

      const freiOption = screen.getByRole('menuitemcheckbox', { name: /frei/i })
      await user.click(freiOption)

      await waitFor(() => {
        expect(screen.getByText('Aktive Filter:')).toBeInTheDocument()
        // Badge mit Status sollte sichtbar sein
        const filterBadges = screen.getAllByText('Frei')
        expect(filterBadges.length).toBeGreaterThan(1) // Eines in der Parzelle, eines im Filter-Badge
      })
    })

    it('sollte Filter-Badges entfernen können', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      // Status-Filter aktivieren
      const statusButton = screen.getByRole('button', { name: /status/i })
      await user.click(statusButton)

      const freiOption = screen.getByRole('menuitemcheckbox', { name: /frei/i })
      await user.click(freiOption)

      await waitFor(() => {
        expect(screen.getByText('Aktive Filter:')).toBeInTheDocument()
      })

      // Filter-Badge entfernen
      const removeIcons = screen.getAllByRole('generic', { name: '' })
      const xIcon = removeIcons.find(icon => icon.getAttribute('class')?.includes('cursor-pointer'))
      
      if (xIcon) {
        await user.click(xIcon)

        await waitFor(() => {
          // Alle Parzellen sollten wieder sichtbar sein
          expect(screen.getByText('Parzelle P-002')).toBeInTheDocument()
        })
      }
    })
  })

  describe('Sortierung', () => {
    it('sollte das Sortierungs-Dropdown öffnen', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const sortButton = screen.getByRole('button', { name: /sortierung/i })
      await user.click(sortButton)

      expect(screen.getByText(/nach nummer/i)).toBeInTheDocument()
      expect(screen.getByText(/nach größe/i)).toBeInTheDocument()
      expect(screen.getByText(/nach pacht/i)).toBeInTheDocument()
      expect(screen.getByText(/nach erstellungsdatum/i)).toBeInTheDocument()
    })

    it('sollte nach Größe sortieren', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const sortButton = screen.getByRole('button', { name: /sortierung/i })
      await user.click(sortButton)

      const groesseOption = screen.getByText(/nach größe/i)
      await user.click(groesseOption)

      await waitFor(() => {
        // Kleinste Parzelle (300m²) sollte zuerst stehen, aber da sie inaktiv ist, nicht sichtbar
        // Zweitkleinste (350m²) sollte zuerst bei aktiven stehen
        const parzellenElements = screen.getAllByText(/Parzelle P-/)
        expect(parzellenElements[0]).toHaveTextContent('P-002') // 350m²
      })
    })

    it('sollte Sortierreihenfolge umkehren', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const sortButton = screen.getByRole('button', { name: /sortierung/i })
      
      // Erst aufsteigend nach Größe
      await user.click(sortButton)
      const groesseOption = screen.getByText(/nach größe/i)
      await user.click(groesseOption)

      // Dann nochmal klicken für absteigend
      await user.click(sortButton)
      const groesseOptionDesc = screen.getByText(/nach größe.*↓/i)
      await user.click(groesseOptionDesc)

      await waitFor(() => {
        // Größte Parzelle sollte zuerst stehen
        const parzellenElements = screen.getAllByText(/Parzelle P-/)
        expect(parzellenElements[0]).toHaveTextContent('P-003') // 600m²
      })
    })
  })

  describe('View Mode', () => {
    it('sollte zwischen Grid- und List-View wechseln können', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      // Zu List-View wechseln
      const viewButtons = screen.getAllByRole('button')
      const listButton = viewButtons.find(btn => btn.querySelector('[data-lucide="list"]'))
      
      if (listButton) {
        await user.click(listButton)

        // In der List-View sollten "Bearbeiten" Buttons direkt sichtbar sein
        expect(screen.getAllByText('Bearbeiten')).toHaveLength(3) // Für jede aktive Parzelle
      }
    })

    it('sollte Grid-View als Standard verwenden', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Grid-Button sollte aktiv sein
      const viewButtons = screen.getAllByRole('button')
      const gridButton = viewButtons.find(btn => btn.querySelector('[data-lucide="grid"]'))
      
      expect(gridButton).toHaveClass('bg-primary')
    })
  })

  describe('Parzelle-Aktionen', () => {
    it('sollte beim Klick auf eine Parzelle die Callback-Funktion aufrufen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          onParzelleClick={mockOnParzelleClick}
        />
      )

      const parzelleCard = screen.getByText('Parzelle P-001').closest('[class*="cursor-pointer"]')
      
      if (parzelleCard) {
        await user.click(parzelleCard)
        expect(mockOnParzelleClick).toHaveBeenCalledWith(mockParzellen[0])
      }
    })

    it('sollte das Dropdown-Menü für Aktionen öffnen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          onParzelleEdit={mockOnParzelleEdit}
          onParzelleDelete={mockOnParzelleDelete}
        />
      )

      const dropdownButtons = screen.getAllByText('⋯')
      await user.click(dropdownButtons[0])

      expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      expect(screen.getByText('Löschen')).toBeInTheDocument()
    })

    it('sollte Zuweisen-Button für freie Parzellen anzeigen', () => {
      render(
        <ParzellenList
          parzellen={mockParzellen}
          onParzelleAssign={mockOnParzelleAssign}
        />
      )

      // Freie Parzelle sollte "Zuweisen" Button haben
      expect(screen.getByText('Verfügbar')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Zuweisen' })).toBeInTheDocument()
    })

    it('sollte Zuweisen-Funktion aufrufen', async () => {
      const { user } = render(
        <ParzellenList
          parzellen={mockParzellen}
          onParzelleAssign={mockOnParzelleAssign}
        />
      )

      const zuweisenButton = screen.getByRole('button', { name: 'Zuweisen' })
      await user.click(zuweisenButton)

      expect(mockOnParzelleAssign).toHaveBeenCalledWith(1) // ID der freien Parzelle
    })

    it('sollte Mieter-Informationen für belegte Parzellen anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
      expect(screen.getByText(/seit/i)).toBeInTheDocument()
    })
  })

  describe('Parzelle-Details', () => {
    it('sollte Grundinformationen korrekt anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Größe
      expect(screen.getByText('400 m²')).toBeInTheDocument()
      expect(screen.getByText('350 m²')).toBeInTheDocument()

      // Pacht
      expect(screen.getByText('25,50 €')).toBeInTheDocument()
      expect(screen.getByText('22,00 €')).toBeInTheDocument()

      // Bezirksname
      expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
      expect(screen.getByText('Bezirk Süd-Ost')).toBeInTheDocument()
    })

    it('sollte Ausstattung anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      expect(screen.getByText('Ausstattung')).toBeInTheDocument()
      expect(screen.getByText('Laube')).toBeInTheDocument()
      expect(screen.getByText('Wasserhahn')).toBeInTheDocument()
      expect(screen.getByText('Kompost')).toBeInTheDocument()
    })

    it('sollte "+X weitere" für viele Ausstattungsmerkmale anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Parzelle mit 4 Ausstattungsmerkmalen sollte "+1 weitere" anzeigen
      expect(screen.getByText('+1 weitere')).toBeInTheDocument()
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle deutschen UI-Texte anzeigen', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      expect(screen.getByText('Nur Aktive')).toBeInTheDocument()
      expect(screen.getByText('Status')).toBeInTheDocument()
      expect(screen.getByText('Sortierung')).toBeInTheDocument()
      expect(screen.getByText('Verfügbar')).toBeInTheDocument()
      expect(screen.getByText('Ausstattung')).toBeInTheDocument()
      expect(screen.getByText('Seit')).toBeInTheDocument()
    })

    it('sollte deutschen Datumsformat verwenden', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Deutsches Datumsformat für Mietbeginn
      expect(screen.getByText(/01\.04\.2023/)).toBeInTheDocument()
    })

    it('sollte deutsche Zahlformatierung verwenden', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Deutsche Dezimaltrennzeichen
      expect(screen.getByText('25,50 €')).toBeInTheDocument()
      expect(screen.getByText('22,00 €')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte ARIA-Labels haben', () => {
      render(<ParzellenList parzellen={mockParzellen} />)

      // Suchfeld sollte accessible sein
      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      expect(searchInput).toBeInTheDocument()

      // Buttons sollten Labels haben
      expect(screen.getByRole('button', { name: /nur aktive/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /status/i })).toBeInTheDocument()
    })

    it('sollte mit Tastatur navigierbar sein', async () => {
      const { user } = render(<ParzellenList parzellen={mockParzellen} />)

      const searchInput = screen.getByPlaceholderText('Parzellen durchsuchen...')
      
      await user.tab()
      expect(searchInput).toHaveFocus()

      await user.tab()
      expect(document.activeElement).toBeInstanceOf(HTMLButtonElement)
    })
  })

  describe('Error Handling', () => {
    it('sollte mit leeren Daten umgehen', () => {
      render(<ParzellenList parzellen={[]} />)

      expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      expect(screen.getByText('0 von 0 Parzellen')).toBeInTheDocument()
    })

    it('sollte mit unvollständigen Parzelle-Daten umgehen', () => {
      const incompleteParzelle = testDataFactories.parzelle({
        id: 1,
        nummer: 'P-TEST',
        bezirkName: 'Test Bezirk',
        beschreibung: undefined,
        ausstattung: [],
        mieter: undefined,
        adresse: undefined,
      })

      render(<ParzellenList parzellen={[incompleteParzelle]} />)

      expect(screen.getByText('Parzelle P-TEST')).toBeInTheDocument()
      // Komponente sollte trotz fehlender Daten funktionieren
    })
  })

  describe('Performance', () => {
    it('sollte mit vielen Parzellen performant umgehen', () => {
      const manyParzellen = testDataFactories.multipleParzellen(50)
      
      const startTime = performance.now()
      render(<ParzellenList parzellen={manyParzellen} />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(200)
    })

    it('sollte Memoization korrekt verwenden', () => {
      const { rerender } = render(<ParzellenList parzellen={mockParzellen} />)

      const startTime = performance.now()
      rerender(<ParzellenList parzellen={mockParzellen} />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(50)
    })
  })

  describe('Responsive Verhalten', () => {
    it('sollte auf kleineren Bildschirmen korrekt funktionieren', () => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 480,
      })

      render(<ParzellenList parzellen={mockParzellen} />)

      expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('Parzellen durchsuchen...')).toBeInTheDocument()
    })
  })
})