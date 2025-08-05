'use client'

// Recent activity component showing latest KGV actions
import * as React from 'react'
import Link from 'next/link'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { 
  ActivityIcon,
  FileTextIcon,
  UserIcon,
  CheckCircleIcon,
  ClockIcon,
  XCircleIcon,
  MessageSquareIcon,
  MapIcon,
  ArrowRightIcon
} from 'lucide-react'
import { formatRelativeTime, formatDate } from '@/lib/utils'
import { AntragStatus } from '@/types/api'

// Mock activity data - replace with real API calls
const mockActivities = [
  {
    id: '1',
    type: 'antrag_genehmigt' as const,
    title: 'Antrag genehmigt',
    description: 'Antrag AZ-2024-001 von Max Mustermann wurde genehmigt',
    user: 'Sarah Schmidt',
    timestamp: new Date(Date.now() - 1000 * 60 * 15), // 15 minutes ago
    status: AntragStatus.Genehmigt,
    entityId: 'antrag-001',
    entityType: 'antrag'
  },
  {
    id: '2',
    type: 'antrag_neu' as const,
    title: 'Neuer Antrag eingegangen',
    description: 'Neuer Antrag von Anna Weber für Bezirk Nord eingegangen',
    user: 'System',
    timestamp: new Date(Date.now() - 1000 * 60 * 45), // 45 minutes ago
    status: AntragStatus.Neu,
    entityId: 'antrag-002',
    entityType: 'antrag'
  },
  {
    id: '3',
    type: 'status_geaendert' as const,
    title: 'Status geändert',
    description: 'Antrag AZ-2024-003 Status zu "In Bearbeitung" geändert',
    user: 'Michael Weber',
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 2), // 2 hours ago
    status: AntragStatus.InBearbeitung,
    entityId: 'antrag-003',
    entityType: 'antrag'
  },
  {
    id: '4',
    type: 'kommentar_hinzugefuegt' as const,
    title: 'Kommentar hinzugefügt',
    description: 'Kommentar zu Antrag AZ-2024-004 hinzugefügt',
    user: 'Lisa Müller',
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 4), // 4 hours ago
    status: AntragStatus.Wartend,
    entityId: 'antrag-004',
    entityType: 'antrag'
  },
  {
    id: '5',
    type: 'bezirk_aktualisiert' as const,
    title: 'Bezirk aktualisiert',
    description: 'Bezirk "Süd-West" Daten wurden aktualisiert',
    user: 'Admin',
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 6), // 6 hours ago
    status: null,
    entityId: 'bezirk-001',
    entityType: 'bezirk'
  },
  {
    id: '6',
    type: 'antrag_abgelehnt' as const,
    title: 'Antrag abgelehnt',
    description: 'Antrag AZ-2024-005 wurde abgelehnt',
    user: 'Thomas Klein',
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 8), // 8 hours ago
    status: AntragStatus.Abgelehnt,
    entityId: 'antrag-005',
    entityType: 'antrag'
  }
]

type ActivityType = typeof mockActivities[0]['type']

function getActivityIcon(type: ActivityType, status: AntragStatus | null) {
  switch (type) {
    case 'antrag_neu':
      return FileTextIcon
    case 'antrag_genehmigt':
      return CheckCircleIcon
    case 'antrag_abgelehnt':
      return XCircleIcon
    case 'status_geaendert':
      return ClockIcon
    case 'kommentar_hinzugefuegt':
      return MessageSquareIcon
    case 'bezirk_aktualisiert':
      return MapIcon
    default:
      return ActivityIcon
  }
}

function getActivityColor(type: ActivityType, status: AntragStatus | null) {
  switch (type) {
    case 'antrag_genehmigt':
      return 'text-success-600 bg-success-50'
    case 'antrag_abgelehnt':
      return 'text-error-600 bg-error-50'
    case 'antrag_neu':
      return 'text-primary-600 bg-primary-50'
    case 'status_geaendert':
      return 'text-warning-600 bg-warning-50'
    case 'kommentar_hinzugefuegt':
      return 'text-purple-600 bg-purple-50'
    case 'bezirk_aktualisiert':
      return 'text-blue-600 bg-blue-50'
    default:
      return 'text-secondary-600 bg-secondary-50'
  }
}

