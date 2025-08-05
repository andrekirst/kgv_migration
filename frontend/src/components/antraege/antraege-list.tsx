'use client'

// List component for displaying Anträge with pagination and sorting
import * as React from 'react'
import { useRouter } from 'next/navigation'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { 
  FileTextIcon, 
  CalendarIcon, 
  UserIcon, 
  MapPinIcon,
  ArrowUpIcon,
  ArrowDownIcon,
  EyeIcon,
  EditIcon,
  TrashIcon
} from 'lucide-react'
import { formatDate } from '@/lib/utils'
import { AntragStatus } from '@/types/api'

// Mock data - replace with API calls
interface Antrag {
  id: string
  aktenzeichen: string
  vorname: string
  nachname: string
  email: string
  telefon: string
  bezirk: string
  status: AntragStatus
  antragsdatum: string
  bearbeitungsdatum?: string
  bemerkungen?: string
}

const mockAntraege: Antrag[] = [
  {
    id: '1',
    aktenzeichen: 'KGV-2024-001',
    vorname: 'Max',
    nachname: 'Mustermann',
    email: 'max.mustermann@example.com',
    telefon: '+49 30 12345678',
    bezirk: 'Nord',
    status: AntragStatus.Neu,
    antragsdatum: '2024-01-15T10:30:00Z'
  },
  {
    id: '2',
    aktenzeichen: 'KGV-2024-002',
    vorname: 'Anna',
    nachname: 'Schmidt',
    email: 'anna.schmidt@example.com',
    telefon: '+49 30 87654321',
    bezirk: 'Süd',
    status: AntragStatus.InBearbeitung,
    antragsdatum: '2024-01-14T14:15:00Z',
    bearbeitungsdatum: '2024-01-16T09:00:00Z'
  },
  {
    id: '3',
    aktenzeichen: 'KGV-2024-003',
    vorname: 'Thomas',
    nachname: 'Weber',
    email: 'thomas.weber@example.com',
    telefon: '+49 30 55566677',
    bezirk: 'Ost',
    status: AntragStatus.Genehmigt,
    antragsdatum: '2024-01-10T16:45:00Z',
    bearbeitungsdatum: '2024-01-12T11:30:00Z'
  }
]

interface AntraegeListProps {
  searchParams: {
    page?: string
    search?: string
    status?: string
    bezirk?: string
    sort?: string
    direction?: 'asc' | 'desc'
  }
}

