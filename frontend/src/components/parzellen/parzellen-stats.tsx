import { Card } from '@/components/ui/card'

interface ParzellenStatistiken {
  gesamtParzellen: number
  freieParzellen: number
  belegteParzellen: number
  reservierteParzellen: number
  wartungParzellen: number
  gesperrteParzellen: number
  durchschnittsPacht: number
  gesamtEinnahmen: number
  auslastung: number
}

interface ParzellenStatsProps {
  getStatistiken: () => Promise<ParzellenStatistiken>
}

export async function ParzellenStats({ getStatistiken }: ParzellenStatsProps) {
  const stats = await getStatistiken()

  const statistikCards = [
    {
      title: 'Gesamt Parzellen',
      value: stats.gesamtParzellen,
      subtitle: `${stats.belegteParzellen} belegt`,
      icon: (
        <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
        </svg>
      ),
      color: 'blue'
    },
    {
      title: 'Freie Parzellen',
      value: stats.freieParzellen,
      subtitle: 'verfügbar',
      icon: (
        <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      color: 'green'
    },
    {
      title: 'Reserviert/Wartung',
      value: stats.reservierteParzellen + stats.wartungParzellen,
      subtitle: `${stats.reservierteParzellen} reserviert, ${stats.wartungParzellen} Wartung`,
      icon: (
        <svg className="w-6 h-6 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      color: 'orange'
    },
    {
      title: 'Monatliche Einnahmen',
      value: `€${stats.gesamtEinnahmen.toLocaleString('de-DE')}`,
      subtitle: `Ø €${stats.durchschnittsPacht.toFixed(2)}`,
      icon: (
        <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
        </svg>
      ),
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
            </div>
            <div className={`p-3 rounded-lg bg-${stat.color}-100`}>
              {stat.icon}
            </div>
          </div>
          
          {/* Progress bar for utilization */}
          {index === 0 && stats.gesamtParzellen > 0 && (
            <div className="mt-4">
              <div className="flex items-center justify-between text-sm text-secondary-600 mb-1">
                <span>Auslastung</span>
                <span>{Math.round(stats.auslastung)}%</span>
              </div>
              <div className="w-full bg-secondary-200 rounded-full h-2">
                <div
                  className={`h-2 rounded-full ${
                    stats.auslastung >= 90 ? 'bg-red-500' :
                    stats.auslastung >= 70 ? 'bg-yellow-500' :
                    'bg-green-500'
                  }`}
                  style={{ width: `${Math.min(stats.auslastung, 100)}%` }}
                />
              </div>
            </div>
          )}
        </Card>
      ))}
    </div>
  )
}