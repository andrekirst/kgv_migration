'use client'

import React, { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { 
  Filter, 
  X, 
  Search,
  SlidersHorizontal,
  Calendar,
  MapPin,
  Users,
  Building,
  BarChart3
} from 'lucide-react'
import { BezirkeFilter } from '@/types/bezirke'

interface BezirkeFiltersProps {
  filters: BezirkeFilter
  onFiltersChange: (filters: BezirkeFilter) => void
  availableOrte?: string[]
  availableBezirksleiter?: string[]
  className?: string
  compact?: boolean
  showResetButton?: boolean
}

export function BezirkeFilters({
  filters,
  onFiltersChange,
  availableOrte = [],
  availableBezirksleiter = [],
  className,
  compact = false,
  showResetButton = true
}: BezirkeFiltersProps) {
  const [localFilters, setLocalFilters] = useState<BezirkeFilter>(filters)
  const [isOpen, setIsOpen] = useState(false)

  // Sync with external filter changes
  useEffect(() => {
    setLocalFilters(filters)
  }, [filters])

  const updateFilter = (key: keyof BezirkeFilter, value: any) => {
    const newFilters = { ...localFilters, [key]: value }
    setLocalFilters(newFilters)
    onFiltersChange(newFilters)
  }

  const clearFilter = (key: keyof BezirkeFilter) => {
    const newFilters = { ...localFilters }
    delete newFilters[key]
    setLocalFilters(newFilters)
    onFiltersChange(newFilters)
  }

  const clearAllFilters = () => {
    const clearedFilters: BezirkeFilter = {}
    setLocalFilters(clearedFilters)
    onFiltersChange(clearedFilters)
  }

  const getActiveFiltersCount = () => {
    if (!localFilters) return 0
    return Object.keys(localFilters).filter(key => {
      const value = localFilters[key as keyof BezirkeFilter]
      return value !== undefined && value !== '' && value !== null
    }).length
  }

  const activeFiltersCount = getActiveFiltersCount()

  const FilterBadge = ({ 
    label, 
    value, 
    onRemove 
  }: { 
    label: string
    value: string | number | boolean
    onRemove: () => void 
  }) => (
    <Badge variant="secondary" className="gap-1 pr-1">
      <span className="text-xs">{label}: {String(value)}</span>
      <Button
        variant="ghost"
        size="sm"
        className="h-4 w-4 p-0 hover:bg-transparent"
        onClick={onRemove}
      >
        <X className="h-3 w-3" />
      </Button>
    </Badge>
  )

  if (compact) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
          <Input
            placeholder="Bezirke durchsuchen..."
            value={localFilters?.search || ''}
            onChange={(e) => updateFilter('search', e.target.value || undefined)}
            className="pl-10"
          />
        </div>
        
        <Popover open={isOpen} onOpenChange={setIsOpen}>
          <PopoverTrigger asChild>
            <Button variant="outline" size="sm" className="gap-2">
              <SlidersHorizontal className="h-4 w-4" />
              Filter
              {activeFiltersCount > 0 && (
                <Badge variant="destructive" className="ml-1 h-5 w-5 p-0 text-xs">
                  {activeFiltersCount}
                </Badge>
              )}
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-80" align="end">
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h4 className="font-medium">Filter</h4>
                {activeFiltersCount > 0 && showResetButton && (
                  <Button variant="ghost" size="sm" onClick={clearAllFilters}>
                    Alle löschen
                  </Button>
                )}
              </div>
              
              <div className="grid gap-4">
                {/* Active Status */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Status</label>
                  <Select
                    value={localFilters?.aktiv === undefined ? 'all' : String(localFilters?.aktiv)}
                    onValueChange={(value) => 
                      updateFilter('aktiv', value === 'all' ? undefined : value === 'true')
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Status auswählen" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Alle</SelectItem>
                      <SelectItem value="true">Nur Aktive</SelectItem>
                      <SelectItem value="false">Nur Inaktive</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Location Filter */}
                {availableOrte.length > 0 && (
                  <div className="space-y-2">
                    <label className="text-sm font-medium flex items-center gap-2">
                      <MapPin className="h-4 w-4" />
                      Ort
                    </label>
                    <Select
                      value={localFilters?.search || ''}
                      onValueChange={(value) => updateFilter('search', value || undefined)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Ort auswählen" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="">Alle Orte</SelectItem>
                        {availableOrte.map((ort) => (
                          <SelectItem key={ort} value={ort}>
                            {ort}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {/* Bezirksleiter Filter */}
                {availableBezirksleiter.length > 0 && (
                  <div className="space-y-2">
                    <label className="text-sm font-medium flex items-center gap-2">
                      <Users className="h-4 w-4" />
                      Bezirksleiter
                    </label>
                    <Select
                      value={localFilters?.search || ''}
                      onValueChange={(value) => updateFilter('search', value || undefined)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Bezirksleiter auswählen" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="">Alle</SelectItem>
                        {availableBezirksleiter.map((leiter) => (
                          <SelectItem key={leiter} value={leiter}>
                            {leiter}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {/* Sort Options */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Sortierung</label>
                  <div className="grid grid-cols-2 gap-2">
                    <Select
                      value={localFilters?.sortBy || 'name'}
                      onValueChange={(value) => updateFilter('sortBy', value as any)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="name">Name</SelectItem>
                        <SelectItem value="erstelltAm">Erstellungsdatum</SelectItem>
                        <SelectItem value="gesamtParzellen">Parzellen-Anzahl</SelectItem>
                      </SelectContent>
                    </Select>
                    <Select
                      value={localFilters?.sortOrder || 'asc'}
                      onValueChange={(value) => updateFilter('sortOrder', value as any)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="asc">Aufsteigend</SelectItem>
                        <SelectItem value="desc">Absteigend</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>
            </div>
          </PopoverContent>
        </Popover>
      </div>
    )
  }

  return (
    <Card className={className}>
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg flex items-center gap-2">
            <Filter className="h-5 w-5" />
            Filter & Suche
          </CardTitle>
          {activeFiltersCount > 0 && showResetButton && (
            <Button variant="outline" size="sm" onClick={clearAllFilters}>
              Alle löschen ({activeFiltersCount})
            </Button>
          )}
        </div>
      </CardHeader>
      
      <CardContent className="space-y-6">
        {/* Search */}
        <div className="space-y-2">
          <label className="text-sm font-medium flex items-center gap-2">
            <Search className="h-4 w-4" />
            Suche
          </label>
          <Input
            placeholder="Name, Beschreibung oder Bezirksleiter..."
            value={localFilters?.search || ''}
            onChange={(e) => updateFilter('search', e.target.value || undefined)}
          />
        </div>

        {/* Active Status */}
        <div className="space-y-2">
          <label className="text-sm font-medium flex items-center gap-2">
            <Building className="h-4 w-4" />
            Status
          </label>
          <Select
            value={localFilters?.aktiv === undefined ? 'all' : String(localFilters?.aktiv)}
            onValueChange={(value) => 
              updateFilter('aktiv', value === 'all' ? undefined : value === 'true')
            }
          >
            <SelectTrigger>
              <SelectValue placeholder="Status auswählen" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Alle Bezirke</SelectItem>
              <SelectItem value="true">Nur Aktive</SelectItem>
              <SelectItem value="false">Nur Inaktive</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Location Filter */}
        {availableOrte.length > 0 && (
          <div className="space-y-2">
            <label className="text-sm font-medium flex items-center gap-2">
              <MapPin className="h-4 w-4" />
              Ort
            </label>
            <Select
              value={localFilters?.search || ''}
              onValueChange={(value) => updateFilter('search', value || undefined)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Ort auswählen" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Alle Orte</SelectItem>
                {availableOrte.map((ort) => (
                  <SelectItem key={ort} value={ort}>
                    {ort}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {/* Bezirksleiter Filter */}
        {availableBezirksleiter.length > 0 && (
          <div className="space-y-2">
            <label className="text-sm font-medium flex items-center gap-2">
              <Users className="h-4 w-4" />
              Bezirksleiter
            </label>
            <Select
              value={localFilters?.search || ''}
              onValueChange={(value) => updateFilter('search', value || undefined)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Bezirksleiter auswählen" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Alle Bezirksleiter</SelectItem>
                {availableBezirksleiter.map((leiter) => (
                  <SelectItem key={leiter} value={leiter}>
                    {leiter}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {/* Sort Options */}
        <div className="space-y-2">
          <label className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Sortierung
          </label>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Sortieren nach</label>
              <Select
                value={localFilters?.sortBy || 'name'}
                onValueChange={(value) => updateFilter('sortBy', value as any)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="name">Name</SelectItem>
                  <SelectItem value="erstelltAm">Erstellungsdatum</SelectItem>
                  <SelectItem value="gesamtParzellen">Parzellen-Anzahl</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Reihenfolge</label>
              <Select
                value={localFilters?.sortOrder || 'asc'}
                onValueChange={(value) => updateFilter('sortOrder', value as any)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="asc">Aufsteigend</SelectItem>
                  <SelectItem value="desc">Absteigend</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        {/* Active Filters Display */}
        {activeFiltersCount > 0 && (
          <div className="space-y-2">
            <label className="text-sm font-medium">Aktive Filter</label>
            <div className="flex flex-wrap gap-2">
              {localFilters?.search && (
                <FilterBadge
                  label="Suche"
                  value={localFilters?.search}
                  onRemove={() => clearFilter('search')}
                />
              )}
              {localFilters?.aktiv !== undefined && (
                <FilterBadge
                  label="Status"
                  value={localFilters?.aktiv ? 'Aktiv' : 'Inaktiv'}
                  onRemove={() => clearFilter('aktiv')}
                />
              )}
              {localFilters?.sortBy && localFilters?.sortBy !== 'name' && (
                <FilterBadge
                  label="Sortierung"
                  value={`${localFilters?.sortBy} ${localFilters?.sortOrder === 'desc' ? 'absteigend' : 'aufsteigend'}`}
                  onRemove={() => {
                    clearFilter('sortBy')
                    clearFilter('sortOrder')
                  }}
                />
              )}
            </div>
          </div>
        )}

        {/* Pagination Controls */}
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <label className="text-xs text-gray-600 dark:text-gray-400">Seite</label>
            <Input
              type="number"
              min="1"
              placeholder="1"
              value={localFilters?.page || ''}
              onChange={(e) => updateFilter('page', e.target.value ? parseInt(e.target.value) : undefined)}
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs text-gray-600 dark:text-gray-400">Anzahl pro Seite</label>
            <Select
              value={String(localFilters?.limit || 20)}
              onValueChange={(value) => updateFilter('limit', parseInt(value))}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="10">10</SelectItem>
                <SelectItem value="20">20</SelectItem>
                <SelectItem value="50">50</SelectItem>
                <SelectItem value="100">100</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}