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
} from '@/components/ui/dropdown-menu'
import { 
  Search, 
  Filter,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Edit,
  Trash2,
  MoreVertical,
  Building,
  Users,
  Phone,
  Mail,
  MapPin,
  BarChart3
} from 'lucide-react'
import { Bezirk, BezirkeFilter } from '@/types/bezirke'

interface BezirkeTableProps {
  bezirke: Bezirk[]
  loading?: boolean
  onFilterChange?: (filters: BezirkeFilter) => void
  onBezirkClick?: (bezirk: Bezirk) => void
  onBezirkEdit?: (bezirkId: number) => void
  onBezirkDelete?: (bezirkId: number) => void
  className?: string
  selectable?: boolean
  selectedIds?: number[]
  onSelectionChange?: (selectedIds: number[]) => void
}

type SortField = 'name' | 'bezirksleiter' | 'gesamtParzellen' | 'auslastung' | 'erstelltAm'

export function BezirkeTable({
  bezirke,
  loading = false,
  onFilterChange,
  onBezirkClick,
  onBezirkEdit,
  onBezirkDelete,
  className,
  selectable = false,
  selectedIds = [],
  onSelectionChange
}: BezirkeTableProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [sortField, setSortField] = useState<SortField>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [showOnlyActive, setShowOnlyActive] = useState(true)

  // Filter and sort bezirke
  const filteredAndSortedBezirke = useMemo(() => {
    let filtered = bezirke.filter(bezirk => {
      const matchesSearch = searchQuery === '' || 
        bezirk.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        bezirk.beschreibung?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        bezirk.bezirksleiter?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        bezirk.adresse?.ort?.toLowerCase().includes(searchQuery.toLowerCase())
      
      const matchesActive = !showOnlyActive || bezirk.aktiv
      
      return matchesSearch && matchesActive
    })

    // Sort bezirke
    filtered.sort((a, b) => {
      let comparison = 0
      
      switch (sortField) {
        case 'name':
          comparison = a.name.localeCompare(b.name, 'de-DE')
          break
        case 'bezirksleiter':
          comparison = (a.bezirksleiter || '').localeCompare(b.bezirksleiter || '', 'de-DE')
          break
        case 'gesamtParzellen':
          comparison = a.statistiken.gesamtParzellen - b.statistiken.gesamtParzellen
          break
        case 'auslastung':
          const auslastungA = a.statistiken.gesamtParzellen > 0 
            ? (a.statistiken.belegteParzellen / a.statistiken.gesamtParzellen) * 100 
            : 0
          const auslastungB = b.statistiken.gesamtParzellen > 0 
            ? (b.statistiken.belegteParzellen / b.statistiken.gesamtParzellen) * 100 
            : 0
          comparison = auslastungA - auslastungB
          break
        case 'erstelltAm':
          comparison = new Date(a.erstelltAm).getTime() - new Date(b.erstelltAm).getTime()
          break
      }
      
      return sortDirection === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [bezirke, searchQuery, sortField, sortDirection, showOnlyActive])

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
      sortBy: field,
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

  const getAuslastung = (bezirk: Bezirk) => {
    return bezirk.statistiken.gesamtParzellen > 0 
      ? Math.round((bezirk.statistiken.belegteParzellen / bezirk.statistiken.gesamtParzellen) * 100)
      : 0
  }

  const getAuslastungColor = (percentage: number) => {
    if (percentage >= 90) return 'text-red-600 dark:text-red-400'
    if (percentage >= 75) return 'text-amber-600 dark:text-amber-400'
    return 'text-green-600 dark:text-green-400'
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('de-DE', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const handleSelectAll = () => {
    if (selectedIds.length === filteredAndSortedBezirke.length) {
      onSelectionChange?.([])
    } else {
      onSelectionChange?.(filteredAndSortedBezirke.map(b => b.id))
    }
  }

  const handleSelectBezirk = (bezirkId: number) => {
    if (selectedIds.includes(bezirkId)) {
      onSelectionChange?.(selectedIds.filter(id => id !== bezirkId))
    } else {
      onSelectionChange?.([...selectedIds, bezirkId])
    }
  }

  if (loading) {
    return (
      <Card className={className}>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Bezirke</CardTitle>
            <div className="flex gap-2">
              <div className="h-10 w-64 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
              <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, index) => (
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
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <CardTitle className="flex items-center gap-2">
            <Building className="h-5 w-5" />
            Bezirke ({filteredAndSortedBezirke.length})
          </CardTitle>
          
          <div className="flex gap-2 w-full sm:w-auto">
            <div className="relative flex-1 sm:flex-initial sm:w-64">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                placeholder="Bezirke durchsuchen..."
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value)
                  onFilterChange?.({
                    search: e.target.value || undefined,
                    aktiv: showOnlyActive || undefined,
                    sortBy: sortField,
                    sortOrder: sortDirection
                  })
                }}
                className="pl-10"
              />
            </div>
            
            <Button
              variant={showOnlyActive ? "default" : "outline"}
              size="sm"
              onClick={() => {
                const newShowOnlyActive = !showOnlyActive
                setShowOnlyActive(newShowOnlyActive)
                onFilterChange?.({
                  search: searchQuery || undefined,
                  aktiv: newShowOnlyActive || undefined,
                  sortBy: sortField,
                  sortOrder: sortDirection
                })
              }}
            >
              <Filter className="h-4 w-4 mr-2" />
              Nur Aktive
            </Button>
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        {filteredAndSortedBezirke.length === 0 ? (
          <div className="text-center py-12">
            <Building className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Keine Bezirke gefunden
            </h3>
            <p className="text-gray-600 dark:text-gray-400">
              {searchQuery 
                ? `Keine Bezirke entsprechen der Suche "${searchQuery}".`
                : 'Es wurden noch keine Bezirke erstellt.'
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
                        checked={selectedIds.length === filteredAndSortedBezirke.length && filteredAndSortedBezirke.length > 0}
                        onChange={handleSelectAll}
                        className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      />
                    </TableHead>
                  )}
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('name')}
                      className="-ml-4 font-semibold"
                    >
                      Name
                      {getSortIcon('name')}
                    </Button>
                  </TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('bezirksleiter')}
                      className="-ml-4 font-semibold"
                    >
                      Bezirksleiter
                      {getSortIcon('bezirksleiter')}
                    </Button>
                  </TableHead>
                  <TableHead>Kontakt</TableHead>
                  <TableHead>Adresse</TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('gesamtParzellen')}
                      className="-ml-4 font-semibold"
                    >
                      Parzellen
                      {getSortIcon('gesamtParzellen')}
                    </Button>
                  </TableHead>
                  <TableHead>
                    <Button 
                      variant="ghost" 
                      onClick={() => handleSort('auslastung')}
                      className="-ml-4 font-semibold"
                    >
                      Auslastung
                      {getSortIcon('auslastung')}
                    </Button>
                  </TableHead>
                  <TableHead>Status</TableHead>
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
                {filteredAndSortedBezirke.map((bezirk) => {
                  const auslastung = getAuslastung(bezirk)
                  
                  return (
                    <TableRow
                      key={bezirk.id}
                      className={`cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 ${!bezirk.aktiv ? 'opacity-60' : ''}`}
                      onClick={() => onBezirkClick?.(bezirk)}
                    >
                      {selectable && (
                        <TableCell>
                          <input
                            type="checkbox"
                            checked={selectedIds.includes(bezirk.id)}
                            onChange={(e) => {
                              e.stopPropagation()
                              handleSelectBezirk(bezirk.id)
                            }}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                        </TableCell>
                      )}
                      <TableCell>
                        <div>
                          <div className="font-medium text-gray-900 dark:text-gray-100">
                            {bezirk.name}
                          </div>
                          {bezirk.beschreibung && (
                            <div className="text-sm text-gray-600 dark:text-gray-400 line-clamp-1">
                              {bezirk.beschreibung}
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        {bezirk.bezirksleiter ? (
                          <div className="flex items-center gap-2">
                            <Users className="h-4 w-4 text-gray-400" />
                            <span className="text-gray-900 dark:text-gray-100">
                              {bezirk.bezirksleiter}
                            </span>
                          </div>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {bezirk.telefon && (
                            <div className="flex items-center gap-1 text-sm">
                              <Phone className="h-3 w-3 text-gray-400" />
                              <a 
                                href={`tel:${bezirk.telefon}`}
                                className="text-blue-600 dark:text-blue-400 hover:underline"
                                onClick={(e) => e.stopPropagation()}
                              >
                                {bezirk.telefon}
                              </a>
                            </div>
                          )}
                          {bezirk.email && (
                            <div className="flex items-center gap-1 text-sm">
                              <Mail className="h-3 w-3 text-gray-400" />
                              <a 
                                href={`mailto:${bezirk.email}`}
                                className="text-blue-600 dark:text-blue-400 hover:underline truncate"
                                onClick={(e) => e.stopPropagation()}
                              >
                                {bezirk.email}
                              </a>
                            </div>
                          )}
                          {!bezirk.telefon && !bezirk.email && (
                            <span className="text-gray-400">—</span>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        {bezirk.adresse?.ort ? (
                          <div className="flex items-start gap-1 text-sm">
                            <MapPin className="h-3 w-3 text-gray-400 mt-0.5 flex-shrink-0" />
                            <div>
                              {bezirk.adresse.strasse && (
                                <div>{bezirk.adresse.strasse} {bezirk.adresse.hausnummer}</div>
                              )}
                              <div>{bezirk.adresse.plz} {bezirk.adresse.ort}</div>
                            </div>
                          </div>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="text-center">
                          <div className="text-sm font-medium text-gray-900 dark:text-gray-100">
                            {bezirk.statistiken.gesamtParzellen}
                          </div>
                          <div className="text-xs text-gray-600 dark:text-gray-400">
                            {bezirk.statistiken.belegteParzellen} belegt, {bezirk.statistiken.freieParzellen} frei
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <BarChart3 className={`h-4 w-4 ${getAuslastungColor(auslastung)}`} />
                          <span className={`font-medium ${getAuslastungColor(auslastung)}`}>
                            {auslastung}%
                          </span>
                        </div>
                        {bezirk.statistiken.warteliste > 0 && (
                          <div className="text-xs text-amber-600 dark:text-amber-400 mt-1">
                            {bezirk.statistiken.warteliste} wartend
                          </div>
                        )}
                      </TableCell>
                      <TableCell>
                        <Badge variant={bezirk.aktiv ? "outline" : "secondary"}>
                          {bezirk.aktiv ? "Aktiv" : "Inaktiv"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="text-sm text-gray-600 dark:text-gray-400">
                          {formatDate(bezirk.erstelltAm)}
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
                              <span className="sr-only">Aktionen für {bezirk.name}</span>
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onBezirkEdit?.(bezirk.id) }}>
                              <Edit className="mr-2 h-4 w-4" />
                              Bearbeiten
                            </DropdownMenuItem>
                            <DropdownMenuItem 
                              onClick={(e) => { e.stopPropagation(); onBezirkDelete?.(bezirk.id) }} 
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