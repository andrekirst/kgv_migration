import { Card } from '@/components/ui/card'
import type { GesamtStatistiken } from '@/types/bezirke'

interface BezirkeStatsProps {
  getStatistiken: () => Promise<GesamtStatistiken>
}

export async function BezirkeStats({ getStatistiken }: BezirkeStatsProps) {
  const stats = await getStatistiken()

  const statistikCards = [
    {
      title: 'Gesamt Bezirke',
      value: stats.gesamtBezirke,
      subtitle: `${stats.aktiveBezirke} aktiv`,
      icon: (
        <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      ),
      trend: stats.trends ? `+${stats.trends.neueParzellen} diese Periode` : undefined,
      color: 'blue'
    },
    {
      title: 'Gesamt Parzellen',
      value: stats.gesamtParzellen,
      subtitle: `${stats.belegteParzellen} belegt`,
      icon: (
        <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
        </svg>
      ),
      trend: stats.trends ? `${stats.trends.neueAntraege > 0 ? '+' : ''}${stats.trends.neueAntraege} Anträge` : undefined,
      color: 'green'
    },
    {
      title: 'Freie Parzellen',
      value: stats.freieParzellen,
      subtitle: `${stats.reservierteParzellen} reserviert`,
      icon: (
        <svg className="w-6 h-6 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      trend: stats.trends ? `${stats.trends.kuendigungen} Kündigungen` : undefined,
      color: 'orange'
    },
    {
      title: 'Auslastung',
      value: `${Math.round(stats.auslastung)}%`,
      subtitle: 'der Parzellen belegt',
      icon: (
        <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      ),
      trend: stats.trends ? stats.trends.zeitraum : undefined,
      color: 'purple'
    }
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {statistikCards.map((stat, index) => (
        <Card key={index} className="p-6">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <p className="text-sm font-medium text-secondary-600 mb-1">
                {stat.title}
              </p>
              <p className="text-2xl font-bold text-secondary-900 mb-1">
                {stat.value}
              </p>
              <p className="text-sm text-secondary-500">
                {stat.subtitle}
              </p>
              {stat.trend && (
                <p className="text-xs text-secondary-400 mt-2">
                  {stat.trend}
                </p>
              )}
            </div>
            <div className={`p-3 rounded-lg bg-${stat.color}-100`}>
              {stat.icon}
            </div>
          </div>
        </Card>
      ))}
    </div>
  )
}