function getStatusBadge(status: AntragStatus | null) {
  if (status === null) return null
  
  const statusMap = {
    [AntragStatus.Neu]: { variant: 'neu' as const, text: 'Neu' },
    [AntragStatus.InBearbeitung]: { variant: 'bearbeitung' as const, text: 'In Bearbeitung' },
    [AntragStatus.Wartend]: { variant: 'wartend' as const, text: 'Wartend' },
    [AntragStatus.Genehmigt]: { variant: 'genehmigt' as const, text: 'Genehmigt' },
    [AntragStatus.Abgelehnt]: { variant: 'abgelehnt' as const, text: 'Abgelehnt' },
    [AntragStatus.Archiviert]: { variant: 'archiviert' as const, text: 'Archiviert' },
  }
  
  const statusInfo = statusMap[status]
  if (!statusInfo) return null
  
  return (
    <Badge variant={statusInfo.variant} className="text-xs">
      {statusInfo.text}
    </Badge>
  )
}

interface ActivityItemProps {
  activity: typeof mockActivities[0]
  isLast: boolean
}

function ActivityItem({ activity, isLast }: ActivityItemProps) {
  const Icon = getActivityIcon(activity.type, activity.status)
  const colorClass = getActivityColor(activity.type, activity.status)
  const statusBadge = getStatusBadge(activity.status)
  
  return (
    <div className={`flex space-x-3 ${!isLast ? 'pb-4' : ''}`}>
      {/* Timeline line */}
      {!isLast && (
        <div className="absolute left-4 top-8 bottom-0 w-px bg-secondary-200" />
      )}
      
      {/* Icon */}
      <div className={`
        relative z-10 flex h-8 w-8 items-center justify-center rounded-full
        ${colorClass}
      `}>
        <Icon className="h-4 w-4" />
      </div>
      
      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between">
          <div className="min-w-0 flex-1">
            <div className="flex items-center space-x-2 mb-1">
              <h4 className="text-sm font-medium text-secondary-900">
                {activity.title}
              </h4>
              {statusBadge}
            </div>
            <p className="text-sm text-secondary-600 mb-2">
              {activity.description}
            </p>
            <div className="flex items-center space-x-2 text-xs text-secondary-500">
              <UserIcon className="h-3 w-3" />
              <span>{activity.user}</span>
              <span>•</span>
              <time 
                dateTime={activity.timestamp.toISOString()}
                title={formatDate(activity.timestamp)}
              >
                {formatRelativeTime(activity.timestamp)}
              </time>
            </div>
          </div>
          
          {/* Action link */}
          {activity.entityType === 'antrag' && (
            <Link
              href={`/antraege/${activity.entityId}`}
              className="ml-4 text-primary-600 hover:text-primary-700 transition-colors"
              title="Antrag anzeigen"
            >
              <ArrowRightIcon className="h-4 w-4" />
            </Link>
          )}
        </div>
      </div>
    </div>
  )
}

export function RecentActivity() {
  const [showAll, setShowAll] = React.useState(false)
  const displayedActivities = showAll ? mockActivities : mockActivities.slice(0, 4)
  
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <ActivityIcon className="h-5 w-5" />
          <span>Letzte Aktivitäten</span>
        </CardTitle>
        <CardDescription>
          Aktuelle Änderungen und Aktionen im System
        </CardDescription>
      </CardHeader>
      <CardContent>
        {displayedActivities.length === 0 ? (
          <div className="text-center py-6">
            <ActivityIcon className="h-12 w-12 text-secondary-400 mx-auto mb-3" />
            <h3 className="text-sm font-medium text-secondary-900 mb-1">
              Keine Aktivitäten
            </h3>
            <p className="text-sm text-secondary-500">
              Es wurden noch keine Aktivitäten aufgezeichnet.
            </p>
          </div>
        ) : (
          <div className="relative">
            {displayedActivities.map((activity, index) => (
              <ActivityItem
                key={activity.id}
                activity={activity}
                isLast={index === displayedActivities.length - 1}
              />
            ))}
          </div>
        )}
        
        {/* Show more/less button */}
        {mockActivities.length > 4 && (
          <div className="mt-4 pt-4 border-t border-secondary-200">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setShowAll(!showAll)}
              className="w-full"
            >
              {showAll ? 'Weniger anzeigen' : `Alle ${mockActivities.length} Aktivitäten anzeigen`}
            </Button>
          </div>
        )}
        
        {/* Footer actions */}
        <div className="mt-4 pt-4 border-t border-secondary-200">
          <div className="flex justify-between items-center">
            <div className="text-xs text-secondary-500">
              Aktualisiert vor wenigen Minuten
            </div>
            <Button variant="outline" size="sm" asChild>
              <Link href="/aktivitaeten">Alle Aktivitäten</Link>
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}