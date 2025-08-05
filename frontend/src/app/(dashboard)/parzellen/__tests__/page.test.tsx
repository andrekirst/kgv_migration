/**
 * Parzellen Page Integration Tests
 * 
 * Testet die vollständige Parzellen-Übersichtsseite mit Server-Side Rendering,
 * komplexen Filtern, Statistiken und Multi-Status-Handling
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import ParzellenPage from '../page'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { ParzellenStatus } from '@/types/bezirke'

// Import MSW server
import '@/test/mocks/server'

// Mock Next.js router
const mockPush = jest.fn()
const mockReplace = jest.fn()

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
    replace: mockReplace,
    back: jest.fn(),
    forward: jest.fn(),
    refresh: jest.fn(),
  }),
  usePathname: () => '/parzellen',
  useSearchParams: () => new URLSearchParams(),
}))

describe('Parzellen Page Integration', () => {
  let mockSearchParams: Promise<{ [key: string]: string | string[] | undefined }>
  let mockParzellenData: any[]

  beforeEach(() => {
    mockSearchParams = Promise.resolve({})
    
    // Erstelle umfassende Test-Parzellen mit verschiedenen Status
    mockParzellenData = [
      testDataFactories.parzelle({
        id: 1,
        nummer: 'P-001',
        bezirkId: 1,
        bezirkName: 'Bezirk Mitte-Nord',
        groesse: 400,
        status: ParzellenStatus.FREI,
        monatlichePacht: 25.50,
        beschreibung: 'Sonnige Parzelle mit Obstbäumen',
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
        aktiv: true,
        mieter: {
          id: 1,
          vorname: 'Max',
          nachname: 'Mustermann',
          email: 'max.mustermann@example.com',
          telefon: '+49 30 12345678'
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
      testDataFactories.parzelle({
        id: 5,
        nummer: 'P-005',
        bezirkId: 3,
        bezirkName: 'Bezirk West',
        groesse: 500,
        status: ParzellenStatus.GESPERRT,
        monatlichePacht: 30.00,
        aktiv: true,
      }),
    ]
    
    // Reset MSW handlers
    server.resetHandlers()
    
    // Setup default successful API responses
    server.use(
      http.get('/api/parzellen', () => {
        return HttpResponse.json({
          success: true,
          data: {
            parzellen: mockParzellenData,
            pagination: {
              page: 1,
              limit: 20,
              total: mockParzellenData.length,
              totalPages: 1
            },
            filters: {}
          }
        })
      }),
      http.get('/api/parzellen/statistiken/gesamt', () => {
        return HttpResponse.json({
          success: true,
          data: {
            gesamtParzellen: 50,
            freieParzellen: 10,
            belegteParzellen: 35,
            reservierteParzellen: 3,
            wartungParzellen: 1,
            gesperrteParzellen: 1,
            durchschnittsPacht: 27.50,
            gesamtEinnahmen: 962.50,
            auslastung: 76.0
          }
        })
      })
    )
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Page Structure and SSR', () => {
    it('sollte die Parzellen-Page korrekt rendern', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      // Page Header
      expect(screen.getByRole('heading', { level: 1, name: 'Parzellenverwaltung' })).toBeInTheDocument()
      expect(screen.getByText('Übersicht und Verwaltung aller Kleingartenverein Parzellen')).toBeInTheDocument()

      // Action buttons
      expect(screen.getByRole('link', { name: 'Zu Bezirken' })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Freie Parzellen' })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Neue Parzelle' })).toBeInTheDocument()
    })

    it('sollte Navigation-Links korrekt setzen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      const neueParzelle = screen.getByRole('link', { name: 'Neue Parzelle' })
      expect(neueParzelle).toHaveAttribute('href', '/parzellen/neu')

      const bezirkeLink = screen.getByRole('link', { name: 'Zu Bezirken' })
      expect(bezirkeLink).toHaveAttribute('href', '/bezirke')

      const freieParzellen = screen.getByRole('link', { name: 'Freie Parzellen' })
      expect(freieParzellen).toHaveAttribute('href', '/parzellen/freie')
    })

    it('sollte Metadata korrekt setzen', async () => {
      const ParzellenPageModule = await import('../page')
      
      expect(ParzellenPageModule.metadata).toEqual({
        title: 'Parzellen Übersicht',
        description: 'Übersicht aller Kleingartenverein Parzellen mit Filterfunktionen und Verwaltungsmöglichkeiten'
      })
      
      expect(ParzellenPageModule.dynamic).toBe('force-dynamic')
    })
  })

  describe('Statistics Integration', () => {
    it('sollte Parzellen-Statistiken erfolgreich laden und anzeigen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('50')).toBeInTheDocument() // Gesamt Parzellen
        expect(screen.getByText('10')).toBeInTheDocument() // Freie Parzellen
        expect(screen.getByText('35')).toBeInTheDocument() // Belegte Parzellen
        expect(screen.getByText('76,0%')).toBeInTheDocument() // Auslastung
      })
    })

    it('sollte verschiedene Parzellen-Status in Statistiken anzeigen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('3')).toBeInTheDocument() // Reservierte Parzellen
        expect(screen.getByText('1')).toBeInTheDocument() // Wartung Parzellen
        expect(screen.getByText('1')).toBeInTheDocument() // Gesperrte Parzellen
      })
    })

    it('sollte finanzielle Statistiken korrekt formatieren', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('27,50 €')).toBeInTheDocument() // Durchschnittspacht
        expect(screen.getByText('962,50 €')).toBeInTheDocument() // Gesamteinnahmen
      })
    })

    it('sollte Fehler beim Laden der Statistiken behandeln', async () => {
      server.use(
        http.get('/api/parzellen/statistiken/gesamt', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Fallback-Werte sollten angezeigt werden
        expect(screen.getByText('0')).toBeInTheDocument()
      })
    })
  })

  describe('Parzellen Table Integration', () => {
    it('sollte Parzellen-Tabelle mit allen Spalten rendern', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Table headers
        expect(screen.getByText('Parzelle')).toBeInTheDocument()
        expect(screen.getByText('Bezirk')).toBeInTheDocument()
        expect(screen.getByText('Größe')).toBeInTheDocument()
        expect(screen.getByText('Pacht')).toBeInTheDocument()
        expect(screen.getByText('Mieter')).toBeInTheDocument()
        expect(screen.getByText('Status')).toBeInTheDocument()
      })
    })

    it('sollte alle Parzellen-Daten korrekt anzeigen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Parzellen-Nummern
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
        expect(screen.getByText('Parzelle P-002')).toBeInTheDocument()
        expect(screen.getByText('Parzelle P-003')).toBeInTheDocument()
        
        // Bezirks-Links
        expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
        expect(screen.getByText('Bezirk Süd-Ost')).toBeInTheDocument()
        
        // Größenangaben
        expect(screen.getByText('400 m²')).toBeInTheDocument()
        expect(screen.getByText('350 m²')).toBeInTheDocument()
        expect(screen.getByText('600 m²')).toBeInTheDocument()
        
        // Pachtbeträge
        expect(screen.getByText('€25.50')).toBeInTheDocument()
        expect(screen.getByText('€22.00')).toBeInTheDocument()
        expect(screen.getByText('€35.00')).toBeInTheDocument()
      })
    })

    it('sollte Mieter-Informationen korrekt anzeigen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Belegte Parzelle mit Mieter
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
        expect(screen.getByText('max.mustermann@example.com')).toBeInTheDocument()
        
        // Freie Parzellen ohne Mieter
        const nichtZugewiesen = screen.getAllByText('Nicht zugewiesen')
        expect(nichtZugewiesen.length).toBeGreaterThan(0)
      })
    })

    it('sollte Status-Badges mit korrekten Farben anzeigen', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Frei')).toBeInTheDocument()
        expect(screen.getByText('Belegt')).toBeInTheDocument()
        expect(screen.getByText('Reserviert')).toBeInTheDocument()
        expect(screen.getByText('Wartung')).toBeInTheDocument()
        expect(screen.getByText('Gesperrt')).toBeInTheDocument()
      })
    })

    it('sollte Action-Buttons für jede Parzelle rendern', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const anzeigenButtons = screen.getAllByText('Anzeigen')
        const bearbeitenButtons = screen.getAllByText('Bearbeiten')
        
        expect(anzeigenButtons.length).toBe(mockParzellenData.length)
        expect(bearbeitenButtons.length).toBe(mockParzellenData.length)
        
        // Teste spezifische Links
        const firstAnzeigenLink = anzeigenButtons[0].closest('a')
        expect(firstAnzeigenLink).toHaveAttribute('href', '/parzellen/1')
        
        const firstBearbeitenLink = bearbeitenButtons[0].closest('a')
        expect(firstBearbeitenLink).toHaveAttribute('href', '/parzellen/1/bearbeiten')
      })
    })

    it('sollte Bezirks-Links korrekt verlinken', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const bezirkLinks = screen.getAllByText('Bezirk Mitte-Nord')
        bezirkLinks.forEach(link => {
          expect(link.closest('a')).toHaveAttribute('href', '/bezirke/1')
        })
        
        const bezirkSuedOst = screen.getByText('Bezirk Süd-Ost')
        expect(bezirkSuedOst.closest('a')).toHaveAttribute('href', '/bezirke/2')
      })
    })

    it('sollte leere Tabelle korrekt behandeln', async () => {
      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: [],
              pagination: { page: 1, limit: 20, total: 0, totalPages: 0 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
        expect(screen.getByText('Es wurden keine Parzellen gefunden, die Ihren Suchkriterien entsprechen.')).toBeInTheDocument()
        expect(screen.getByRole('link', { name: 'Erste Parzelle erstellen' })).toBeInTheDocument()
      })
    })
  })

  describe('Complex Filters Integration', () => {
    it('sollte Such-Filter korrekt parsen und anwenden', async () => {
      const searchParams = Promise.resolve({
        search: 'P-001',
        bezirkId: '1',
        status: ['frei', 'belegt'],
        groesseMin: '300',
        groesseMax: '500',
        pachtMin: '20.00',
        pachtMax: '30.00',
        aktiv: 'true'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          
          // Teste dass alle Filter-Parameter korrekt übertragen werden
          expect(url.searchParams.get('search')).toBe('P-001')
          expect(url.searchParams.get('bezirkId')).toBe('1')
          expect(url.searchParams.getAll('status')).toEqual(['frei', 'belegt'])
          expect(url.searchParams.get('groesseMin')).toBe('300')
          expect(url.searchParams.get('groesseMax')).toBe('500')
          expect(url.searchParams.get('pachtMin')).toBe('20.00')
          expect(url.searchParams.get('pachtMax')).toBe('30.00')
          expect(url.searchParams.get('aktiv')).toBe('true')
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: mockParzellenData.filter(p => p.nummer === 'P-001'),
              pagination: { page: 1, limit: 20, total: 1, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
      })
    })

    it('sollte Multi-Status-Filter korrekt handhaben', async () => {
      const searchParams = Promise.resolve({
        status: ['frei', 'reserviert']
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          const statusParams = url.searchParams.getAll('status')
          expect(statusParams).toEqual(['frei', 'reserviert'])
          
          const filteredParzellen = mockParzellenData.filter(p => 
            ['frei', 'reserviert'].includes(p.status)
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: filteredParzellen,
              pagination: { page: 1, limit: 20, total: filteredParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument() // Frei
        expect(screen.getByText('Parzelle P-003')).toBeInTheDocument() // Reserviert
        expect(screen.queryByText('Parzelle P-002')).not.toBeInTheDocument() // Belegt - sollte nicht angezeigt werden
      })
    })

    it('sollte Größen-Range-Filter korrekt anwenden', async () => {
      const searchParams = Promise.resolve({
        groesseMin: '350',
        groesseMax: '450'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('groesseMin')).toBe('350')
          expect(url.searchParams.get('groesseMax')).toBe('450')
          
          const filteredParzellen = mockParzellenData.filter(p => 
            p.groesse >= 350 && p.groesse <= 450
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: filteredParzellen,
              pagination: { page: 1, limit: 20, total: filteredParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument() // 400m²
        expect(screen.getByText('Parzelle P-002')).toBeInTheDocument() // 350m²
        expect(screen.queryByText('Parzelle P-003')).not.toBeInTheDocument() // 600m² - zu groß
      })
    })

    it('sollte Pacht-Range-Filter korrekt anwenden', async () => {
      const searchParams = Promise.resolve({
        pachtMin: '22.00',
        pachtMax: '30.00'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('pachtMin')).toBe('22.00')
          expect(url.searchParams.get('pachtMax')).toBe('30.00')
          
          const filteredParzellen = mockParzellenData.filter(p => 
            p.monatlichePacht >= 22.00 && p.monatlichePacht <= 30.00
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: filteredParzellen,
              pagination: { page: 1, limit: 20, total: filteredParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-002')).toBeInTheDocument() // 22.00€
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument() // 25.50€
        expect(screen.queryByText('Parzelle P-003')).not.toBeInTheDocument() // 35.00€ - zu teuer
      })
    })
  })

  describe('Sorting Integration', () => {
    it('sollte nach Nummer sortieren', async () => {
      const searchParams = Promise.resolve({
        sortBy: 'nummer',
        sortOrder: 'desc'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('sortBy')).toBe('nummer')
          expect(url.searchParams.get('sortOrder')).toBe('desc')
          
          const sortedParzellen = [...mockParzellenData].sort((a, b) => 
            b.nummer.localeCompare(a.nummer)
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: sortedParzellen,
              pagination: { page: 1, limit: 20, total: sortedParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        const parzellenElements = screen.getAllByText(/Parzelle P-/)
        // Parzellen sollten in umgekehrter Reihenfolge angezeigt werden
        expect(parzellenElements[0]).toHaveTextContent('P-005')
      })
    })

    it('sollte nach Größe sortieren', async () => {
      const searchParams = Promise.resolve({
        sortBy: 'groesse',
        sortOrder: 'asc'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('sortBy')).toBe('groesse')
          expect(url.searchParams.get('sortOrder')).toBe('asc')
          
          const sortedParzellen = [...mockParzellenData].sort((a, b) => a.groesse - b.groesse)
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: sortedParzellen,
              pagination: { page: 1, limit: 20, total: sortedParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        const groessenElements = screen.getAllByText(/\d+ m²/)
        // Kleinste Parzelle zuerst
        expect(groessenElements[0]).toHaveTextContent('300 m²')
      })
    })

    it('sollte nach Pacht sortieren', async () => {
      const searchParams = Promise.resolve({
        sortBy: 'monatlichePacht',
        sortOrder: 'desc'
      })

      server.use(
        http.get('/api/parzellen', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('sortBy')).toBe('monatlichePacht')
          expect(url.searchParams.get('sortOrder')).toBe('desc')
          
          const sortedParzellen = [...mockParzellenData].sort((a, b) => 
            b.monatlichePacht - a.monatlichePacht
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: sortedParzellen,
              pagination: { page: 1, limit: 20, total: sortedParzellen.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        const pachtElements = screen.getAllByText(/€\d+\.\d+/)
        // Höchste Pacht zuerst
        expect(pachtElements[0]).toHaveTextContent('€35.00')
      })
    })
  })

  describe('Pagination Integration', () => {
    it('sollte Pagination korrekt anzeigen bei mehreren Seiten', async () => {
      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: mockParzellenData.slice(0, 3),
              pagination: {
                page: 1,
                limit: 3,
                total: mockParzellenData.length,
                totalPages: Math.ceil(mockParzellenData.length / 3)
              },
              filters: {}
            }
          })
        })
      )

      const searchParams = Promise.resolve({ limit: '3' })
      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText(/Zeige 1 bis 3 von/)).toBeInTheDocument()
        expect(screen.getByRole('link', { name: 'Nächste' })).toBeInTheDocument()
        expect(screen.queryByRole('link', { name: 'Vorherige' })).not.toBeInTheDocument()
      })
    })

    it('sollte komplexe Filter in Pagination-URLs beibehalten', async () => {
      const searchParams = Promise.resolve({
        search: 'test',
        status: ['frei', 'belegt'],
        bezirkId: '1',
        page: '1',
        limit: '2'
      })

      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: mockParzellenData.slice(0, 2),
              pagination: {
                page: 1,
                limit: 2,
                total: mockParzellenData.length,
                totalPages: Math.ceil(mockParzellenData.length / 2)
              },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams })
      render(component)

      await waitFor(() => {
        const nextLink = screen.getByRole('link', { name: 'Nächste' })
        const href = nextLink.getAttribute('href')
        
        // Alle Filter sollten in der URL erhalten bleiben
        expect(href).toContain('search=test')
        expect(href).toContain('status=frei')
        expect(href).toContain('status=belegt')
        expect(href).toContain('bezirkId=1')
        expect(href).toContain('page=2')
      })
    })
  })

  describe('Error Handling', () => {
    it('sollte API-Fehler beim Laden der Parzellen behandeln', async () => {
      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      })
    })

    it('sollte mit unvollständigen Parzelle-Daten umgehen', async () => {
      const incompleteParzelle = testDataFactories.parzelle({
        id: 999,
        nummer: 'P-INCOMPLETE',
        bezirkName: 'Test Bezirk',
        groesse: 0,
        monatlichePacht: 0,
        beschreibung: undefined,
        mieter: undefined,
        status: ParzellenStatus.FREI
      })

      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: [incompleteParzelle],
              pagination: { page: 1, limit: 20, total: 1, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-INCOMPLETE')).toBeInTheDocument()
        expect(screen.getByText('Test Bezirk')).toBeInTheDocument()
        expect(screen.getByText('0 m²')).toBeInTheDocument()
        expect(screen.getByText('€0.00')).toBeInTheDocument()
        expect(screen.getByText('Nicht zugewiesen')).toBeInTheDocument()
        expect(screen.getByText('Frei')).toBeInTheDocument()
      })
    })

    it('sollte Netzwerk-Fehler behandeln', async () => {
      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.error()
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Keine Parzellen gefunden')).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('sollte Loading-Skeletons für Statistiken anzeigen', async () => {
      server.use(
        http.get('/api/parzellen/statistiken/gesamt', async () => {
          await new Promise(resolve => setTimeout(resolve, 1000))
          return HttpResponse.json({ success: true, data: {} })
        })
      )

      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte deutsche Interface-Texte verwenden', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Parzellenverwaltung')).toBeInTheDocument()
        expect(screen.getByText('Übersicht und Verwaltung aller Kleingartenverein Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Neue Parzelle')).toBeInTheDocument()
        expect(screen.getByText('Freie Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Zu Bezirken')).toBeInTheDocument()
        expect(screen.getByText('pro Monat')).toBeInTheDocument()
        expect(screen.getByText('Nicht zugewiesen')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Status-Texte verwenden', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Frei')).toBeInTheDocument()
        expect(screen.getByText('Belegt')).toBeInTheDocument()
        expect(screen.getByText('Reserviert')).toBeInTheDocument()
        expect(screen.getByText('Wartung')).toBeInTheDocument()
        expect(screen.getByText('Gesperrt')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Deutsche Dezimaltrennzeichen in Statistiken
        expect(screen.getByText('27,50 €')).toBeInTheDocument()
        expect(screen.getByText('962,50 €')).toBeInTheDocument()
        expect(screen.getByText('76,0%')).toBeInTheDocument()
        
        // Preise in Tabelle (amerikanisches Format für consistency mit API)
        expect(screen.getByText('€25.50')).toBeInTheDocument()
        expect(screen.getByText('€22.00')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('sollte semantische HTML-Struktur verwenden', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByRole('table')).toBeInTheDocument()
        expect(screen.getAllByRole('columnheader')).toHaveLength(7)
        expect(screen.getAllByRole('row')).toHaveLength(mockParzellenData.length + 1)
      })
    })

    it('sollte Links und Buttons korrekt beschriften', async () => {
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const links = screen.getAllByRole('link')
        links.forEach(link => {
          expect(link).toHaveAttribute('href')
          expect(link.textContent).toBeTruthy()
        })

        const buttons = screen.getAllByRole('button')
        buttons.forEach(button => {
          expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
        })
      })
    })
  })

  describe('Performance', () => {
    it('sollte große Parzellen-Listen effizient handhaben', async () => {
      const largeParzellen = Array.from({ length: 100 }, (_, i) => 
        testDataFactories.parzelle({ 
          id: i + 1, 
          nummer: `P-${String(i + 1).padStart(3, '0')}`,
          status: i % 4 === 0 ? ParzellenStatus.FREI : 
                  i % 4 === 1 ? ParzellenStatus.BELEGT :
                  i % 4 === 2 ? ParzellenStatus.RESERVIERT :
                  ParzellenStatus.WARTUNG
        })
      )

      server.use(
        http.get('/api/parzellen', () => {
          return HttpResponse.json({
            success: true,
            data: {
              parzellen: largeParzellen,
              pagination: { page: 1, limit: 100, total: 100, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const startTime = performance.now()
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(1000)
    })

    it('sollte SSR effizient durchführen', async () => {
      const startTime = performance.now()
      
      const component = await ParzellenPage({ searchParams: mockSearchParams })
      render(component)
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(500)
    })
  })
})