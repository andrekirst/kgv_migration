import { Suspense } from 'react'
import { Metadata } from 'next'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { BezirkeFilters } from '@/components/bezirke/bezirke-filters'
import { BezirkeStats } from '@/components/bezirke/bezirke-stats'
import { apiClient } from '@/lib/api-client'
import type { Bezirk, BezirkeFilter } from '@/types/bezirke'

export const dynamic = 'force-dynamic'

export const metadata: Metadata = {
  title: 'Bezirke Übersicht',
  description: 'Übersicht aller Kleingartenverein Bezirke mit Statistiken und Verwaltungsfunktionen',
}

interface BezirkePageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}

async function getBezirke(filters: BezirkeFilter) {
  try {
    const queryParams = new URLSearchParams()
    
    if (filters.search) queryParams.set('search', filters.search)
    if (filters.aktiv !== undefined) queryParams.set('aktiv', String(filters.aktiv))
    if (filters.page) queryParams.set('page', String(filters.page))
    if (filters.limit) queryParams.set('limit', String(filters.limit))
    if (filters.sortBy) queryParams.set('sortBy', filters.sortBy)
    if (filters.sortOrder) queryParams.set('sortOrder', filters.sortOrder)

    const response = await apiClient.get(`/bezirke?${queryParams.toString()}`)
    return response.data
  } catch (error) {
    console.error('Fehler beim Laden der Bezirke:', error)
    return {
      bezirke: [],
      pagination: { page: 1, limit: 20, total: 0, totalPages: 0 },
      filters: {}
    }
  }
}

async function getGesamtStatistiken() {
  try {
    const response = await apiClient.get('/bezirke/statistiken/gesamt')
    return response.data
  } catch (error) {
    console.error('Fehler beim Laden der Statistiken:', error)
    return {
      gesamtBezirke: 0,
      aktiveBezirke: 0,
      gesamtParzellen: 0,
      belegteParzellen: 0,
      freieParzellen: 0,
      auslastung: 0,
      trends: { neueAntraege: 0, kuendigungen: 0, neueParzellen: 0, zeitraum: 'Aktueller Monat' }
    }
  }
}

