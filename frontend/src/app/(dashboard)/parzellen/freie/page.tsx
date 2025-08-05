import { Suspense } from 'react'
import { Metadata } from 'next'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ParzellenFilters } from '@/components/parzellen/parzellen-filters'
import { apiClient } from '@/lib/api-client'
import type { Parzelle, ParzellenFilter, ParzellenStatus } from '@/types/bezirke'

export const dynamic = 'force-dynamic'

export const metadata: Metadata = {
  title: 'Freie Parzellen | KGV Verwaltung',
  description: 'Übersicht aller verfügbaren Parzellen mit Zuweisung und Reservierung',
}

interface FreieParzellenPageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}

async function getFreieParzellen(filters: ParzellenFilter) {
  try {
    const queryParams = new URLSearchParams()
    
    // Force status to only show available plots
    queryParams.append('status', 'frei')
    
    if (filters.search) queryParams.set('search', filters.search)
    if (filters.bezirkId) queryParams.set('bezirkId', String(filters.bezirkId))
    if (filters.groesseMin) queryParams.set('groesseMin', String(filters.groesseMin))
    if (filters.groesseMax) queryParams.set('groesseMax', String(filters.groesseMax))
    if (filters.pachtMin) queryParams.set('pachtMin', String(filters.pachtMin))
    if (filters.pachtMax) queryParams.set('pachtMax', String(filters.pachtMax))
    if (filters.page) queryParams.set('page', String(filters.page))
    if (filters.limit) queryParams.set('limit', String(filters.limit))
    if (filters.sortBy) queryParams.set('sortBy', filters.sortBy)
    if (filters.sortOrder) queryParams.set('sortOrder', filters.sortOrder)

    const response = await apiClient.get(`/parzellen?${queryParams.toString()}`)
    return response.data
  } catch (error) {
    console.error('Fehler beim Laden der freien Parzellen:', error)
    return {
      parzellen: [],
      pagination: { page: 1, limit: 20, total: 0, totalPages: 0 },
      filters: {}
    }
  }
}

