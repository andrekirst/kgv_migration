import { Suspense } from 'react'
import { Metadata } from 'next'
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ParzellenFilters } from '@/components/parzellen/parzellen-filters'
import { apiClient } from '@/lib/api-client'
import type { Bezirk, Parzelle, ParzellenFilter, ParzellenStatus } from '@/types/bezirke'

export const dynamic = 'force-dynamic'

interface BezirkParzellenPageProps {
  params: Promise<{ id: string }>
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}

export async function generateMetadata({ params }: BezirkParzellenPageProps): Promise<Metadata> {
  const { id } = await params
  
  try {
    const response = await apiClient.get(`/bezirke/${id}`)
    const bezirk: Bezirk = response.data
    
    return {
      title: `Parzellen in ${bezirk.name} | Bezirksverwaltung`,
      description: `Übersicht aller Parzellen im Bezirk ${bezirk.name}`,
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

async function getBezirkParzellen(bezirkId: string, filters: ParzellenFilter) {
  try {
    const queryParams = new URLSearchParams()
    
    queryParams.set('bezirkId', bezirkId)
    if (filters.search) queryParams.set('search', filters.search)
    if (filters.status && filters.status.length > 0) {
      filters.status.forEach(status => queryParams.append('status', status))
    }
    if (filters.aktiv !== undefined) queryParams.set('aktiv', String(filters.aktiv))
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
    console.error('Fehler beim Laden der Parzellen:', error)
    return {
      parzellen: [],
      pagination: { page: 1, limit: 20, total: 0, totalPages: 0 },
      filters: {}
    }
  }
}

function BezirkHeader({ bezirk }: { bezirk: Bezirk }) {
  return (
    <div className="bg-white border border-secondary-200 rounded-lg p-6">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-secondary-900 mb-2">
            Parzellen in {bezirk.name}
          </h1>
          <p className="text-secondary-600">
            Verwalten Sie alle Parzellen in diesem Bezirk
          </p>
        </div>
        <div className="flex gap-3">
          <Link href={`/bezirke/${bezirk.id}`}>
            <Button variant="outline">
              Zurück zum Bezirk
            </Button>
          </Link>
          <Link href={`/parzellen/neu?bezirkId=${bezirk.id}`}>
            <Button>
              Neue Parzelle
            </Button>
          </Link>
        </div>
      </div>
    </div>
  )
}

function ParzellenTable({ parzellen }: { parzellen: Parzelle[] }) {
  if (parzellen.length === 0) {
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
              d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
            />
          </svg>
          <h3 className="text-lg font-medium text-secondary-900 mb-2">
            Keine Parzellen gefunden
          </h3>
          <p className="text-secondary-600 mb-4">
            Es wurden keine Parzellen gefunden, die Ihren Suchkriterien entsprechen.
          </p>
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
                Parzelle
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Größe
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Pacht
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-secondary-500 uppercase tracking-wider">
                Mieter
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
            {parzellen.map((parzelle) => (
              <tr key={parzelle.id} className="hover:bg-secondary-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div>
                    <div className="text-sm font-medium text-secondary-900">
                      <Link 
                        href={`/parzellen/${parzelle.id}`}
                        className="hover:text-primary-600"
                      >
                        Parzelle {parzelle.nummer}
                      </Link>
                    </div>
                    {parzelle.beschreibung && (
                      <div className="text-sm text-secondary-500 max-w-xs truncate">
                        {parzelle.beschreibung}
                      </div>
                    )}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-secondary-900">
                    {parzelle.groesse} m²
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-secondary-900">
                    €{parzelle.monatlichePacht.toFixed(2)}
                  </div>
                  <div className="text-sm text-secondary-500">
                    pro Monat
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {parzelle.mieter ? (
                    <div>
                      <div className="text-sm text-secondary-900">
                        {parzelle.mieter.vorname} {parzelle.mieter.nachname}
                      </div>
                      {parzelle.mieter.email && (
                        <div className="text-sm text-secondary-500">
                          {parzelle.mieter.email}
                        </div>
                      )}
                    </div>
                  ) : (
                    <div className="text-sm text-secondary-500">
                      Nicht zugewiesen
                    </div>
                  )}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <Badge 
                    variant={
                      parzelle.status === 'belegt' ? 'default' :
                      parzelle.status === 'frei' ? 'secondary' :
                      parzelle.status === 'reserviert' ? 'outline' :
                      'destructive'
                    }
                  >
                    {getStatusText(parzelle.status)}
                  </Badge>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <div className="flex gap-2">
                    <Link href={`/parzellen/${parzelle.id}`}>
                      <Button variant="ghost" size="sm">
                        Anzeigen
                      </Button>
                    </Link>
                    <Link href={`/parzellen/${parzelle.id}/bearbeiten`}>
                      <Button variant="ghost" size="sm">
                        Bearbeiten
                      </Button>
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function getStatusText(status: ParzellenStatus): string {
  switch (status) {
    case ParzellenStatus.FREI:
      return 'Frei'
    case ParzellenStatus.BELEGT:
      return 'Belegt'
    case ParzellenStatus.RESERVIERT:
      return 'Reserviert'
    case ParzellenStatus.WARTUNG:
      return 'Wartung'
    case ParzellenStatus.GESPERRT:
      return 'Gesperrt'
    default:
      return status
  }
}

function ParzellenTableSkeleton() {
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

export default async function BezirkParzellenPage({ params, searchParams }: BezirkParzellenPageProps) {
  const { id } = await params
  const resolvedSearchParams = await searchParams
  
  const bezirk = await getBezirk(id)
  
  if (!bezirk) {
    notFound()
  }

  // Parse search parameters
  const filters: ParzellenFilter = {
    bezirkId: parseInt(id),
    search: typeof resolvedSearchParams.search === 'string' ? resolvedSearchParams.search : undefined,
    status: resolvedSearchParams.status 
      ? Array.isArray(resolvedSearchParams.status) 
        ? resolvedSearchParams.status as ParzellenStatus[]
        : [resolvedSearchParams.status as ParzellenStatus]
      : undefined,
    aktiv: resolvedSearchParams.aktiv === 'false' ? false : undefined,
    groesseMin: resolvedSearchParams.groesseMin ? parseInt(resolvedSearchParams.groesseMin as string) : undefined,
    groesseMax: resolvedSearchParams.groesseMax ? parseInt(resolvedSearchParams.groesseMax as string) : undefined,
    pachtMin: resolvedSearchParams.pachtMin ? parseFloat(resolvedSearchParams.pachtMin as string) : undefined,
    pachtMax: resolvedSearchParams.pachtMax ? parseFloat(resolvedSearchParams.pachtMax as string) : undefined,
    page: resolvedSearchParams.page ? parseInt(resolvedSearchParams.page as string) : 1,
    limit: resolvedSearchParams.limit ? parseInt(resolvedSearchParams.limit as string) : 20,
    sortBy: (resolvedSearchParams.sortBy as 'nummer' | 'groesse' | 'monatlichePacht' | 'erstelltAm') || 'nummer',
    sortOrder: (resolvedSearchParams.sortOrder as 'asc' | 'desc') || 'asc',
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
          <li>
            <Link href={`/bezirke/${id}`} className="text-secondary-500 hover:text-secondary-700">
              {bezirk.name}
            </Link>
          </li>
          <li className="text-secondary-400">/</li>
          <li className="text-secondary-900 font-medium">
            Parzellen
          </li>
        </ol>
      </nav>

      {/* Header */}
      <BezirkHeader bezirk={bezirk} />

      {/* Filters */}
      <ParzellenFilters initialFilters={filters} showBezirkFilter={false} />

      {/* Parzellen Table */}
      <Suspense fallback={<ParzellenTableSkeleton />}>
        <ParzellenContent filters={filters} />
      </Suspense>
    </div>
  )
}

async function ParzellenContent({ filters }: { filters: ParzellenFilter }) {
  const data = await getBezirkParzellen(filters.bezirkId!.toString(), filters)
  
  return (
    <div className="space-y-6">
      <ParzellenTable parzellen={data.parzellen} />
      
      {/* Pagination */}
      {data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-secondary-700">
            Zeige {((data.pagination.page - 1) * data.pagination.limit) + 1} bis {Math.min(data.pagination.page * data.pagination.limit, data.pagination.total)} von {data.pagination.total} Parzellen
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