export function AntraegeList({ searchParams }: AntraegeListProps) {
  const router = useRouter()
  
  // Parse search params
  const currentPage = parseInt(searchParams.page || '1', 10)
  const search = searchParams.search || ''
  const statusFilter = searchParams.status
  const bezirkFilter = searchParams.bezirk
  const sortField = searchParams.sort || 'antragsdatum'
  const sortDirection = searchParams.direction || 'desc'

  // Filter and sort data
  const filteredAntraege = React.useMemo(() => {
    const filtered = mockAntraege.filter(antrag => {
      const matchesSearch = !search || 
        antrag.aktenzeichen.toLowerCase().includes(search.toLowerCase()) ||
        antrag.vorname.toLowerCase().includes(search.toLowerCase()) ||
        antrag.nachname.toLowerCase().includes(search.toLowerCase()) ||
        antrag.email.toLowerCase().includes(search.toLowerCase())
      
      const matchesStatus = !statusFilter || antrag.status.toString() === statusFilter
      const matchesBezirk = !bezirkFilter || antrag.bezirk.toLowerCase() === bezirkFilter.toLowerCase()
      
      return matchesSearch && matchesStatus && matchesBezirk
    })

    // Sort
    filtered.sort((a, b) => {
      let aValue: any, bValue: any
      
      switch (sortField) {
        case 'aktenzeichen':
          aValue = a.aktenzeichen
          bValue = b.aktenzeichen
          break
        case 'name':
          aValue = `${a.nachname}, ${a.vorname}`
          bValue = `${b.nachname}, ${b.vorname}`
          break
        case 'bezirk':
          aValue = a.bezirk
          bValue = b.bezirk
          break
        case 'status':
          aValue = a.status
          bValue = b.status
          break
        case 'antragsdatum':
        default:
          aValue = new Date(a.antragsdatum)
          bValue = new Date(b.antragsdatum)
          break
      }
      
      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1
      return 0
    })

    return filtered
  }, [search, statusFilter, bezirkFilter, sortField, sortDirection])

  const handleSort = (field: string) => {
    const params = new URLSearchParams(window.location.search)
    const newDirection = sortField === field && sortDirection === 'asc' ? 'desc' : 'asc'
    
    params.set('sort', field)
    params.set('direction', newDirection)
    router.push(`/antraege?${params.toString()}`)
  }

  const getStatusBadge = (status: AntragStatus) => {
    const statusConfig = {
      [AntragStatus.Neu]: { label: 'Neu', variant: 'secondary' as const },
      [AntragStatus.InBearbeitung]: { label: 'In Bearbeitung', variant: 'default' as const },
      [AntragStatus.Wartend]: { label: 'Wartend', variant: 'secondary' as const },
      [AntragStatus.Genehmigt]: { label: 'Genehmigt', variant: 'secondary' as const },
      [AntragStatus.Abgelehnt]: { label: 'Abgelehnt', variant: 'destructive' as const },
      [AntragStatus.Archiviert]: { label: 'Archiviert', variant: 'secondary' as const }
    }
    
    const config = statusConfig[status]
    return (
      <Badge variant={config.variant} className="text-xs">
        {config.label}
      </Badge>
    )
  }

  const SortButton = ({ field, children }: { field: string; children: React.ReactNode }) => (
    <Button
      variant="ghost"
      size="sm"
      onClick={() => handleSort(field)}
      className="h-auto p-1 font-medium hover:bg-secondary-50"
    >
      {children}
      {sortField === field && (
        sortDirection === 'asc' ? 
          <ArrowUpIcon className="ml-1 h-3 w-3" /> : 
          <ArrowDownIcon className="ml-1 h-3 w-3" />
      )}
    </Button>
  )

  if (filteredAntraege.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center justify-center py-12">
          <FileTextIcon className="h-12 w-12 text-secondary-400 mb-4" />
          <h3 className="text-lg font-medium text-secondary-900 mb-2">
            Keine Anträge gefunden
          </h3>
          <p className="text-sm text-secondary-500 text-center max-w-md">
            {search || statusFilter || bezirkFilter ? 
              'Versuchen Sie andere Suchkriterien oder entfernen Sie einige Filter.' :
              'Es wurden noch keine Anträge eingereicht.'
            }
          </p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      {/* Results count */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-secondary-600">
          {filteredAntraege.length} {filteredAntraege.length === 1 ? 'Antrag' : 'Anträge'} gefunden
        </p>
      </div>

      {/* Desktop table view */}
      <div className="hidden lg:block">
        <Card>
          <CardHeader className="pb-3">
            <div className="grid grid-cols-12 gap-4 text-sm font-medium text-secondary-700">
              <div className="col-span-2">
                <SortButton field="aktenzeichen">Aktenzeichen</SortButton>
              </div>
              <div className="col-span-3">
                <SortButton field="name">Name</SortButton>
              </div>
              <div className="col-span-2">
                <SortButton field="bezirk">Bezirk</SortButton>
              </div>
              <div className="col-span-2">
                <SortButton field="status">Status</SortButton>
              </div>
              <div className="col-span-2">
                <SortButton field="antragsdatum">Antragsdatum</SortButton>
              </div>
              <div className="col-span-1 text-right">
                Aktionen
              </div>
            </div>
          </CardHeader>
          <CardContent className="pt-0">
            <div className="space-y-2">
              {filteredAntraege.map((antrag) => (
                <div
                  key={antrag.id}
                  className="grid grid-cols-12 gap-4 items-center py-3 border-b border-secondary-100 last:border-b-0 hover:bg-secondary-50 rounded-lg transition-colors"
                >
                  <div className="col-span-2">
                    <code className="text-sm font-mono bg-secondary-100 px-2 py-1 rounded">
                      {antrag.aktenzeichen}
                    </code>
                  </div>
                  <div className="col-span-3">
                    <div>
                      <p className="font-medium text-secondary-900">
                        {antrag.nachname}, {antrag.vorname}
                      </p>
                      <p className="text-sm text-secondary-500">{antrag.email}</p>
                    </div>
                  </div>
                  <div className="col-span-2">
                    <div className="flex items-center text-sm text-secondary-600">
                      <MapPinIcon className="h-4 w-4 mr-1" />
                      {antrag.bezirk}
                    </div>
                  </div>
                  <div className="col-span-2">
                    {getStatusBadge(antrag.status)}
                  </div>
                  <div className="col-span-2">
                    <div className="flex items-center text-sm text-secondary-600">
                      <CalendarIcon className="h-4 w-4 mr-1" />
                      {formatDate(antrag.antragsdatum)}
                    </div>
                  </div>
                  <div className="col-span-1 flex justify-end space-x-1">
                    <Button size="sm" variant="ghost" className="h-8 w-8 p-0">
                      <EyeIcon className="h-4 w-4" />
                    </Button>
                    <Button size="sm" variant="ghost" className="h-8 w-8 p-0">
                      <EditIcon className="h-4 w-4" />
                    </Button>
                    <Button size="sm" variant="ghost" className="h-8 w-8 p-0 text-destructive">
                      <TrashIcon className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Mobile card view */}
      <div className="lg:hidden space-y-4">
        {filteredAntraege.map((antrag) => (
          <Card key={antrag.id} className="hover:shadow-md transition-shadow">
            <CardContent className="p-4">
              <div className="flex items-start justify-between mb-3">
                <div>
                  <code className="text-xs font-mono bg-secondary-100 px-2 py-1 rounded">
                    {antrag.aktenzeichen}
                  </code>
                  <h3 className="font-medium text-secondary-900 mt-1">
                    {antrag.nachname}, {antrag.vorname}
                  </h3>
                </div>
                {getStatusBadge(antrag.status)}
              </div>
              
              <div className="space-y-2 text-sm text-secondary-600">
                <div className="flex items-center">
                  <UserIcon className="h-4 w-4 mr-2" />
                  {antrag.email}
                </div>
                <div className="flex items-center">
                  <MapPinIcon className="h-4 w-4 mr-2" />
                  {antrag.bezirk}
                </div>
                <div className="flex items-center">
                  <CalendarIcon className="h-4 w-4 mr-2" />
                  {formatDate(antrag.antragsdatum)}
                </div>
              </div>
              
              <div className="flex justify-end space-x-2 mt-4">
                <Button size="sm" variant="outline">
                  <EyeIcon className="h-4 w-4 mr-1" />
                  Ansehen
                </Button>
                <Button size="sm" variant="outline">
                  <EditIcon className="h-4 w-4 mr-1" />
                  Bearbeiten
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}