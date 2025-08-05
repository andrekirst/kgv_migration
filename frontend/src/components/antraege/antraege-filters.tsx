'use client'

// Filters component for Anträge with German KGV-specific filters
import * as React from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import * as Select from '@radix-ui/react-select'
import * as Checkbox from '@radix-ui/react-checkbox'
import { 
  SearchIcon, 
  FilterIcon, 
  XIcon, 
  ChevronDownIcon,
  CheckIcon,
  CalendarIcon,
  MapIcon
} from 'lucide-react'
import { AntragStatus } from '@/types/api'
import { debounce, cn } from '@/lib/utils'

// Mock data - replace with API calls
const bezirkeOptions = [
  { value: 'nord', label: 'Nord' },
  { value: 'sued', label: 'Süd' },
  { value: 'ost', label: 'Ost' },
  { value: 'west', label: 'West' },
  { value: 'zentrum', label: 'Zentrum' },
]

const statusOptions = [
  { value: AntragStatus.Neu.toString(), label: 'Neu', color: 'bg-blue-100 text-blue-800' },
  { value: AntragStatus.InBearbeitung.toString(), label: 'In Bearbeitung', color: 'bg-yellow-100 text-yellow-800' },
  { value: AntragStatus.Wartend.toString(), label: 'Wartend', color: 'bg-orange-100 text-orange-800' },
  { value: AntragStatus.Genehmigt.toString(), label: 'Genehmigt', color: 'bg-green-100 text-green-800' },
  { value: AntragStatus.Abgelehnt.toString(), label: 'Abgelehnt', color: 'bg-red-100 text-red-800' },
  { value: AntragStatus.Archiviert.toString(), label: 'Archiviert', color: 'bg-gray-100 text-gray-800' },
]

interface FilterState {
  search: string
  status: string[]
  bezirk: string[]
  aktiv: boolean | null
  dateFrom: string
  dateTo: string
}

