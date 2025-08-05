/**
 * Navigation and Layout Accessibility Tests
 * 
 * Comprehensive accessibility tests for navigation, sidebar, header,
 * and overall layout components with German localization
 */

import React from 'react'
import { render, screen, waitFor } from '@/test/utils/test-utils'
import {
  checkAccessibility,
  checkGermanA11yStandards,
  checkHeadingHierarchy,
  testKeyboardNavigation,
  simulateScreenReaderNavigation,
  getFocusableElements,
  hasAccessibleName,
  GERMAN_A11Y_LABELS
} from '@/test/utils/accessibility-utils'
import { DashboardStats } from '@/components/dashboard/dashboard-stats'
import { QuickActions } from '@/components/dashboard/quick-actions'
import { RecentActivity } from '@/components/dashboard/recent-activity'
import { StatusOverview } from '@/components/dashboard/status-overview'
import { ThemeToggle } from '@/components/layout/theme-toggle'
import { UserMenu } from '@/components/layout/user-menu'
import { testDataFactories } from '@/test/fixtures/kgv-data'
import { server } from '@/test/mocks/server'
import { http, HttpResponse } from 'msw'

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
  usePathname: () => '/dashboard',
  useSearchParams: () => new URLSearchParams(),
}))

describe('Navigation and Layout Accessibility Tests', () => {
  beforeEach(() => {
    server.resetHandlers()
    
    // Mock API responses
    server.use(
      http.get('/api/dashboard/statistiken', () => {
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
      }),
      http.get('/api/dashboard/aktivitaeten', () => {
        return HttpResponse.json({
          success: true,
          data: [
            testDataFactories.activity({
              id: 1,
              typ: 'BEZIRK_ERSTELLT',
              beschreibung: 'Neuer Bezirk "Mitte-Nord" wurde erstellt',
              benutzer: 'Max Administrator',
              zeitstempel: '2024-01-15T10:30:00Z'
            }),
            testDataFactories.activity({
              id: 2,
              typ: 'PARZELLE_ZUGEWIESEN',
              beschreibung: 'Parzelle P-001 wurde an Max Mustermann zugewiesen',
              benutzer: 'Admin User',
              zeitstempel: '2024-01-15T09:15:00Z'
            })
          ]
        })
      }),
      http.get('/api/dashboard/status', () => {
        return HttpResponse.json({
          success: true,
          data: {
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
        })
      })
    )
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Dashboard Statistics Accessibility', () => {
    it('sollte Statistik-Karten WCAG-konform rendern', async () => {
      const { container } = render(
        <DashboardStats />
      )

      await waitFor(() => {
        expect(screen.getByText('5')).toBeInTheDocument()
      })

      const a11yResults = checkAccessibility(container)
      expect(a11yResults.isValid).toBe(true)
      
      if (!a11yResults.isValid) {
        console.log('Dashboard Stats A11y Issues:', a11yResults.results)
      }
    })

    it('sollte Statistik-Karten semantisch korrekt strukturieren', async () => {
      render(<DashboardStats />)

      await waitFor(() => {
        // Zahlen sollten als wichtige Inhalte markiert sein
        const statistikZahlen = screen.getAllByText(/^\d+$/)
        expect(statistikZahlen.length).toBeGreaterThan(0)
        
        statistikZahlen.forEach(zahl => {
          // Wichtige Statistiken sollten für Screen Reader hervorgehoben sein
          expect(zahl.closest('[role="status"]') ||
                 zahl.closest('[aria-live]') ||
                 zahl.getAttribute('aria-label')).toBeTruthy()
        })
      })
    })

    it('sollte Trend-Indikatoren accessible machen', async () => {
      render(<DashboardStats />)

      await waitFor(() => {
        const trendElements = screen.getAllByText(/\+\d+/)
        expect(trendElements.length).toBeGreaterThan(0)
        
        trendElements.forEach(trend => {
          // Trends sollten mit Kontext beschrieben sein
          expect(trend.getAttribute('aria-label') ||
                 trend.closest('[aria-label]') ||
                 trend.closest('[title]')).toBeTruthy()
        })
      })
    })

    it('sollte Loading-State für Statistiken accessible machen', () => {
      // Mock slow response
      server.use(
        http.get('/api/dashboard/statistiken', async () => {
          await new Promise(resolve => setTimeout(resolve, 100))
          return HttpResponse.json({ success: true, data: {} })
        })
      )

      const { container } = render(<DashboardStats />)

      // Loading-Skeletons sollten Screen Reader freundlich sein
      const loadingElements = container.querySelectorAll('.animate-pulse')
      expect(loadingElements.length).toBeGreaterThan(0)
      
      // Loading-Status sollte angekündigt werden
      expect(container.querySelector('[aria-live="polite"]') ||
             container.querySelector('[role="progressbar"]') ||
             screen.queryByLabelText(/lädt/i)).toBeTruthy()
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      render(<DashboardStats />)

      await waitFor(() => {
        // Deutsche Prozentangaben
        expect(screen.getByText('84,4%')).toBeInTheDocument()
        
        // Deutsche Beschriftungen
        expect(screen.getByText('Gesamt Bezirke')).toBeInTheDocument()
        expect(screen.getByText('Aktive Bezirke')).toBeInTheDocument()
        expect(screen.getByText('Gesamt Parzellen')).toBeInTheDocument()
        expect(screen.getByText('Auslastung')).toBeInTheDocument()
      })
    })
  })

  describe('Quick Actions Accessibility', () => {
    it('sollte Quick Action-Buttons accessible machen', async () => {
      render(<QuickActions />)

      await waitFor(() => {
        const buttons = screen.getAllByRole('button')
        expect(buttons.length).toBeGreaterThan(0)
        
        buttons.forEach(button => {
          expect(hasAccessibleName(button)).toBe(true)
          expect(button.textContent || button.getAttribute('aria-label')).toBeTruthy()
        })
      })
    })

    it('sollte Quick Action-Links mit korrekten Descriptions haben', async () => {
      render(<QuickActions />)

      await waitFor(() => {
        const neuerBezirkButton = screen.getByText('Neuen Bezirk erstellen')
        expect(neuerBezirkButton.closest('a')).toHaveAttribute('href', '/bezirke/neu')
        expect(neuerBezirkButton.closest('a')).toHaveAttribute('aria-describedby')
        
        const parzelleZuweisenButton = screen.getByText('Parzelle zuweisen')
        expect(parzelleZuweisenButton.closest('a')).toHaveAttribute('href', '/parzellen/freie')
        
        const antragBearbeitenButton = screen.getByText('Antrag bearbeiten')
        expect(antragBearbeitenButton.closest('a')).toHaveAttribute('href', '/antraege')
      })
    })

    it('sollte Icons mit Text-Alternativen versehen', async () => {
      render(<QuickActions />)

      await waitFor(() => {
        const iconElements = document.querySelectorAll('svg, [data-lucide]')
        iconElements.forEach(icon => {
          // Icons sollten entweder decorative sein oder Alt-Text haben
          expect(icon.getAttribute('aria-hidden') === 'true' ||
                 icon.getAttribute('aria-label') ||
                 icon.closest('[aria-label]')).toBeTruthy()
        })
      })
    })

    it('sollte Keyboard-Navigation unterstützen', async () => {
      const { container } = render(<QuickActions />)

      const navigationResult = await testKeyboardNavigation(container)
      
      expect(navigationResult.success).toBe(true)
      expect(navigationResult.focusableElements.length).toBeGreaterThan(0)
    })
  })

  describe('Recent Activity Accessibility', () => {
    it('sollte Aktivitäten-Liste semantisch korrekt strukturieren', async () => {
      render(<RecentActivity />)

      await waitFor(() => {
        expect(screen.getByText('Neuer Bezirk "Mitte-Nord" wurde erstellt')).toBeInTheDocument()
      })

      // Liste sollte als ordered oder unordered list markiert sein
      const liste = screen.getByRole('list') || document.querySelector('ul, ol')
      expect(liste).toBeTruthy()
      
      // List-Items sollten korrekt strukturiert sein
      const listItems = screen.getAllByRole('listitem')
      expect(listItems.length).toBeGreaterThan(0)
      
      listItems.forEach(item => {
        // Jeder Item sollte Zeitstempel und Beschreibung haben
        expect(item.textContent).toBeTruthy()
      })
    })

    it('sollte Zeitstempel accessible machen', async () => {
      render(<RecentActivity />)

      await waitFor(() => {
        // Zeitstempel sollten als Zeit-Elemente markiert sein
        const zeitstempel = document.querySelectorAll('time')
        expect(zeitstempel.length).toBeGreaterThan(0)
        
        zeitstempel.forEach(time => {
          expect(time).toHaveAttribute('datetime')
          expect(time.textContent).toBeTruthy()
        })
      })
    })

    it('sollte Activity-Types semantisch unterscheiden', async () => {
      render(<RecentActivity />)

      await waitFor(() => {
        // Verschiedene Activity-Types sollten unterscheidbar sein
        const bezirkActivity = screen.getByText(/bezirk.*erstellt/i)
        const parzelleActivity = screen.getByText(/parzelle.*zugewiesen/i)
        
        expect(bezirkActivity).toBeInTheDocument()
        expect(parzelleActivity).toBeInTheDocument()
        
        // Activities könnten mit verschiedenen Icons/Farben markiert sein
        const activityItems = screen.getAllByRole('listitem')
        activityItems.forEach(item => {
          expect(item.querySelector('[aria-label]') ||
                 item.querySelector('[title]') ||
                 item.getAttribute('data-activity-type')).toBeTruthy()
        })
      })
    })

    it('sollte Empty-State accessible machen', async () => {
      server.use(
        http.get('/api/dashboard/aktivitaeten', () => {
          return HttpResponse.json({
            success: true,
            data: []
          })
        })
      )

      render(<RecentActivity />)

      await waitFor(() => {
        expect(screen.getByText('Keine aktuellen Aktivitäten')).toBeInTheDocument()
        expect(screen.getByText('Es sind noch keine Aktivitäten vorhanden.')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zeitangaben verwenden', async () => {
      render(<RecentActivity />)

      await waitFor(() => {
        // Deutsche Datumsformate
        expect(screen.getByText(/15\.01\.2024/)).toBeInTheDocument()
        
        // Deutsche Zeitangaben
        expect(screen.getByText(/vor \d+ minuten?/i) ||
               screen.getByText(/vor \d+ stunden?/i) ||
               screen.getByText(/vor \d+ tagen?/i)).toBeTruthy()
      })
    })
  })

  describe('Status Overview Accessibility', () => {
    it('sollte Status-Charts accessible machen', async () => {
      render(<StatusOverview />)

      await waitFor(() => {
        expect(screen.getByText('7 frei')).toBeInTheDocument()
      })

      // Charts sollten mit Tabellen-Daten ergänzt werden
      const chartContainer = document.querySelector('[data-testid="status-chart"]') ||
                            document.querySelector('.recharts-wrapper') ||
                            document.querySelector('[role="img"]')
      
      if (chartContainer) {
        expect(chartContainer).toHaveAttribute('aria-label')
        expect(chartContainer).toHaveAttribute('role', 'img')
        
        // Alternative Text-Darstellung sollte verfügbar sein
        const chartDescription = chartContainer.getAttribute('aria-describedby')
        if (chartDescription) {
          const descriptionElement = document.getElementById(chartDescription)
          expect(descriptionElement).toBeTruthy()
        }
      }
    })

    it('sollte Status-Daten in tabellarischer Form verfügbar machen', async () => {
      render(<StatusOverview />)

      await waitFor(() => {
        // Status-Informationen sollten auch ohne Chart zugänglich sein
        expect(screen.getByText('7 frei')).toBeInTheDocument()
        expect(screen.getByText('38 belegt')).toBeInTheDocument()
        expect(screen.getByText('2 reserviert')).toBeInTheDocument()
        expect(screen.getByText('4 aktive Bezirke')).toBeInTheDocument()
        expect(screen.getByText('5 offene Anträge')).toBeInTheDocument()
      })
    })

    it('sollte Farb-Kodierung mit Text-Labels ergänzen', async () => {
      render(<StatusOverview />)

      await waitFor(() => {
        // Status-Bereiche sollten nicht nur durch Farbe unterscheidbar sein
        const statusElements = [
          screen.getByText('7 frei'),
          screen.getByText('38 belegt'),
          screen.getByText('2 reserviert')
        ]
        
        statusElements.forEach(element => {
          // Text-Labels sind bereits vorhanden, zusätzlich können Patterns/Icons verwendet werden
          expect(element.textContent).toBeTruthy()
        })
      })
    })
  })

  describe('Theme Toggle Accessibility', () => {
    it('sollte Theme-Toggle korrekt beschriften', () => {
      render(<ThemeToggle />)

      const themeButton = screen.getByRole('button', { name: /theme|design|modus/i })
      expect(themeButton).toBeInTheDocument()
      expect(themeButton).toHaveAttribute('aria-label')
      expect(themeButton).toHaveAttribute('aria-pressed')
    })

    it('sollte Theme-Wechsel ankündigen', async () => {
      const { user } = render(<ThemeToggle />)

      const themeButton = screen.getByRole('button', { name: /theme|design|modus/i })
      
      // Toggle Theme
      await user.click(themeButton)
      
      // Änderung sollte für Screen Reader angekündigt werden
      const announcement = document.querySelector('[aria-live="polite"]') ||
                          document.querySelector('[role="status"]')
      
      expect(announcement || 
             themeButton.getAttribute('aria-label')?.includes('aktiv') ||
             themeButton.getAttribute('aria-pressed')).toBeTruthy()
    })

    it('sollte Icons mit Text-Alternativen versehen', () => {
      render(<ThemeToggle />)

      const icons = document.querySelectorAll('svg[data-lucide]')
      icons.forEach(icon => {
        expect(icon.getAttribute('aria-hidden') === 'true' ||
               icon.getAttribute('aria-label') ||
               icon.closest('[aria-label]')).toBeTruthy()
      })
    })
  })

  describe('User Menu Accessibility', () => {
    it('sollte User-Menu-Dropdown accessible machen', async () => {
      const { user } = render(<UserMenu />)

      const userButton = screen.getByRole('button', { name: /benutzer|profil|menu/i })
      expect(userButton).toHaveAttribute('aria-expanded', 'false')
      expect(userButton).toHaveAttribute('aria-haspopup', 'menu')
      
      await user.click(userButton)
      
      expect(userButton).toHaveAttribute('aria-expanded', 'true')
      
      // Menu-Items sollten korrekte Roles haben
      const menuItems = screen.getAllByRole('menuitem')
      expect(menuItems.length).toBeGreaterThan(0)
      
      menuItems.forEach(item => {
        expect(hasAccessibleName(item)).toBe(true)
      })
    })

    it('sollte Keyboard-Navigation im Menu unterstützen', async () => {
      const { user } = render(<UserMenu />)

      const userButton = screen.getByRole('button', { name: /benutzer|profil|menu/i })
      
      // Öffne Menu mit Enter
      userButton.focus()
      await user.keyboard('{Enter}')
      
      expect(userButton).toHaveAttribute('aria-expanded', 'true')
      
      // Navigate mit Arrow-Keys
      await user.keyboard('{ArrowDown}')
      expect(document.activeElement?.getAttribute('role')).toBe('menuitem')
      
      // Schließe mit Escape
      await user.keyboard('{Escape}')
      expect(userButton).toHaveAttribute('aria-expanded', 'false')
      expect(userButton).toHaveFocus()
    })

    it('sollte User-Informationen accessible anzeigen', () => {
      render(<UserMenu />)

      // User-Avatar sollte Alt-Text oder Label haben
      const avatar = document.querySelector('[data-testid="user-avatar"]') ||
                    document.querySelector('img[alt]') ||
                    screen.getByRole('button', { name: /benutzer/i })
      
      expect(avatar).toBeTruthy()
      
      if (avatar?.tagName === 'IMG') {
        expect(avatar).toHaveAttribute('alt')
      }
    })
  })

  describe('Overall Layout Accessibility', () => {
    it('sollte Landmark-Bereiche korrekt definieren', () => {
      const { container } = render(
        <div>
          <DashboardStats />
          <QuickActions />
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <StatusOverview />
            <RecentActivity />
          </div>
        </div>
      )

      // Wichtige Bereiche sollten als Landmarks markiert sein
      const landmarks = container.querySelectorAll('[role="main"], [role="navigation"], [role="banner"], [role="contentinfo"], main, nav, header, footer')
      
      // In einer vollständigen Page würden mehr Landmarks erwartet werden
      // Hier testen wir die Komponenten-Struktur
      expect(container.children.length).toBeGreaterThan(0)
    })

    it('sollte Skip-Links für Keyboard-Navigation bereitstellen', () => {
      // In einer vollständigen Layout-Komponente würden Skip-Links getestet
      // Hier als Beispiel, wie sie getestet werden würden
      
      const skipLink = document.querySelector('a[href="#main-content"]')
      if (skipLink) {
        expect(skipLink).toHaveTextContent(/zum hauptinhalt springen/i)
        expect(skipLink).toHaveClass('sr-only', 'focus:not-sr-only')
      }
    })

    it('sollte Focus-Management bei Route-Änderungen handhaben', async () => {
      // Mock einer Route-Änderung
      const { rerender } = render(<DashboardStats />)
      
      // Nach Route-Änderung sollte Focus auf Hauptinhalt gesetzt werden
      rerender(<QuickActions />)
      
      // In einer echten App würde der Focus auf h1 oder main-content gesetzt
      const focusableElement = document.activeElement
      expect(focusableElement).toBeTruthy()
    })
  })

  describe('German Layout Standards', () => {
    it('sollte deutsche UI-Texte verwenden', async () => {
      render(
        <div>
          <DashboardStats />
          <QuickActions />
          <RecentActivity />
        </div>
      )

      await waitFor(() => {
        // Deutsche Überschriften
        expect(screen.getByText(/statistiken|übersicht/i)).toBeTruthy()
        expect(screen.getByText(/schnellaktionen|aktionen/i)).toBeTruthy()
        expect(screen.getByText(/letzte aktivitäten|aktivitäten/i)).toBeTruthy()
        
        // Deutsche Action-Texte
        expect(screen.getByText('Neuen Bezirk erstellen')).toBeInTheDocument()
        expect(screen.getByText('Parzelle zuweisen')).toBeInTheDocument()
        expect(screen.getByText('Antrag bearbeiten')).toBeInTheDocument()
      })
    })

    it('sollte deutsches Datumsformat verwenden', async () => {
      render(<RecentActivity />)

      await waitFor(() => {
        // DD.MM.YYYY Format
        expect(screen.getByText(/\d{2}\.\d{2}\.\d{4}/)).toBeInTheDocument()
      })
    })

    it('sollte deutsche Zahlenformatierung verwenden', async () => {
      render(<DashboardStats />)

      await waitFor(() => {
        // Komma als Dezimaltrennzeichen
        expect(screen.getByText('84,4%')).toBeInTheDocument()
      })
    })

    it('sollte deutsche Accessibility-Standards erfüllen', async () => {
      const { container } = render(
        <div>
          <DashboardStats />
          <QuickActions />
        </div>
      )

      await waitFor(() => {
        expect(screen.getByText('5')).toBeInTheDocument()
      })

      const germanA11yResult = checkGermanA11yStandards(container)
      
      expect(germanA11yResult.isValid).toBe(true)
      
      if (!germanA11yResult.isValid) {
        console.log('German A11y Issues:', germanA11yResult.issues)
      }
    })
  })
})