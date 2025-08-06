import { Suspense } from 'react'
import { Metadata } from 'next'
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { apiClient } from '@/lib/api-client'
import type { Bezirk, BezirkStatistiken, Parzelle } from '@/types/bezirke'

export const dynamic = 'force-dynamic'

interface BezirkDetailPageProps {
  params: Promise<{ id: string }>
}

export async function generateMetadata({ params }: BezirkDetailPageProps): Promise<Metadata> {
  const { id } = await params
  
  try {
    const response = await apiClient.get(`/bezirke/${id}`)
    const bezirk: Bezirk = response.data
    
    return {
      title: `${bezirk.name} | Bezirksverwaltung`,
      description: `Details und Statistiken für Bezirk ${bezirk.name}`,
    }
  } catch {
    return {
      title: 'Bezirk nicht gefunden',
      description: 'Der angeforderte Bezirk konnte nicht gefunden werden.',
    }
  }
}

async function getBezirk(id: string): Promise<Bezirk | null> {
  try {
    const response = await apiClient.get(`/bezirke/${id}`)
    return response.data
  } catch (error: any) {
    if (error.status === 404) {
      return null
    }
    throw error
  }
}

async function getBezirkStatistiken(id: string): Promise<BezirkStatistiken> {
  try {
    const response = await apiClient.get(`/bezirke/${id}/statistiken`)
    return response.data
  } catch (error) {
    // Return default statistics if API fails
    return {
      bezirkId: parseInt(id),
      bezirkName: '',
      gesamtParzellen: 0,
      belegteParzellen: 0,
      freieParzellen: 0,
      reservierteParzellen: 0,
      wartungParzellen: 0,
      gesperrteParzellen: 0,
      durchschnittsPacht: 0,
      gesamtEinnahmen: 0,
      warteliste: 0,
      auslastung: 0,
    }
  }
}

async function getBezirkParzellen(id: string, limit: number = 5): Promise<Parzelle[]> {
  try {
    const response = await apiClient.get(`/bezirke/${id}/parzellen?limit=${limit}`)
    return response.data.parzellen || []
  } catch (error) {
    return []
  }
}

