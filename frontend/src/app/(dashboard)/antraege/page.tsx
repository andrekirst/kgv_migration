// Anträge list page with search, filter, and pagination
import * as React from 'react'
import { Metadata } from 'next'
import { AntraegeList } from '@/components/antraege/antraege-list'
import { AntraegeHeader } from '@/components/antraege/antraege-header'
import { AntraegeFilters } from '@/components/antraege/antraege-filters'

export const metadata: Metadata = {
  title: 'Anträge',
  description: 'Verwaltung aller Kleingartenanträge - Suchen, Filtern und Bearbeiten von Anträgen',
}

interface AntraegePageProps {
  searchParams: {
    page?: string
    search?: string
    status?: string
    bezirk?: string
    sort?: string
    direction?: 'asc' | 'desc'
  }
}

export default function AntraegePage({ searchParams }: AntraegePageProps) {
  return (
    <div className="space-y-6">
      {/* Page header with actions */}
      <AntraegeHeader />
      
      {/* Filters and search */}
      <AntraegeFilters />
      
      {/* Applications list */}
      <AntraegeList searchParams={searchParams} />
    </div>
  )
}