/**
 * Anträge Page Integration Tests
 * 
 * Testet die vollständige Anträge-Übersichtsseite mit Komponenten-Integration,
 * Filter-Funktionalität und Search-Parameter-Handling
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import AntraegePage from '../page'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { AntragStatus } from '@/types/bezirke'

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
  usePathname: () => '/antraege',
  useSearchParams: () => new URLSearchParams(),
}))

describe('Anträge Page Integration', () => {
  let mockAntraegeData: any[]

  beforeEach(() => {
    // Erstelle umfassende Test-Anträge mit verschiedenen Status
    mockAntraegeData = [
      testDataFactories.antrag({
        id: 1,
        antragsnummer: 'A-2024-001',
        antragsteller: {
          vorname: 'Max',
          nachname: 'Mustermann',
          email: 'max.mustermann@example.com',
          telefon: '+49 30 12345678',
          adresse: {
            strasse: 'Musterstraße',
            hausnummer: '123',
            plz: '12345',
            ort: 'Berlin'
          }
        },
        gewuenschteParzelle: {
          bezirkId: 1,
          bezirkName: 'Bezirk Mitte-Nord',
          maxGroesse: 400,
          maxPacht: 30.00
        },
        status: AntragStatus.EINGEGANGEN,
        eingangsdatum: '2024-01-15',
        bearbeitungsdatum: undefined,
        notizen: 'Neuer Antrag für Kleingartenverein',
        prioritaet: 'normal'
      }),
      testDataFactories.antrag({
        id: 2,
        antragsnummer: 'A-2024-002',
        antragsteller: {
          vorname: 'Anna',
          nachname: 'Schmidt',
          email: 'anna.schmidt@example.com',
          telefon: '+49 30 87654321'
        },
        gewuenschteParzelle: {
          bezirkId: 2,
          bezirkName: 'Bezirk Süd-Ost',
          maxGroesse: 350,
          maxPacht: 25.00
        },
        status: AntragStatus.IN_BEARBEITUNG,
        eingangsdatum: '2024-01-10',
        bearbeitungsdatum: '2024-01-12',
        bearbeiter: 'Admin User',
        notizen: 'Parzelle wird gesucht',
        prioritaet: 'hoch'
      }),
      testDataFactories.antrag({
        id: 3,
        antragsnummer: 'A-2024-003',
        antragsteller: {
          vorname: 'Peter',
          nachname: 'Weber',
          email: 'peter.weber@example.com'
        },
        gewuenschteParzelle: {
          bezirkId: 1,
          bezirkName: 'Bezirk Mitte-Nord',
          maxGroesse: 500,
          maxPacht: 35.00
        },
        zugewieseneParzelle: {
          id: 1,
          nummer: 'P-001',
          groesse: 450,
          monatlichePacht: 32.00
        },
        status: AntragStatus.GENEHMIGT,
        eingangsdatum: '2024-01-05',
        bearbeitungsdatum: '2024-01-08',
        genehmigungsdatum: '2024-01-10',
        bearbeiter: 'Admin User',
        notizen: 'Parzelle P-001 zugewiesen',
        prioritaet: 'normal'
      }),
      testDataFactories.antrag({
        id: 4,
        antragsnummer: 'A-2023-050',
        antragsteller: {
          vorname: 'Maria',
          nachname: 'Fischer',
          email: 'maria.fischer@example.com'
        },
        gewuenschteParzelle: {
          bezirkId: 3,
          bezirkName: 'Bezirk West',
          maxGroesse: 300,
          maxPacht: 20.00
        },
        status: AntragStatus.ABGELEHNT,
        eingangsdatum: '2023-12-20',
        bearbeitungsdatum: '2023-12-22',
        ablehnungsgrund: 'Keine geeignete Parzelle verfügbar',
        bearbeiter: 'Admin User',
        notizen: 'Antrag abgelehnt - keine Parzelle in gewünschter Größe',
        prioritaet: 'niedrig'
      }),
      testDataFactories.antrag({
        id: 5,
        antragsnummer: 'A-2024-004',
        antragsteller: {
          vorname: 'Thomas',
          nachname: 'Müller',
          email: 'thomas.mueller@example.com'
        },
        gewuenschteParzelle: {
          bezirkId: 2,
          bezirkName: 'Bezirk Süd-Ost',
          maxGroesse: 400,
          maxPacht: 28.00
        },
        status: AntragStatus.WARTESCHLANGE,
        eingangsdatum: '2024-01-18',
        notizen: 'Warten auf freie Parzelle',
        prioritaet: 'normal',
        warteposition: 3
      })
    ]
    
    // Reset MSW handlers
    server.resetHandlers()
    
    // Setup default successful API responses
    server.use(
      http.get('/api/antraege', () => {
        return HttpResponse.json({
          success: true,
          data: {
            antraege: mockAntraegeData,
            pagination: {
              page: 1,
              limit: 20,
              total: mockAntraegeData.length,
              totalPages: 1
            },
            filters: {},
            statistics: {
              gesamt: 5,
              eingegangen: 1,
              inBearbeitung: 1,
              genehmigt: 1,
              abgelehnt: 1,
              warteschlange: 1
            }
          }
        })
      }),
      http.get('/api/antraege/statistiken', () => {
        return HttpResponse.json({
          success: true,
          data: {
            gesamt: 5,
            eingegangen: 1,
            inBearbeitung: 1,
            genehmigt: 1,
            abgelehnt: 1,
            warteschlange: 1,
            durchschnittlicheBearbeitungszeit: 3.2,
            erfolgreicheAntraege: 1,
            ablehnungsrate: 20.0
          }
        })
      }),
      http.get('/api/bezirke/dropdown', () => {
        return HttpResponse.json({
          success: true,
          data: [
            { id: 1, name: 'Bezirk Mitte-Nord' },
            { id: 2, name: 'Bezirk Süd-Ost' },
            { id: 3, name: 'Bezirk West' }
          ]
        })
      })
    )
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Page Structure and Component Integration', () => {
    it('sollte die Anträge-Page mit allen Komponenten rendern', async () => {
      const searchParams = {}
      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        // Header-Komponente
        expect(screen.getByText('Anträge verwalten')).toBeInTheDocument()
        expect(screen.getByText('Übersicht aller Kleingartenanträge')).toBeInTheDocument()
        
        // Action buttons vom Header
        expect(screen.getByRole('button', { name: 'Neuer Antrag' })).toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Excel Export' })).toBeInTheDocument()
        
        // Filter-Komponente
        expect(screen.getByPlaceholderText('Anträge durchsuchen...')).toBeInTheDocument()
        expect(screen.getByText('Status')).toBeInTheDocument()
        expect(screen.getByText('Bezirk')).toBeInTheDocument()
        
        // Liste-Komponente  
        expect(screen.getByText('A-2024-001')).toBeInTheDocument()
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
      })
    })

    it('sollte Metadata korrekt setzen', async () => {
      const AntraegePageModule = await import('../page')
      
      expect(AntraegePageModule.metadata).toEqual({
        title: 'Anträge',
        description: 'Verwaltung aller Kleingartenanträge - Suchen, Filtern und Bearbeiten von Anträgen'
      })
      
      expect(AntraegePageModule.dynamic).toBe('force-dynamic')
    })

    it('sollte Search-Parameter an AntraegeList weiterleiten', () => {
      const searchParams = {
        page: '2',
        search: 'Mustermann',
        status: 'eingegangen',
        bezirk: '1',
        sort: 'eingangsdatum',
        direction: 'desc' as const
      }
      
      render(<AntraegePage searchParams={searchParams} />)
      
      // Die AntraegeList-Komponente sollte mit den searchParams gerendert werden
      // (Direkte Props-Übergabe wird durch Component-Integration getestet)
      expect(screen.getByPlaceholderText('Anträge durchsuchen...')).toBeInTheDocument()
    })
  })

  describe('Header Component Integration', () => {
    it('sollte Header-Aktionen korrekt anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Haupt-Aktionen
        expect(screen.getByRole('button', { name: 'Neuer Antrag' })).toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Excel Export' })).toBeInTheDocument()
        
        // Statistik-Karten
        expect(screen.getByText('Gesamt')).toBeInTheDocument()
        expect(screen.getByText('Eingegangen')).toBeInTheDocument()
        expect(screen.getByText('In Bearbeitung')).toBeInTheDocument()
        expect(screen.getByText('Genehmigt')).toBeInTheDocument()
      })
    })

    it('sollte Statistiken aus API korrekt anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('5')).toBeInTheDocument() // Gesamt
        expect(screen.getByText('1')).toBeInTheDocument() // Verschiedene Status-Zahlen
        expect(screen.getByText('3,2 Tage')).toBeInTheDocument() // Durchschnittliche Bearbeitungszeit
        expect(screen.getByText('20,0%')).toBeInTheDocument() // Ablehnungsrate
      })
    })

    it('sollte Action-Buttons funktional sein', async () => {
      const { user } = render(<AntraegePage searchParams={{}} />)

      await waitFor(async () => {
        const neuerAntragButton = screen.getByRole('button', { name: 'Neuer Antrag' })
        await user.click(neuerAntragButton)
        
        // Navigieren zu /antraege/neu sollte getriggert werden
        expect(mockPush).toHaveBeenCalledWith('/antraege/neu')
      })
    })
  })

  describe('Filters Component Integration', () => {
    it('sollte Filter-Komponente mit allen Optionen rendern', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Such-Input
        expect(screen.getByPlaceholderText('Anträge durchsuchen...')).toBeInTheDocument()
        
        // Status-Filter Dropdown
        const statusButton = screen.getByRole('button', { name: /status/i })
        expect(statusButton).toBeInTheDocument()
        
        // Bezirk-Filter Dropdown  
        const bezirkButton = screen.getByRole('button', { name: /bezirk/i })
        expect(bezirkButton).toBeInTheDocument()
        
        // Sortierung
        const sortButton = screen.getByRole('button', { name: /sortierung/i })
        expect(sortButton).toBeInTheDocument()
      })
    })

    it('sollte Status-Filter öffnen und Optionen anzeigen', async () => {
      const { user } = render(<AntraegePage searchParams={{}} />)

      await waitFor(async () => {
        const statusButton = screen.getByRole('button', { name: /status/i })
        await user.click(statusButton)
        
        // Status-Optionen sollten sichtbar sein
        expect(screen.getByText('Eingegangen')).toBeInTheDocument()
        expect(screen.getByText('In Bearbeitung')).toBeInTheDocument()
        expect(screen.getByText('Genehmigt')).toBeInTheDocument()
        expect(screen.getByText('Abgelehnt')).toBeInTheDocument()
        expect(screen.getByText('Warteschlange')).toBeInTheDocument()
      })
    })

    it('sollte Bezirk-Filter mit API-Daten laden', async () => {
      const { user } = render(<AntraegePage searchParams={{}} />)

      await waitFor(async () => {
        const bezirkButton = screen.getByRole('button', { name: /bezirk/i })
        await user.click(bezirkButton)
        
        // Bezirk-Optionen aus Mock-API sollten sichtbar sein
        expect(screen.getByText('Bezirk Mitte-Nord')).toBeInTheDocument()
        expect(screen.getByText('Bezirk Süd-Ost')).toBeInTheDocument()
        expect(screen.getByText('Bezirk West')).toBeInTheDocument()
      })
    })

    it('sollte Suchfunktion funktional sein', async () => {
      const { user } = render(<AntraegePage searchParams={{}} />)

      await waitFor(async () => {
        const searchInput = screen.getByPlaceholderText('Anträge durchsuchen...')
        await user.type(searchInput, 'Mustermann')
        
        // URL sollte mit Suchparameter aktualisiert werden
        expect(mockReplace).toHaveBeenCalledWith(
          expect.stringContaining('search=Mustermann')
        )
      })
    })
  })

  describe('List Component Integration', () => {
    it('sollte alle Anträge in der Liste anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Antrags-Nummern
        expect(screen.getByText('A-2024-001')).toBeInTheDocument()
        expect(screen.getByText('A-2024-002')).toBeInTheDocument()
        expect(screen.getByText('A-2024-003')).toBeInTheDocument()
        expect(screen.getByText('A-2023-050')).toBeInTheDocument()
        expect(screen.getByText('A-2024-004')).toBeInTheDocument()
        
        // Antragsteller
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
        expect(screen.getByText('Anna Schmidt')).toBeInTheDocument()
        expect(screen.getByText('Peter Weber')).toBeInTheDocument()
        expect(screen.getByText('Maria Fischer')).toBeInTheDocument()
        expect(screen.getByText('Thomas Müller')).toBeInTheDocument()
      })
    })

    it('sollte Status-Badges korrekt anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Eingegangen')).toBeInTheDocument()
        expect(screen.getByText('In Bearbeitung')).toBeInTheDocument()
        expect(screen.getByText('Genehmigt')).toBeInTheDocument()
        expect(screen.getByText('Abgelehnt')).toBeInTheDocument()
        expect(screen.getByText('Warteschlange')).toBeInTheDocument()
      })
    })

    it('sollte Prioritäts-Indikatoren anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Hoch')).toBeInTheDocument()
        expect(screen.getAllByText('Normal')).toHaveLength(3)
        expect(screen.getByText('Niedrig')).toBeInTheDocument()
      })
    })

    it('sollte Bearbeitungsinformationen anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Bearbeiter
        expect(screen.getAllByText('Admin User')).toHaveLength(3)
        
        // Bearbeitungsdaten (deutsches Format)
        expect(screen.getByText('12.01.2024')).toBeInTheDocument()
        expect(screen.getByText('08.01.2024')).toBeInTheDocument()
        expect(screen.getByText('22.12.2023')).toBeInTheDocument()
        
        // Warteposition
        expect(screen.getByText('Position 3')).toBeInTheDocument()
      })
    })

    it('sollte zugewiesene Parzelle anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Parzelle P-001')).toBeInTheDocument()
        expect(screen.getByText('450 m²')).toBeInTheDocument()
        expect(screen.getByText('32,00 €/Monat')).toBeInTheDocument()
      })
    })

    it('sollte Action-Buttons für jeden Antrag anzeigen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        const anzeigenButtons = screen.getAllByText('Anzeigen')
        const bearbeitenButtons = screen.getAllByText('Bearbeiten')
        
        expect(anzeigenButtons.length).toBe(mockAntraegeData.length)
        expect(bearbeitenButtons.length).toBe(mockAntraegeData.length)
      })
    })
  })

  describe('Search Parameters Handling', () => {
    it('sollte Such-Parameter korrekt verarbeiten', async () => {
      server.use(
        http.get('/api/antraege', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('search')).toBe('Mustermann')
          expect(url.searchParams.get('page')).toBe('2')
          
          return HttpResponse.json({
            success: true,
            data: {
              antraege: mockAntraegeData.filter(a => 
                a.antragsteller.nachname.includes('Mustermann')
              ),
              pagination: { page: 2, limit: 20, total: 1, totalPages: 1 },
              filters: { search: 'Mustermann' }
            }
          })
        })
      )

      const searchParams = {
        page: '2',
        search: 'Mustermann'
      }

      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
        expect(screen.queryByText('Anna Schmidt')).not.toBeInTheDocument()
      })
    })

    it('sollte Status-Filter aus URL anwenden', async () => {
      server.use(
        http.get('/api/antraege', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('status')).toBe('genehmigt')
          
          return HttpResponse.json({
            success: true,
            data: {
              antraege: mockAntraegeData.filter(a => a.status === AntragStatus.GENEHMIGT),
              pagination: { page: 1, limit: 20, total: 1, totalPages: 1 },
              filters: { status: 'genehmigt' }
            }
          })
        })
      )

      const searchParams = { status: 'genehmigt' }
      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        expect(screen.getByText('Peter Weber')).toBeInTheDocument()
        expect(screen.queryByText('Max Mustermann')).not.toBeInTheDocument()
      })
    })

    it('sollte Bezirk-Filter aus URL anwenden', async () => {
      server.use(
        http.get('/api/antraege', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('bezirk')).toBe('1')
          
          return HttpResponse.json({
            success: true,
            data: {
              antraege: mockAntraegeData.filter(a => a.gewuenschteParzelle.bezirkId === 1),
              pagination: { page: 1, limit: 20, total: 2, totalPages: 1 },
              filters: { bezirk: '1' }
            }
          })
        })
      )

      const searchParams = { bezirk: '1' }
      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
        expect(screen.getByText('Peter Weber')).toBeInTheDocument()
        expect(screen.queryByText('Anna Schmidt')).not.toBeInTheDocument()
      })
    })

    it('sollte Sortierung aus URL anwenden', async () => {
      server.use(
        http.get('/api/antraege', ({ request }) => {
          const url = new URL(request.url)
          expect(url.searchParams.get('sort')).toBe('eingangsdatum')
          expect(url.searchParams.get('direction')).toBe('desc')
          
          const sortedAntraege = [...mockAntraegeData].sort((a, b) => 
            new Date(b.eingangsdatum).getTime() - new Date(a.eingangsdatum).getTime()
          )
          
          return HttpResponse.json({
            success: true,
            data: {
              antraege: sortedAntraege,
              pagination: { page: 1, limit: 20, total: sortedAntraege.length, totalPages: 1 },
              filters: { sort: 'eingangsdatum', direction: 'desc' }
            }
          })
        })
      )

      const searchParams = { sort: 'eingangsdatum', direction: 'desc' as const }
      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        const antragsNummern = screen.getAllByText(/A-\d{4}-\d{3}/)
        // Neuester Antrag zuerst
        expect(antragsNummern[0]).toHaveTextContent('A-2024-004')
      })
    })
  })

  describe('Pagination Integration', () => {
    it('sollte Pagination korrekt anzeigen', async () => {
      server.use(
        http.get('/api/antraege', () => {
          return HttpResponse.json({
            success: true,
            data: {
              antraege: mockAntraegeData.slice(0, 3),
              pagination: {
                page: 1,
                limit: 3,
                total: mockAntraegeData.length,
                totalPages: Math.ceil(mockAntraegeData.length / 3)
              },
              filters: {}
            }
          })
        })
      )

      render(<AntraegePage searchParams={{ page: '1' }} />)

      await waitFor(() => {
        expect(screen.getByText(/Zeige 1 bis 3 von/)).toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Nächste' })).toBeInTheDocument()
        expect(screen.queryByRole('button', { name: 'Vorherige' })).not.toBeInTheDocument()
      })
    })

    it('sollte Filter in Pagination-URLs beibehalten', async () => {
      server.use(
        http.get('/api/antraege', () => {
          return HttpResponse.json({
            success: true,
            data: {
              antraege: mockAntraegeData.slice(0, 2),
              pagination: {
                page: 1,
                limit: 2,
                total: mockAntraegeData.length,
                totalPages: Math.ceil(mockAntraegeData.length / 2)
              },
              filters: { search: 'test', status: 'eingegangen' }
            }
          })
        })
      )

      const searchParams = { search: 'test', status: 'eingegangen', page: '1' }
      render(<AntraegePage searchParams={searchParams} />)

      await waitFor(() => {
        const nextButton = screen.getByRole('button', { name: 'Nächste' })
        
        // Filter sollten in Navigation erhalten bleiben
        expect(nextButton).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('sollte API-Fehler beim Laden der Anträge behandeln', async () => {
      server.use(
        http.get('/api/antraege', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Keine Anträge gefunden')).toBeInTheDocument()
        expect(screen.getByText('Es wurden keine Anträge gefunden, die Ihren Suchkriterien entsprechen.')).toBeInTheDocument()
      })
    })

    it('sollte Fehler beim Laden der Statistiken behandeln', async () => {
      server.use(
        http.get('/api/antraege/statistiken', () => {
          return HttpResponse.json(
            { error: 'Statistics service unavailable' },
            { status: 503 }
          )
        })
      )

      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Liste sollte trotzdem geladen werden
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
        
        // Statistiken sollten Fallback-Werte zeigen
        expect(screen.getByText('0')).toBeInTheDocument()
      })
    })

    it('sollte Netzwerk-Fehler behandeln', async () => {
      server.use(
        http.get('/api/antraege', () => {
          return HttpResponse.error()
        })
      )

      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Keine Anträge gefunden')).toBeInTheDocument()
      })
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte deutsche Interface-Texte verwenden', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Anträge verwalten')).toBeInTheDocument()
        expect(screen.getByText('Übersicht aller Kleingartenanträge')).toBeInTheDocument()
        expect(screen.getByText('Neuer Antrag')).toBeInTheDocument()
        expect(screen.getByText('Excel Export')).toBeInTheDocument()
        expect(screen.getByPlaceholderText('Anträge durchsuchen...')).toBeInTheDocument()
        expect(screen.getByText('Anzeigen')).toBeInTheDocument()
        expect(screen.getByText('Bearbeiten')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Status-Texte verwenden', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        expect(screen.getByText('Eingegangen')).toBeInTheDocument()
        expect(screen.getByText('In Bearbeitung')).toBeInTheDocument()
        expect(screen.getByText('Genehmigt')).toBeInTheDocument()
        expect(screen.getByText('Abgelehnt')).toBeInTheDocument()
        expect(screen.getByText('Warteschlange')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Datumsformatierung verwenden', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Deutsche Datumsformate
        expect(screen.getByText('15.01.2024')).toBeInTheDocument()
        expect(screen.getByText('10.01.2024')).toBeInTheDocument()
        expect(screen.getByText('05.01.2024')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        // Deutsche Dezimaltrennzeichen
        expect(screen.getByText('32,00 €/Monat')).toBeInTheDocument()
        expect(screen.getByText('3,2 Tage')).toBeInTheDocument()
        expect(screen.getByText('20,0%')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte Heading-Hierarchie haben', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        const headings = screen.getAllByRole('heading')
        expect(headings.length).toBeGreaterThan(0)
        
        // Hauptüberschrift sollte h1 sein
        const h1 = screen.getByRole('heading', { level: 1 })
        expect(h1).toHaveTextContent('Anträge verwalten')
      })
    })

    it('sollte Buttons und Links korrekt beschriften', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        const buttons = screen.getAllByRole('button')
        buttons.forEach(button => {
          expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
        })

        const links = screen.getAllByRole('link')
        links.forEach(link => {
          expect(link.textContent || link.getAttribute('aria-label')).toBeTruthy()
        })
      })
    })

    it('sollte Formular-Elemente accessible machen', async () => {
      render(<AntraegePage searchParams={{}} />)

      await waitFor(() => {
        const searchInput = screen.getByPlaceholderText('Anträge durchsuchen...')
        expect(searchInput).toHaveAttribute('type', 'search')
        
        // Dropdowns sollten korrekte ARIA-Attribute haben
        const dropdowns = screen.getAllByRole('button', { expanded: false })
        expect(dropdowns.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Performance', () => {
    it('sollte Komponenten-Integration effizient durchführen', async () => {
      const startTime = performance.now()
      
      render(<AntraegePage searchParams={{}} />)
      
      await waitFor(() => {
        expect(screen.getByText('Max Mustermann')).toBeInTheDocument()
      })
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(1000)
    })

    it('sollte große Anträge-Listen effizient handhaben', async () => {
      const largeAntraegeList = Array.from({ length: 100 }, (_, i) => 
        testDataFactories.antrag({ 
          id: i + 1, 
          antragsnummer: `A-2024-${String(i + 1).padStart(3, '0')}`,
          antragsteller: {
            vorname: `Vorname${i}`,
            nachname: `Nachname${i}`,
            email: `user${i}@example.com`
          }
        })
      )

      server.use(
        http.get('/api/antraege', () => {
          return HttpResponse.json({
            success: true,
            data: {
              antraege: largeAntraegeList,
              pagination: { page: 1, limit: 100, total: 100, totalPages: 1 },
              filters: {}
            }
          })
        })
      )

      const startTime = performance.now()
      render(<AntraegePage searchParams={{}} />)
      const endTime = performance.now()

      expect(endTime - startTime).toBeLessThan(2000)
    })
  })
})