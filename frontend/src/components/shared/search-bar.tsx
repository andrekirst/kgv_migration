'use client'

import React, { useState, useCallback, useEffect, useRef } from 'react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { 
  Search, 
  X, 
  Filter,
  History,
  Clock,
  Zap,
  Settings
} from 'lucide-react'

interface SearchBarProps {
  placeholder?: string
  value?: string
  onChange?: (value: string) => void
  onSearch?: (query: string) => void
  onClear?: () => void
  className?: string
  showFilters?: boolean
  showHistory?: boolean
  maxHistoryItems?: number
  searchHistory?: string[]
  onHistoryUpdate?: (history: string[]) => void
  debounceMs?: number
  size?: 'sm' | 'md' | 'lg'
  variant?: 'default' | 'ghost' | 'outline'
  disabled?: boolean
  autoFocus?: boolean
  suggestions?: string[]
  showShortcuts?: boolean
}

interface SearchShortcut {
  key: string
  label: string
  description: string
  example: string
}

const searchShortcuts: SearchShortcut[] = [
  {
    key: 'status:',
    label: 'Status',
    description: 'Nach Status filtern',
    example: 'status:frei'
  },
  {
    key: 'bezirk:',
    label: 'Bezirk',
    description: 'Nach Bezirk filtern',
    example: 'bezirk:nord'
  },
  {
    key: 'größe:',
    label: 'Größe',
    description: 'Nach Größe filtern',
    example: 'größe:>100'
  },
  {
    key: 'pacht:',
    label: 'Pacht',
    description: 'Nach Pacht filtern',
    example: 'pacht:<50'
  },
  {
    key: 'datum:',
    label: 'Datum',
    description: 'Nach Datum filtern',
    example: 'datum:2024'
  }
]