function BezirkeTable({ bezirke }: { bezirke: Bezirk[] }) {
  if (!bezirke || bezirke.length === 0) {
    return (
      <Card className="p-8 text-center">
        <div className="text-secondary-500">
          <svg
            className="mx-auto h-12 w-12 mb-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
          <h3 className="text-lg font-medium text-secondary-900 mb-2">
            Keine Bezirke gefunden
          </h3>
          <p className="text-secondary-600 mb-4">
            Es wurden keine Bezirke gefunden, die Ihren Suchkriterien entsprechen.
          </p>
          <Link href="/bezirke/neu">
            <Button>
              Ersten Bezirk erstellen
            </Button>
          </Link>
        </div>
      </Card>
    )
  }

  return (
    <div className="bg-white shadow-sm border border-secondary-200 rounded-lg overflow-hidden">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-secondary-200">
          <thead className="bg-secondary-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Bezirk
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Bezirksleiter
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Parzellen
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Auslastung
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Status
              </th>
              <th className="relative px-6 py-3">
                <span className="sr-only">Aktionen</span>
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-secondary-200">
            {bezirke.map((bezirk) => {
              const auslastung = bezirk.statistiken.gesamtParzellen > 0 
                ? Math.round((bezirk.statistiken.belegteParzellen / bezirk.statistiken.gesamtParzellen) * 100)
                : 0

              return (
                <tr key={bezirk.id} className="hover:bg-secondary-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div>
                      <div className="text-sm font-medium text-secondary-900">
                        <Link 
                          href={`/bezirke/${bezirk.id}`}
                          className="hover:text-primary-600"
                        >
                          {bezirk.name}
                        </Link>
                      </div>
                      {bezirk.beschreibung && (
                        <div className="text-sm text-secondary-500 max-w-xs truncate">
                          {bezirk.beschreibung}
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-secondary-900">
                      {bezirk.bezirksleiter || 'Nicht zugewiesen'}
                    </div>
                    {bezirk.email && (
                      <div className="text-sm text-secondary-500">
                        {bezirk.email}
                      </div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-secondary-900">
                      {bezirk.statistiken.belegteParzellen} / {bezirk.statistiken.gesamtParzellen}
                    </div>
                    <div className="text-sm text-secondary-500">
                      {bezirk.statistiken.freieParzellen} frei
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="text-sm text-secondary-900 mr-2">
                        {auslastung}%
                      </div>
                      <div className="w-16 bg-secondary-200 rounded-full h-2">
                        <div
                          className={`h-2 rounded-full ${
                            auslastung >= 90 ? 'bg-red-500' :
                            auslastung >= 70 ? 'bg-yellow-500' :
                            'bg-green-500'
                          }`}
                          style={{ width: `${auslastung}%` }}
                        />
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <Badge variant={bezirk.aktiv ? 'default' : 'secondary'}>
                      {bezirk.aktiv ? 'Aktiv' : 'Inaktiv'}
                    </Badge>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <div className="flex gap-2">
                      <Link href={`/bezirke/${bezirk.id}`}>
                        <Button variant="ghost" size="sm">
                          Anzeigen
                        </Button>
                      </Link>
                      <Link href={`/bezirke/${bezirk.id}/bearbeiten`}>
                        <Button variant="ghost" size="sm">
                          Bearbeiten
                        </Button>
                      </Link>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function BezirkeTableSkeleton() {
  return (
    <div className="bg-white shadow-sm border border-secondary-200 rounded-lg overflow-hidden animate-pulse">
      <div className="px-6 py-3 bg-secondary-50">
        <div className="h-4 bg-secondary-200 rounded w-1/4"></div>
      </div>
      {[...Array(5)].map((_, i) => (
        <div key={i} className="px-6 py-4 border-b border-secondary-200">
          <div className="flex items-center space-x-4">
            <div className="flex-1">
              <div className="h-4 bg-secondary-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-secondary-200 rounded w-1/2"></div>
            </div>
            <div className="h-4 bg-secondary-200 rounded w-20"></div>
            <div className="h-4 bg-secondary-200 rounded w-16"></div>
          </div>
        </div>
      ))}
    </div>
  )
}

export default async function BezirkePage({ searchParams }: BezirkePageProps) {
  const resolvedSearchParams = await searchParams
  
  // Parse search parameters
  const filters: BezirkeFilter = {
    search: typeof resolvedSearchParams.search === 'string' ? resolvedSearchParams.search : undefined,
    aktiv: resolvedSearchParams.aktiv === 'false' ? false : undefined,
    page: resolvedSearchParams.page ? parseInt(resolvedSearchParams.page as string) : 1,
    limit: resolvedSearchParams.limit ? parseInt(resolvedSearchParams.limit as string) : 20,
    sortBy: (resolvedSearchParams.sortBy as 'name' | 'erstelltAm' | 'gesamtParzellen') || 'name',
    sortOrder: (resolvedSearchParams.sortOrder as 'asc' | 'desc') || 'asc',
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-secondary-900">
            Bezirke verwalten
          </h2>
          <p className="text-secondary-600">
            Übersicht aller Kleingartenverein Bezirke
          </p>
        </div>
        <div className="flex gap-3">
          <Link href="/parzellen">
            <Button variant="outline">
              Zu Parzellen
            </Button>
          </Link>
          <Link href="/bezirke/neu">
            <Button>
              Neuer Bezirk
            </Button>
          </Link>
        </div>
      </div>

      {/* Statistics Overview */}
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
        <BezirkeStats getStatistiken={getGesamtStatistiken} />
      </Suspense>

      {/* Filters */}
      <BezirkeFilters initialFilters={filters} />

      {/* Bezirke Table */}
      <Suspense fallback={<BezirkeTableSkeleton />}>
        <BezirkeContent filters={filters} />
      </Suspense>
    </div>
  )
}

async function BezirkeContent({ filters }: { filters: BezirkeFilter }) {
  const data = await getBezirke(filters)
  
  // Fallback für den Fall, dass data null oder undefined ist
  const safeBezirke = data?.bezirke || []
  const safePagination = data?.pagination || null
  
  return (
    <div className="space-y-6">
      <BezirkeTable bezirke={safeBezirke} />
      
      {/* Pagination */}
      {safePagination && safePagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-secondary-700">
            Zeige {((safePagination.page - 1) * safePagination.limit) + 1} bis {Math.min(safePagination.page * safePagination.limit, safePagination.total)} von {safePagination.total} Bezirken
          </div>
          <div className="flex gap-2">
            {safePagination && safePagination.page > 1 && (
              <Link href={`?${new URLSearchParams({ ...filters as any, page: String(safePagination.page - 1) }).toString()}`}>
                <Button variant="outline" size="sm">
                  Vorherige
                </Button>
              </Link>
            )}
            {safePagination && safePagination.page < safePagination.totalPages && (
              <Link href={`?${new URLSearchParams({ ...filters as any, page: String(safePagination.page + 1) }).toString()}`}>
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