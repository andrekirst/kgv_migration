// Comprehensive example demonstrating React Query API integration
// This component showcases best practices for using the KGV API hooks
'use client'

import React from 'react'
import { 
  useBezirke, 
  useBezirk, 
  useCreateBezirk, 
  useUpdateBezirk, 
  useDeleteBezirk,
  useParzellen,
  useAvailableParzellen,
  useCreateParzelle,
  useAntraege,
  usePendingAntraege,
  useDashboardStats
} from '@/hooks/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card } from '@/components/ui/card'
import type { BezirkeFilter, ParzellenFilter, BezirkCreateRequest } from '@/types/bezirke'
import type { AntragFilter } from '@/types/api'

/**
 * Example component demonstrating comprehensive API integration patterns
 */
export function ApiIntegrationExample() {
  // State for filters and forms
  const [bezirkeFilter, setBezirkeFilter] = React.useState<BezirkeFilter>({
    aktiv: true,
    page: 1,
    limit: 10,
    sortBy: 'name',
    sortOrder: 'asc'
  })

  const [parzellenFilter, setParzellenFilter] = React.useState<ParzellenFilter>({
    aktiv: true,
    page: 1,
    limit: 10
  })

  const [selectedBezirkId, setSelectedBezirkId] = React.useState<number | null>(null)
  const [newBezirkData, setNewBezirkData] = React.useState<BezirkCreateRequest>({
    name: '',
    beschreibung: ''
  })

  // ============================
  // QUERY EXAMPLES
  // ============================

  // Example 1: Basic list query with filters
  const {
    data: bezirkeData,
    isLoading: bezirkeLoading,
    isError: bezirkeError,
    error: bezirkeErrorDetails,
    isFetching: bezirkeFetching,
    refetch: refetchBezirke
  } = useBezirke(bezirkeFilter, {
    // Additional React Query options
    enabled: true, // Query is enabled
    refetchOnWindowFocus: true,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })

  // Example 2: Dependent query (only runs when selectedBezirkId is available)
  const {
    data: selectedBezirk,
    isLoading: bezirkLoading,
    error: bezirkError
  } = useBezirk(selectedBezirkId, {
    enabled: !!selectedBezirkId, // Only fetch when ID is available
    retry: 3,
    staleTime: 10 * 60 * 1000 // 10 minutes for individual items
  })

  // Example 3: Multiple related queries
  const { data: parzellenData, isLoading: parzellenLoading } = useParzellen(parzellenFilter)
  const { data: availableParzellen } = useAvailableParzellen({ 
    bezirkId: selectedBezirkId || undefined 
  })
  const { data: antraegeData } = useAntraege(
    { aktiv: true, status: [0, 1, 2] }, // Filter for active pending applications
    { pageNumber: 1, pageSize: 20 }
  )

  // Example 4: Real-time data with frequent updates
  const { data: pendingAntraege } = usePendingAntraege(undefined, {
    refetchInterval: 30000, // Refetch every 30 seconds
    refetchIntervalInBackground: true
  })

  // Example 5: Dashboard statistics
  const { data: dashboardStats, isLoading: statsLoading } = useDashboardStats({
    refetchInterval: 60000, // Update every minute
    staleTime: 2 * 60 * 1000 // 2 minutes stale time
  })

  // ============================
  // MUTATION EXAMPLES
  // ============================

  // Example 1: Create mutation with optimistic updates
  const createBezirkMutation = useCreateBezirk({
    onMutate: async (newBezirk) => {
      // Optimistic update: immediately show the new bezirk in UI
      console.log('Creating bezirk:', newBezirk)
      
      // You could optimistically update the cache here
      // queryClient.setQueryData(queryKeys.bezirke.list(bezirkeFilter), old => ...)
    },
    onSuccess: (createdBezirk) => {
      console.log('Bezirk created successfully:', createdBezirk)
      // Clear form
      setNewBezirkData({ name: '', beschreibung: '' })
      // Could trigger additional actions like navigation
    },
    onError: (error) => {
      console.error('Failed to create bezirk:', error)
      // Error handling is automatically done by the hook + error handler
    }
  })

  // Example 2: Update mutation
  const updateBezirkMutation = useUpdateBezirk({
    onSuccess: (updatedBezirk) => {
      console.log('Bezirk updated:', updatedBezirk)
      // The cache is automatically updated by the hook
    }
  })

  // Example 3: Delete mutation with confirmation
  const deleteBezirkMutation = useDeleteBezirk({
    onMutate: async ({ id }) => {
      // Could show loading state or optimistically remove from list
      console.log('Deleting bezirk:', id)
    },
    onSuccess: () => {
      // Reset selection if deleted bezirk was selected
      if (selectedBezirkId && selectedBezirkId === selectedBezirkId) {
        setSelectedBezirkId(null)
      }
    }
  })

  // Example 4: Create Parzelle mutation
  const createParzelleMutation = useCreateParzelle({
    onSuccess: (newParzelle) => {
      console.log('Parzelle created:', newParzelle)
      // Could update local state or trigger navigation
    }
  })

  // ============================
  // EVENT HANDLERS
  // ============================

  const handleCreateBezirk = async () => {
    if (!newBezirkData.name.trim()) {
      alert('Bitte geben Sie einen Namen ein')
      return
    }

    try {
      await createBezirkMutation.mutateAsync(newBezirkData)
    } catch (error) {
      // Error is handled by the hook and error handler
      console.error('Create bezirk failed:', error)
    }
  }

  const handleUpdateBezirk = async (id: number, updates: Partial<BezirkCreateRequest>) => {
    try {
      await updateBezirkMutation.mutateAsync({ id, ...updates })
    } catch (error) {
      console.error('Update bezirk failed:', error)
    }
  }

  const handleDeleteBezirk = async (id: number) => {
    if (!confirm('Sind Sie sicher, dass Sie diesen Bezirk löschen möchten?')) {
      return
    }

    try {
      await deleteBezirkMutation.mutateAsync({ id, force: false })
    } catch (error) {
      console.error('Delete bezirk failed:', error)
    }
  }

  const handleFilterChange = (newFilter: Partial<BezirkeFilter>) => {
    setBezirkeFilter(prev => ({ ...prev, ...newFilter, page: 1 })) // Reset to page 1
  }

  const handlePageChange = (page: number) => {
    setBezirkeFilter(prev => ({ ...prev, page }))
  }

  // ============================
  // RENDER
  // ============================

  return (
    <div className=\"space-y-6 p-6\">\n      <h1 className=\"text-2xl font-bold\">API Integration Example</h1>\n      \n      {/* Dashboard Stats Example */}\n      <Card className=\"p-4\">\n        <h2 className=\"text-lg font-semibold mb-4\">Dashboard Statistics</h2>\n        {statsLoading ? (\n          <div>Loading statistics...</div>\n        ) : dashboardStats ? (\n          <div className=\"grid grid-cols-2 md:grid-cols-4 gap-4\">\n            <div className=\"text-center\">\n              <div className=\"text-2xl font-bold\">{dashboardStats.totalAntraege}</div>\n              <div className=\"text-sm text-gray-600\">Total Anträge</div>\n            </div>\n            <div className=\"text-center\">\n              <div className=\"text-2xl font-bold\">{dashboardStats.activeAntraege}</div>\n              <div className=\"text-sm text-gray-600\">Active Anträge</div>\n            </div>\n            <div className=\"text-center\">\n              <div className=\"text-2xl font-bold\">{dashboardStats.pendingAntraege}</div>\n              <div className=\"text-sm text-gray-600\">Pending Anträge</div>\n            </div>\n            <div className=\"text-center\">\n              <div className=\"text-2xl font-bold\">{dashboardStats.approvedThisMonth}</div>\n              <div className=\"text-sm text-gray-600\">Approved This Month</div>\n            </div>\n          </div>\n        ) : (\n          <div>No statistics available</div>\n        )}\n      </Card>\n\n      {/* Create Bezirk Form Example */}\n      <Card className=\"p-4\">\n        <h2 className=\"text-lg font-semibold mb-4\">Create New Bezirk</h2>\n        <div className=\"space-y-4\">\n          <Input\n            placeholder=\"Bezirk Name\"\n            value={newBezirkData.name}\n            onChange={(e) => setNewBezirkData(prev => ({ ...prev, name: e.target.value }))}\n          />\n          <Input\n            placeholder=\"Beschreibung (optional)\"\n            value={newBezirkData.beschreibung || ''}\n            onChange={(e) => setNewBezirkData(prev => ({ ...prev, beschreibung: e.target.value }))}\n          />\n          <Button \n            onClick={handleCreateBezirk}\n            disabled={createBezirkMutation.isPending}\n          >\n            {createBezirkMutation.isPending ? 'Creating...' : 'Create Bezirk'}\n          </Button>\n        </div>\n      </Card>\n\n      {/* Bezirke List Example */}\n      <Card className=\"p-4\">\n        <div className=\"flex justify-between items-center mb-4\">\n          <h2 className=\"text-lg font-semibold\">Bezirke List</h2>\n          <div className=\"flex gap-2\">\n            <Input\n              placeholder=\"Search...\"\n              value={bezirkeFilter.search || ''}\n              onChange={(e) => handleFilterChange({ search: e.target.value })}\n              className=\"w-48\"\n            />\n            <Button \n              variant=\"outline\" \n              onClick={() => refetchBezirke()}\n              disabled={bezirkeFetching}\n            >\n              {bezirkeFetching ? 'Refreshing...' : 'Refresh'}\n            </Button>\n          </div>\n        </div>\n\n        {bezirkeLoading ? (\n          <div className=\"text-center py-8\">Loading bezirke...</div>\n        ) : bezirkeError ? (\n          <div className=\"text-center py-8 text-red-600\">\n            Error loading bezirke: {bezirkeErrorDetails?.message}\n          </div>\n        ) : bezirkeData?.bezirke ? (\n          <div>\n            <div className=\"space-y-2\">\n              {bezirkeData.bezirke.map((bezirk) => (\n                <div \n                  key={bezirk.id} \n                  className={`p-3 border rounded cursor-pointer hover:bg-gray-50 ${\n                    selectedBezirkId === bezirk.id ? 'border-blue-500 bg-blue-50' : ''\n                  }`}\n                  onClick={() => setSelectedBezirkId(bezirk.id)}\n                >\n                  <div className=\"flex justify-between items-center\">\n                    <div>\n                      <h3 className=\"font-medium\">{bezirk.name}</h3>\n                      {bezirk.beschreibung && (\n                        <p className=\"text-sm text-gray-600\">{bezirk.beschreibung}</p>\n                      )}\n                      <div className=\"text-xs text-gray-500\">\n                        {bezirk.statistiken.gesamtParzellen} Parzellen | \n                        {bezirk.statistiken.freieParzellen} frei\n                      </div>\n                    </div>\n                    <div className=\"flex gap-2\">\n                      <Button\n                        size=\"sm\"\n                        variant=\"outline\"\n                        onClick={(e) => {\n                          e.stopPropagation()\n                          handleUpdateBezirk(bezirk.id, { \n                            name: bezirk.name + ' (Updated)' \n                          })\n                        }}\n                        disabled={updateBezirkMutation.isPending}\n                      >\n                        Update\n                      </Button>\n                      <Button\n                        size=\"sm\"\n                        variant=\"destructive\"\n                        onClick={(e) => {\n                          e.stopPropagation()\n                          handleDeleteBezirk(bezirk.id)\n                        }}\n                        disabled={deleteBezirkMutation.isPending}\n                      >\n                        Delete\n                      </Button>\n                    </div>\n                  </div>\n                </div>\n              ))}\n            </div>\n\n            {/* Pagination */}\n            <div className=\"flex justify-between items-center mt-4\">\n              <div className=\"text-sm text-gray-600\">\n                Showing {bezirkeData.bezirke.length} of {bezirkeData.pagination.total} bezirke\n              </div>\n              <div className=\"flex gap-2\">\n                <Button\n                  size=\"sm\"\n                  variant=\"outline\"\n                  disabled={bezirkeFilter.page === 1}\n                  onClick={() => handlePageChange(bezirkeFilter.page! - 1)}\n                >\n                  Previous\n                </Button>\n                <span className=\"flex items-center px-2\">\n                  Page {bezirkeFilter.page} of {bezirkeData.pagination.totalPages}\n                </span>\n                <Button\n                  size=\"sm\"\n                  variant=\"outline\"\n                  disabled={bezirkeFilter.page === bezirkeData.pagination.totalPages}\n                  onClick={() => handlePageChange(bezirkeFilter.page! + 1)}\n                >\n                  Next\n                </Button>\n              </div>\n            </div>\n          </div>\n        ) : (\n          <div className=\"text-center py-8\">No bezirke found</div>\n        )}\n      </Card>\n\n      {/* Selected Bezirk Details */}\n      {selectedBezirkId && (\n        <Card className=\"p-4\">\n          <h2 className=\"text-lg font-semibold mb-4\">Selected Bezirk Details</h2>\n          {bezirkLoading ? (\n            <div>Loading bezirk details...</div>\n          ) : bezirkError ? (\n            <div className=\"text-red-600\">Error loading bezirk details</div>\n          ) : selectedBezirk ? (\n            <div className=\"space-y-2\">\n              <div><strong>Name:</strong> {selectedBezirk.name}</div>\n              <div><strong>Description:</strong> {selectedBezirk.beschreibung || 'N/A'}</div>\n              <div><strong>Bezirksleiter:</strong> {selectedBezirk.bezirksleiter || 'N/A'}</div>\n              <div><strong>Status:</strong> {selectedBezirk.aktiv ? 'Active' : 'Inactive'}</div>\n              <div><strong>Total Parzellen:</strong> {selectedBezirk.statistiken.gesamtParzellen}</div>\n              <div><strong>Free Parzellen:</strong> {selectedBezirk.statistiken.freieParzellen}</div>\n            </div>\n          ) : null}\n        </Card>\n      )}\n\n      {/* Real-time Pending Applications */}\n      <Card className=\"p-4\">\n        <h2 className=\"text-lg font-semibold mb-4\">Pending Applications (Real-time)</h2>\n        {pendingAntraege ? (\n          <div>\n            <div className=\"text-sm text-gray-600 mb-2\">\n              {pendingAntraege.totalCount} pending applications\n            </div>\n            <div className=\"space-y-1\">\n              {pendingAntraege.data.slice(0, 5).map((antrag) => (\n                <div key={antrag.id} className=\"text-sm p-2 bg-yellow-50 rounded\">\n                  {antrag.vollName} - {antrag.statusBeschreibung}\n                </div>\n              ))}\n            </div>\n          </div>\n        ) : (\n          <div>Loading pending applications...</div>\n        )}\n      </Card>\n\n      {/* Available Parzellen */}\n      {availableParzellen && availableParzellen.parzellen.length > 0 && (\n        <Card className=\"p-4\">\n          <h2 className=\"text-lg font-semibold mb-4\">Available Parzellen</h2>\n          <div className=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4\">\n            {availableParzellen.parzellen.slice(0, 6).map((parzelle) => (\n              <div key={parzelle.id} className=\"p-3 border rounded\">\n                <div className=\"font-medium\">{parzelle.nummer}</div>\n                <div className=\"text-sm text-gray-600\">{parzelle.bezirkName}</div>\n                <div className=\"text-sm\">{parzelle.groesse}m² - €{parzelle.monatlichePacht}/month</div>\n              </div>\n            ))}\n          </div>\n        </Card>\n      )}\n    </div>\n  )\n}\n\n// Export as default for easy importing\nexport default ApiIntegrationExample