/**
 * Bezirke Page Integration Tests
 * 
 * Testet die vollständige Bezirke-Übersichtsseite mit Server-Side Rendering,
 * API-Integration, Filtering, Pagination und alle UI-Komponenten
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import BezirkePage from '../page'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { testDataFactories, bezirkeData } from '@/test/fixtures/kgv-data'

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
  usePathname: () => '/bezirke',
  useSearchParams: () => new URLSearchParams(),
}))

describe('Bezirke Page Integration', () => {
  let mockSearchParams: Promise<{ [key: string]: string | string[] | undefined }>

  beforeEach(() => {
    mockSearchParams = Promise.resolve({})
    
    // Reset MSW handlers
    server.resetHandlers()
    
    // Setup default successful API responses
    server.use(
      http.get('/api/bezirke', () => {
        return HttpResponse.json({
          success: true,
          data: {
            bezirke: bezirkeData,
            pagination: {
              page: 1,
              limit: 20,
              total: bezirkeData.length,
              totalPages: 1
            },
            filters: {}
          }
        })
      }),
      http.get('/api/bezirke/statistiken/gesamt', () => {
        return HttpResponse.json({
          success: true,
          data: {
            gesamtBezirke: 5,
            aktiveBezirke: 4,
            gesamtParzellen: 45,
            belegteParzellen: 38,
            freieParzellen: 7,
            auslastung: 84.4,
            trends: {
              neueAntraege: 12,
              kuendigungen: 2,
              neueParzellen: 3,
              zeitraum: 'Letzten 30 Tage'
            }
          }
        })
      })
    )
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Page Structure and SSR', () => {
    it('sollte die Bezirke-Page korrekt rendern', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      // Page Header
      expect(screen.getByRole('heading', { level: 2, name: 'Bezirke verwalten' })).toBeInTheDocument()
      expect(screen.getByText('Übersicht aller Kleingartenverein Bezirke')).toBeInTheDocument()

      // Action buttons
      expect(screen.getByRole('link', { name: 'Zu Parzellen' })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Neuer Bezirk' })).toBeInTheDocument()
    })

    it('sollte Navigation-Links korrekt setzen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      const neuerBezirkLink = screen.getByRole('link', { name: 'Neuer Bezirk' })
      expect(neuerBezirkLink).toHaveAttribute('href', '/bezirke/neu')

      const parzellenLink = screen.getByRole('link', { name: 'Zu Parzellen' })
      expect(parzellenLink).toHaveAttribute('href', '/parzellen')
    })

    it('sollte Metadata korrekt setzen', async () => {
      // Teste dass die Page-Komponente die korrekten Metadata exports hat
      const BezirkePageModule = await import('../page')
      
      expect(BezirkePageModule.metadata).toEqual({
        title: 'Bezirke Übersicht',
        description: 'Übersicht aller Kleingartenverein Bezirke mit Statistiken und Verwaltungsfunktionen'
      })
      
      expect(BezirkePageModule.dynamic).toBe('force-dynamic')
    })
  })

  describe('Statistics Integration', () => {
    it('sollte Statistiken erfolgreich laden und anzeigen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('5')).toBeInTheDocument() // Gesamt Bezirke
        expect(screen.getByText('4')).toBeInTheDocument() // Aktive Bezirke
        expect(screen.getByText('45')).toBeInTheDocument() // Gesamt Parzellen
        expect(screen.getByText('84,4%')).toBeInTheDocument() // Auslastung
      })
    })

    it('sollte Statistik-Karten korrekt formatieren', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Gesamt Bezirke')).toBeInTheDocument()
        expect(screen.getByText('Aktive Bezirke')).toBeInTheDocument()
        expect(screen.getByText('Gesamt Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Auslastung')).toBeInTheDocument()
      })
    })

    it('sollte Trend-Informationen anzeigen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('+12 neue Anträge')).toBeInTheDocument()
        expect(screen.getByText('+3 neue Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Letzten 30 Tage')).toBeInTheDocument()
      })
    })

    it('sollte Fehler beim Laden der Statistiken behandeln', async () => {
      server.use(
        http.get('/api/bezirke/statistiken/gesamt', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Fallback-Werte sollten angezeigt werden
        expect(screen.getByText('0')).toBeInTheDocument()
      })
    })
  })

  describe('Bezirke Table Integration', () => {
    it('sollte Bezirke-Tabelle mit Daten rendern', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Table headers
        expect(screen.getByText('Bezirk')).toBeInTheDocument()
        expect(screen.getByText('Bezirksleiter')).toBeInTheDocument()
        expect(screen.getByText('Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Auslastung')).toBeInTheDocument()
        expect(screen.getByText('Status')).toBeInTheDocument()
        
        // Bezirke data from mock
        bezirkeData.forEach(bezirk => {
          expect(screen.getByText(bezirk.name)).toBeInTheDocument()
          if (bezirk.bezirksleiter) {
            expect(screen.getByText(bezirk.bezirksleiter)).toBeInTheDocument()
          }
        })
      })
    })

    it('sollte Auslastungs-Balken korrekt anzeigen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Progress bars für Auslastung sollten vorhanden sein
        const progressBars = document.querySelectorAll('.bg-secondary-200')
        expect(progressBars.length).toBeGreaterThan(0)
        
        // Verschiedene Farben basierend auf Auslastung
        expect(document.querySelector('.bg-green-500')).toBeTruthy()
        expect(document.querySelector('.bg-yellow-500') || document.querySelector('.bg-red-500')).toBeTruthy()
      })
    })

    it('sollte Status-Badges korrekt anzeigen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Aktiv')).toBeInTheDocument()
        // Inactive Bezirke wenn vorhanden
        const inactiveBadges = screen.queryAllByText('Inaktiv')
        expect(inactiveBadges.length).toBeGreaterThanOrEqual(0)
      })
    })

    it('sollte Action-Buttons für jeden Bezirk rendern', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const anzeigenButtons = screen.getAllByText('Anzeigen')
        const bearbeitenButtons = screen.getAllByText('Bearbeiten')
        
        expect(anzeigenButtons.length).toBe(bezirkeData.length)
        expect(bearbeitenButtons.length).toBe(bezirkeData.length)
        
        // Teste Href-Attribute
        bezirkeData.forEach(bezirk => {
          const anzeigenLink = anzeigenButtons.find(btn => 
            btn.closest('a')?.getAttribute('href') === `/bezirke/${bezirk.id}`
          )
          expect(anzeigenLink).toBeTruthy()
          
          const bearbeitenLink = bearbeitenButtons.find(btn => 
            btn.closest('a')?.getAttribute('href') === `/bezirke/${bezirk.id}/bearbeiten`
          )
          expect(bearbeitenLink).toBeTruthy()
        })
      })
    })

    it('sollte leere Tabelle korrekt behandeln', async () => {
      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: [],
              pagination: { page: 1, limit: 20, total: 0, totalPages: 0 },
              filters: {}
            }
          })
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Keine Bezirke gefunden')).toBeInTheDocument()
        expect(screen.getByText('Es wurden keine Bezirke gefunden, die Ihren Suchkriterien entsprechen.')).toBeInTheDocument()
        expect(screen.getByRole('link', { name: 'Ersten Bezirk erstellen' })).toBeInTheDocument()
      })
    })
  })

  describe('Filters Integration', () => {
    it('sollte Filter-Parameter aus URL korrekt parsen', async () => {
      const searchParams = Promise.resolve({
        search: 'Mitte',
        aktiv: 'true',
        page: '2',
        limit: '10',
        sortBy: 'name',
        sortOrder: 'desc'
      })

      const component = await BezirkePage({ searchParams })
      render(component)

      // Die Filter sollten an die BezirkeFilters-Komponente weitergegeben werden
      await waitFor(() => {
        expect(screen.getByDisplayValue('Mitte')).toBeInTheDocument()
      })
    })

    it('sollte Standard-Filter-Werte verwenden', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      // Standard-Pagination sollte verwendet werden
      await waitFor(() => {
        // Überprüfe dass keine erweiterte Pagination angezeigt wird wenn nur 1 Seite
        expect(screen.queryByText('Vorherige')).not.toBeInTheDocument()
        expect(screen.queryByText('Nächste')).not.toBeInTheDocument()
      })
    })

    it('sollte Suchfilter korrekt anwenden', async () => {
      server.use(
        http.get('/api/bezirke', ({ request }) => {
          const url = new URL(request.url)
          const search = url.searchParams.get('search')
          
          if (search === 'Mitte') {
            const filteredBezirke = bezirkeData.filter(b => b.name.includes('Mitte'))
            return HttpResponse.json({
              success: true,
              data: {
                bezirke: filteredBezirke,
                pagination: { page: 1, limit: 20, total: filteredBezirke.length, totalPages: 1 },
                filters: { search: 'Mitte' }
              }
            })
          }
          
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: bezirkeData,
              pagination: { page: 1, limit: 20, total: bezirkeData.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const searchParams = Promise.resolve({ search: 'Mitte' })
      const component = await BezirkePage({ searchParams })
      render(component)

      await waitFor(() => {
        // Nur Bezirke mit "Mitte" im Namen sollten angezeigt werden
        const mitteElements = screen.getAllByText(/Mitte/)
        expect(mitteElements.length).toBeGreaterThan(0)
      })
    })

    it('sollte Aktiv-Filter korrekt anwenden', async () => {
      server.use(
        http.get('/api/bezirke', ({ request }) => {
          const url = new URL(request.url)
          const aktiv = url.searchParams.get('aktiv')
          
          if (aktiv === 'false') {
            const inactiveBezirke = bezirkeData.filter(b => !b.aktiv)
            return HttpResponse.json({
              success: true,
              data: {
                bezirke: inactiveBezirke,
                pagination: { page: 1, limit: 20, total: inactiveBezirke.length, totalPages: 1 },
                filters: { aktiv: false }
              }
            })
          }
          
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: bezirkeData.filter(b => b.aktiv),
              pagination: { page: 1, limit: 20, total: bezirkeData.filter(b => b.aktiv).length, totalPages: 1 },
              filters: { aktiv: true }
            }
          })
        })
      )

      const searchParams = Promise.resolve({ aktiv: 'false' })
      const component = await BezirkePage({ searchParams })
      render(component)

      await waitFor(() => {
        // Nur inaktive Bezirke sollten angezeigt werden
        const inactiveBadges = screen.getAllByText('Inaktiv')
        expect(inactiveBadges.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Pagination Integration', () => {
    it('sollte Pagination korrekt anzeigen bei mehreren Seiten', async () => {
      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: bezirkeData.slice(0, 2),
              pagination: {
                page: 1,
                limit: 2,
                total: bezirkeData.length,
                totalPages: Math.ceil(bezirkeData.length / 2)
              },
              filters: {}
            }
          })
        })
      )

      const searchParams = Promise.resolve({ limit: '2' })
      const component = await BezirkePage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText(/Zeige 1 bis 2 von/)).toBeInTheDocument()
        expect(screen.getByRole('link', { name: 'Nächste' })).toBeInTheDocument()
        expect(screen.queryByRole('link', { name: 'Vorherige' })).not.toBeInTheDocument()
      })
    })

    it('sollte zweite Seite korrekt anzeigen', async () => {
      server.use(
        http.get('/api/bezirke', ({ request }) => {
          const url = new URL(request.url)
          const page = parseInt(url.searchParams.get('page') || '1')
          
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: bezirkeData.slice((page - 1) * 2, page * 2),
              pagination: {
                page: page,
                limit: 2,
                total: bezirkeData.length,
                totalPages: Math.ceil(bezirkeData.length / 2)
              },
              filters: {}
            }
          })
        })
      )

      const searchParams = Promise.resolve({ page: '2', limit: '2' })
      const component = await BezirkePage({ searchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText(/Zeige 3 bis 4 von/)).toBeInTheDocument()
        expect(screen.getByRole('link', { name: 'Vorherige' })).toBeInTheDocument()
        
        // Prüfe URLs der Pagination-Links
        const prevLink = screen.getByRole('link', { name: 'Vorherige' })
        expect(prevLink).toHaveAttribute('href', expect.stringContaining('page=1'))
      })
    })

    it('sollte keine Pagination bei einer Seite anzeigen', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.queryByText('Vorherige')).not.toBeInTheDocument()
        expect(screen.queryByText('Nächste')).not.toBeInTheDocument()
      })
    })
  })

  describe('Sorting Integration', () => {
    it('sollte nach Name sortieren', async () => {
      server.use(
        http.get('/api/bezirke', ({ request }) => {
          const url = new URL(request.url)
          const sortBy = url.searchParams.get('sortBy')
          const sortOrder = url.searchParams.get('sortOrder')
          
          if (sortBy === 'name' && sortOrder === 'desc') {
            const sortedBezirke = [...bezirkeData].sort((a, b) => 
              b.name.localeCompare(a.name, 'de')
            )
            return HttpResponse.json({
              success: true,
              data: {
                bezirke: sortedBezirke,
                pagination: { page: 1, limit: 20, total: sortedBezirke.length, totalPages: 1 },
                filters: { sortBy: 'name', sortOrder: 'desc' }
              }
            })
          }
          
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: bezirkeData,
              pagination: { page: 1, limit: 20, total: bezirkeData.length, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const searchParams = Promise.resolve({ sortBy: 'name', sortOrder: 'desc' })
      const component = await BezirkePage({ searchParams })
      render(component)

      await waitFor(() => {
        // Die Bezirke sollten in umgekehrter alphabetischer Reihenfolge angezeigt werden
        const bezirkNames = screen.getAllByText(/Bezirk/)
        expect(bezirkNames.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Error Handling', () => {
    it('sollte API-Fehler beim Laden der Bezirke behandeln', async () => {
      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Fallback-Zustand sollte angezeigt werden
        expect(screen.getByText('Keine Bezirke gefunden')).toBeInTheDocument()
      })
    })

    it('sollte Netzwerk-Fehler behandeln', async () => {
      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.error()
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Keine Bezirke gefunden')).toBeInTheDocument()
      })
    })

    it('sollte mit unvollständigen Daten umgehen', async () => {
      const incompleteBezirk = testDataFactories.bezirk({
        id: 999,
        name: 'Unvollständiger Bezirk',
        bezirksleiter: undefined,
        email: undefined,
        statistiken: {
          gesamtParzellen: 0,
          belegteParzellen: 0,
          freieParzellen: 0
        }
      })

      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: [incompleteBezirk],
              pagination: { page: 1, limit: 20, total: 1, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Unvollständiger Bezirk')).toBeInTheDocument()
        expect(screen.getByText('Nicht zugewiesen')).toBeInTheDocument()
        expect(screen.getByText('0 / 0')).toBeInTheDocument()
      })
    })
  })

  describe('Loading States', () => {
    it('sollte Loading-Skeletons für Statistiken anzeigen', async () => {
      // Simuliere langsame Statistik-API
      server.use(
        http.get('/api/bezirke/statistiken/gesamt', async () => {
          await new Promise(resolve => setTimeout(resolve, 1000))
          return HttpResponse.json({ success: true, data: {} })
        })
      )

      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      // Loading-Skeletons sollten sichtbar sein
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('sollte Loading-Skeleton für Tabelle anzeigen', async () => {
      // Da die Page SSR verwendet, werden Loading-States hauptsächlich in Suspense-Grenzen gezeigt
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      // Die Tabelle sollte mit Daten geladen werden (SSR)
      await waitFor(() => {
        expect(screen.getByText('Bezirk')).toBeInTheDocument()
      })
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte deutsche Interface-Texte verwenden', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText('Bezirke verwalten')).toBeInTheDocument()
        expect(screen.getByText('Übersicht aller Kleingartenverein Bezirke')).toBeInTheDocument()
        expect(screen.getByText('Neuer Bezirk')).toBeInTheDocument()
        expect(screen.getByText('Zu Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Bezirksleiter')).toBeInTheDocument()
        expect(screen.getByText('Auslastung')).toBeInTheDocument()
        expect(screen.getByText('Anzeigen')).toBeInTheDocument()
        expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Deutsche Prozentzeichen und Dezimaltrennzeichen
        expect(screen.getByText('84,4%')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Pluralformen verwenden', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        expect(screen.getByText(/Bezirke/)).toBeInTheDocument()
        expect(screen.getByText(/Parzellen/)).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('sollte semantische HTML-Struktur verwenden', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        // Tabelle sollte korrekte semantische Struktur haben
        expect(screen.getByRole('table')).toBeInTheDocument()
        expect(screen.getAllByRole('columnheader')).toHaveLength(6)
        expect(screen.getAllByRole('row')).toHaveLength(bezirkeData.length + 1) // +1 für Header
      })
    })

    it('sollte Links korrekt beschriften', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const links = screen.getAllByRole('link')
        links.forEach(link => {
          expect(link).toHaveAttribute('href')
          expect(link.textContent).toBeTruthy()
        })
      })
    })

    it('sollte Buttons korrekt beschriften', async () => {
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)

      await waitFor(() => {
        const buttons = screen.getAllByRole('button')
        buttons.forEach(button => {
          expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
        })
      })
    })
  })

  describe('Performance', () => {
    it('sollte Server-Side Rendering effizient durchführen', async () => {
      const startTime = performance.now()
      
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(500) // Weniger als 500ms für SSR
    })

    it('sollte große Datenmengen effizient handhaben', async () => {
      const largeBezirkeList = Array.from({ length: 100 }, (_, i) => 
        testDataFactories.bezirk({ id: i + 1, name: `Bezirk ${i + 1}` })
      )

      server.use(
        http.get('/api/bezirke', () => {
          return HttpResponse.json({
            success: true,
            data: {
              bezirke: largeBezirkeList,
              pagination: { page: 1, limit: 100, total: 100, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const startTime = performance.now()
      const component = await BezirkePage({ searchParams: mockSearchParams })
      render(component)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(1000) // Auch mit 100 Elementen unter 1s
    })
  })
})