import { Suspense } from 'react'
import { Metadata } from 'next'
import Link from 'next/link'
import { notFound } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { apiClient } from '@/lib/api-client'
import type { Parzelle, ParzellenHistory, ParzellenStatus } from '@/types/bezirke'

export const dynamic = 'force-dynamic'

interface ParzelleDetailPageProps {
  params: Promise<{ id: string }>
}

export async function generateMetadata({ params }: ParzelleDetailPageProps): Promise<Metadata> {
  const { id } = await params
  
  try {
    const response = await apiClient.get(`/parzellen/${id}`)
    const parzelle: Parzelle = response.data
    
    return {
      title: `Parzelle ${parzelle.nummer} | Parzellenverwaltung`,
      description: `Details und Informationen für Parzelle ${parzelle.nummer} im ${parzelle.bezirkName}`,
    }
  } catch {
    return {
      title: 'Parzelle nicht gefunden',
      description: 'Die angeforderte Parzelle konnte nicht gefunden werden.',
    }
  }
}

async function getParzelle(id: string): Promise<Parzelle | null> {
  try {
    const response = await apiClient.get(`/parzellen/${id}`)
    return response.data
  } catch (error: any) {
    if (error.status === 404) {
      return null
    }
    throw error
  }
}

async function getParzellenHistory(id: string): Promise<ParzellenHistory[]> {
  try {
    const response = await apiClient.get(`/parzellen/${id}/history`)
    return response.data || []
  } catch (error) {
    return []
  }
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

function getStatusVariant(status: ParzellenStatus) {
  switch (status) {
    case ParzellenStatus.FREI:
      return 'secondary'
    case ParzellenStatus.BELEGT:
      return 'default'
    case ParzellenStatus.RESERVIERT:
      return 'outline'
    case ParzellenStatus.WARTUNG:
      return 'outline'
    case ParzellenStatus.GESPERRT:
      return 'destructive'
    default:
      return 'secondary'
  }
}

function ParzelleHeader({ parzelle }: { parzelle: Parzelle }) {
  return (
    <div className="bg-white border border-secondary-200 rounded-lg p-6">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
        {/* Basic Info */}
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-4">
            <h1 className="text-3xl font-bold text-secondary-900">
              Parzelle {parzelle.nummer}
            </h1>
            <Badge variant={getStatusVariant(parzelle.status)}>
              {getStatusText(parzelle.status)}
            </Badge>
            <Badge variant={parzelle.aktiv ? 'default' : 'secondary'}>
              {parzelle.aktiv ? 'Aktiv' : 'Inaktiv'}
            </Badge>
          </div>
          
          <div className="flex items-center gap-2 mb-4">
            <Link 
              href={`/bezirke/${parzelle.bezirkId}`}
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              {parzelle.bezirkName}
            </Link>
            <span className="text-secondary-400">•</span>
            <span className="text-secondary-600">{parzelle.groesse} m²</span>
            <span className="text-secondary-400">•</span>
            <span className="text-secondary-600">€{parzelle.monatlichePacht.toFixed(2)}/Monat</span>
          </div>

          {parzelle.beschreibung && (
            <p className="text-secondary-600 mb-4">
              {parzelle.beschreibung}
            </p>
          )}
        </div>

        {/* Actions */}
        <div className="flex flex-col gap-3 lg:w-48">
          <Link href={`/parzellen/${parzelle.id}/bearbeiten`}>
            <Button className="w-full">
              Bearbeiten
            </Button>
          </Link>
          {parzelle.status === 'frei' && (
            <Button variant="outline" className="w-full">
              Reservieren
            </Button>
          )}
          <Link href="/parzellen">
            <Button variant="outline" className="w-full">
              Zurück zur Übersicht
            </Button>
          </Link>
        </div>
      </div>
    </div>
  )
}

function ParzelleDetails({ parzelle }: { parzelle: Parzelle }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* Financial Information */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Finanzielle Informationen
        </h3>
        
        <div className="space-y-4">
          <div className="flex justify-between">
            <span className="text-secondary-600">Monatliche Pacht:</span>
            <span className="font-medium text-secondary-900">
              €{parzelle.monatlichePacht.toFixed(2)}
            </span>
          </div>
          
          {parzelle.kaution && (
            <div className="flex justify-between">
              <span className="text-secondary-600">Kaution:</span>
              <span className="font-medium text-secondary-900">
                €{parzelle.kaution.toFixed(2)}
              </span>
            </div>
          )}
          
          <div className="flex justify-between">
            <span className="text-secondary-600">Kündigungsfrist:</span>
            <span className="font-medium text-secondary-900">
              {parzelle.kuendigungsfrist} Monate
            </span>
          </div>
          
          {parzelle.mietbeginn && (
            <div className="flex justify-between">
              <span className="text-secondary-600">Mietbeginn:</span>
              <span className="font-medium text-secondary-900">
                {new Date(parzelle.mietbeginn).toLocaleDateString('de-DE')}
              </span>
            </div>
          )}
          
          {parzelle.mietende && (
            <div className="flex justify-between">
              <span className="text-secondary-600">Mietende:</span>
              <span className="font-medium text-secondary-900">
                {new Date(parzelle.mietende).toLocaleDateString('de-DE')}
              </span>
            </div>
          )}
        </div>
      </Card>

      {/* Technical Information */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Technische Daten
        </h3>
        
        <div className="space-y-4">
          <div className="flex justify-between">
            <span className="text-secondary-600">Größe:</span>
            <span className="font-medium text-secondary-900">
              {parzelle.groesse} m²
            </span>
          </div>
          
          <div className="flex justify-between">
            <span className="text-secondary-600">Parzellennummer:</span>
            <span className="font-medium text-secondary-900">
              {parzelle.nummer}
            </span>
          </div>
          
          <div className="flex justify-between">
            <span className="text-secondary-600">Erstellt am:</span>
            <span className="font-medium text-secondary-900">
              {new Date(parzelle.erstelltAm).toLocaleDateString('de-DE')}
            </span>
          </div>
          
          <div className="flex justify-between">
            <span className="text-secondary-600">Zuletzt aktualisiert:</span>
            <span className="font-medium text-secondary-900">
              {new Date(parzelle.aktualisiertAm).toLocaleDateString('de-DE')}
            </span>
          </div>
        </div>
      </Card>

      {/* Current Tenant */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Aktueller Mieter
        </h3>
        
        {parzelle.mieter ? (
          <div className="space-y-4">
            <div>
              <div className="font-medium text-secondary-900">
                {parzelle.mieter.vorname} {parzelle.mieter.nachname}
              </div>
            </div>
            
            {parzelle.mieter.email && (
              <div>
                <span className="text-secondary-600">E-Mail:</span>
                <div className="mt-1">
                  <a 
                    href={`mailto:${parzelle.mieter.email}`}
                    className="text-primary-600 hover:text-primary-700"
                  >
                    {parzelle.mieter.email}
                  </a>
                </div>
              </div>
            )}
            
            {parzelle.mieter.telefon && (
              <div>
                <span className="text-secondary-600">Telefon:</span>
                <div className="mt-1">
                  <a 
                    href={`tel:${parzelle.mieter.telefon}`}
                    className="text-primary-600 hover:text-primary-700"
                  >
                    {parzelle.mieter.telefon}
                  </a>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="text-center py-8 text-secondary-500">
            <svg className="mx-auto h-12 w-12 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            <p className="text-secondary-600 mb-4">
              Kein Mieter zugewiesen
            </p>
            {parzelle.status === 'frei' && (
              <Button size="sm">
                Mieter zuweisen
              </Button>
            )}
          </div>
        )}
      </Card>

      {/* Equipment & Features */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Ausstattung & Merkmale
        </h3>
        
        {parzelle.ausstattung && parzelle.ausstattung.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {parzelle.ausstattung.map((item, index) => (
              <span 
                key={index}
                className="inline-block px-3 py-1 text-sm bg-secondary-100 text-secondary-700 rounded-full"
              >
                {item}
              </span>
            ))}
          </div>
        ) : (
          <p className="text-secondary-500">
            Keine spezielle Ausstattung erfasst
          </p>
        )}
      </Card>
    </div>
  )
}

function ParzelleAddress({ parzelle }: { parzelle: Parzelle }) {
  if (!parzelle.adresse || !Object.values(parzelle.adresse).some(v => v?.trim())) {
    return null
  }

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold text-secondary-900 mb-4">
        Adresse
      </h3>
      
      <div className="text-secondary-700">
        {parzelle.adresse.strasse && parzelle.adresse.hausnummer && (
          <div className="mb-1">
            {parzelle.adresse.strasse} {parzelle.adresse.hausnummer}
          </div>
        )}
        {parzelle.adresse.plz && parzelle.adresse.ort && (
          <div>
            {parzelle.adresse.plz} {parzelle.adresse.ort}
          </div>
        )}
      </div>
    </Card>
  )
}

function ParzelleHistory({ history }: { history: ParzellenHistory[] }) {
  if (history.length === 0) {
    return (
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-secondary-900 mb-4">
          Verlauf
        </h3>
        <p className="text-secondary-500">
          Keine Verlaufseinträge verfügbar
        </p>
      </Card>
    )
  }

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold text-secondary-900 mb-4">
        Verlauf
      </h3>
      
      <div className="space-y-4">
        {history.slice(0, 10).map((entry) => (
          <div key={entry.id} className="flex items-start gap-3 pb-4 border-b border-secondary-100 last:border-b-0">
            <div className="flex-shrink-0 w-2 h-2 bg-primary-500 rounded-full mt-2"></div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <span className="text-sm font-medium text-secondary-900 capitalize">
                  {entry.aktion}
                </span>
                <span className="text-xs text-secondary-500">
                  {new Date(entry.zeitpunkt).toLocaleString('de-DE')}
                </span>
              </div>
              <p className="text-sm text-secondary-600">
                {entry.beschreibung}
              </p>
              {entry.durchgefuehrtVon && (
                <p className="text-xs text-secondary-500 mt-1">
                  von {entry.durchgefuehrtVon}
                </p>
              )}
            </div>
          </div>
        ))}
        
        {history.length > 10 && (
          <div className="text-center pt-4">
            <Button variant="outline" size="sm">
              Alle {history.length} Einträge anzeigen
            </Button>
          </div>
        )}
      </div>
    </Card>
  )
}

function ParzelleComments({ parzelle }: { parzelle: Parzelle }) {
  if (!parzelle.bemerkungen?.trim()) {
    return null
  }

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold text-secondary-900 mb-4">
        Bemerkungen
      </h3>
      <div className="prose prose-sm max-w-none">
        <p className="text-secondary-700 whitespace-pre-wrap">
          {parzelle.bemerkungen}
        </p>
      </div>
    </Card>
  )
}

function LoadingSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header Skeleton */}
      <div className="bg-white border border-secondary-200 rounded-lg p-6 animate-pulse">
        <div className="flex items-center gap-3 mb-4">
          <div className="h-8 bg-secondary-200 rounded w-1/3"></div>
          <div className="h-6 bg-secondary-200 rounded w-16"></div>
        </div>
        <div className="h-4 bg-secondary-200 rounded w-2/3 mb-4"></div>
        <div className="h-4 bg-secondary-200 rounded w-1/2"></div>
      </div>

      {/* Content Skeleton */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {[...Array(4)].map((_, i) => (
          <Card key={i} className="p-6 animate-pulse">
            <div className="h-5 bg-secondary-200 rounded w-1/3 mb-4"></div>
            <div className="space-y-3">
              {[...Array(4)].map((_, j) => (
                <div key={j} className="flex justify-between">
                  <div className="h-4 bg-secondary-200 rounded w-1/3"></div>
                  <div className="h-4 bg-secondary-200 rounded w-1/4"></div>
                </div>
              ))}
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default async function ParzelleDetailPage({ params }: ParzelleDetailPageProps) {
  const { id } = await params
  
  const parzelle = await getParzelle(id)
  
  if (!parzelle) {
    notFound()
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <nav className="flex" aria-label="Breadcrumb">
        <ol className="flex items-center space-x-2">
          <li>
            <Link href="/parzellen" className="text-secondary-500 hover:text-secondary-700">
              Parzellen
            </Link>
          </li>
          <li className="text-secondary-400">/</li>
          <li className="text-secondary-900 font-medium">
            Parzelle {parzelle.nummer}
          </li>
        </ol>
      </nav>

      {/* Header */}
      <ParzelleHeader parzelle={parzelle} />

      {/* Main Details */}
      <ParzelleDetails parzelle={parzelle} />

      {/* Address */}
      <ParzelleAddress parzelle={parzelle} />

      {/* Comments */}
      <ParzelleComments parzelle={parzelle} />

      {/* History */}
      <Suspense fallback={
        <Card className="p-6 animate-pulse">
          <div className="h-5 bg-secondary-200 rounded w-1/4 mb-4"></div>
          <div className="space-y-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-16 bg-secondary-200 rounded"></div>
            ))}
          </div>
        </Card>
      }>
        <ParzelleHistoryWrapper parzelleId={id} />
      </Suspense>
    </div>
  )
}

async function ParzelleHistoryWrapper({ parzelleId }: { parzelleId: string }) {
  const history = await getParzellenHistory(parzelleId)
  return <ParzelleHistory history={history} />
}