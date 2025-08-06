'use client'

import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { BezirkForm } from '@/components/forms'
import type { Bezirk } from '@/types/bezirke'

export default function NeueBezirkPage() {
  const router = useRouter()

  const handleSuccess = (bezirk: Bezirk) => {
    console.log('handleSuccess called with bezirk:', bezirk)
    // Navigiere zurück zur Bezirke-Übersicht nach erfolgreicher Erstellung
    console.log('Navigating to /bezirke')
    router.push('/bezirke')
  }

  const handleCancel = () => {
    // Navigiere zurück zur Bezirke-Übersicht
    router.push('/bezirke')
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <nav className="flex" aria-label="Breadcrumb">
        <ol className="flex items-center space-x-2">
          <li>
            <Link href="/bezirke" className="text-secondary-500 hover:text-secondary-700">
              Bezirke
            </Link>
          </li>
          <li className="text-secondary-400">/</li>
          <li className="text-secondary-900 font-medium">
            Neuer Bezirk
          </li>
        </ol>
      </nav>

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-secondary-900">
            Neuen Bezirk erstellen
          </h1>
          <p className="text-secondary-600">
            Erfassen Sie die Grunddaten für einen neuen Kleingartenverein Bezirk
          </p>
        </div>
        <Link href="/bezirke">
          <Button variant="outline">
            Zurück zur Übersicht
          </Button>
        </Link>
      </div>

      {/* Form */}
      <BezirkForm
        mode="create"
        onSuccess={handleSuccess}
        onCancel={handleCancel}
        className="mt-6"
      />
    </div>
  )
}