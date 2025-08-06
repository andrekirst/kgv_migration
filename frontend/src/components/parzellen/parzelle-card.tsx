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
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu'
import { 
  MapPin, 
  User,
  Phone,
  Mail,
  Euro,
  Ruler,
  Building,
  Calendar,
  CheckCircle,
  Clock,
  AlertTriangle,
  Ban,
  Edit,
  Trash2,
  UserPlus,
  MoreVertical,
  Package,
  FileText,
  History
} from 'lucide-react'
import { Parzelle, ParzellenStatus } from '@/types/bezirke'

interface ParzelleCardProps {
  parzelle: Parzelle
  onEdit?: (id: number) => void
  onDelete?: (id: number) => void
  onAssign?: (id: number) => void
  onViewHistory?: (id: number) => void
  onClick?: () => void
  className?: string
  compact?: boolean
  showBezirk?: boolean
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

export function ParzelleCard({ 
  parzelle, 
  onEdit, 
  onDelete, 
  onAssign,
  onViewHistory,
  onClick,
  className = '',
  compact = false,
  showBezirk = true
}: ParzelleCardProps) {
  const config = statusConfig[parzelle.status]
  const StatusIcon = config.icon

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('de-DE', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('de-DE', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount)
  }

  const handleCardClick = () => {
    if (onClick) {
      onClick()
    }
  }

  const isAvailable = parzelle.status === ParzellenStatus.FREI

  if (compact) {
    return (
      <Card 
        className={`cursor-pointer transition-all hover:shadow-md hover:scale-[1.02] ${!parzelle.aktiv ? 'opacity-60' : ''} ${className}`}
        onClick={handleCardClick}
      >
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-1">
                <h3 className="font-semibold text-base text-gray-900 dark:text-gray-100 truncate">
                  Parzelle {parzelle.nummer}
                </h3>
                <Badge variant={config.variant} className={`gap-1 ${config.color} ${config.bg} text-xs`}>
                  <StatusIcon className="h-3 w-3" />
                  {config.label}
                </Badge>
                {!parzelle.aktiv && (
                  <Badge variant="secondary" className="text-xs">
                    Inaktiv
                  </Badge>
                )}
              </div>
              
              <div className="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-400">
                {showBezirk && (
                  <div className="flex items-center gap-1">
                    <Building className="h-3 w-3" />
                    <span className="truncate">{parzelle.bezirkName}</span>
                  </div>
                )}
                <div className="flex items-center gap-1">
                  <Ruler className="h-3 w-3" />
                  {parzelle.groesse} m²
                </div>
                <div className="flex items-center gap-1">
                  <Euro className="h-3 w-3" />
                  {formatCurrency(parzelle.monatlichePacht)}
                </div>
              </div>
            </div>
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button 
                  variant="ghost" 
                  size="sm" 
                  className="h-8 w-8 p-0 flex-shrink-0"
                  onClick={(e) => e.stopPropagation()}
                >
                  <MoreVertical className="h-4 w-4" />
                  <span className="sr-only">Aktionen für Parzelle {parzelle.nummer}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.(parzelle.id) }}>
                  <Edit className="mr-2 h-4 w-4" />
                  Bearbeiten
                </DropdownMenuItem>
                {isAvailable && (
                  <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onAssign?.(parzelle.id) }}>
                    <UserPlus className="mr-2 h-4 w-4" />
                    Zuweisen
                  </DropdownMenuItem>
                )}
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onViewHistory?.(parzelle.id) }}>
                  <History className="mr-2 h-4 w-4" />
                  Verlauf
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem 
                  onClick={(e) => { e.stopPropagation(); onDelete?.(parzelle.id) }} 
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
      className={`cursor-pointer transition-all hover:shadow-lg hover:scale-[1.02] ${!parzelle.aktiv ? 'opacity-60' : ''} ${className}`}
      onClick={handleCardClick}
    >
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-xl mb-2 flex items-center gap-2 flex-wrap">
              <span className="truncate">Parzelle {parzelle.nummer}</span>
              <Badge variant={config.variant} className={`gap-1 ${config.color} ${config.bg} flex-shrink-0`}>
                <StatusIcon className="h-3 w-3" />
                {config.label}
              </Badge>
              {!parzelle.aktiv && (
                <Badge variant="secondary" className="text-xs flex-shrink-0">
                  Inaktiv
                </Badge>
              )}
            </CardTitle>
            {showBezirk && (
              <p className="text-gray-600 dark:text-gray-400 text-sm flex items-center gap-1">
                <Building className="h-4 w-4" />
                {parzelle.bezirkName}
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
                <span className="sr-only">Aktionen für Parzelle {parzelle.nummer}</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onEdit?.(parzelle.id) }}>
                <Edit className="mr-2 h-4 w-4" />
                Bearbeiten
              </DropdownMenuItem>
              {isAvailable && (
                <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onAssign?.(parzelle.id) }}>
                  <UserPlus className="mr-2 h-4 w-4" />
                  Zuweisen
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onViewHistory?.(parzelle.id) }}>
                <History className="mr-2 h-4 w-4" />
                Verlauf anzeigen
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem 
                onClick={(e) => { e.stopPropagation(); onDelete?.(parzelle.id) }} 
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
        {/* Basic Information */}
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3">
            <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-1">
              <Ruler className="h-4 w-4" />
              Größe
            </div>
            <div className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {parzelle.groesse} m²
            </div>
          </div>
          <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-3">
            <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-1">
              <Euro className="h-4 w-4" />
              Monatliche Pacht
            </div>
            <div className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {formatCurrency(parzelle.monatlichePacht)}
            </div>
          </div>
        </div>

        {/* Mieter Information */}
        {parzelle.mieter ? (
          <div className={`rounded-lg p-4 ${config.bg}`}>
            <div className="flex items-center gap-2 mb-3">
              <StatusIcon className={`h-5 w-5 ${config.color}`} />
              <span className="font-medium text-gray-900 dark:text-gray-100">
                Vermietet
              </span>
            </div>
            
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <User className="h-4 w-4 text-gray-600 dark:text-gray-400" />
                <span className="font-medium text-gray-900 dark:text-gray-100">
                  {parzelle.mieter.vorname} {parzelle.mieter.nachname}
                </span>
              </div>
              
              <div className="flex flex-col sm:flex-row gap-2 sm:gap-4">
                {parzelle.mieter.telefon && (
                  <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                    <Phone className="h-3 w-3" />
                    <a 
                      href={`tel:${parzelle.mieter.telefon}`} 
                      className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {parzelle.mieter.telefon}
                    </a>
                  </div>
                )}
                {parzelle.mieter.email && (
                  <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                    <Mail className="h-3 w-3" />
                    <a 
                      href={`mailto:${parzelle.mieter.email}`} 
                      className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors truncate"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {parzelle.mieter.email}
                    </a>
                  </div>
                )}
              </div>
              
              {parzelle.mietbeginn && (
                <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                  <Calendar className="h-3 w-3" />
                  <span>
                    Miete seit {formatDate(parzelle.mietbeginn)}
                    {parzelle.mietende && ` bis ${formatDate(parzelle.mietende)}`}
                  </span>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400" />
                <span className="font-medium text-green-900 dark:text-green-100">
                  Verfügbar
                </span>
              </div>
              {isAvailable && (
                <Button 
                  variant="outline" 
                  size="sm" 
                  className="text-green-700 border-green-300 hover:bg-green-100 dark:hover:bg-green-900/40"
                  onClick={(e) => { e.stopPropagation(); onAssign?.(parzelle.id) }}
                >
                  <UserPlus className="h-4 w-4 mr-2" />
                  Zuweisen
                </Button>
              )}
            </div>
            <p className="text-sm text-green-700 dark:text-green-300">
              Diese Parzelle ist verfügbar und kann einem neuen Mieter zugewiesen werden.
            </p>
          </div>
        )}
        
        {/* Address */}
        {parzelle.adresse?.ort && (
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
              <MapPin className="h-4 w-4" />
              Adresse
            </div>
            <div className="pl-6 text-sm text-gray-600 dark:text-gray-400">
              {parzelle.adresse.strasse && (
                <div>{parzelle.adresse.strasse} {parzelle.adresse.hausnummer}</div>
              )}
              <div>{parzelle.adresse.plz} {parzelle.adresse.ort}</div>
            </div>
          </div>
        )}
        
        {/* Description */}
        {parzelle.beschreibung && (
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
              <FileText className="h-4 w-4" />
              Beschreibung
            </div>
            <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed pl-6">
              {parzelle.beschreibung}
            </p>
          </div>
        )}
        
        {/* Equipment */}
        {parzelle.ausstattung && parzelle.ausstattung.length > 0 && (
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
              <Package className="h-4 w-4" />
              Ausstattung
            </div>
            <div className="flex flex-wrap gap-2 pl-6">
              {parzelle.ausstattung.map((item, index) => (
                <Badge key={index} variant="outline" className="text-xs">
                  {item}
                </Badge>
              ))}
            </div>
          </div>
        )}
        
        {/* Additional Information */}
        {(parzelle.kaution || parzelle.kuendigungsfrist) && (
          <div className="border-t pt-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
              {parzelle.kaution && (
                <div>
                  <span className="text-gray-600 dark:text-gray-400">Kaution:</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                    {formatCurrency(parzelle.kaution)}
                  </span>
                </div>
              )}
              <div>
                <span className="text-gray-600 dark:text-gray-400">Kündigungsfrist:</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {parzelle.kuendigungsfrist} Monat{parzelle.kuendigungsfrist !== 1 ? 'e' : ''}
                </span>
              </div>
            </div>
          </div>
        )}
        
        {/* Remarks */}
        {parzelle.bemerkungen && (
          <div className="bg-yellow-50 dark:bg-yellow-900/20 rounded-lg p-3">
            <div className="text-sm font-medium text-yellow-800 dark:text-yellow-200 mb-1">
              Bemerkungen
            </div>
            <p className="text-sm text-yellow-700 dark:text-yellow-300">
              {parzelle.bemerkungen}
            </p>
          </div>
        )}
        
        {/* Footer Info */}
        <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 pt-2 border-t">
          <div className="flex items-center gap-1">
            <Calendar className="h-3 w-3" />
            Erstellt {formatDate(parzelle.erstelltAm)}
          </div>
          {parzelle.erstelltAm !== parzelle.aktualisiertAm && (
            <div>
              Aktualisiert {formatDate(parzelle.aktualisiertAm)}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}