function FreieParzellenGrid({ parzellen }: { parzellen: Parzelle[] }) {
  if (parzellen.length === 0) {
    return (
      <Card className="p-8 text-center">
        <div className="text-secondary-500">
          <svg
            className="mx-auto h-16 w-16 mb-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <h3 className="text-xl font-medium text-secondary-900 mb-2">
            Keine freien Parzellen verfügbar
          </h3>
          <p className="text-secondary-600 mb-6">
            Derzeit sind alle Parzellen belegt oder reserviert. Neue Parzellen können über die Bezirksverwaltung erstellt werden.
          </p>
          <div className="flex flex-col sm:flex-row gap-3 justify-center">
            <Link href="/parzellen">
              <Button variant="outline">
                Alle Parzellen anzeigen
              </Button>
            </Link>
            <Link href="/parzellen/neu">
              <Button>
                Neue Parzelle erstellen
              </Button>
            </Link>
          </div>
        </div>
      </Card>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {parzellen.map((parzelle) => (
        <Card key={parzelle.id} className="p-6 hover:shadow-lg transition-shadow">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h3 className="text-lg font-semibold text-secondary-900 mb-1">
                <Link 
                  href={`/parzellen/${parzelle.id}`}
                  className="hover:text-primary-600"
                >
                  Parzelle {parzelle.nummer}
                </Link>
              </h3>
              <p className="text-sm text-secondary-600">
                {parzelle.bezirkName}
              </p>
            </div>
            <Badge variant="secondary" className="bg-green-100 text-green-800">
              Frei
            </Badge>
          </div>

          {/* Key Information */}
          <div className="space-y-3 mb-6">
            <div className="flex justify-between">
              <span className="text-sm text-secondary-600">Größe:</span>
              <span className="text-sm font-medium text-secondary-900">
                {parzelle.groesse} m²
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-secondary-600">Monatliche Pacht:</span>
              <span className="text-sm font-medium text-secondary-900">
                €{parzelle.monatlichePacht.toFixed(2)}
              </span>
            </div>
            {parzelle.kaution && (
              <div className="flex justify-between">
                <span className="text-sm text-secondary-600">Kaution:</span>
                <span className="text-sm font-medium text-secondary-900">
                  €{parzelle.kaution.toFixed(2)}
                </span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-sm text-secondary-600">Kündigungsfrist:</span>
              <span className="text-sm font-medium text-secondary-900">
                {parzelle.kuendigungsfrist} Monate
              </span>
            </div>
          </div>

          {/* Equipment/Features */}
          {parzelle.ausstattung && parzelle.ausstattung.length > 0 && (
            <div className="mb-6">
              <h4 className="text-sm font-medium text-secondary-700 mb-2">
                Ausstattung:
              </h4>
              <div className="flex flex-wrap gap-1">
                {parzelle.ausstattung.slice(0, 3).map((item, index) => (
                  <span 
                    key={index}
                    className="inline-block px-2 py-1 text-xs bg-secondary-100 text-secondary-700 rounded"
                  >
                    {item}
                  </span>
                ))}
                {parzelle.ausstattung.length > 3 && (
                  <span className="inline-block px-2 py-1 text-xs bg-secondary-100 text-secondary-700 rounded">
                    +{parzelle.ausstattung.length - 3} weitere
                  </span>
                )}
              </div>
            </div>
          )}

          {/* Address */}
          {parzelle.adresse && (
            <div className="mb-6">
              <h4 className="text-sm font-medium text-secondary-700 mb-1">
                Adresse:
              </h4>
              <div className="text-sm text-secondary-600">
                {parzelle.adresse.strasse && parzelle.adresse.hausnummer && (
                  <div>{parzelle.adresse.strasse} {parzelle.adresse.hausnummer}</div>
                )}
                {parzelle.adresse.plz && parzelle.adresse.ort && (
                  <div>{parzelle.adresse.plz} {parzelle.adresse.ort}</div>
                )}
              </div>
            </div>
          )}

          {/* Description */}
          {parzelle.beschreibung && (
            <div className="mb-6">
              <h4 className="text-sm font-medium text-secondary-700 mb-1">
                Beschreibung:
              </h4>
              <p className="text-sm text-secondary-600 line-clamp-2">
                {parzelle.beschreibung}
              </p>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-2">
            <Link href={`/parzellen/${parzelle.id}`} className="flex-1">
              <Button variant="outline" size="sm" className="w-full">
                Details anzeigen
              </Button>
            </Link>
            <Button size="sm" className="flex-1">
              Reservieren
            </Button>
          </div>
        </Card>
      ))}
    </div>
  )
}

function FreieParzellenSkeleton() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {[...Array(6)].map((_, i) => (
        <Card key={i} className="p-6 animate-pulse">
          <div className="flex items-start justify-between mb-4">
            <div className="flex-1">
              <div className="h-5 bg-secondary-200 rounded w-3/4 mb-2"></div>
              <div className="h-4 bg-secondary-200 rounded w-1/2"></div>
            </div>
            <div className="h-6 bg-secondary-200 rounded w-16"></div>
          </div>
          <div className="space-y-3 mb-6">
            {[...Array(4)].map((_, j) => (
              <div key={j} className="flex justify-between">
                <div className="h-4 bg-secondary-200 rounded w-1/3"></div>
                <div className="h-4 bg-secondary-200 rounded w-1/4"></div>
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <div className="h-8 bg-secondary-200 rounded flex-1"></div>
            <div className="h-8 bg-secondary-200 rounded flex-1"></div>
          </div>
        </Card>
      ))}
    </div>
  )
}

export default async function FreieParzellenPage({ searchParams }: FreieParzellenPageProps) {
  const resolvedSearchParams = await searchParams
  
  // Parse search parameters (excluding status since we force it to 'frei')
  const filters: ParzellenFilter = {
    search: typeof resolvedSearchParams.search === 'string' ? resolvedSearchParams.search : undefined,
    bezirkId: resolvedSearchParams.bezirkId ? parseInt(resolvedSearchParams.bezirkId as string) : undefined,
    status: ['frei' as ParzellenStatus], // Force only free plots
    groesseMin: resolvedSearchParams.groesseMin ? parseInt(resolvedSearchParams.groesseMin as string) : undefined,
    groesseMax: resolvedSearchParams.groesseMax ? parseInt(resolvedSearchParams.groesseMax as string) : undefined,
    pachtMin: resolvedSearchParams.pachtMin ? parseFloat(resolvedSearchParams.pachtMin as string) : undefined,
    pachtMax: resolvedSearchParams.pachtMax ? parseFloat(resolvedSearchParams.pachtMax as string) : undefined,
    page: resolvedSearchParams.page ? parseInt(resolvedSearchParams.page as string) : 1,
    limit: resolvedSearchParams.limit ? parseInt(resolvedSearchParams.limit as string) : 12, // Show more per page in grid view
    sortBy: (resolvedSearchParams.sortBy as 'nummer' | 'groesse' | 'monatlichePacht' | 'erstelltAm') || 'nummer',
    sortOrder: (resolvedSearchParams.sortOrder as 'asc' | 'desc') || 'asc',
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-secondary-900">
            Freie Parzellen
          </h1>
          <p className="text-secondary-600">
            Verfügbare Parzellen für neue Mitglieder
          </p>
        </div>
        <div className="flex gap-3">
          <Link href="/parzellen">
            <Button variant="outline">
              Alle Parzellen
            </Button>
          </Link>
          <Link href="/parzellen/neu">
            <Button>
              Neue Parzelle
            </Button>
          </Link>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-lg bg-green-100 mr-4">
              <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-secondary-600">Freie Parzellen</p>
              <Suspense fallback={<div className="h-6 bg-secondary-200 rounded w-16 animate-pulse"></div>}>
                <FreieParzellenCount />
              </Suspense>
            </div>
          </div>
        </Card>
        
        <Card className="p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-lg bg-blue-100 mr-4">
              <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-secondary-600">Ø Pacht/Monat</p>
              <Suspense fallback={<div className="h-6 bg-secondary-200 rounded w-20 animate-pulse"></div>}>
                <DurchschnittsPacht />
              </Suspense>
            </div>
          </div>
        </Card>
        
        <Card className="p-6">
          <div className="flex items-center">
            <div className="p-3 rounded-lg bg-purple-100 mr-4">
              <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-secondary-600">Ø Größe</p>
              <Suspense fallback={<div className="h-6 bg-secondary-200 rounded w-16 animate-pulse"></div>}>
                <DurchschnittsGroesse />
              </Suspense>
            </div>
          </div>
        </Card>
      </div>

      {/* Filters - Modified to hide status filter */}
      <ParzellenFilters initialFilters={filters} showBezirkFilter={true} />

      {/* Free Plots Grid */}
      <Suspense fallback={<FreieParzellenSkeleton />}>
        <FreieParzellenContent filters={filters} />
      </Suspense>
    </div>
  )
}

async function FreieParzellenContent({ filters }: { filters: ParzellenFilter }) {
  const data = await getFreieParzellen(filters)
  
  return (
    <div className="space-y-6">
      <FreieParzellenGrid parzellen={data.parzellen} />
      
      {/* Pagination */}
      {data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-secondary-700">
            Zeige {((data.pagination.page - 1) * data.pagination.limit) + 1} bis {Math.min(data.pagination.page * data.pagination.limit, data.pagination.total)} von {data.pagination.total} freien Parzellen
          </div>
          <div className="flex gap-2">
            {data.pagination.page > 1 && (
              <Link href={`?${new URLSearchParams({ ...filters as any, page: String(data.pagination.page - 1) }).toString()}`}>
                <Button variant="outline" size="sm">
                  Vorherige
                </Button>
              </Link>
            )}
            {data.pagination.page < data.pagination.totalPages && (
              <Link href={`?${new URLSearchParams({ ...filters as any, page: String(data.pagination.page + 1) }).toString()}`}>
                <Button variant="outline" size="sm">
                  Nächste
                </Button>
              </Link>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

async function FreieParzellenCount() {
  try {
    const response = await apiClient.get('/parzellen/statistiken/gesamt')
    return <p className="text-2xl font-bold text-secondary-900">{response.data.freieParzellen}</p>
  } catch {
    return <p className="text-2xl font-bold text-secondary-900">-</p>
  }
}

async function DurchschnittsPacht() {
  try {
    const response = await apiClient.get('/parzellen?status=frei&limit=1000')
    const parzellen: Parzelle[] = response.data.parzellen || []
    
    if (parzellen.length === 0) {
      return <p className="text-2xl font-bold text-secondary-900">-</p>
    }
    
    const durchschnitt = parzellen.reduce((sum, p) => sum + p.monatlichePacht, 0) / parzellen.length
    return <p className="text-2xl font-bold text-secondary-900">€{durchschnitt.toFixed(2)}</p>
  } catch {
    return <p className="text-2xl font-bold text-secondary-900">-</p>
  }
}

async function DurchschnittsGroesse() {
  try {
    const response = await apiClient.get('/parzellen?status=frei&limit=1000')
    const parzellen: Parzelle[] = response.data.parzellen || []
    
    if (parzellen.length === 0) {
      return <p className="text-2xl font-bold text-secondary-900">-</p>
    }
    
    const durchschnitt = parzellen.reduce((sum, p) => sum + p.groesse, 0) / parzellen.length
    return <p className="text-2xl font-bold text-secondary-900">{Math.round(durchschnitt)} m²</p>
  } catch {
    return <p className="text-2xl font-bold text-secondary-900">-</p>
  }
}