export function SearchBar({
  placeholder = 'Suchen...',
  value = '',
  onChange,
  onSearch,
  onClear,
  className = '',
  showFilters = false,
  showHistory = true,
  maxHistoryItems = 5,
  searchHistory = [],
  onHistoryUpdate,
  debounceMs = 300,
  size = 'md',
  variant = 'default',
  disabled = false,
  autoFocus = false,
  suggestions = [],
  showShortcuts = true
}: SearchBarProps) {
  const [localValue, setLocalValue] = useState(value)
  const [isOpen, setIsOpen] = useState(false)
  const [showSuggestions, setShowSuggestions] = useState(false)
  const debounceTimeoutRef = useRef<NodeJS.Timeout>()
  const inputRef = useRef<HTMLInputElement>(null)

  const sizeClasses = {
    sm: 'h-8 text-sm',
    md: 'h-10 text-sm',
    lg: 'h-12 text-base'
  }

  const variantClasses = {
    default: 'border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950',
    ghost: 'border-transparent bg-transparent',
    outline: 'border-gray-300 bg-transparent dark:border-gray-700'
  }

  // Sync external value changes
  useEffect(() => {
    setLocalValue(value)
  }, [value])

  // Debounced search
  useEffect(() => {
    if (debounceTimeoutRef.current) {
      clearTimeout(debounceTimeoutRef.current)
    }

    debounceTimeoutRef.current = setTimeout(() => {
      if (localValue !== value) {
        onChange?.(localValue)
        if (localValue.trim() && onSearch) {
          onSearch(localValue.trim())
          updateSearchHistory(localValue.trim())
        }
      }
    }, debounceMs)

    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current)
      }
    }
  }, [localValue, value, onChange, onSearch, debounceMs])

  const updateSearchHistory = useCallback((query: string) => {
    if (!showHistory || !onHistoryUpdate || query.length < 2) return

    const newHistory = [
      query,
      ...searchHistory.filter(item => item !== query)
    ].slice(0, maxHistoryItems)

    onHistoryUpdate(newHistory)
  }, [showHistory, onHistoryUpdate, searchHistory, maxHistoryItems])

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value
    setLocalValue(newValue)
    setShowSuggestions(newValue.length > 0)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      const query = localValue.trim()
      if (query) {
        onSearch?.(query)
        updateSearchHistory(query)
        setShowSuggestions(false)
      }
    } else if (e.key === 'Escape') {
      setShowSuggestions(false)
      inputRef.current?.blur()
    }
  }

  const handleClear = () => {
    setLocalValue('')
    onChange?.('')
    onClear?.()
    setShowSuggestions(false)
    inputRef.current?.focus()
  }

  const handleHistoryClick = (historyQuery: string) => {
    setLocalValue(historyQuery)
    onChange?.(historyQuery)
    onSearch?.(historyQuery)
    setShowSuggestions(false)
    setIsOpen(false)
  }

  const handleSuggestionClick = (suggestion: string) => {
    setLocalValue(suggestion)
    onChange?.(suggestion)
    onSearch?.(suggestion)
    updateSearchHistory(suggestion)
    setShowSuggestions(false)
  }

  const handleShortcutClick = (shortcut: SearchShortcut) => {
    const newValue = localValue + shortcut.key
    setLocalValue(newValue)
    onChange?.(newValue)
    inputRef.current?.focus()
    setIsOpen(false)
  }

  const filteredSuggestions = suggestions.filter(suggestion =>
    suggestion.toLowerCase().includes(localValue.toLowerCase()) && 
    suggestion !== localValue
  ).slice(0, 5)

  const filteredHistory = searchHistory.filter(item =>
    item.toLowerCase().includes(localValue.toLowerCase()) && 
    item !== localValue
  ).slice(0, 3)

  return (
    <div className={`relative ${className}`}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
        <Input
          ref={inputRef}
          type="text"
          placeholder={placeholder}
          value={localValue}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={() => setShowSuggestions(localValue.length > 0)}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
          disabled={disabled}
          autoFocus={autoFocus}
          className={`pl-10 pr-20 ${sizeClasses[size]} ${variantClasses[variant]}`}
        />
        
        <div className="absolute right-2 top-1/2 transform -translate-y-1/2 flex items-center gap-1">
          {localValue && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClear}
              className="h-6 w-6 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              <X className="h-3 w-3" />
              <span className="sr-only">Suche löschen</span>
            </Button>
          )}
          
          {(showFilters || showHistory || showShortcuts) && (
            <Popover open={isOpen} onOpenChange={setIsOpen}>
              <PopoverTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  <Settings className="h-3 w-3" />
                  <span className="sr-only">Suchoptionen</span>
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-80" align="end">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <h4 className="font-medium">Suchoptionen</h4>
                  </div>
                  
                  {/* Search History */}
                  {showHistory && searchHistory.length > 0 && (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                        <History className="h-4 w-4" />
                        Zuletzt gesucht
                      </div>
                      <div className="space-y-1">
                        {searchHistory.slice(0, maxHistoryItems).map((item, index) => (
                          <Button
                            key={index}
                            variant="ghost"
                            size="sm"
                            onClick={() => handleHistoryClick(item)}
                            className="w-full justify-start text-left h-auto py-2"
                          >
                            <Clock className="h-3 w-3 mr-2 text-gray-400" />
                            <span className="truncate">{item}</span>
                          </Button>
                        ))}
                      </div>
                    </div>
                  )}
                  
                  {/* Search Shortcuts */}
                  {showShortcuts && (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                        <Zap className="h-4 w-4" />
                        Suchverknüpfungen
                      </div>
                      <div className="space-y-1">
                        {searchShortcuts.map((shortcut) => (
                          <Button
                            key={shortcut.key}
                            variant="ghost"
                            size="sm"
                            onClick={() => handleShortcutClick(shortcut)}
                            className="w-full justify-start text-left h-auto py-2"
                          >
                            <div className="flex-1">
                              <div className="flex items-center gap-2">
                                <Badge variant="outline" className="text-xs">
                                  {shortcut.key}
                                </Badge>
                                <span className="font-medium">{shortcut.label}</span>
                              </div>
                              <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                                {shortcut.description} • Beispiel: {shortcut.example}
                              </div>
                            </div>
                          </Button>
                        ))}
                      </div>
                    </div>
                  )}
                  
                  {/* Keyboard Shortcuts Info */}
                  <div className="border-t pt-3 text-xs text-gray-600 dark:text-gray-400">
                    <div className="space-y-1">
                      <div className="flex justify-between">
                        <span>Enter</span>
                        <span>Suchen</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Esc</span>
                        <span>Schließen</span>
                      </div>
                    </div>
                  </div>
                </div>
              </PopoverContent>
            </Popover>
          )}
        </div>
      </div>
      
      {/* Suggestions Dropdown */}
      {showSuggestions && (filteredSuggestions.length > 0 || filteredHistory.length > 0) && (
        <div className="absolute z-50 w-full mt-1 bg-white dark:bg-gray-950 border border-gray-200 dark:border-gray-800 rounded-md shadow-lg">
          <div className="py-2 max-h-64 overflow-y-auto">
            {/* Recent History */}
            {filteredHistory.length > 0 && (
              <div className="px-3 py-1">
                <div className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-2 flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  Zuletzt gesucht
                </div>
                {filteredHistory.map((item, index) => (
                  <button
                    key={`history-${index}`}
                    onClick={() => handleHistoryClick(item)}
                    className="w-full text-left px-2 py-1.5 text-sm hover:bg-gray-100 dark:hover:bg-gray-800 rounded flex items-center gap-2"
                  >
                    <History className="h-3 w-3 text-gray-400" />
                    <span className="truncate">{item}</span>
                  </button>
                ))}
              </div>
            )}
            
            {/* Suggestions */}
            {filteredSuggestions.length > 0 && (
              <div className="px-3 py-1">
                {filteredHistory.length > 0 && (
                  <div className="border-t border-gray-200 dark:border-gray-700 my-2" />
                )}
                <div className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-2 flex items-center gap-1">
                  <Search className="h-3 w-3" />
                  Vorschläge
                </div>
                {filteredSuggestions.map((suggestion, index) => (
                  <button
                    key={`suggestion-${index}`}
                    onClick={() => handleSuggestionClick(suggestion)}
                    className="w-full text-left px-2 py-1.5 text-sm hover:bg-gray-100 dark:hover:bg-gray-800 rounded flex items-center gap-2"
                  >
                    <Search className="h-3 w-3 text-gray-400" />
                    <span className="truncate">{suggestion}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}