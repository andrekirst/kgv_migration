// Dashboard page with KGV statistics and KPIs
import * as React from 'react'
import { Metadata } from 'next'
import { DashboardStats } from '@/components/dashboard/dashboard-stats'
import { RecentActivity } from '@/components/dashboard/recent-activity'
import { QuickActions } from '@/components/dashboard/quick-actions'
import { StatusOverview } from '@/components/dashboard/status-overview'

export const dynamic = 'force-dynamic'

export const metadata: Metadata = {
  title: 'Dashboard',
  description: 'Übersicht über Anträge, Statistiken und aktuelle Aktivitäten im KGV-System',
}

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-secondary-900">
          Dashboard
        </h1>
        <p className="mt-2 text-secondary-600">
          Willkommen im KGV-Verwaltungssystem. Hier finden Sie eine Übersicht über alle wichtigen Kennzahlen und Aktivitäten.
        </p>
      </div>

      {/* Quick actions */}
      <QuickActions />

      {/* Main stats */}
      <DashboardStats />

      {/* Two column layout for additional content */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Status overview */}
        <StatusOverview />
        
        {/* Recent activity */}
        <RecentActivity />
      </div>
    </div>
  )
}