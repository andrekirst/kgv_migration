'use client'

// Header component for Anträge pages
import * as React from 'react'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { 
  PlusIcon, 
  FileTextIcon, 
  DownloadIcon, 
  FilterIcon,
  RefreshCwIcon
} from 'lucide-react'

export function AntraegeHeader() {
  const [isRefreshing, setIsRefreshing] = React.useState(false)
  
  const handleRefresh = async () => {
    setIsRefreshing(true)
    // Simulate refresh
    await new Promise(resolve => setTimeout(resolve, 1000))
    setIsRefreshing(false)
  }
  
  return (
    <div className="flex flex-col space-y-4 sm:flex-row sm:items-center sm:justify-between sm:space-y-0">
      {/* Title and description */}
      <div>
        <h1 className="text-3xl font-bold text-secondary-900 flex items-center space-x-2">
          <FileTextIcon className="h-8 w-8" />
          <span>Anträge</span>
        </h1>
        <p className="mt-2 text-secondary-600">
          Verwalten Sie alle Kleingartenanträge - von der Eingabe bis zur Genehmigung
        </p>
      </div>
      
      {/* Action buttons */}
      <div className="flex items-center space-x-3">
        {/* Refresh button */}
        <Button
          variant="outline"
          size="sm"
          onClick={handleRefresh}
          disabled={isRefreshing}
          loading={isRefreshing}
          loadingText="Aktualisiert..."
        >
          <RefreshCwIcon className="h-4 w-4" />
          Aktualisieren
        </Button>
        
        {/* Export button */}
        <Button variant="outline" size="sm">
          <DownloadIcon className="h-4 w-4" />
          Exportieren
        </Button>
        
        {/* New application button */}
        <Button asChild>
          <Link href="/antraege/neu" className="flex items-center gap-2">
            <PlusIcon className="h-4 w-4" />
            Neuer Antrag
          </Link>
        </Button>
      </div>
    </div>
  )
}