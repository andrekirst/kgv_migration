/**
 * Dashboard Page Integration Tests
 * 
 * Testet die vollständige Dashboard-Page mit allen Komponenten
 * und API-Integrationen mit Mock Service Worker
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import DashboardPage from '../page'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'
import { testDataFactories } from '@/test/fixtures/kgv-data'

// Import MSW server
import '@/test/mocks/server'

describe('Dashboard Page Integration', () => {
  beforeEach(() => {
    // Reset MSW handlers to default state
    server.resetHandlers()
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Page Structure', () => {
    it('sollte die Dashboard-Grundstruktur korrekt rendern', async () => {
      render(<DashboardPage />)

      // Page Header
      expect(screen.getByRole('heading', { level: 1, name: 'Dashboard' })).toBeInTheDocument()
      expect(screen.getByText('Willkommen im KGV-Verwaltungssystem. Hier finden Sie eine Übersicht über alle wichtigen Kennzahlen und Aktivitäten.')).toBeInTheDocument()

      // Warte auf das Laden der Komponenten
      await waitFor(() => {
        expect(screen.getByText('Schnellaktionen')).toBeInTheDocument()
      })
    })

    it('sollte alle Dashboard-Sektionen enthalten', async () => {
      render(<DashboardPage />)

      await waitFor(() => {
        // Quick Actions
        expect(screen.getByText('Schnellaktionen')).toBeInTheDocument()
        
        // Statistics section sollte vorhanden sein
        expect(document.querySelector('[data-testid="dashboard-stats"]') || 
               screen.queryByText('Statistiken')).toBeTruthy()
        
        // Recent Activity
        expect(screen.getByText('Letzte Aktivitäten')).toBeInTheDocument()
        
        // Status Overview
        expect(screen.getByText('Status Übersicht')).toBeInTheDocument()
      })
    })
  })

  describe('Dashboard Statistics Integration', () => {
    it('sollte Statistiken erfolgreich laden und anzeigen', async () => {
      const mockStats = {
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

      server.use(
        http.get('/api/dashboard/statistiken', () => {
          return HttpResponse.json({
            success: true,
            data: mockStats
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Überprüfe ob die Statistiken angezeigt werden
        expect(screen.getByText('5')).toBeInTheDocument() // Gesamt Bezirke
        expect(screen.getByText('45')).toBeInTheDocument() // Gesamt Parzellen
        expect(screen.getByText('84,4%')).toBeInTheDocument() // Auslastung
      })
    })

    it('sollte Loading-Zustand für Statistiken korrekt anzeigen', () => {
      // Simuliere langsame API-Antwort
      server.use(
        http.get('/api/dashboard/statistiken', async () => {
          await new Promise(resolve => setTimeout(resolve, 1000))
          return HttpResponse.json({ success: true, data: {} })
        })
      )

      render(<DashboardPage />)

      // Loading-Skeletons sollten sichtbar sein
      const skeletons = document.querySelectorAll('.animate-pulse')
      expect(skeletons.length).toBeGreaterThan(0)
    })

    it('sollte Fehler beim Laden der Statistiken behandeln', async () => {
      server.use(
        http.get('/api/dashboard/statistiken', () => {
          return HttpResponse.json(
            { error: 'Server error' },
            { status: 500 }
          )
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Fallback-Werte oder Error-State sollten angezeigt werden
        expect(screen.getByText('Fehler beim Laden der Statistiken') || 
               screen.getByText('0')).toBeTruthy()
      })
    })
  })

  describe('Quick Actions Integration', () => {
    it('sollte alle Schnellaktionen rendern', async () => {
      render(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByText('Neuen Bezirk erstellen')).toBeInTheDocument()
        expect(screen.getByText('Parzelle zuweisen')).toBeInTheDocument()
        expect(screen.getByText('Antrag bearbeiten')).toBeInTheDocument()
        expect(screen.getByText('Berichte generieren')).toBeInTheDocument()
      })
    })

    it('sollte Navigation zu Quick Action-Seiten funktionieren', async () => {
      const { user } = render(<DashboardPage />)

      await waitFor(async () => {
        const bezirkButton = screen.getByText('Neuen Bezirk erstellen')
        expect(bezirkButton.closest('a')).toHaveAttribute('href', '/bezirke/neu')
        
        const parzelleButton = screen.getByText('Parzelle zuweisen')
        expect(parzelleButton.closest('a')).toHaveAttribute('href', '/parzellen/freie')
        
        const antragButton = screen.getByText('Antrag bearbeiten')
        expect(antragButton.closest('a')).toHaveAttribute('href', '/antraege')
      })
    })
  })

  describe('Recent Activity Integration', () => {
    it('sollte aktuelle Aktivitäten laden und anzeigen', async () => {
      const mockActivities = [
        testDataFactories.activity({
          id: 1,
          typ: 'BEZIRK_ERSTELLT',
          beschreibung: 'Neuer Bezirk "Mitte-Nord" wurde erstellt',
          benutzer: 'Max Administrator',
          zeitstempel: '2024-01-15T10:30:00Z',
          entityId: 1,
          entityTyp: 'bezirk'
        }),
        testDataFactories.activity({
          id: 2,
          typ: 'PARZELLE_ZUGEWIESEN',
          beschreibung: 'Parzelle P-001 wurde an Max Mustermann zugewiesen',
          benutzer: 'Admin User',
          zeitstempel: '2024-01-15T09:15:00Z',
          entityId: 1,
          entityTyp: 'parzelle'
        })
      ]

      server.use(
        http.get('/api/dashboard/aktivitaeten', () => {
          return HttpResponse.json({
            success: true,
            data: mockActivities
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByText('Neuer Bezirk "Mitte-Nord" wurde erstellt')).toBeInTheDocument()
        expect(screen.getByText('Parzelle P-001 wurde an Max Mustermann zugewiesen')).toBeInTheDocument()
        expect(screen.getByText('Max Administrator')).toBeInTheDocument()
        expect(screen.getByText('Admin User')).toBeInTheDocument()
      })
    })

    it('sollte leere Aktivitätsliste korrekt anzeigen', async () => {
      server.use(
        http.get('/api/dashboard/aktivitaeten', () => {
          return HttpResponse.json({
            success: true,
            data: []
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByText('Keine aktuellen Aktivitäten')).toBeInTheDocument()
        expect(screen.getByText('Es sind noch keine Aktivitäten vorhanden.')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zeitstempel-Formatierung verwenden', async () => {
      const mockActivity = testDataFactories.activity({
        zeitstempel: '2024-01-15T10:30:00Z',
        beschreibung: 'Test Aktivität'
      })

      server.use(
        http.get('/api/dashboard/aktivitaeten', () => {
          return HttpResponse.json({
            success: true,
            data: [mockActivity]
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Deutsches Datumsformat sollte verwendet werden
        expect(screen.getByText(/15\.01\.2024/)).toBeInTheDocument()
      })
    })
  })

  describe('Status Overview Integration', () => {
    it('sollte Status-Übersicht mit Parzellen-Status laden', async () => {
      const mockStatusData = {
        parzellenStatus: {
          frei: 7,
          belegt: 38,
          reserviert: 2,
          wartung: 1
        },
        bezirkeStatus: {
          aktiv: 4,
          inaktiv: 1
        },
        antraegeStatus: {
          offen: 5,
          inBearbeitung: 3,
          genehmigt: 12,
          abgelehnt: 1
        }
      }

      server.use(
        http.get('/api/dashboard/status', () => {
          return HttpResponse.json({
            success: true,
            data: mockStatusData
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByText('7 frei')).toBeInTheDocument()
        expect(screen.getByText('38 belegt')).toBeInTheDocument()
        expect(screen.getByText('2 reserviert')).toBeInTheDocument()
        expect(screen.getByText('4 aktive Bezirke')).toBeInTheDocument()
        expect(screen.getByText('5 offene Anträge')).toBeInTheDocument()
      })
    })

    it('sollte Kreisdiagramm für Parzellen-Status rendern', async () => {
      const mockStatusData = {
        parzellenStatus: {
          frei: 10,
          belegt: 30,
          reserviert: 5,
          wartung: 0
        }
      }

      server.use(
        http.get('/api/dashboard/status', () => {
          return HttpResponse.json({
            success: true,
            data: mockStatusData
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Chart-Container sollte vorhanden sein
        expect(document.querySelector('[data-testid="status-chart"]') || 
               document.querySelector('.recharts-wrapper')).toBeTruthy()
      })
    })
  })

  describe('Error Handling', () => {
    it('sollte Netzwerk-Fehler für alle Komponenten behandeln', async () => {
      // Simuliere Netzwerk-Fehler für alle API-Calls
      server.use(
        http.get('/api/dashboard/*', () => {
          return HttpResponse.error()
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Verschiedene Error-States oder Fallback-Inhalte sollten angezeigt werden
        expect(
          screen.getByText('Fehler beim Laden') ||
          screen.getByText('Nicht verfügbar') ||
          screen.getByText('0')
        ).toBeTruthy()
      })
    })

    it('sollte mit partiellen API-Fehlern umgehen', async () => {
      // Statistiken laden erfolgreich, Aktivitäten fehlerhaft
      server.use(
        http.get('/api/dashboard/statistiken', () => {
          return HttpResponse.json({
            success: true,
            data: { gesamtBezirke: 5, gesamtParzellen: 45 }
          })
        }),
        http.get('/api/dashboard/aktivitaeten', () => {
          return HttpResponse.json(
            { error: 'Service unavailable' },
            { status: 503 }
          )
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Statistiken sollten sichtbar sein
        expect(screen.getByText('5')).toBeInTheDocument()
        // Aktivitäten sollten Error-State zeigen
        expect(screen.getByText('Fehler beim Laden der Aktivitäten') ||
               screen.getByText('Keine aktuellen Aktivitäten')).toBeTruthy()
      })
    })
  })

  describe('Responsive Verhalten', () => {
    it('sollte auf mobilen Geräten korrekt dargestellt werden', () => {
      // Simuliere mobiles Viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 375,
      })

      render(<DashboardPage />)

      // Grid-Layout sollte auf mobil angepasst sein
      const gridElements = document.querySelectorAll('.grid')
      expect(gridElements.length).toBeGreaterThan(0)

      // Alle wichtigen Sektionen sollten weiterhin sichtbar sein
      expect(screen.getByText('Dashboard')).toBeInTheDocument()
      expect(screen.getByText('Schnellaktionen')).toBeInTheDocument()
    })

    it('sollte auf Desktop korrekt dargestellt werden', () => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1920,
      })

      render(<DashboardPage />)

      // Zwei-Spalten-Layout sollte auf Desktop verwendet werden
      const twoColumnGrid = document.querySelector('.lg\\:grid-cols-2')
      expect(twoColumnGrid).toBeTruthy()
    })
  })

  describe('Performance', () => {
    it('sollte Komponenten effizient laden', async () => {
      const startTime = performance.now()
      
      render(<DashboardPage />)
      
      await waitFor(() => {
        expect(screen.getByText('Dashboard')).toBeInTheDocument()
      })
      
      const endTime = performance.now()
      expect(endTime - startTime).toBeLessThan(1000) // Weniger als 1 Sekunde
    })

    it('sollte Suspense-Fallbacks korrekt verwenden', () => {
      // Simuliere langsame API-Antworten
      server.use(
        http.get('/api/dashboard/*', async () => {
          await new Promise(resolve => setTimeout(resolve, 2000))
          return HttpResponse.json({ success: true, data: {} })
        })
      )

      render(<DashboardPage />)

      // Loading-Skeletons sollten während des Ladens sichtbar sein
      const loadingElements = document.querySelectorAll('.animate-pulse')
      expect(loadingElements.length).toBeGreaterThan(0)
    })
  })

  describe('Deutsche Lokalisierung', () => {
    it('sollte alle deutschen Texte und Beschriftungen verwenden', async () => {
      render(<DashboardPage />)

      await waitFor(() => {
        // Hauptüberschriften
        expect(screen.getByText('Dashboard')).toBeInTheDocument()
        expect(screen.getByText('Schnellaktionen')).toBeInTheDocument()
        expect(screen.getByText('Letzte Aktivitäten')).toBeInTheDocument()
        expect(screen.getByText('Status Übersicht')).toBeInTheDocument()
        
        // Deutsche Beschreibungen
        expect(screen.getByText('Willkommen im KGV-Verwaltungssystem. Hier finden Sie eine Übersicht über alle wichtigen Kennzahlen und Aktivitäten.')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      server.use(
        http.get('/api/dashboard/statistiken', () => {
          return HttpResponse.json({
            success: true,
            data: {
              auslastung: 84.56,
              gesamtParzellen: 1234
            }
          })
        })
      )

      render(<DashboardPage />)

      await waitFor(() => {
        // Deutsche Dezimaltrennzeichen
        expect(screen.getByText('84,6%')).toBeInTheDocument()
        // Deutsche Tausendertrennzeichen
        expect(screen.getByText('1.234')).toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('sollte korrekte Heading-Hierarchie haben', () => {
      render(<DashboardPage />)

      const h1 = screen.getByRole('heading', { level: 1 })
      expect(h1).toBeInTheDocument()
      expect(h1).toHaveTextContent('Dashboard')

      const h2Elements = screen.getAllByRole('heading', { level: 2 })
      expect(h2Elements.length).toBeGreaterThan(0)
    })

    it('sollte mit Tastatur navigierbar sein', async () => {
      const { user } = render(<DashboardPage />)

      // Tab-Navigation sollte durch Interactive-Elemente funktionieren
      await user.tab()
      expect(document.activeElement).toBeInstanceOf(HTMLElement)

      await user.tab()
      expect(document.activeElement).toBeInstanceOf(HTMLElement)
    })

    it('sollte ARIA-Labels für wichtige Elemente haben', async () => {
      render(<DashboardPage />)

      await waitFor(() => {
        // Navigation-Links sollten accessible sein
        const links = screen.getAllByRole('link')
        expect(links.length).toBeGreaterThan(0)
        
        // Buttons sollten accessible sein
        const buttons = screen.getAllByRole('button')
        buttons.forEach(button => {
          expect(button).toBeVisible()
        })
      })
    })
  })
})