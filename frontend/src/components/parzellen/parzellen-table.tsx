'use client'

import React, { useState, useMemo } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuCheckboxItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
} from '@/components/ui/dropdown-menu'
import { 
  Search, 
  Filter,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Edit,
  Trash2,
  UserPlus,
  MoreVertical,
  Building,
  User,
  Euro,
  Ruler,
  CheckCircle,
  Clock,
  AlertTriangle,
  Ban,
  Calendar,
  Phone,
  Mail
} from 'lucide-react'
import { Parzelle, ParzellenFilter, ParzellenStatus } from '@/types/bezirke'

interface ParzellenTableProps {
  parzellen: Parzelle[]
  loading?: boolean
  onFilterChange?: (filters: ParzellenFilter) => void
  onParzelleClick?: (parzelle: Parzelle) => void
  onParzelleEdit?: (parzelleId: number) => void
  onParzelleAssign?: (parzelleId: number) => void
  onParzelleDelete?: (parzelleId: number) => void
  className?: string
  showBezirk?: boolean
  selectable?: boolean
  selectedIds?: number[]
  onSelectionChange?: (selectedIds: number[]) => void
}

type SortField = 'nummer' | 'bezirkName' | 'groesse' | 'monatlichePacht' | 'status' | 'mieter' | 'erstelltAm'

const statusConfig = {
  [ParzellenStatus.FREI]: {
    label: 'Frei',
    icon: CheckCircle,
    color: 'text-green-600 dark:text-green-400',
    bg: 'bg-green-50 dark:bg-green-900/20',
    variant: 'outline' as const
  },
  [ParzellenStatus.BELEGT]: {
    label: 'Belegt',
    icon: User,
    color: 'text-blue-600 dark:text-blue-400',
    bg: 'bg-blue-50 dark:bg-blue-900/20',
    variant: 'secondary' as const
  },
  [ParzellenStatus.RESERVIERT]: {
    label: 'Reserviert',
    icon: Clock,
    color: 'text-amber-600 dark:text-amber-400',
    bg: 'bg-amber-50 dark:bg-amber-900/20',
    variant: 'outline' as const
  },
  [ParzellenStatus.WARTUNG]: {
    label: 'Wartung',
    icon: AlertTriangle,
    color: 'text-orange-600 dark:text-orange-400',
    bg: 'bg-orange-50 dark:bg-orange-900/20',
    variant: 'outline' as const
  },
  [ParzellenStatus.GESPERRT]: {
    label: 'Gesperrt',
    icon: Ban,
    color: 'text-red-600 dark:text-red-400',
    bg: 'bg-red-50 dark:bg-red-900/20',
    variant: 'destructive' as const
  }
}

