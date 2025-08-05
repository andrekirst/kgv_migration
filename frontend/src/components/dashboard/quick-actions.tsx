'use client'

// Quick actions component for common KGV tasks
import * as React from 'react'
import Link from 'next/link'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { 
  PlusIcon, 
  SearchIcon, 
  DownloadIcon, 
  SettingsIcon,
  FileTextIcon,
  UsersIcon,
  MapIcon,
  BarChart3Icon
} from 'lucide-react'

const quickActions = [
  {
    title: 'Neuer Antrag',
    description: 'Einen neuen Kleingartenantrag erstellen',
    href: '/antraege/neu',
    icon: PlusIcon,
    color: 'bg-primary-500 hover:bg-primary-600',
    textColor: 'text-white'
  },
  {
    title: 'Anträge suchen',
    description: 'Bestehende Anträge durchsuchen',
    href: '/antraege',
    icon: SearchIcon,
    color: 'bg-secondary-100 hover:bg-secondary-200',
    textColor: 'text-secondary-900'
  },
  {
    title: 'Personen verwalten',
    description: 'Antragsteller und Mitarbeiter verwalten',
    href: '/personen',
    icon: UsersIcon,
    color: 'bg-success-100 hover:bg-success-200',
    textColor: 'text-success-900'
  },
  {
    title: 'Bezirke & Parzellen',
    description: 'Bezirksverwaltung und Parzellenzuteilung',
    href: '/bezirke',
    icon: MapIcon,
    color: 'bg-warning-100 hover:bg-warning-200',
    textColor: 'text-warning-900'
  },
  {
    title: 'Berichte erstellen',
    description: 'Statistische Auswertungen und Exporte',
    href: '/berichte',
    icon: BarChart3Icon,
    color: 'bg-purple-100 hover:bg-purple-200',
    textColor: 'text-purple-900'
  },
  {
    title: 'Systemeinstellungen',
    description: 'Konfiguration und Verwaltung',
    href: '/einstellungen',
    icon: SettingsIcon,
    color: 'bg-gray-100 hover:bg-gray-200',
    textColor: 'text-gray-900'
  }
]

export function QuickActions() {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <FileTextIcon className="h-5 w-5" />
          <span>Schnelle Aktionen</span>
        </CardTitle>
        <CardDescription>
          Häufig verwendete Funktionen für die tägliche Arbeit
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {quickActions.map((action) => {
            const Icon = action.icon
            
            return (
              <Link
                key={action.title}
                href={action.href}
                className="group"
              >
                <div className={`
                  ${action.color} ${action.textColor}
                  rounded-lg p-4 transition-all duration-200 
                  transform hover:scale-105 focus:scale-105
                  focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2
                  group-hover:shadow-md
                `}>
                  <div className="flex items-start space-x-3">
                    <Icon className="h-6 w-6 flex-shrink-0 mt-0.5" />
                    <div className="min-w-0 flex-1">
                      <h3 className="font-semibold text-sm group-hover:underline">
                        {action.title}
                      </h3>
                      <p className="text-xs opacity-80 mt-1 leading-relaxed">
                        {action.description}
                      </p>
                    </div>
                  </div>
                </div>
              </Link>
            )
          })}
        </div>
        
        {/* Additional help section */}
        <div className="mt-6 pt-6 border-t border-secondary-200">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between space-y-3 sm:space-y-0">
            <div>
              <h4 className="font-medium text-secondary-900">Benötigen Sie Hilfe?</h4>
              <p className="text-sm text-secondary-600">
                Dokumentation und Support-Ressourcen
              </p>
            </div>
            <div className="flex space-x-3">
              <Button variant="outline" size="sm" asChild>
                <Link href="/hilfe">
                  Dokumentation
                </Link>
              </Button>
              <Button variant="outline" size="sm" asChild>
                <Link href="/support">
                  Support
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}