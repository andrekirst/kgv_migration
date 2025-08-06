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
  Building,
  User,
  Euro,
  Ruler,
  CheckCircle,
  Clock,
  AlertTriangle,
  Ban,
  BarChart3
} from 'lucide-react'
import { ParzellenFilter, ParzellenStatus } from '@/types/bezirke'

interface ParzellenFiltersProps {
  filters: ParzellenFilter
  onFiltersChange: (filters: ParzellenFilter) => void
  availableBezirke?: Array<{ id: number; name: string }>
  className?: string
  compact?: boolean
  showResetButton?: boolean
  showBezirkFilter?: boolean
}

const statusConfig = {
  [ParzellenStatus.FREI]: {
    label: 'Frei',
    icon: CheckCircle,
    color: 'text-green-600 dark:text-green-400'
  },
  [ParzellenStatus.BELEGT]: {
    label: 'Belegt',
    icon: User,
    color: 'text-blue-600 dark:text-blue-400'
  },
  [ParzellenStatus.RESERVIERT]: {
    label: 'Reserviert',
    icon: Clock,
    color: 'text-amber-600 dark:text-amber-400'
  },
  [ParzellenStatus.WARTUNG]: {
    label: 'Wartung',
    icon: AlertTriangle,
    color: 'text-orange-600 dark:text-orange-400'
  },
  [ParzellenStatus.GESPERRT]: {
    label: 'Gesperrt',
    icon: Ban,
    color: 'text-red-600 dark:text-red-400'
  }
}

