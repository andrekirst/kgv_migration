import { Metadata } from 'next'

export const metadata: Metadata = {
  title: 'Bezirksverwaltung | KGV Verwaltung',
  description: 'Verwaltung der Kleingartenverein Bezirke und Parzellen',
}

interface BezirkeLayoutProps {
  children: React.ReactNode
}

export default function BezirkeLayout({ children }: BezirkeLayoutProps) {
  return (
    <div className="space-y-6">
      <div className="border-b border-secondary-200 pb-4">
        <h1 className="text-3xl font-bold text-secondary-900">
          Bezirksverwaltung
        </h1>
        <p className="mt-2 text-secondary-600">
          Verwalten Sie Bezirke und deren Parzellen
        </p>
      </div>
      {children}
    </div>
  )
}