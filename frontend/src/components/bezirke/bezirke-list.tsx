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
} from '@/components/ui/dropdown-menu'
import { 
  Search, 
  Filter, 
  Grid, 
  List, 
  SortAsc, 
  SortDesc, 
  MapPin, 
  Users,
  Phone,
  Mail,
  Building,
  BarChart3
} from 'lucide-react'
import { Bezirk, BezirkeFilter } from '@/types/bezirke'

interface BezirkeListProps {
  bezirke: Bezirk[]
  loading?: boolean
  onFilterChange?: (filters: BezirkeFilter) => void
  onBezirkClick?: (bezirk: Bezirk) => void
  onBezirkEdit?: (bezirkId: number) => void
  onBezirkDelete?: (bezirkId: number) => void
  className?: string
}

export function BezirkeList({
  bezirke,
  loading = false,
  onFilterChange,
  onBezirkClick,
  onBezirkEdit,
  onBezirkDelete,
  className
}: BezirkeListProps) {
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid')
  const [searchQuery, setSearchQuery] = useState('')
  const [sortBy, setSortBy] = useState<'name' | 'erstelltAm' | 'gesamtParzellen'>('name')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const [showOnlyActive, setShowOnlyActive] = useState(true)

  // Filter and sort bezirke
  const filteredAndSortedBezirke = useMemo(() => {
    let filtered = bezirke.filter(bezirk => {
      const matchesSearch = searchQuery === '' || 
        bezirk.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        bezirk.beschreibung?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        bezirk.bezirksleiter?.toLowerCase().includes(searchQuery.toLowerCase())
      
      const matchesActive = !showOnlyActive || bezirk.aktiv
      
      return matchesSearch && matchesActive
    })

    // Sort bezirke
    filtered.sort((a, b) => {
      let comparison = 0
      
      switch (sortBy) {
        case 'name':
          comparison = a.name.localeCompare(b.name, 'de-DE')
          break
        case 'erstelltAm':
          comparison = new Date(a.erstelltAm).getTime() - new Date(b.erstelltAm).getTime()
          break
        case 'gesamtParzellen':
          comparison = a.statistiken.gesamtParzellen - b.statistiken.gesamtParzellen
          break
      }
      
      return sortOrder === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [bezirke, searchQuery, sortBy, sortOrder, showOnlyActive])

  const handleSearchChange = useCallback((value: string) => {
    setSearchQuery(value)
    onFilterChange?.({
      search: value || undefined,
      aktiv: showOnlyActive || undefined,
      sortBy,
      sortOrder
    })
  }, [onFilterChange, showOnlyActive, sortBy, sortOrder])

  const handleSortChange = useCallback((newSortBy: typeof sortBy) => {
    const newSortOrder = sortBy === newSortBy && sortOrder === 'asc' ? 'desc' : 'asc'
    setSortBy(newSortBy)
    setSortOrder(newSortOrder)
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: showOnlyActive || undefined,
      sortBy: newSortBy,
      sortOrder: newSortOrder
    })
  }, [sortBy, sortOrder, onFilterChange, searchQuery, showOnlyActive])

  const handleActiveFilterToggle = useCallback(() => {
    const newShowOnlyActive = !showOnlyActive
    setShowOnlyActive(newShowOnlyActive)
    onFilterChange?.({
      search: searchQuery || undefined,
      aktiv: newShowOnlyActive || undefined,
      sortBy,
      sortOrder
    })
  }, [showOnlyActive, onFilterChange, searchQuery, sortBy, sortOrder])

  if (loading) {
    return (
      <div className={`space-y-4 ${className}`}>
        <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
          <div className="flex-1 max-w-sm">
            <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
          </div>
          <div className="flex gap-2">
            <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
            <div className="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded-md animate-pulse" />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 6 }).map((_, index) => (
            <div key={index} className="h-64 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Filter and Search Controls */}
      <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
          <Input
            placeholder="Bezirke durchsuchen..."
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="pl-10"
          />
        </div>
        
        <div className="flex gap-2 flex-wrap">
          <Button
            variant={showOnlyActive ? "default" : "outline"}
            size="sm"
            onClick={handleActiveFilterToggle}
            className="gap-2"
          >
            <Filter className="h-4 w-4" />
            Nur Aktive
          </Button>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2">
                {sortOrder === 'asc' ? <SortAsc className="h-4 w-4" /> : <SortDesc className="h-4 w-4" />}
                Sortierung
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => handleSortChange('name')}>
                Nach Name {sortBy === 'name' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleSortChange('erstelltAm')}>
                Nach Erstellungsdatum {sortBy === 'erstelltAm' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleSortChange('gesamtParzellen')}>
                Nach Parzellen-Anzahl {sortBy === 'gesamtParzellen' && (sortOrder === 'asc' ? '↑' : '↓')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          
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

      {/* Results Summary */}
      <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400">
        <span>
          {filteredAndSortedBezirke.length} von {bezirke.length} Bezirken
          {searchQuery && ` gefunden für "${searchQuery}"`}
        </span>
        {!showOnlyActive && (
          <span className="text-amber-600 dark:text-amber-400">
            Inaktive Bezirke werden angezeigt
          </span>
        )}
      </div>

      {/* Bezirke Grid/List */}
      {filteredAndSortedBezirke.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Building className="h-12 w-12 text-gray-400 mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
              Keine Bezirke gefunden
            </h3>
            <p className="text-gray-600 dark:text-gray-400 text-center max-w-md">
              {searchQuery 
                ? `Keine Bezirke entsprechen der Suche "${searchQuery}".`
                : 'Es wurden noch keine Bezirke erstellt.'
              }
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className={
          viewMode === 'grid' 
            ? 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'
            : 'space-y-4'
        }>
          {filteredAndSortedBezirke.map((bezirk) => (
            <BezirkCard
              key={bezirk.id}
              bezirk={bezirk}
              viewMode={viewMode}
              onClick={() => onBezirkClick?.(bezirk)}
              onEdit={() => onBezirkEdit?.(bezirk.id)}
              onDelete={() => onBezirkDelete?.(bezirk.id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

interface BezirkCardProps {
  bezirk: Bezirk
  viewMode: 'grid' | 'list'
  onClick?: () => void
  onEdit?: () => void
  onDelete?: () => void
}

function BezirkCard({ bezirk, viewMode, onClick, onEdit, onDelete }: BezirkCardProps) {
  const auslastung = Math.round((bezirk.statistiken.belegteParzellen / bezirk.statistiken.gesamtParzellen) * 100) || 0
  
  const getAuslastungColor = (percentage: number) => {
    if (percentage >= 90) return 'text-red-600 dark:text-red-400'
    if (percentage >= 75) return 'text-amber-600 dark:text-amber-400'
    return 'text-green-600 dark:text-green-400'
  }

  if (viewMode === 'list') {
    return (
      <Card className={`cursor-pointer transition-all hover:shadow-md ${!bezirk.aktiv ? 'opacity-60' : ''}`} onClick={onClick}>
        <CardContent className="p-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="flex-1">
              <div className="flex items-start gap-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="font-semibold text-lg text-gray-900 dark:text-gray-100">
                      {bezirk.name}
                    </h3>
                    {!bezirk.aktiv && (
                      <Badge variant="secondary" className="text-xs">
                        Inaktiv
                      </Badge>
                    )}
                  </div>
                  
                  {bezirk.beschreibung && (
                    <p className="text-gray-600 dark:text-gray-400 text-sm mb-2 line-clamp-2">
                      {bezirk.beschreibung}
                    </p>
                  )}
                  
                  <div className="flex flex-wrap gap-4 text-sm text-gray-600 dark:text-gray-400">
                    {bezirk.bezirksleiter && (
                      <div className="flex items-center gap-1">
                        <Users className="h-4 w-4" />
                        {bezirk.bezirksleiter}
                      </div>
                    )}
                    {bezirk.adresse?.ort && (
                      <div className="flex items-center gap-1">
                        <MapPin className="h-4 w-4" />
                        {bezirk.adresse.ort}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
            
            <div className="flex items-center gap-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                  {bezirk.statistiken.gesamtParzellen}
                </div>
                <div className="text-xs text-gray-600 dark:text-gray-400">Parzellen</div>
              </div>
              
              <div className="text-center">
                <div className={`text-2xl font-bold ${getAuslastungColor(auslastung)}`}>
                  {auslastung}%
                </div>
                <div className="text-xs text-gray-600 dark:text-gray-400">Auslastung</div>
              </div>
              
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={(e) => { e.stopPropagation(); onEdit?.() }}>
                  Bearbeiten
                </Button>
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
                    <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDelete?.() }} className="text-red-600">
                      Löschen
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={`cursor-pointer transition-all hover:shadow-md ${!bezirk.aktiv ? 'opacity-60' : ''}`} onClick={onClick}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <CardTitle className="text-lg mb-1 flex items-center gap-2">
              {bezirk.name}
              {!bezirk.aktiv && (
                <Badge variant="secondary" className="text-xs">
                  Inaktiv
                </Badge>
              )}
            </CardTitle>
            {bezirk.beschreibung && (
              <p className="text-gray-600 dark:text-gray-400 text-sm line-clamp-2">
                {bezirk.beschreibung}
              </p>
            )}
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
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDelete?.() }} className="text-red-600">
                Löschen
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Bezirksleiter and Contact */}
        {(bezirk.bezirksleiter || bezirk.telefon || bezirk.email) && (
          <div className="space-y-2">
            {bezirk.bezirksleiter && (
              <div className="flex items-center gap-2 text-sm">
                <Users className="h-4 w-4 text-gray-400" />
                <span className="text-gray-900 dark:text-gray-100">{bezirk.bezirksleiter}</span>
              </div>
            )}
            <div className="flex gap-4">
              {bezirk.telefon && (
                <div className="flex items-center gap-1 text-sm text-gray-600 dark:text-gray-400">
                  <Phone className="h-3 w-3" />
                  {bezirk.telefon}
                </div>
              )}
              {bezirk.email && (
                <div className="flex items-center gap-1 text-sm text-gray-600 dark:text-gray-400">
                  <Mail className="h-3 w-3" />
                  {bezirk.email}
                </div>
              )}
            </div>
          </div>
        )}
        
        {/* Address */}
        {bezirk.adresse?.ort && (
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
            <MapPin className="h-4 w-4" />
            <span>
              {bezirk.adresse.strasse && `${bezirk.adresse.strasse} ${bezirk.adresse.hausnummer || ''}, `}
              {bezirk.adresse.plz} {bezirk.adresse.ort}
            </span>
          </div>
        )}
        
        {/* Statistics */}
        <div className="border-t pt-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Parzellen-Übersicht
            </span>
            <Badge variant="outline" className="gap-1">
              <BarChart3 className="h-3 w-3" />
              {auslastung}% Auslastung
            </Badge>
          </div>
          
          <div className="grid grid-cols-3 gap-3 text-center">
            <div>
              <div className="text-lg font-bold text-gray-900 dark:text-gray-100">
                {bezirk.statistiken.gesamtParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400">Gesamt</div>
            </div>
            <div>
              <div className="text-lg font-bold text-green-600 dark:text-green-400">
                {bezirk.statistiken.belegteParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400">Belegt</div>
            </div>
            <div>
              <div className="text-lg font-bold text-blue-600 dark:text-blue-400">
                {bezirk.statistiken.freieParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400">Frei</div>
            </div>
          </div>
          
          {bezirk.statistiken.warteliste > 0 && (
            <div className="mt-3 text-center">
              <Badge variant="outline" className="text-amber-600 dark:text-amber-400">
                {bezirk.statistiken.warteliste} auf Warteliste
              </Badge>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}