export function ParzellenFilters({
  filters,
  onFiltersChange,
  availableBezirke = [],
  className,
  compact = false,
  showResetButton = true,
  showBezirkFilter = true
}: ParzellenFiltersProps) {
  const [localFilters, setLocalFilters] = useState<ParzellenFilter>(filters)
  const [isOpen, setIsOpen] = useState(false)

  // Sync with external filter changes
  useEffect(() => {
    setLocalFilters(filters)
  }, [filters])

  const updateFilter = (key: keyof ParzellenFilter, value: any) => {
    const newFilters = { ...localFilters, [key]: value }
    setLocalFilters(newFilters)
    onFiltersChange(newFilters)
  }

  const clearFilter = (key: keyof ParzellenFilter) => {
    const newFilters = { ...localFilters }
    delete newFilters[key]
    setLocalFilters(newFilters)
    onFiltersChange(newFilters)
  }

  const clearAllFilters = () => {
    const clearedFilters: ParzellenFilter = {}
    setLocalFilters(clearedFilters)
    onFiltersChange(clearedFilters)
  }

  const getActiveFiltersCount = () => {
    return Object.keys(localFilters).filter(key => {
      const value = localFilters[key as keyof ParzellenFilter]
      if (Array.isArray(value)) {
        return value.length > 0
      }
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
    value: string | number | boolean | string[]
    onRemove: () => void 
  }) => (
    <Badge variant="secondary" className="gap-1 pr-1">
      <span className="text-xs">
        {label}: {Array.isArray(value) ? value.join(', ') : String(value)}
      </span>
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
            placeholder="Parzellen durchsuchen..."
            value={localFilters.search || ''}
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
                {/* Status Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Status</label>
                  <Select
                    value={localFilters.status?.length === 1 ? localFilters.status[0] : 'all'}
                    onValueChange={(value) => 
                      updateFilter('status', value === 'all' ? undefined : [value as ParzellenStatus])
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Status auswählen" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Alle Status</SelectItem>
                      {Object.entries(statusConfig).map(([status, config]) => {
                        const Icon = config.icon
                        return (
                          <SelectItem key={status} value={status}>
                            <div className="flex items-center gap-2">
                              <Icon className={`h-4 w-4 ${config.color}`} />
                              {config.label}
                            </div>
                          </SelectItem>
                        )
                      })}
                    </SelectContent>
                  </Select>
                </div>

                {/* Bezirk Filter */}
                {showBezirkFilter && availableBezirke.length > 0 && (
                  <div className="space-y-2">
                    <label className="text-sm font-medium flex items-center gap-2">
                      <Building className="h-4 w-4" />
                      Bezirk
                    </label>
                    <Select
                      value={String(localFilters.bezirkId || '')}
                      onValueChange={(value) => updateFilter('bezirkId', value ? parseInt(value) : undefined)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Bezirk auswählen" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="">Alle Bezirke</SelectItem>
                        {availableBezirke.map((bezirk) => (
                          <SelectItem key={bezirk.id} value={String(bezirk.id)}>
                            {bezirk.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {/* Active Status */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Aktiv</label>
                  <Select
                    value={localFilters.aktiv === undefined ? 'all' : String(localFilters.aktiv)}
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

                {/* Sort Options */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Sortierung</label>
                  <div className="grid grid-cols-2 gap-2">
                    <Select
                      value={localFilters.sortBy || 'nummer'}
                      onValueChange={(value) => updateFilter('sortBy', value as any)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="nummer">Nummer</SelectItem>
                        <SelectItem value="groesse">Größe</SelectItem>
                        <SelectItem value="monatlichePacht">Pacht</SelectItem>
                        <SelectItem value="erstelltAm">Erstellungsdatum</SelectItem>
                      </SelectContent>
                    </Select>
                    <Select
                      value={localFilters.sortOrder || 'asc'}
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
            placeholder="Nummer, Beschreibung oder Mieter..."
            value={localFilters.search || ''}
            onChange={(e) => updateFilter('search', e.target.value || undefined)}
          />
        </div>

        {/* Status Filter */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Status</label>
          <div className="grid grid-cols-2 gap-2">
            {Object.entries(statusConfig).map(([status, config]) => {
              const Icon = config.icon
              const isSelected = localFilters.status?.includes(status as ParzellenStatus)
              
              return (
                <Button
                  key={status}
                  variant={isSelected ? "default" : "outline"}
                  size="sm"
                  onClick={() => {
                    const currentStatus = localFilters.status || []
                    const newStatus = isSelected
                      ? currentStatus.filter(s => s !== status)
                      : [...currentStatus, status as ParzellenStatus]
                    
                    updateFilter('status', newStatus.length > 0 ? newStatus : undefined)
                  }}
                  className="justify-start gap-2"
                >
                  <Icon className={`h-4 w-4 ${isSelected ? 'text-white' : config.color}`} />
                  {config.label}
                </Button>
              )
            })}
          </div>
        </div>

        {/* Bezirk Filter */}
        {showBezirkFilter && availableBezirke.length > 0 && (
          <div className="space-y-2">
            <label className="text-sm font-medium flex items-center gap-2">
              <Building className="h-4 w-4" />
              Bezirk
            </label>
            <Select
              value={String(localFilters.bezirkId || '')}
              onValueChange={(value) => updateFilter('bezirkId', value ? parseInt(value) : undefined)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Bezirk auswählen" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Alle Bezirke</SelectItem>
                {availableBezirke.map((bezirk) => (
                  <SelectItem key={bezirk.id} value={String(bezirk.id)}>
                    {bezirk.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {/* Size Range */}
        <div className="space-y-2">
          <label className="text-sm font-medium flex items-center gap-2">
            <Ruler className="h-4 w-4" />
            Größe (m²)
          </label>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Von</label>
              <Input
                type="number"
                placeholder="Min"
                value={localFilters.groesseMin || ''}
                onChange={(e) => updateFilter('groesseMin', e.target.value ? parseInt(e.target.value) : undefined)}
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Bis</label>
              <Input
                type="number"
                placeholder="Max"
                value={localFilters.groesseMax || ''}
                onChange={(e) => updateFilter('groesseMax', e.target.value ? parseInt(e.target.value) : undefined)}
              />
            </div>
          </div>
        </div>

        {/* Pacht Range */}
        <div className="space-y-2">
          <label className="text-sm font-medium flex items-center gap-2">
            <Euro className="h-4 w-4" />
            Monatliche Pacht (€)
          </label>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Von</label>
              <Input
                type="number"
                step="0.01"
                placeholder="Min"
                value={localFilters.pachtMin || ''}
                onChange={(e) => updateFilter('pachtMin', e.target.value ? parseFloat(e.target.value) : undefined)}
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Bis</label>
              <Input
                type="number"
                step="0.01"
                placeholder="Max"
                value={localFilters.pachtMax || ''}
                onChange={(e) => updateFilter('pachtMax', e.target.value ? parseFloat(e.target.value) : undefined)}
              />
            </div>
          </div>
        </div>

        {/* Active Status */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Aktiv</label>
          <Select
            value={localFilters.aktiv === undefined ? 'all' : String(localFilters.aktiv)}
            onValueChange={(value) => 
              updateFilter('aktiv', value === 'all' ? undefined : value === 'true')
            }
          >
            <SelectTrigger>
              <SelectValue placeholder="Status auswählen" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Alle Parzellen</SelectItem>
              <SelectItem value="true">Nur Aktive</SelectItem>
              <SelectItem value="false">Nur Inaktive</SelectItem>
            </SelectContent>
          </Select>
        </div>

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
                value={localFilters.sortBy || 'nummer'}
                onValueChange={(value) => updateFilter('sortBy', value as any)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="nummer">Nummer</SelectItem>
                  <SelectItem value="groesse">Größe</SelectItem>
                  <SelectItem value="monatlichePacht">Pacht</SelectItem>
                  <SelectItem value="erstelltAm">Erstellungsdatum</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <label className="text-xs text-gray-600 dark:text-gray-400">Reihenfolge</label>
              <Select
                value={localFilters.sortOrder || 'asc'}
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
              {localFilters.search && (
                <FilterBadge
                  label="Suche"
                  value={localFilters.search}
                  onRemove={() => clearFilter('search')}
                />
              )}
              {localFilters.status && localFilters.status.length > 0 && (
                <FilterBadge
                  label="Status"
                  value={localFilters.status.map(s => statusConfig[s]?.label || s)}
                  onRemove={() => clearFilter('status')}
                />
              )}
              {localFilters.bezirkId && (
                <FilterBadge
                  label="Bezirk"
                  value={availableBezirke.find(b => b.id === localFilters.bezirkId)?.name || String(localFilters.bezirkId)}
                  onRemove={() => clearFilter('bezirkId')}
                />
              )}
              {(localFilters.groesseMin || localFilters.groesseMax) && (
                <FilterBadge
                  label="Größe"
                  value={`${localFilters.groesseMin || '0'}-${localFilters.groesseMax || '∞'} m²`}
                  onRemove={() => {
                    clearFilter('groesseMin')
                    clearFilter('groesseMax')
                  }}
                />
              )}
              {(localFilters.pachtMin || localFilters.pachtMax) && (
                <FilterBadge
                  label="Pacht"
                  value={`${localFilters.pachtMin || '0'}-${localFilters.pachtMax || '∞'} €`}
                  onRemove={() => {
                    clearFilter('pachtMin')
                    clearFilter('pachtMax')
                  }}
                />
              )}
              {localFilters.aktiv !== undefined && (
                <FilterBadge
                  label="Aktiv"
                  value={localFilters.aktiv ? 'Ja' : 'Nein'}
                  onRemove={() => clearFilter('aktiv')}
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
              value={localFilters.page || ''}
              onChange={(e) => updateFilter('page', e.target.value ? parseInt(e.target.value) : undefined)}
            />
          </div>
          <div className="space-y-1">
            <label className="text-xs text-gray-600 dark:text-gray-400">Anzahl pro Seite</label>
            <Select
              value={String(localFilters.limit || 20)}
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