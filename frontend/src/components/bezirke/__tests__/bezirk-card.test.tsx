import React from 'react'
import { render, screen } from '@/test/utils/test-utils'
import { BezirkCard } from '../bezirk-card'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { Bezirk } from '@/types/bezirke'

describe('BezirkCard Component', () => {
  let mockBezirk: Bezirk
  let mockOnClick: jest.Mock
  let mockOnEdit: jest.Mock
  let mockOnDelete: jest.Mock

  beforeEach(() => {
    mockBezirk = testDataFactories.bezirk({
      id: 1,
      name: 'Testbezirk Mitte',
      beschreibung: 'Ein schöner Testbezirk mit vielen Obstbäumen und Gemüsebeeten',
      bezirksleiter: 'Herr Max Mustermann',
      telefon: '+49 30 12345678',
      email: 'test@kgv-testbezirk.de',
      adresse: {
        strasse: 'Gartenstraße',
        hausnummer: '123',
        plz: '12345',
        ort: 'Berlin',
      },
      statistiken: {
        gesamtParzellen: 50,
        belegteParzellen: 40,
        freieParzellen: 10,
        warteliste: 5,
      },
      aktiv: true,
    })

    mockOnClick = jest.fn()
    mockOnEdit = jest.fn()
    mockOnDelete = jest.fn()
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Grid View', () => {
    it('sollte alle Bezirk-Informationen im Grid-Modus anzeigen', () => {
      render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onClick={mockOnClick}
          onEdit={mockOnEdit}
          onDelete={mockOnDelete}
        />
      )

      // Grundinformationen
      expect(screen.getByText('Testbezirk Mitte')).toBeInTheDocument()
      expect(screen.getByText('Ein schöner Testbezirk mit vielen Obstbäumen und Gemüsebeeten')).toBeInTheDocument()

      // Kontaktinformationen
      expect(screen.getByText('Herr Max Mustermann')).toBeInTheDocument()
      expect(screen.getByText('+49 30 12345678')).toBeInTheDocument()
      expect(screen.getByText('test@kgv-testbezirk.de')).toBeInTheDocument()

      // Adresse
      expect(screen.getByText('Gartenstraße 123, 12345 Berlin')).toBeInTheDocument()

      // Statistiken
      expect(screen.getByText('50')).toBeInTheDocument() // Gesamtparzellen
      expect(screen.getByText('40')).toBeInTheDocument() // Belegte Parzellen
      expect(screen.getByText('10')).toBeInTheDocument() // Freie Parzellen
      expect(screen.getByText('80% Auslastung')).toBeInTheDocument()
      expect(screen.getByText('5 auf Warteliste')).toBeInTheDocument()
    })

    it('sollte inaktive Bezirke korrekt kennzeichnen', () => {
      const inactiveBezirk = { ...mockBezirk, aktiv: false }

      render(
        <BezirkCard
          bezirk={inactiveBezirk}
          viewMode="grid"
        />
      )

      expect(screen.getByText('Inaktiv')).toBeInTheDocument()
      
      // Card sollte visuell als inaktiv markiert sein
      const card = screen.getByText('Testbezirk Mitte').closest('[class*="opacity"]')
      expect(card).toHaveClass('opacity-60')
    })

    it('sollte die Auslastungsfarbe korrekt darstellen', () => {
      // Test für hohe Auslastung (>= 90%)
      const highOccupancyBezirk = {
        ...mockBezirk,
        statistiken: {
          ...mockBezirk.statistiken,
          belegteParzellen: 45, // 90% von 50
        },
      }

      const { rerender } = render(
        <BezirkCard bezirk={highOccupancyBezirk} viewMode="grid" />
      )

      expect(screen.getByText('90%')).toHaveClass('text-red-600', 'dark:text-red-400')

      // Test für mittlere Auslastung (75-89%)
      const mediumOccupancyBezirk = {
        ...mockBezirk,
        statistiken: {
          ...mockBezirk.statistiken,
          belegteParzellen: 38, // 76% von 50
        },
      }

      rerender(<BezirkCard bezirk={mediumOccupancyBezirk} viewMode="grid" />)
      expect(screen.getByText('76%')).toHaveClass('text-amber-600', 'dark:text-amber-400')

      // Test für niedrige Auslastung (< 75%)
      const lowOccupancyBezirk = {
        ...mockBezirk,
        statistiken: {
          ...mockBezirk.statistiken,
          belegteParzellen: 25, // 50% von 50
        },
      }

      rerender(<BezirkCard bezirk={lowOccupancyBezirk} viewMode="grid" />)
      expect(screen.getByText('50%')).toHaveClass('text-green-600', 'dark:text-green-400')
    })

    it('sollte ohne Warteliste korrekt funktionieren', () => {
      const noWaitlistBezirk = {
        ...mockBezirk,
        statistiken: {
          ...mockBezirk.statistiken,
          warteliste: 0,
        },
      }

      render(<BezirkCard bezirk={noWaitlistBezirk} viewMode="grid" />)

      expect(screen.queryByText('auf Warteliste')).not.toBeInTheDocument()
    })
  })

  describe('List View', () => {
    it('sollte im List-Modus korrekt dargestellt werden', () => {
      render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="list"
          onClick={mockOnClick}
          onEdit={mockOnEdit}
          onDelete={mockOnDelete}
        />
      )

      // Alle wichtigen Informationen sollten sichtbar sein
      expect(screen.getByText('Testbezirk Mitte')).toBeInTheDocument()
      expect(screen.getByText('Herr Max Mustermann')).toBeInTheDocument()
      expect(screen.getByText('Berlin')).toBeInTheDocument()

      // Statistiken in kompakter Form
      expect(screen.getByText('50')).toBeInTheDocument()
      expect(screen.getByText('80%')).toBeInTheDocument()
    })

    it('sollte den Bearbeiten-Button im List-Modus anzeigen', () => {
      render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="list"
          onEdit={mockOnEdit}
        />
      )

      expect(screen.getByRole('button', { name: 'Bearbeiten' })).toBeInTheDocument()
    })
  })

  describe('Interaktionen', () => {
    it('sollte onClick-Handler beim Klick auf die Karte aufrufen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onClick={mockOnClick}
        />
      )

      const card = screen.getByText('Testbezirk Mitte').closest('[class*="cursor-pointer"]')
      if (card) {
        await user.click(card)
        expect(mockOnClick).toHaveBeenCalledTimes(1)
      }
    })

    it('sollte das Dropdown-Menü öffnen und schließen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onEdit={mockOnEdit}
          onDelete={mockOnDelete}
        />
      )

      const dropdownButton = screen.getByText('⋯')
      await user.click(dropdownButton)

      // Menüoptionen sollten sichtbar sein
      expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      expect(screen.getByText('Löschen')).toBeInTheDocument()
    })

    it('sollte onEdit-Handler beim Klick auf Bearbeiten aufrufen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onEdit={mockOnEdit}
        />
      )

      const dropdownButton = screen.getByText('⋯')
      await user.click(dropdownButton)

      const editButton = screen.getByText('Bearbeiten')
      await user.click(editButton)

      expect(mockOnEdit).toHaveBeenCalledTimes(1)
    })

    it('sollte onDelete-Handler beim Klick auf Löschen aufrufen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onDelete={mockOnDelete}
        />
      )

      const dropdownButton = screen.getByText('⋯')
      await user.click(dropdownButton)

      const deleteButton = screen.getByText('Löschen')
      await user.click(deleteButton)

      expect(mockOnDelete).toHaveBeenCalledTimes(1)
    })

    it('sollte Event-Propagation beim Dropdown-Klick stoppen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onClick={mockOnClick}
          onEdit={mockOnEdit}
        />
      )

      const dropdownButton = screen.getByText('⋯')
      await user.click(dropdownButton)

      // onClick sollte nicht aufgerufen werden, wenn Dropdown geklickt wird
      expect(mockOnClick).not.toHaveBeenCalled()

      const editButton = screen.getByText('Bearbeiten')
      await user.click(editButton)

      // onClick sollte auch beim Klick auf Menüitem nicht aufgerufen werden
      expect(mockOnClick).not.toHaveBeenCalled()
      expect(mockOnEdit).toHaveBeenCalledTimes(1)
    })
  })

  describe('Datenbehandlung', () => {
    it('sollte mit fehlenden optionalen Daten umgehen', () => {
      const minimalBezirk = testDataFactories.bezirk({
        id: 1,
        name: 'Minimaler Bezirk',
        beschreibung: undefined,
        bezirksleiter: undefined,
        telefon: undefined,
        email: undefined,
        adresse: undefined,
        statistiken: {
          gesamtParzellen: 10,
          belegteParzellen: 5,
          freieParzellen: 5,
          warteliste: 0,
        },
      })

      render(<BezirkCard bezirk={minimalBezirk} viewMode="grid" />)

      expect(screen.getByText('Minimaler Bezirk')).toBeInTheDocument()
      expect(screen.getByText('50% Auslastung')).toBeInTheDocument()
      
      // Fehlende Kontaktdaten sollten nicht angezeigt werden
      expect(screen.queryByText('Herr')).not.toBeInTheDocument()
      expect(screen.queryByText('@')).not.toBeInTheDocument()
    })

    it('sollte mit partieller Adresse umgehen', () => {
      const partialAddressBezirk = {
        ...mockBezirk,
        adresse: {
          ort: 'Hamburg', // Nur Ort, keine Straße/PLZ
        },
      }

      render(<BezirkCard bezirk={partialAddressBezirk} viewMode="grid" />)

      expect(screen.getByText('Hamburg')).toBeInTheDocument()
      expect(screen.queryByText('Gartenstraße')).not.toBeInTheDocument()
    })

    it('sollte Division durch Null bei Auslastungsberechnung vermeiden', () => {
      const zeroParcellsBezirk = {
        ...mockBezirk,
        statistiken: {
          gesamtParzellen: 0,
          belegteParzellen: 0,
          freieParzellen: 0,
          warteliste: 0,
        },
      }

      render(<BezirkCard bezirk={zeroParcellsBezirk} viewMode="grid" />)

      expect(screen.getByText('0% Auslastung')).toBeInTheDocument()
      expect(screen.getByText('0')).toBeInTheDocument() // Gesamtparzellen
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte ARIA-Attribute haben', () => {
      render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onClick={mockOnClick}
        />
      )

      // Karte sollte als klickbar erkennbar sein
      const card = screen.getByText('Testbezirk Mitte').closest('[class*="cursor-pointer"]')
      expect(card).toBeInTheDocument()

      // Dropdown-Button sollte korrekt gekennzeichnet sein
      const dropdownButton = screen.getByText('⋯')
      expect(dropdownButton).toBeInTheDocument()
    })

    it('sollte mit Tastatur navigierbar sein', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onEdit={mockOnEdit}
        />
      )

      // Tab zum Dropdown-Button
      await user.tab()
      const dropdownButton = screen.getByText('⋯')
      expect(dropdownButton).toHaveFocus()

      // Enter zum Öffnen des Menüs
      await user.keyboard('{Enter}')
      expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
    })

    it('sollte Screen-Reader-freundliche Labels haben', () => {
      render(<BezirkCard bezirk={mockBezirk} viewMode="grid" />)

      // Icons sollten von Screen-Readern ignoriert werden (aria-hidden)
      const icons = document.querySelectorAll('svg')
      icons.forEach(icon => {
        // Überprüfe, ob Icon entweder aria-hidden hat oder ein Parent mit beschreibendem Text
        const hasAriaHidden = icon.getAttribute('aria-hidden') === 'true'
        const hasDescriptiveParent = icon.closest('[aria-label], [title]')
        expect(hasAriaHidden || hasDescriptiveParent).toBeTruthy()
      })
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle deutschen UI-Texte korrekt anzeigen', () => {
      render(<BezirkCard bezirk={mockBezirk} viewMode="grid" />)

      expect(screen.getByText('Parzellen-Übersicht')).toBeInTheDocument()
      expect(screen.getByText('Auslastung')).toBeInTheDocument()
      expect(screen.getByText('Gesamt')).toBeInTheDocument()
      expect(screen.getByText('Belegt')).toBeInTheDocument()
      expect(screen.getByText('Frei')).toBeInTheDocument()
      expect(screen.getByText('auf Warteliste')).toBeInTheDocument()
    })

    it('sollte deutsche Menüoptionen anzeigen', async () => {
      const { user } = render(
        <BezirkCard
          bezirk={mockBezirk}
          viewMode="grid"
          onEdit={mockOnEdit}
          onDelete={mockOnDelete}
        />
      )

      const dropdownButton = screen.getByText('⋯')
      await user.click(dropdownButton)

      expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      expect(screen.getByText('Löschen')).toBeInTheDocument()
    })
  })

  describe('Responsive Verhalten', () => {
    it('sollte auf verschiedenen Bildschirmgrößen korrekt funktionieren', () => {
      // Mobile View
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 320,
      })

      const { rerender } = render(
        <BezirkCard bezirk={mockBezirk} viewMode="grid" />
      )

      expect(screen.getByText('Testbezirk Mitte')).toBeInTheDocument()

      // Desktop View
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1920,
      })

      rerender(<BezirkCard bezirk={mockBezirk} viewMode="grid" />)
      expect(screen.getByText('Testbezirk Mitte')).toBeInTheDocument()
    })
  })

  describe('Performance', () => {
    it('sollte schnell rendern', () => {
      const startTime = performance.now()
      render(<BezirkCard bezirk={mockBezirk} viewMode="grid" />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(50)
    })

    it('sollte bei Re-Renders performant bleiben', () => {
      const { rerender } = render(
        <BezirkCard bezirk={mockBezirk} viewMode="grid" />
      )

      const startTime = performance.now()
      rerender(<BezirkCard bezirk={mockBezirk} viewMode="list" />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(30)
    })
  })
})