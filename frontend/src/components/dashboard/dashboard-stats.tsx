'use client'

// Dashboard statistics component with KGV KPIs
import * as React from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { 
  FileTextIcon, 
  UsersIcon, 
  CheckCircleIcon, 
  ClockIcon,
  TrendingUpIcon,
  TrendingDownIcon,
  CalendarIcon,
  MapIcon
} from 'lucide-react'
import { formatDate } from '@/lib/utils'

// Mock data - replace with real API calls
const mockStats = {
  totalAntraege: 1247,
  activeAntraege: 892,
  pendingAntraege: 156,
  approvedThisMonth: 34,
  averageProcessingTime: 12.5,
  rejectedThisMonth: 8,
  totalBezirke: 12,
  availableParcels: 45,
  trends: {
    totalAntraege: 5.2,
    activeAntraege: -2.1,
    pendingAntraege: 8.7,
    approvedThisMonth: 12.3,
  }
}

interface StatCardProps {
  title: string
  value: string | number
  description: string
  icon: React.ComponentType<{ className?: string }>
  trend?: number
  badge?: {
    text: string
    variant: 'default' | 'secondary' | 'success' | 'warning' | 'destructive'
  }
}

function StatCard({ title, value, description, icon: Icon, trend, badge }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-secondary-600">
          {title}
        </CardTitle>
        <div className="flex items-center space-x-2">
          {badge && (
            <Badge variant={badge.variant} className="text-xs">
              {badge.text}
            </Badge>
          )}
          <Icon className="h-4 w-4 text-secondary-500" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-secondary-900">{value}</div>
        <div className="flex items-center justify-between mt-2">
          <p className="text-xs text-secondary-600">{description}</p>
          {trend !== undefined && (
            <div className={`flex items-center text-xs ${
              trend > 0 ? 'text-success-600' : trend < 0 ? 'text-error-600' : 'text-secondary-500'
            }`}>
              {trend > 0 ? (
                <TrendingUpIcon className="w-3 h-3 mr-1" />
              ) : trend < 0 ? (
                <TrendingDownIcon className="w-3 h-3 mr-1" />
              ) : null}
              {Math.abs(trend)}%
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

export function DashboardStats() {
  const currentMonth = formatDate(new Date(), 'MMMM yyyy')
  
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      <StatCard
        title="Gesamte Anträge"
        value={mockStats.totalAntraege.toLocaleString('de-DE')}
        description="Alle Anträge im System"
        icon={FileTextIcon}
        trend={mockStats.trends.totalAntraege}
        badge={{
          text: "Aktiv",
          variant: "success"
        }}
      />
      
      <StatCard
        title="Aktive Anträge"
        value={mockStats.activeAntraege.toLocaleString('de-DE')}
        description="Derzeit in Bearbeitung"
        icon={ClockIcon}
        trend={mockStats.trends.activeAntraege}
        badge={{
          text: "Bearbeitung",
          variant: "warning"
        }}
      />
      
      <StatCard
        title="Wartende Anträge"
        value={mockStats.pendingAntraege.toLocaleString('de-DE')}
        description="Warten auf Bearbeitung"
        icon={UsersIcon}
        trend={mockStats.trends.pendingAntraege}
        badge={{
          text: "Wartend",
          variant: "secondary"
        }}
      />
      
      <StatCard
        title={`Genehmigt ${currentMonth}`}
        value={mockStats.approvedThisMonth.toLocaleString('de-DE')}
        description="Genehmigte Anträge"
        icon={CheckCircleIcon}
        trend={mockStats.trends.approvedThisMonth}
        badge={{
          text: "Genehmigt",
          variant: "success"
        }}
      />
      
      <StatCard
        title="Ø Bearbeitungszeit"
        value={`${mockStats.averageProcessingTime} Tage`}
        description="Durchschnittliche Dauer"
        icon={CalendarIcon}
        badge={{
          text: "Performance",
          variant: "default"
        }}
      />
      
      <StatCard
        title={`Abgelehnt ${currentMonth}`}
        value={mockStats.rejectedThisMonth.toLocaleString('de-DE')}
        description="Abgelehnte Anträge"
        icon={FileTextIcon}
        badge={{
          text: "Abgelehnt",
          variant: "destructive"
        }}
      />
      
      <StatCard
        title="Bezirke"
        value={mockStats.totalBezirke.toLocaleString('de-DE')}
        description="Verwaltete Bezirke"
        icon={MapIcon}
        badge={{
          text: "Aktiv",
          variant: "default"
        }}
      />
      
      <StatCard
        title="Verfügbare Parzellen"
        value={mockStats.availableParcels.toLocaleString('de-DE')}
        description="Zur Vergabe verfügbar"
        icon={MapIcon}
        badge={{
          text: "Verfügbar",
          variant: "success"
        }}
      />
    </div>
  )
}