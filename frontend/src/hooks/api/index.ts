// Export all API hooks for easy importing
// Usage: import { useBezirke, useCreateParzelle, useAntraege } from '@/hooks/api'

// Bezirke (Districts) hooks
export {
  useBezirke,
  useBezirk,
  useBezirkeSearch,
  useBezirkeStatistics,
  useBezirkeDropdown,
  useCreateBezirk,
  useUpdateBezirk,
  useDeleteBezirk,
  usePrefetchBezirk,
  useCachedBezirk,
  useOptimisticBezirkUpdate
} from './use-bezirke'

// Parzellen (Plots) hooks
export {
  useParzellen,
  useParzellenByBezirk,
  useAvailableParzellen,
  useParzelle,
  useParzelleHistory,
  useCreateParzelle,
  useUpdateParzelle,
  useDeleteParzelle,
  useAssignParzelle,
  useUnassignParzelle,
  usePrefetchParzelle,
  useCachedParzelle,
  useOptimisticParzelleUpdate
} from './use-parzellen'

// Anträge (Applications) hooks
export {
  useAntraege,
  usePendingAntraege,
  useAntrag,
  useAntragHistory,
  useDashboardStats,
  useAntraegeSearch,
  useCreateAntrag,
  useUpdateAntrag,
  useUpdateAntragStatus,
  useDeleteAntrag,
  useBulkUpdateAntragStatus,
  usePrefetchAntrag,
  useCachedAntrag,
  useOptimisticAntragUpdate,
  useRealtimeAntraege
} from './use-antraege'

// Re-export query keys and helpers for advanced usage
export { queryKeys, queryKeyHelpers, mutationKeys } from '@/lib/query-keys'
export { apiClient } from '@/lib/api-client'