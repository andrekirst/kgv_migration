'use client'

// Status overview component with KGV application status distribution
import * as React from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { 
  PieChartIcon,
  FileTextIcon,
  ClockIcon,
  CheckCircleIcon,
  XCircleIcon,
  ArchiveIcon,
  AlertCircleIcon
} from 'lucide-react'
import { AntragStatus } from '@/types/api'

// Mock data - replace with real API calls
const mockStatusData = [
  {
    status: AntragStatus.Neu,
    label: 'Neu',
    count: 89,
    percentage: 18.2,
    color: 'bg-blue-500',
    icon: FileTextIcon,
    badge: 'neu'
  },
  {
    status: AntragStatus.InBearbeitung,
    label: 'In Bearbeitung',
    count: 156,
    percentage: 32.0,
    color: 'bg-yellow-500',
    icon: ClockIcon,
    badge: 'bearbeitung'
  },
  {
    status: AntragStatus.Wartend,
    label: 'Wartend',
    count: 67,
    percentage: 13.7,
    color: 'bg-orange-500',
    icon: AlertCircleIcon,
    badge: 'wartend'
  },
  {
    status: AntragStatus.Genehmigt,
    label: 'Genehmigt',
    count: 124,
    percentage: 25.4,
    color: 'bg-green-500',
    icon: CheckCircleIcon,
    badge: 'genehmigt'
  },
  {
    status: AntragStatus.Abgelehnt,
    label: 'Abgelehnt',
    count: 31,
    percentage: 6.3,
    color: 'bg-red-500',
    icon: XCircleIcon,
    badge: 'abgelehnt'
  },
  {
    status: AntragStatus.Archiviert,
    label: 'Archiviert',
    count: 21,
    percentage: 4.3,
    color: 'bg-gray-500',
    icon: ArchiveIcon,
    badge: 'archiviert'
  }
]

const totalApplications = mockStatusData.reduce((sum, item) => sum + item.count, 0)

interface StatusItemProps {
  status: typeof mockStatusData[0]
  isLast: boolean
}

function StatusItem({ status, isLast }: StatusItemProps) {
  const Icon = status.icon
  
  return (
    <div className={`flex items-center justify-between py-3 ${!isLast ? 'border-b border-secondary-100' : ''}`}>
      <div className="flex items-center space-x-3">
        <div className={`w-3 h-3 rounded-full ${status.color.replace('bg-', 'bg-')}`} />
        <div className="flex items-center space-x-2">
          <Icon className="w-4 h-4 text-secondary-500" />
          <span className="text-sm font-medium text-secondary-900">{status.label}</span>
        </div>
      </div>
      <div className="flex items-center space-x-3">
        <Badge variant={status.badge as any} className="text-xs">
          {status.count}
        </Badge>
        <span className="text-sm text-secondary-600 font-mono">
          {status.percentage.toFixed(1)}%
        </span>
      </div>
    </div>
  )
}

export function StatusOverview() {
  const [selectedStatus, setSelectedStatus] = React.useState<AntragStatus | null>(null)
  
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <PieChartIcon className="h-5 w-5" />
          <span>Status-Übersicht</span>
        </CardTitle>
        <CardDescription>
          Verteilung der Anträge nach Bearbeitungsstatus
        </CardDescription>
      </CardHeader>
      <CardContent>
        {/* Summary */}
        <div className="mb-6 p-4 bg-secondary-50 rounded-lg">
          <div className="text-center">
            <div className="text-2xl font-bold text-secondary-900">
              {totalApplications.toLocaleString('de-DE')}
            </div>
            <div className="text-sm text-secondary-600">
              Anträge insgesamt
            </div>
          </div>
        </div>
        
        {/* Visual representation using bars */}
        <div className="mb-6">
          <div className="flex h-4 rounded-lg overflow-hidden bg-secondary-100">
            {mockStatusData.map((status, index) => (
              <div
                key={status.status}
                className={`${status.color} transition-all duration-300 hover:opacity-80 cursor-pointer`}
                style={{ width: `${status.percentage}%` }}
                title={`${status.label}: ${status.count} (${status.percentage.toFixed(1)}%)`}
                onClick={() => setSelectedStatus(
                  selectedStatus === status.status ? null : status.status
                )}
              />
            ))}
          </div>
          <div className="mt-2 text-xs text-secondary-500 text-center">
            Klicken Sie auf einen Bereich für Details
          </div>
        </div>
        
        {/* Status list */}
        <div className="space-y-0">
          {mockStatusData.map((status, index) => (
            <StatusItem 
              key={status.status}
              status={status}
              isLast={index === mockStatusData.length - 1}
            />
          ))}
        </div>
        
        {/* Selected status details */}
        {selectedStatus !== null && (
          <div className="mt-4 p-4 bg-primary-50 rounded-lg border border-primary-200">
            {(() => {
              const status = mockStatusData.find(s => s.status === selectedStatus)
              if (!status) return null
              
              return (
                <div>
                  <h4 className="font-semibold text-primary-900 mb-2">
                    {status.label} - Details
                  </h4>
                  <div className="text-sm text-primary-800 space-y-1">
                    <div>Anzahl: {status.count} Anträge</div>
                    <div>Anteil: {status.percentage.toFixed(1)}% aller Anträge</div>
                    <div>Status: {getStatusDescription(status.status)}</div>
                  </div>
                </div>
              )
            })()}
          </div>
        )}
        
        {/* Action buttons */}
        <div className="mt-6 pt-4 border-t border-secondary-200">
          <div className="flex flex-wrap gap-2">
            <button className="text-xs text-primary-600 hover:text-primary-700 hover:underline">
              Detaillierte Statistiken
            </button>
            <span className="text-xs text-secondary-300">•</span>
            <button className="text-xs text-primary-600 hover:text-primary-700 hover:underline">
              Bericht exportieren
            </button>
            <span className="text-xs text-secondary-300">•</span>
            <button className="text-xs text-primary-600 hover:text-primary-700 hover:underline">
              Filter anwenden
            </button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function getStatusDescription(status: AntragStatus): string {
  switch (status) {
    case AntragStatus.Neu:
      return 'Neue eingereichte Anträge, noch nicht bearbeitet'
    case AntragStatus.InBearbeitung:
      return 'Anträge werden derzeit bearbeitet'
    case AntragStatus.Wartend:
      return 'Anträge warten auf weitere Informationen'
    case AntragStatus.Genehmigt:
      return 'Erfolgreich genehmigte Anträge'
    case AntragStatus.Abgelehnt:
      return 'Abgelehnte Anträge mit Begründung'
    case AntragStatus.Archiviert:
      return 'Archivierte, abgeschlossene Anträge'
    default:
      return 'Unbekannter Status'
  }
}