export function AntraegeFilters() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [showAdvancedFilters, setShowAdvancedFilters] = React.useState(false)
  
  // Initialize filter state from URL parameters
  const [filters, setFilters] = React.useState<FilterState>({
    search: searchParams.get('search') || '',
    status: searchParams.getAll('status'),
    bezirk: searchParams.getAll('bezirk'),
    aktiv: searchParams.get('aktiv') ? searchParams.get('aktiv') === 'true' : null,
    dateFrom: searchParams.get('dateFrom') || '',
    dateTo: searchParams.get('dateTo') || '',
  })
  
  // Debounced search function
  const debouncedUpdateURL = React.useMemo(
    () => debounce((newFilters: FilterState) => {
      const params = new URLSearchParams()
      
      if (newFilters.search) params.set('search', newFilters.search)
      newFilters.status.forEach(status => params.append('status', status))
      newFilters.bezirk.forEach(bezirk => params.append('bezirk', bezirk))
      if (newFilters.aktiv !== null) params.set('aktiv', newFilters.aktiv.toString())
      if (newFilters.dateFrom) params.set('dateFrom', newFilters.dateFrom)
      if (newFilters.dateTo) params.set('dateTo', newFilters.dateTo)
      
      router.push(`/antraege?${params.toString()}`)
    }, 300),
    [router]
  )
  
  // Update URL when filters change
  React.useEffect(() => {
    debouncedUpdateURL(filters)
  }, [filters, debouncedUpdateURL])
  
  const updateFilter = <K extends keyof FilterState>(
    key: K,
    value: FilterState[K]
  ) => {
    setFilters(prev => ({ ...prev, [key]: value }))
  }
  
  const toggleArrayFilter = (key: 'status' | 'bezirk', value: string) => {
    setFilters(prev => ({
      ...prev,
      [key]: prev[key].includes(value)
        ? prev[key].filter(item => item !== value)
        : [...prev[key], value]
    }))
  }
  
  const clearFilters = () => {
    setFilters({
      search: '',
      status: [],
      bezirk: [],
      aktiv: null,
      dateFrom: '',
      dateTo: '',
    })
    router.push('/antraege')
  }
  
  const hasActiveFilters = filters.search || 
    filters.status.length > 0 || 
    filters.bezirk.length > 0 || 
    filters.aktiv !== null ||
    filters.dateFrom ||
    filters.dateTo
  
  const activeFilterCount = [
    filters.search && 1,
    filters.status.length,
    filters.bezirk.length,
    filters.aktiv !== null && 1,
    filters.dateFrom && 1,
    filters.dateTo && 1,
  ].filter(Boolean).reduce((sum, count) => sum + (count as number), 0)
  
  return (
    <Card>
      <CardContent className="p-6">
        {/* Search and basic filters */}
        <div className="flex flex-col space-y-4 sm:flex-row sm:items-center sm:space-y-0 sm:space-x-4">
          {/* Search input */}
          <div className="flex-1">
            <div className="relative">
              <SearchIcon className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-400" />
              <Input
                placeholder="Anträge durchsuchen (Name, Aktenzeichen, etc.)"
                value={filters.search}
                onChange={(e) => updateFilter('search', e.target.value)}
                className="pl-10"
              />
            </div>
          </div>
          
          {/* Quick status filters */}
          <div className="flex flex-wrap gap-2">
            {statusOptions.slice(0, 3).map((status) => {
              const isSelected = filters.status.includes(status.value)
              return (
                <Button
                  key={status.value}
                  variant={isSelected ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => toggleArrayFilter('status', status.value)}
                  className={cn(
                    'text-xs',
                    isSelected && status.color
                  )}
                >
                  {status.label}
                  {isSelected && <XIcon className="ml-1 h-3 w-3" />}
                </Button>
              )
            })}
          </div>
          
          {/* Advanced filters toggle */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
            className="flex items-center space-x-2"
          >
            <FilterIcon className="h-4 w-4" />
            <span>Filter</span>
            {activeFilterCount > 0 && (
              <Badge variant="secondary" className="ml-1 text-xs">
                {activeFilterCount}
              </Badge>
            )}
            <ChevronDownIcon 
              className={cn(
                'h-4 w-4 transition-transform',
                showAdvancedFilters && 'rotate-180'
              )}
            />
          </Button>
        </div>
        
        {/* Advanced filters */}
        {showAdvancedFilters && (
          <div className="mt-6 space-y-4 border-t border-secondary-200 pt-6">
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {/* Status filter */}
              <div>
                <label className="block text-sm font-medium text-secondary-700 mb-2">
                  Status
                </label>
                <div className="space-y-2">
                  {statusOptions.map((status) => (
                    <label
                      key={status.value}
                      className="flex items-center space-x-2 text-sm cursor-pointer"
                    >
                      <Checkbox.Root
                        checked={filters.status.includes(status.value)}
                        onCheckedChange={() => toggleArrayFilter('status', status.value)}
                        className="flex h-4 w-4 items-center justify-center rounded border border-secondary-300 bg-white data-[state=checked]:bg-primary-600 data-[state=checked]:border-primary-600"
                      >
                        <Checkbox.Indicator>
                          <CheckIcon className="h-3 w-3 text-white" />
                        </Checkbox.Indicator>
                      </Checkbox.Root>
                      <span className={cn('flex-1', status.color)}>{status.label}</span>
                    </label>
                  ))}
                </div>
              </div>
              
              {/* Bezirk filter */}
              <div>
                <label className="block text-sm font-medium text-secondary-700 mb-2">
                  <MapIcon className="inline h-4 w-4 mr-1" />
                  Bezirk
                </label>
                <div className="space-y-2">
                  {bezirkeOptions.map((bezirk) => (
                    <label
                      key={bezirk.value}
                      className="flex items-center space-x-2 text-sm cursor-pointer"
                    >
                      <Checkbox.Root
                        checked={filters.bezirk.includes(bezirk.value)}
                        onCheckedChange={() => toggleArrayFilter('bezirk', bezirk.value)}
                        className="flex h-4 w-4 items-center justify-center rounded border border-secondary-300 bg-white data-[state=checked]:bg-primary-600 data-[state=checked]:border-primary-600"
                      >
                        <Checkbox.Indicator>
                          <CheckIcon className="h-3 w-3 text-white" />
                        </Checkbox.Indicator>
                      </Checkbox.Root>
                      <span>{bezirk.label}</span>
                    </label>
                  ))}
                </div>
              </div>
              
              {/* Date range filter */}
              <div>
                <label className="block text-sm font-medium text-secondary-700 mb-2">
                  <CalendarIcon className="inline h-4 w-4 mr-1" />
                  Bewerbungsdatum
                </label>
                <div className="space-y-2">
                  <Input
                    type="date"
                    placeholder="Von"
                    value={filters.dateFrom}
                    onChange={(e) => updateFilter('dateFrom', e.target.value)}
                    className="text-sm"
                  />
                  <Input
                    type="date"
                    placeholder="Bis"
                    value={filters.dateTo}
                    onChange={(e) => updateFilter('dateTo', e.target.value)}
                    className="text-sm"
                  />
                </div>
              </div>
            </div>
            
            {/* Active filter */}
            <div>
              <label className="block text-sm font-medium text-secondary-700 mb-2">
                Aktivitätsstatus
              </label>
              <div className="flex space-x-4">
                {[
                  { value: true, label: 'Nur aktive Anträge' },
                  { value: false, label: 'Nur inaktive Anträge' },
                  { value: null, label: 'Alle Anträge' },
                ].map((option) => (
                  <label
                    key={String(option.value)}
                    className="flex items-center space-x-2 text-sm cursor-pointer"
                  >
                    <input
                      type="radio"
                      name="aktiv"
                      checked={filters.aktiv === option.value}
                      onChange={() => updateFilter('aktiv', option.value)}
                      className="h-4 w-4 text-primary-600 border-secondary-300 focus:ring-primary-500"
                    />
                    <span>{option.label}</span>
                  </label>
                ))}
              </div>
            </div>
            
            {/* Filter actions */}
            <div className="flex items-center justify-between pt-4 border-t border-secondary-200">
              <div className="text-sm text-secondary-600">
                {hasActiveFilters && (
                  <span>{activeFilterCount} Filter aktiv</span>
                )}
              </div>
              <div className="flex space-x-2">
                {hasActiveFilters && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={clearFilters}
                  >
                    <XIcon className="h-4 w-4" />
                    Filter zurücksetzen
                  </Button>
                )}
              </div>
            </div>
          </div>
        )}
        
        {/* Active filter tags */}
        {hasActiveFilters && (
          <div className="mt-4 flex flex-wrap gap-2">
            {filters.search && (
              <Badge variant="outline" className="flex items-center space-x-1">
                <span>Suche: {filters.search}</span>
                <XIcon 
                  className="h-3 w-3 cursor-pointer" 
                  onClick={() => updateFilter('search', '')}
                />
              </Badge>
            )}
            {filters.status.map((statusValue) => {
              const status = statusOptions.find(s => s.value === statusValue)
              return status ? (
                <Badge key={statusValue} variant="outline" className="flex items-center space-x-1">
                  <span>{status.label}</span>
                  <XIcon 
                    className="h-3 w-3 cursor-pointer" 
                    onClick={() => toggleArrayFilter('status', statusValue)}
                  />
                </Badge>
              ) : null
            })}
            {filters.bezirk.map((bezirkValue) => {
              const bezirk = bezirkeOptions.find(b => b.value === bezirkValue)
              return bezirk ? (
                <Badge key={bezirkValue} variant="outline" className="flex items-center space-x-1">
                  <span>{bezirk.label}</span>
                  <XIcon 
                    className="h-3 w-3 cursor-pointer" 
                    onClick={() => toggleArrayFilter('bezirk', bezirkValue)}
                  />
                </Badge>
              ) : null
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}