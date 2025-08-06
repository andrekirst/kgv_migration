'use client'

import React, { useState, useCallback, useMemo } from 'react'
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
  Grid, 
  List, 
  SortAsc, 
  SortDesc, 
  MapPin, 
  User,
  Euro,
  Ruler,
  Building,
  Calendar,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  Ban
} from 'lucide-react'
import { Parzelle, ParzellenFilter, ParzellenStatus } from '@/types/bezirke'

interface ParzellenListProps {
  parzellen: Parzelle[]
  loading?: boolean
  onFilterChange?: (filters: ParzellenFilter) => void
  onParzelleClick?: (parzelle: Parzelle) => void
  onParzelleEdit?: (parzelleId: number) => void
  onParzelleAssign?: (parzelleId: number) => void
  onParzelleDelete?: (parzelleId: number) => void
  className?: string
  showBezirkFilter?: boolean
  defaultBezirkId?: number
}

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

export function ParzellenList({
  parzellen,
  loading = false,
  onFilterChange,
  onParzelleClick,
  onParzelleEdit,
  onParzelleAssign,
  onParzelleDelete,
  className,
  showBezirkFilter = true,
  defaultBezirkId
}: ParzellenListProps) {
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid')
  const [searchQuery, setSearchQuery] = useState('')
  const [sortBy, setSortBy] = useState<'nummer' | 'groesse' | 'monatlichePacht' | 'erstelltAm'>('nummer')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [selectedStatuses, setSelectedStatuses] = useState<ParzellenStatus[]>([])
  const [showOnlyActive, setShowOnlyActive] = useState(true)
  const [groesseRange, setGroesseRange] = useState<{ min?: number; max?: number }>({})
  const [pachtRange, setPachtRange] = useState<{ min?: number; max?: number }>({})

  // Get unique bezirke for filter
  const availableBezirke = useMemo(() => {
    const bezirkeMap = new Map()
    parzellen.forEach(parzelle => {
      if (!bezirkeMap.has(parzelle.bezirkId)) {
        bezirkeMap.set(parzelle.bezirkId, parzelle.bezirkName)
      }
    })
    return Array.from(bezirkeMap.entries()).map(([id, name]) => ({ id: Number(id), name: String(name) }))
  }, [parzellen])

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
      
      const matchesGroesse = 
        (!groesseRange.min || parzelle.groesse >= groesseRange.min) &&
        (!groesseRange.max || parzelle.groesse <= groesseRange.max)
      
      const matchesPacht = 
        (!pachtRange.min || parzelle.monatlichePacht >= pachtRange.min) &&
        (!pachtRange.max || parzelle.monatlichePacht <= pachtRange.max)
      
      return matchesSearch && matchesActive && matchesStatus && matchesGroesse && matchesPacht
    })

    // Sort parzellen
    filtered.sort((a, b) => {
      let comparison = 0
      
      switch (sortBy) {
        case 'nummer':
          comparison = a.nummer.localeCompare(b.nummer, 'de-DE', { numeric: true })
          break
        case 'groesse':
          comparison = a.groesse - b.groesse
          break
        case 'monatlichePacht':
          comparison = a.monatlichePacht - b.monatlichePacht
          break
        case 'erstelltAm':
          comparison = new Date(a.erstelltAm).getTime() - new Date(b.erstelltAm).getTime()
          break
      }
      
      return sortOrder === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [parzellen, searchQuery, sortBy, sortOrder, showOnlyActive, selectedStatuses, groesseRange, pachtRange])

  const handleSearchChange = useCallback((value: string) => {
    setSearchQuery(value)
    onFilterChange?.({
      search: value || undefined,
      aktiv: showOnlyActive || undefined,
      status: selectedStatuses.length > 0 ? selectedStatuses : undefined,
      groesseMin: groesseRange.min,
      groesseMax: groesseRange.max,
      pachtMin: pachtRange.min,
      pachtMax: pachtRange.max,
      sortBy,
      sortOrder
    })
  }, [onFilterChange, showOnlyActive, selectedStatuses, groesseRange, pachtRange, sortBy, sortOrder])

  const handleSortChange = useCallback((newSortBy: typeof sortBy) => {
    const newSortOrder = sortBy === newSortBy && sortOrder === 'asc' ? 'desc' : 'asc'
    setSortBy(newSortBy)
    setSortOrder(newSortOrder)
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: showOnlyActive || undefined,
      status: selectedStatuses.length > 0 ? selectedStatuses : undefined,
      groesseMin: groesseRange.min,
      groesseMax: groesseRange.max,
      pachtMin: pachtRange.min,
      pachtMax: pachtRange.max,
      sortBy: newSortBy,
      sortOrder: newSortOrder
    })
  }, [sortBy, sortOrder, onFilterChange, searchQuery, showOnlyActive, selectedStatuses, groesseRange, pachtRange])

  const handleStatusToggle = useCallback((status: ParzellenStatus) => {
    const newStatuses = selectedStatuses.includes(status)
      ? selectedStatuses.filter(s => s !== status)
      : [...selectedStatuses, status]
    
    setSelectedStatuses(newStatuses)
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: showOnlyActive || undefined,
      status: newStatuses.length > 0 ? newStatuses : undefined,
      groesseMin: groesseRange.min,
      groesseMax: groesseRange.max,
      pachtMin: pachtRange.min,
      pachtMax: pachtRange.max,
      sortBy,
      sortOrder
    })
  }, [selectedStatuses, onFilterChange, searchQuery, showOnlyActive, groesseRange, pachtRange, sortBy, sortOrder])

  if (loading) {
    return (
      <div className={`space-y-4 ${className}`}>
        <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
          <div className="flex-1 max-w-sm">
            <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
          </div>
          <div className="flex gap-2 flex-wrap">
            <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {Array.from({ length: 9 }).map((_, index) => (
            <div key={index} className="h-80 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Filter and Search Controls */}
      <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
          <Input
            placeholder="Parzellen durchsuchen..."
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="pl-10"
          />
        </div>
        
        <div className="flex gap-2 flex-wrap">
          <Button
            variant={showOnlyActive ? "default" : "outline"}
            size="sm"
            onClick={() => setShowOnlyActive(!showOnlyActive)}
            className="gap-2"
          >
            <Filter className="h-4 w-4" />
            Nur Aktive
          </Button>
          
          {/* Status Filter */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2">
                <Filter className="h-4 w-4" />
                Status
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
          
          {/* Sort Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2">
                {sortOrder === 'asc' ? <SortAsc className="h-4 w-4" /> : <SortDesc className="h-4 w-4" />}
                Sortierung
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => handleSortChange('nummer')}>
                Nach Nummer {sortBy === 'nummer' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleSortChange('groesse')}>
                Nach Größe {sortBy === 'groesse' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleSortChange('monatlichePacht')}>
                Nach Pacht {sortBy === 'monatlichePacht' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleSortChange('erstelltAm')}>
                Nach Erstellungsdatum {sortBy === 'erstelltAm' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          
          {/* View Mode Toggle */}
          <div className="flex border rounded-md">
            <Button
              variant={viewMode === 'grid' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('grid')}
              className="rounded-r-none"
            >
              <Grid className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === 'list' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('list')}
              className="rounded-l-none"
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Active Filters Display */}
      {(selectedStatuses.length > 0 || groesseRange.min || groesseRange.max || pachtRange.min || pachtRange.max) && (
        <div className="flex flex-wrap gap-2 items-center">
          <span className="text-sm text-gray-600 dark:text-gray-400">Aktive Filter:</span>
          {selectedStatuses.map(status => {
            const config = statusConfig[status]
            const Icon = config.icon
            return (
              <Badge key={status} variant="outline" className="gap-1">
                <Icon className={`h-3 w-3 ${config.color}`} />
                {config.label}
                <XCircle 
                  className="h-3 w-3 cursor-pointer" 
                  onClick={() => handleStatusToggle(status)}
                />
              </Badge>
            )
          })}
          {(selectedStatuses.length > 0 || groesseRange.min || groesseRange.max || pachtRange.min || pachtRange.max) && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSelectedStatuses([])
                setGroesseRange({})
                setPachtRange({})
              }}
              className="text-red-600 hover:text-red-700"
            >
              Alle Filter löschen
            </Button>
          )}
        </div>
      )}

      {/* Results Summary */}
      <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400">
        <span>
          {filteredAndSortedParzellen.length} von {parzellen.length} Parzellen
          {searchQuery && ` gefunden für "${searchQuery}"`}
        </span>
        {!showOnlyActive && (
          <span className="text-amber-600 dark:text-amber-400">
            Inaktive Parzellen werden angezeigt
          </span>
        )}
      </div>

      {/* Parzellen Grid/List */}
      {filteredAndSortedParzellen.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Building className="h-12 w-12 text-gray-400 mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Keine Parzellen gefunden
            </h3>
            <p className="text-gray-600 dark:text-gray-400 text-center max-w-md">
              {searchQuery || selectedStatuses.length > 0
                ? 'Keine Parzellen entsprechen den aktuellen Filterkriterien.'
                : 'Es wurden noch keine Parzellen erstellt.'
              }
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className={
          viewMode === 'grid' 
            ? 'grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6'
            : 'space-y-4'
        }>
          {filteredAndSortedParzellen.map((parzelle) => (
            <ParzelleCardPreview
              key={parzelle.id}
              parzelle={parzelle}
              viewMode={viewMode}
              onClick={() => onParzelleClick?.(parzelle)}
              onEdit={() => onParzelleEdit?.(parzelle.id)}
              onAssign={() => onParzelleAssign?.(parzelle.id)}
              onDelete={() => onParzelleDelete?.(parzelle.id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

interface ParzelleCardPreviewProps {
  parzelle: Parzelle
  viewMode: 'grid' | 'list'
  onClick?: () => void
  onEdit?: () => void
  onAssign?: () => void
  onDelete?: () => void
}

function ParzelleCardPreview({ parzelle, viewMode, onClick, onEdit, onAssign, onDelete }: ParzelleCardPreviewProps) {
  const statusConfig = {
    [ParzellenStatus.FREI]: {
      label: 'Frei',
      icon: CheckCircle,
      color: 'text-green-600 dark:text-green-400',
      bg: 'bg-green-50 dark:bg-green-900/20'
    },
    [ParzellenStatus.BELEGT]: {
      label: 'Belegt',
      icon: User,
      color: 'text-blue-600 dark:text-blue-400',
      bg: 'bg-blue-50 dark:bg-blue-900/20'
    },
    [ParzellenStatus.RESERVIERT]: {
      label: 'Reserviert',
      icon: Clock,
      color: 'text-amber-600 dark:text-amber-400',
      bg: 'bg-amber-50 dark:bg-amber-900/20'
    },
    [ParzellenStatus.WARTUNG]: {
      label: 'Wartung',
      icon: AlertTriangle,
      color: 'text-orange-600 dark:text-orange-400',
      bg: 'bg-orange-50 dark:bg-orange-900/20'
    },
    [ParzellenStatus.GESPERRT]: {
      label: 'Gesperrt',
      icon: Ban,
      color: 'text-red-600 dark:text-red-400',
      bg: 'bg-red-50 dark:bg-red-900/20'
    }
  }

  const config = statusConfig[parzelle.status]
  const StatusIcon = config.icon

  if (viewMode === 'list') {
    return (
      <Card className={`cursor-pointer transition-all hover:shadow-md ${!parzelle.aktiv ? 'opacity-60' : ''}`} onClick={onClick}>
        <CardContent className="p-4">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div className="flex-1">
              <div className="flex items-start gap-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h3 className="font-semibold text-lg text-gray-900 dark:text-gray-100">
                      Parzelle {parzelle.nummer}
                    </h3>
                    <Badge variant="outline" className={`gap-1 ${config.color} ${config.bg}`}>
                      <StatusIcon className="h-3 w-3" />
                      {config.label}
                    </Badge>
                    {!parzelle.aktiv && (
                      <Badge variant="secondary" className="text-xs">
                        Inaktiv
                      </Badge>
                    )}
                  </div>
                  
                  <div className="flex flex-wrap gap-4 text-sm text-gray-600 dark:text-gray-400 mb-2">
                    <div className="flex items-center gap-1">
                      <Building className="h-4 w-4" />
                      {parzelle.bezirkName}
                    </div>
                    <div className="flex items-center gap-1">
                      <Ruler className="h-4 w-4" />
                      {parzelle.groesse} m²
                    </div>
                    <div className="flex items-center gap-1">
                      <Euro className="h-4 w-4" />
                      {parzelle.monatlichePacht.toFixed(2)} €/Monat
                    </div>
                  </div>
                  
                  {parzelle.mieter && (
                    <div className="flex items-center gap-1 text-sm text-gray-600 dark:text-gray-400">
                      <User className="h-4 w-4" />
                      {parzelle.mieter.vorname} {parzelle.mieter.nachname}
                    </div>
                  )}
                </div>
              </div>
            </div>
            
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={(e) => { e.stopPropagation(); onEdit?.() }}>
                Bearbeiten
              </Button>
              {parzelle.status === ParzellenStatus.FREI && (
                <Button variant="default" size="sm" onClick={(e) => { e.stopPropagation(); onAssign?.() }}>
                  Zuweisen
                </Button>
              )}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm">
                    ⋯
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.() }}>
                    Bearbeiten
                  </DropdownMenuItem>
                  {parzelle.status === ParzellenStatus.FREI && (
                    <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onAssign?.() }}>
                      Zuweisen
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDelete?.() }} className="text-red-600">
                    Löschen
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={`cursor-pointer transition-all hover:shadow-md hover:scale-[1.02] ${!parzelle.aktiv ? 'opacity-60' : ''}`} onClick={onClick}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <CardTitle className="text-lg mb-1 flex items-center gap-2 flex-wrap">
              Parzelle {parzelle.nummer}
              <Badge variant="outline" className={`gap-1 ${config.color} ${config.bg}`}>
                <StatusIcon className="h-3 w-3" />
                {config.label}
              </Badge>
              {!parzelle.aktiv && (
                <Badge variant="secondary" className="text-xs">
                  Inaktiv
                </Badge>
              )}
            </CardTitle>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              {parzelle.bezirkName}
            </p>
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" onClick={(e) => e.stopPropagation()}>
                ⋯
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.() }}>
                Bearbeiten
              </DropdownMenuItem>
              {parzelle.status === ParzellenStatus.FREI && (
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onAssign?.() }}>
                  Zuweisen
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDelete?.() }} className="text-red-600">
                Löschen
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Basic Info */}
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="flex items-center gap-1 text-gray-600 dark:text-gray-400">
            <Ruler className="h-4 w-4" />
            <span>{parzelle.groesse} m²</span>
          </div>
          <div className="flex items-center gap-1 text-gray-600 dark:text-gray-400">
            <Euro className="h-4 w-4" />
            <span>{parzelle.monatlichePacht.toFixed(2)} €</span>
          </div>
        </div>
        
        {/* Mieter Info */}
        {parzelle.mieter ? (
          <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3">
            <div className="flex items-center gap-2 text-sm">
              <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <span className="font-medium text-blue-900 dark:text-blue-100">
                {parzelle.mieter.vorname} {parzelle.mieter.nachname}
              </span>
            </div>
            {parzelle.mietbeginn && (
              <div className="flex items-center gap-1 text-xs text-blue-700 dark:text-blue-300 mt-1">
                <Calendar className="h-3 w-3" />
                Seit {new Date(parzelle.mietbeginn).toLocaleDateString('de-DE')}
              </div>
            )}
          </div>
        ) : (
          <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-3 text-center">
            <div className="text-sm text-green-700 dark:text-green-300 font-medium">
              Verfügbar
            </div>
            {parzelle.status === ParzellenStatus.FREI && (
              <Button 
                variant="outline" 
                size="sm" 
                className="mt-2 text-green-700 border-green-300 hover:bg-green-100"
                onClick={(e) => { e.stopPropagation(); onAssign?.() }}
              >
                Zuweisen
              </Button>
            )}
          </div>
        )}
        
        {/* Address */}
        {parzelle.adresse?.ort && (
          <div className="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400">
            <MapPin className="h-4 w-4 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              {parzelle.adresse.strasse && (
                <div>{parzelle.adresse.strasse} {parzelle.adresse.hausnummer}</div>
              )}
              <div>{parzelle.adresse.plz} {parzelle.adresse.ort}</div>
            </div>
          </div>
        )}
        
        {/* Equipment */}
        {parzelle.ausstattung && parzelle.ausstattung.length > 0 && (
          <div className="border-t pt-3">
            <div className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Ausstattung
            </div>
            <div className="flex flex-wrap gap-1">
              {parzelle.ausstattung.slice(0, 3).map((item, index) => (
                <Badge key={index} variant="outline" className="text-xs">
                  {item}
                </Badge>
              ))}
              {parzelle.ausstattung.length > 3 && (
                <Badge variant="outline" className="text-xs">
                  +{parzelle.ausstattung.length - 3} weitere
                </Badge>
              )}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}