export function ParzellenTable({
  parzellen,
  loading = false,
  onFilterChange,
  onParzelleClick,
  onParzelleEdit,
  onParzelleAssign,
  onParzelleDelete,
  className,
  showBezirk = true,
  selectable = false,
  selectedIds = [],
  onSelectionChange
}: ParzellenTableProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [sortField, setSortField] = useState<SortField>('nummer')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [selectedStatuses, setSelectedStatuses] = useState<ParzellenStatus[]>([])
  const [showOnlyActive, setShowOnlyActive] = useState(true)

  // Filter and sort parzellen
  const filteredAndSortedParzellen = useMemo(() => {
    let filtered = parzellen.filter(parzelle => {
      const matchesSearch = searchQuery === '' || 
        parzelle.nummer.toLowerCase().includes(searchQuery.toLowerCase()) ||
        parzelle.bezirkName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        parzelle.beschreibung?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        parzelle.mieter?.vorname.toLowerCase().includes(searchQuery.toLowerCase()) ||
        parzelle.mieter?.nachname.toLowerCase().includes(searchQuery.toLowerCase())
      
      const matchesActive = !showOnlyActive || parzelle.aktiv
      const matchesStatus = selectedStatuses.length === 0 || selectedStatuses.includes(parzelle.status)
      
      return matchesSearch && matchesActive && matchesStatus
    })

    // Sort parzellen
    filtered.sort((a, b) => {
      let comparison = 0
      
      switch (sortField) {
        case 'nummer':
          comparison = a.nummer.localeCompare(b.nummer, 'de-DE', { numeric: true })
          break
        case 'bezirkName':
          comparison = a.bezirkName.localeCompare(b.bezirkName, 'de-DE')
          break
        case 'groesse':
          comparison = a.groesse - b.groesse
          break
        case 'monatlichePacht':
          comparison = a.monatlichePacht - b.monatlichePacht
          break
        case 'status':
          comparison = a.status.localeCompare(b.status, 'de-DE')
          break
        case 'mieter':
          const mieterA = a.mieter ? `${a.mieter.nachname}, ${a.mieter.vorname}` : ''
          const mieterB = b.mieter ? `${b.mieter.nachname}, ${b.mieter.vorname}` : ''
          comparison = mieterA.localeCompare(mieterB, 'de-DE')
          break
        case 'erstelltAm':
          comparison = new Date(a.erstelltAm).getTime() - new Date(b.erstelltAm).getTime()
          break
      }
      
      return sortDirection === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [parzellen, searchQuery, sortField, sortDirection, selectedStatuses, showOnlyActive])

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
    
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: showOnlyActive || undefined,
      status: selectedStatuses.length > 0 ? selectedStatuses : undefined,
      sortBy: field as any,
      sortOrder: sortField === field && sortDirection === 'asc' ? 'desc' : 'asc'
    })
  }

  const getSortIcon = (field: SortField) => {
    if (sortField !== field) {
      return <ArrowUpDown className="ml-2 h-4 w-4 opacity-50" />
    }
    return sortDirection === 'asc' 
      ? <ArrowUp className="ml-2 h-4 w-4" />
      : <ArrowDown className="ml-2 h-4 w-4" />
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount)
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('de-DE', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const handleStatusToggle = (status: ParzellenStatus) => {
    const newStatuses = selectedStatuses.includes(status)
      ? selectedStatuses.filter(s => s !== status)
      : [...selectedStatuses, status]
    
    setSelectedStatuses(newStatuses)
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: showOnlyActive || undefined,
      status: newStatuses.length > 0 ? newStatuses : undefined,
      sortBy: sortField as any,
      sortOrder: sortDirection
    })
  }

  const handleSelectAll = () => {
    if (selectedIds.length === filteredAndSortedParzellen.length) {
      onSelectionChange?.([])
    } else {
      onSelectionChange?.(filteredAndSortedParzellen.map(p => p.id))
    }
  }

  const handleSelectParzelle = (parzelleId: number) => {
    if (selectedIds.includes(parzelleId)) {
      onSelectionChange?.(selectedIds.filter(id => id !== parzelleId))
    } else {
      onSelectionChange?.([...selectedIds, parzelleId])
    }
  }

  if (loading) {
    return (
      <Card className={className}>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Parzellen</CardTitle>
            <div className="flex gap-2">
              <div className="h-10 w-64 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
              <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 8 }).map((_, index) => (
              <div key={index} className="h-16 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4">
          <CardTitle className="flex items-center gap-2">
            <Building className="h-5 w-5" />
            Parzellen ({filteredAndSortedParzellen.length})
          </CardTitle>
          
          <div className="flex flex-col sm:flex-row gap-2 w-full lg:w-auto">
            <div className="relative flex-1 sm:flex-initial sm:w-64">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                placeholder="Parzellen durchsuchen..."
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value)
                  onFilterChange?.({
                    search: e.target.value || undefined,
                    aktiv: showOnlyActive || undefined,
                    status: selectedStatuses.length > 0 ? selectedStatuses : undefined,
                    sortBy: sortField as any,
                    sortOrder: sortDirection
                  })
                }}
                className="pl-10"
              />
            </div>
            
            <div className="flex gap-2">
              <Button
                variant={showOnlyActive ? "default" : "outline"}
                size="sm"
                onClick={() => {
                  const newShowOnlyActive = !showOnlyActive
                  setShowOnlyActive(newShowOnlyActive)
                  onFilterChange?.({
                    search: searchQuery || undefined,
                    aktiv: newShowOnlyActive || undefined,
                    status: selectedStatuses.length > 0 ? selectedStatuses : undefined,
                    sortBy: sortField as any,
                    sortOrder: sortDirection
                  })
                }}
              >
                <Filter className="h-4 w-4 sm:mr-2" />
                <span className="hidden sm:inline">Nur Aktive</span>
              </Button>
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="sm" className="gap-2">
                    <Filter className="h-4 w-4" />
                    <span className="hidden sm:inline">Status</span>
                    {selectedStatuses.length > 0 && (
                      <Badge variant="secondary" className="ml-1 h-4 w-4 p-0 text-xs">
                        {selectedStatuses.length}
                      </Badge>
                    )}
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  <DropdownMenuLabel>Status filtern</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  {Object.entries(statusConfig).map(([status, config]) => {
                    const Icon = config.icon
                    return (
                      <DropdownMenuCheckboxItem
                        key={status}
                        checked={selectedStatuses.includes(status as ParzellenStatus)}
                        onCheckedChange={() => handleStatusToggle(status as ParzellenStatus)}
                      >
                        <Icon className={`mr-2 h-4 w-4 ${config.color}`} />
                        {config.label}
                      </DropdownMenuCheckboxItem>
                    )
                  })}
                  {selectedStatuses.length > 0 && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onClick={() => setSelectedStatuses([])}>
                        Alle auswählen
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        {filteredAndSortedParzellen.length === 0 ? (
          <div className="text-center py-12">
            <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Keine Parzellen gefunden
            </h3>
            <p className="text-gray-600 dark:text-gray-400">
              {searchQuery || selectedStatuses.length > 0
                ? 'Keine Parzellen entsprechen den aktuellen Filterkriterien.'
                : 'Es wurden noch keine Parzellen erstellt.'
              }
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  {selectable && (
                    <TableHead className="w-12">
                      <input
                        type="checkbox"
                        checked={selectedIds.length === filteredAndSortedParzellen.length && filteredAndSortedParzellen.length > 0}
                        onChange={handleSelectAll}
                        className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      />
                    </TableHead>
                  )}
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('nummer')}
                      className="-ml-4 font-semibold"
                    >
                      Nummer
                      {getSortIcon('nummer')}
                    </Button>
                  </TableHead>
                  {showBezirk && (
                    <TableHead>
                      <Button 
                        variant="ghost" 
                        onClick={() => handleSort('bezirkName')}
                        className="-ml-4 font-semibold"
                      >
                        Bezirk
                        {getSortIcon('bezirkName')}
                      </Button>
                    </TableHead>
                  )}
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('groesse')}
                      className="-ml-4 font-semibold"
                    >
                      Größe
                      {getSortIcon('groesse')}
                    </Button>
                  </TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('monatlichePacht')}
                      className="-ml-4 font-semibold"
                    >
                      Pacht
                      {getSortIcon('monatlichePacht')}
                    </Button>
                  </TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('status')}
                      className="-ml-4 font-semibold"
                    >
                      Status
                      {getSortIcon('status')}
                    </Button>
                  </TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('mieter')}
                      className="-ml-4 font-semibold"
                    >
                      Mieter
                      {getSortIcon('mieter')}
                    </Button>
                  </TableHead>
                  <TableHead>Mietzeit</TableHead>
                  <TableHead>Aktiv</TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('erstelltAm')}
                      className="-ml-4 font-semibold"
                    >
                      Erstellt
                      {getSortIcon('erstelltAm')}
                    </Button>
                  </TableHead>
                  <TableHead className="w-12"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredAndSortedParzellen.map((parzelle) => {
                  const config = statusConfig[parzelle.status]
                  const StatusIcon = config.icon
                  
                  return (
                    <TableRow
                      key={parzelle.id}
                      className={`cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 ${!parzelle.aktiv ? 'opacity-60' : ''}`}
                      onClick={() => onParzelleClick?.(parzelle)}
                    >
                      {selectable && (
                        <TableCell>
                          <input
                            type="checkbox"
                            checked={selectedIds.includes(parzelle.id)}
                            onChange={(e) => {
                              e.stopPropagation()
                              handleSelectParzelle(parzelle.id)
                            }}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                        </TableCell>
                      )}
                      <TableCell>
                        <div className="font-medium text-gray-900 dark:text-gray-100">
                          {parzelle.nummer}
                        </div>
                        {parzelle.beschreibung && (
                          <div className="text-sm text-gray-600 dark:text-gray-400 line-clamp-1">
                            {parzelle.beschreibung}
                          </div>
                        )}
                      </TableCell>
                      {showBezirk && (
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Building className="h-4 w-4 text-gray-400" />
                            <span className="text-gray-900 dark:text-gray-100">
                              {parzelle.bezirkName}
                            </span>
                          </div>
                        </TableCell>
                      )}
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Ruler className="h-4 w-4 text-gray-400" />
                          <span className="font-medium">{parzelle.groesse} m²</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Euro className="h-4 w-4 text-gray-400" />
                          <span className="font-medium">{formatCurrency(parzelle.monatlichePacht)}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={config.variant} className={`gap-1 ${config.color} ${config.bg}`}>
                          <StatusIcon className="h-3 w-3" />
                          {config.label}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {parzelle.mieter ? (
                          <div>
                            <div className="font-medium text-gray-900 dark:text-gray-100">
                              {parzelle.mieter.vorname} {parzelle.mieter.nachname}
                            </div>
                            <div className="flex gap-3 text-xs text-gray-600 dark:text-gray-400">
                              {parzelle.mieter.telefon && (
                                <a 
                                  href={`tel:${parzelle.mieter.telefon}`}
                                  className="flex items-center gap-1 hover:text-blue-600 dark:hover:text-blue-400"
                                  onClick={(e) => e.stopPropagation()}
                                >
                                  <Phone className="h-3 w-3" />
                                  {parzelle.mieter.telefon}
                                </a>
                              )}
                              {parzelle.mieter.email && (
                                <a 
                                  href={`mailto:${parzelle.mieter.email}`}
                                  className="flex items-center gap-1 hover:text-blue-600 dark:hover:text-blue-400 truncate"
                                  onClick={(e) => e.stopPropagation()}
                                >
                                  <Mail className="h-3 w-3" />
                                  {parzelle.mieter.email}
                                </a>
                              )}
                            </div>
                          </div>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        {parzelle.mietbeginn ? (
                          <div className="text-sm">
                            <div className="flex items-center gap-1 text-gray-600 dark:text-gray-400">
                              <Calendar className="h-3 w-3" />
                              <span>Seit {formatDate(parzelle.mietbeginn)}</span>
                            </div>
                            {parzelle.mietende && (
                              <div className="text-xs text-gray-500 dark:text-gray-400">
                                Bis {formatDate(parzelle.mietende)}
                              </div>
                            )}
                          </div>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <Badge variant={parzelle.aktiv ? "outline" : "secondary"}>
                          {parzelle.aktiv ? "Aktiv" : "Inaktiv"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="text-sm text-gray-600 dark:text-gray-400">
                          {formatDate(parzelle.erstelltAm)}
                        </div>
                      </TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button 
                              variant="ghost" 
                              size="sm" 
                              className="h-8 w-8 p-0"
                              onClick={(e) => e.stopPropagation()}
                            >
                              <MoreVertical className="h-4 w-4" />
                              <span className="sr-only">Aktionen für Parzelle {parzelle.nummer}</span>
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onParzelleEdit?.(parzelle.id) }}>
                              <Edit className="mr-2 h-4 w-4" />
                              Bearbeiten
                            </DropdownMenuItem>
                            {parzelle.status === ParzellenStatus.FREI && (
                              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onParzelleAssign?.(parzelle.id) }}>
                                <UserPlus className="mr-2 h-4 w-4" />
                                Zuweisen
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem 
                              onClick={(e) => { e.stopPropagation(); onParzelleDelete?.(parzelle.id) }} 
                              className="text-red-600 focus:text-red-600"
                            >
                              <Trash2 className="mr-2 h-4 w-4" />
                              Löschen
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  )
}