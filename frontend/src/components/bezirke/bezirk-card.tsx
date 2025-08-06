'use client'

import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { 
  MapPin, 
  Users,
  Phone,
  Mail,
  BarChart3,
  Edit,
  Trash2,
  MoreVertical,
  Building,
  Calendar
} from 'lucide-react'
import { Bezirk } from '@/types/bezirke'

interface BezirkCardProps {
  bezirk: Bezirk
  onEdit?: (id: number) => void
  onDelete?: (id: number) => void
  onViewParzellen?: (id: number) => void
  onClick?: () => void
  className?: string
  compact?: boolean
}

export function BezirkCard({ 
  bezirk, 
  onEdit, 
  onDelete, 
  onViewParzellen,
  onClick,
  className = '',
  compact = false
}: BezirkCardProps) {
  const auslastung = bezirk.statistiken.gesamtParzellen > 0 
    ? Math.round((bezirk.statistiken.belegteParzellen / bezirk.statistiken.gesamtParzellen) * 100)
    : 0
  
  const getAuslastungColor = (percentage: number) => {
    if (percentage >= 90) return 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20'
    if (percentage >= 75) return 'text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20'
    return 'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20'
  }

  const getAuslastungBadgeVariant = (percentage: number) => {
    if (percentage >= 90) return 'destructive'
    if (percentage >= 75) return 'secondary'
    return 'outline'
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('de-DE', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const handleCardClick = () => {
    if (onClick) {
      onClick()
    } else if (onViewParzellen) {
      onViewParzellen(bezirk.id)
    }
  }

  if (compact) {
    return (
      <Card 
        className={`cursor-pointer transition-all hover:shadow-md hover:scale-[1.02] ${!bezirk.aktiv ? 'opacity-60' : ''} ${className}`}
        onClick={handleCardClick}
      >
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <h3 className="font-semibold text-base text-gray-900 dark:text-gray-100 truncate">
                  {bezirk.name}
                </h3>
                {!bezirk.aktiv && (
                  <Badge variant="secondary" className="text-xs">
                    Inaktiv
                  </Badge>
                )}
              </div>
              
              <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400">
                <div className="flex items-center gap-1">
                  <Building className="h-3 w-3" />
                  {bezirk.statistiken.gesamtParzellen}
                </div>
                <div className={`flex items-center gap-1 ${getAuslastungColor(auslastung).split(' ')[0]}`}>
                  <BarChart3 className="h-3 w-3" />
                  {auslastung}%
                </div>
              </div>
            </div>
            
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
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onViewParzellen?.(bezirk.id) }}>
                  <Building className="mr-2 h-4 w-4" />
                  Parzellen anzeigen
                </DropdownMenuItem>
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.(bezirk.id) }}>
                  <Edit className="mr-2 h-4 w-4" />
                  Bearbeiten
                </DropdownMenuItem>
                <DropdownMenuItem 
                  onClick={(e) => { e.stopPropagation(); onDelete?.(bezirk.id) }} 
                  className="text-red-600 focus:text-red-600"
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Löschen
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card 
      className={`cursor-pointer transition-all hover:shadow-lg hover:scale-[1.02] ${!bezirk.aktiv ? 'opacity-60' : ''} ${className}`}
      onClick={handleCardClick}
    >
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-xl mb-2 flex items-center gap-2">
              <span className="truncate">{bezirk.name}</span>
              {!bezirk.aktiv && (
                <Badge variant="secondary" className="text-xs flex-shrink-0">
                  Inaktiv
                </Badge>
              )}
            </CardTitle>
            {bezirk.beschreibung && (
              <p className="text-gray-600 dark:text-gray-400 text-sm line-clamp-2 leading-relaxed">
                {bezirk.beschreibung}
              </p>
            )}
          </div>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button 
                variant="ghost" 
                size="sm" 
                className="h-8 w-8 p-0 flex-shrink-0 ml-2"
                onClick={(e) => e.stopPropagation()}
              >
                <MoreVertical className="h-4 w-4" />
                <span className="sr-only">Aktionen für {bezirk.name}</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onViewParzellen?.(bezirk.id) }}>
                <Building className="mr-2 h-4 w-4" />
                Parzellen anzeigen
              </DropdownMenuItem>
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.(bezirk.id) }}>
                <Edit className="mr-2 h-4 w-4" />
                Bearbeiten
              </DropdownMenuItem>
              <DropdownMenuItem 
                onClick={(e) => { e.stopPropagation(); onDelete?.(bezirk.id) }} 
                className="text-red-600 focus:text-red-600"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Löschen
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Contact Information */}
        {(bezirk.bezirksleiter || bezirk.telefon || bezirk.email) && (
          <div className="space-y-2">
            {bezirk.bezirksleiter && (
              <div className="flex items-center gap-2 text-sm">
                <Users className="h-4 w-4 text-gray-400 flex-shrink-0" />
                <span className="text-gray-900 dark:text-gray-100 font-medium">
                  {bezirk.bezirksleiter}
                </span>
              </div>
            )}
            
            <div className="flex flex-col sm:flex-row gap-2 sm:gap-4">
              {bezirk.telefon && (
                <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                  <Phone className="h-3 w-3 flex-shrink-0" />
                  <a 
                    href={`tel:${bezirk.telefon}`} 
                    className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {bezirk.telefon}
                  </a>
                </div>
              )}
              {bezirk.email && (
                <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                  <Mail className="h-3 w-3 flex-shrink-0" />
                  <a 
                    href={`mailto:${bezirk.email}`} 
                    className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors truncate"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {bezirk.email}
                  </a>
                </div>
              )}
            </div>
          </div>
        )}
        
        {/* Address */}
        {bezirk.adresse?.ort && (
          <div className="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400">
            <MapPin className="h-4 w-4 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              {bezirk.adresse.strasse && (
                <div>{bezirk.adresse.strasse} {bezirk.adresse.hausnummer}</div>
              )}
              <div>{bezirk.adresse.plz} {bezirk.adresse.ort}</div>
            </div>
          </div>
        )}
        
        {/* Statistics */}
        <div className="border-t pt-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Parzellen-Übersicht
            </span>
            <Badge 
              variant={getAuslastungBadgeVariant(auslastung)}
              className={`gap-1 ${getAuslastungColor(auslastung)}`}
            >
              <BarChart3 className="h-3 w-3" />
              {auslastung}% Auslastung
            </Badge>
          </div>
          
          <div className="grid grid-cols-3 gap-3 text-center">
            <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3">
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {bezirk.statistiken.gesamtParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                Gesamt
              </div>
            </div>
            <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-3">
              <div className="text-2xl font-bold text-green-600 dark:text-green-400">
                {bezirk.statistiken.belegteParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                Belegt
              </div>
            </div>
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3">
              <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
                {bezirk.statistiken.freieParzellen}
              </div>
              <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                Frei
              </div>
            </div>
          </div>
          
          {bezirk.statistiken.warteliste > 0 && (
            <div className="mt-3 text-center">
              <Badge variant="outline" className="text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20">
                {bezirk.statistiken.warteliste} auf Warteliste
              </Badge>
            </div>
          )}
        </div>
        
        {/* Footer Info */}
        <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 pt-2 border-t">
          <div className="flex items-center gap-1">
            <Calendar className="h-3 w-3" />
            Erstellt {formatDate(bezirk.erstelltAm)}
          </div>
          {bezirk.erstelltAm !== bezirk.aktualisiertAm && (
            <div>
              Aktualisiert {formatDate(bezirk.aktualisiertAm)}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}