function BezirkHeader({ bezirk }: { bezirk: Bezirk }) {
  return (
    <div className="bg-white border border-secondary-200 rounded-lg p-6">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
        {/* Basic Info */}
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-4">
            <h1 className="text-3xl font-bold text-secondary-900">
              {bezirk.name}
            </h1>
            <Badge variant={bezirk.aktiv ? 'default' : 'secondary'}>
              {bezirk.aktiv ? 'Aktiv' : 'Inaktiv'}
            </Badge>
          </div>
          
          {bezirk.beschreibung && (
            <p className="text-secondary-600 mb-4">
              {bezirk.beschreibung}
            </p>
          )}

          {/* Contact Information */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {bezirk.bezirksleiter && (
              <div>
                <h3 className="text-sm font-medium text-secondary-700 mb-1">
                  Bezirksleiter
                </h3>
                <p className="text-secondary-900">{bezirk.bezirksleiter}</p>
              </div>
            )}
            
            {bezirk.email && (
              <div>
                <h3 className="text-sm font-medium text-secondary-700 mb-1">
                  E-Mail
                </h3>
                <a 
                  href={`mailto:${bezirk.email}`}
                  className="text-primary-600 hover:text-primary-700"
                >
                  {bezirk.email}
                </a>
              </div>
            )}
            
            {bezirk.telefon && (
              <div>
                <h3 className="text-sm font-medium text-secondary-700 mb-1">
                  Telefon
                </h3>
                <a 
                  href={`tel:${bezirk.telefon}`}
                  className="text-primary-600 hover:text-primary-700"
                >
                  {bezirk.telefon}
                </a>
              </div>
            )}
            
            {bezirk.adresse && (
              <div>
                <h3 className="text-sm font-medium text-secondary-700 mb-1">
                  Adresse
                </h3>
                <div className="text-secondary-900">
                  {bezirk.adresse.strasse && bezirk.adresse.hausnummer && (
                    <div>{bezirk.adresse.strasse} {bezirk.adresse.hausnummer}</div>
                  )}
                  {bezirk.adresse.plz && bezirk.adresse.ort && (
                    <div>{bezirk.adresse.plz} {bezirk.adresse.ort}</div>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex flex-col gap-3 lg:w-48">
          <Link href={`/bezirke/${bezirk.id}/bearbeiten`}>
            <Button className="w-full">
              Bearbeiten
            </Button>
          </Link>
          <Link href={`/bezirke/${bezirk.id}/parzellen`}>
            <Button variant="outline" className="w-full">
              Alle Parzellen
            </Button>
          </Link>
          <Link href="/bezirke">
            <Button variant="outline" className="w-full">
              Zurück zur Übersicht
            </Button>
          </Link>
        </div>
      </div>
    </div>
  )
}

function StatistikCards({ statistiken }: { statistiken: BezirkStatistiken }) {
  const auslastung = Math.round(statistiken.auslastung)
  
  const cards = [
    {
      title: 'Gesamt Parzellen',
      value: statistiken.gesamtParzellen,
      subtitle: 'im Bezirk',
      color: 'blue',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5" />
        </svg>
      )
    },
    {
      title: 'Belegte Parzellen',
      value: statistiken.belegteParzellen,
      subtitle: `${auslastung}% Auslastung`,
      color: 'green',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      )
    },
    {
      title: 'Freie Parzellen',
      value: statistiken.freieParzellen,
      subtitle: 'verfügbar',
      color: 'orange',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      )
    },
    {
      title: 'Monatliche Einnahmen',
      value: `€${statistiken.gesamtEinnahmen.toLocaleString('de-DE')}`,
      subtitle: `Ø €${statistiken.durchschnittsPacht.toFixed(2)}`,
      color: 'purple',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
        </svg>
      )
    }
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {cards.map((card, index) => (
        <Card key={index} className="p-6">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <p className="text-sm font-medium text-secondary-600 mb-1">
                {card.title}
              </p>
              <p className="text-2xl font-bold text-secondary-900 mb-1">
                {card.value}
              </p>
              <p className="text-sm text-secondary-500">
                {card.subtitle}
              </p>
            </div>
            <div className={`p-3 rounded-lg bg-${card.color}-100 text-${card.color}-600`}>
              {card.icon}
            </div>
          </div>
        </Card>
      ))}
    </div>
  )
}

function RecentParzellen({ parzellen, bezirkId }: { parzellen: Parzelle[], bezirkId: string }) {
  if (parzellen.length === 0) {
    return (
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Aktuelle Parzellen
        </h3>
        <div className="text-center py-8">
          <p className="text-secondary-500 mb-4">
            Keine Parzellen gefunden
          </p>
          <Link href={`/parzellen/neu?bezirkId=${bezirkId}`}>
            <Button>
              Erste Parzelle erstellen
            </Button>
          </Link>
        </div>
      </Card>
    )
  }

  return (
    <Card className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-secondary-900">
          Aktuelle Parzellen
        </h3>
        <Link href={`/bezirke/${bezirkId}/parzellen`}>
          <Button variant="outline" size="sm">
            Alle anzeigen
          </Button>
        </Link>
      </div>
      
      <div className="space-y-4">
        {parzellen.map((parzelle) => (
          <div key={parzelle.id} className="flex items-center justify-between p-4 border border-secondary-200 rounded-lg hover:bg-secondary-50">
            <div className="flex-1">
              <div className="flex items-center gap-3 mb-2">
                <Link 
                  href={`/parzellen/${parzelle.id}`}
                  className="font-medium text-secondary-900 hover:text-primary-600"
                >
                  Parzelle {parzelle.nummer}
                </Link>
                <Badge 
                  variant={
                    parzelle.status === 'belegt' ? 'default' :
                    parzelle.status === 'frei' ? 'secondary' :
                    parzelle.status === 'reserviert' ? 'outline' :
                    'destructive'
                  }
                >
                  {parzelle.status}
                </Badge>
              </div>
              <div className="text-sm text-secondary-600">
                {parzelle.groesse}m² • €{parzelle.monatlichePacht}/Monat
                {parzelle.mieter && (
                  <span> • {parzelle.mieter.vorname} {parzelle.mieter.nachname}</span>
                )}
              </div>
            </div>
            <div className="flex gap-2">
              <Link href={`/parzellen/${parzelle.id}`}>
                <Button variant="ghost" size="sm">
                  Details
                </Button>
              </Link>
            </div>
          </div>
        ))}
      </div>
    </Card>
  )
}

function LoadingSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header Skeleton */}
      <div className="bg-white border border-secondary-200 rounded-lg p-6 animate-pulse">
        <div className="h-8 bg-secondary-200 rounded w-1/3 mb-4"></div>
        <div className="h-4 bg-secondary-200 rounded w-2/3 mb-4"></div>
        <div className="grid grid-cols-2 gap-4">
          <div className="h-4 bg-secondary-200 rounded w-3/4"></div>
          <div className="h-4 bg-secondary-200 rounded w-3/4"></div>
        </div>
      </div>

      {/* Stats Skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {[...Array(4)].map((_, i) => (
          <Card key={i} className="p-6 animate-pulse">
            <div className="h-4 bg-secondary-200 rounded w-1/2 mb-2"></div>
            <div className="h-8 bg-secondary-200 rounded w-3/4 mb-2"></div>
            <div className="h-3 bg-secondary-200 rounded w-1/2"></div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default async function BezirkDetailPage({ params }: BezirkDetailPageProps) {
  const { id } = await params
  
  const bezirk = await getBezirk(id)
  
  if (!bezirk) {
    notFound()
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <nav className="flex" aria-label="Breadcrumb">
        <ol className="flex items-center space-x-2">
          <li>
            <Link href="/bezirke" className="text-secondary-500 hover:text-secondary-700">
              Bezirke
            </Link>
          </li>
          <li className="text-secondary-400">/</li>
          <li className="text-secondary-900 font-medium">
            {bezirk.name}
          </li>
        </ol>
      </nav>

      {/* Header */}
      <BezirkHeader bezirk={bezirk} />

      {/* Statistics */}
      <Suspense fallback={
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <Card key={i} className="p-6 animate-pulse">
              <div className="h-4 bg-secondary-200 rounded w-1/2 mb-2"></div>
              <div className="h-8 bg-secondary-200 rounded w-3/4"></div>
            </Card>
          ))}
        </div>
      }>
        <StatistikCardsWrapper bezirkId={id} />
      </Suspense>

      {/* Recent Parzellen */}
      <Suspense fallback={
        <Card className="p-6 animate-pulse">
          <div className="h-6 bg-secondary-200 rounded w-1/4 mb-4"></div>
          <div className="space-y-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-16 bg-secondary-200 rounded"></div>
            ))}
          </div>
        </Card>
      }>
        <RecentParzellenWrapper bezirkId={id} />
      </Suspense>
    </div>
  )
}

async function StatistikCardsWrapper({ bezirkId }: { bezirkId: string }) {
  const statistiken = await getBezirkStatistiken(bezirkId)
  return <StatistikCards statistiken={statistiken} />
}

async function RecentParzellenWrapper({ bezirkId }: { bezirkId: string }) {
  const parzellen = await getBezirkParzellen(bezirkId, 5)
  return <RecentParzellen parzellen={parzellen} bezirkId={bezirkId} />
}