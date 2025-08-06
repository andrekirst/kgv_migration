// React Query hooks for Anträge (Applications) API operations
'use client'

import React from 'react'
import { 
  useQuery, 
  useMutation, 
  useQueryClient, 
  UseQueryOptions,
  UseMutationOptions
} from '@tanstack/react-query'
import { apiClient } from '@/lib/api-client'
import { queryKeys, STALE_TIMES, CACHE_TIMES } from '@/lib/query-keys'
import toast from 'react-hot-toast'
import type {
  AntragDto,
  AntragListDto,
  CreateAntragRequest,
  UpdateAntragRequest,
  AntragFilter,
  AntragStatus,
  VerlaufDto,
  PaginationParams,
  DashboardStats
} from '@/types/api'
import type { ApiResponse, PaginatedResponse } from '@/types/api'

// ============================
// QUERY HOOKS
// ============================

/**
 * Hook to fetch paginated list of applications with filtering and sorting
 * @param filters - Filter parameters
 * @param pagination - Pagination parameters
 * @param options - React Query options
 */
export function useAntraege(
  filters?: AntragFilter,
  pagination?: PaginationParams,
  options?: Omit<UseQueryOptions<PaginatedResponse<AntragListDto>>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.antraege.list(filters, pagination),
    queryFn: async () => {
      const params = new URLSearchParams()
      
      // Filters
      if (filters?.search) params.append('search', filters.search)
      if (filters?.status?.length) {
        filters.status.forEach(status => params.append('status', status.toString()))
      }
      if (filters?.bezirk?.length) {
        filters.bezirk.forEach(bezirk => params.append('bezirk', bezirk))
      }
      if (filters?.aktiv !== undefined) params.append('aktiv', filters.aktiv.toString())
      if (filters?.bewerbungsdatumVon) params.append('bewerbungsdatumVon', filters.bewerbungsdatumVon)
      if (filters?.bewerbungsdatumBis) params.append('bewerbungsdatumBis', filters.bewerbungsdatumBis)
      if (filters?.ort?.length) {
        filters.ort.forEach(ort => params.append('ort', ort))
      }
      
      // Pagination
      if (pagination?.pageNumber) params.append('pageNumber', pagination.pageNumber.toString())
      if (pagination?.pageSize) params.append('pageSize', pagination.pageSize.toString())
      if (pagination?.sortBy) params.append('sortBy', pagination.sortBy)
      if (pagination?.sortDirection) params.append('sortDirection', pagination.sortDirection)

      const response = await apiClient.get<PaginatedResponse<AntragListDto>>(
        `/antraege?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch applications')
      }

      return response.data
    },
    staleTime: STALE_TIMES.DYNAMIC, // Applications change frequently
    gcTime: CACHE_TIMES.DYNAMIC,
    enabled: true,
    ...options
  })
}

/**
 * Hook to fetch pending applications
 * @param filters - Additional filter parameters
 * @param options - React Query options
 */
export function usePendingAntraege(
  filters?: Omit<AntragFilter, 'status'>,
  options?: Omit<UseQueryOptions<PaginatedResponse<AntragListDto>>, 'queryKey' | 'queryFn'>
) {
  const pendingFilters: AntragFilter = {
    ...filters,
    status: [AntragStatus.Neu, AntragStatus.InBearbeitung, AntragStatus.Wartend],
    aktiv: true
  }

  return useQuery({
    queryKey: queryKeys.antraege.pending(pendingFilters),
    queryFn: async () => {
      const params = new URLSearchParams({
        aktiv: 'true'
      })
      
      // Add pending statuses
      params.append('status', AntragStatus.Neu.toString())
      params.append('status', AntragStatus.InBearbeitung.toString())
      params.append('status', AntragStatus.Wartend.toString())
      
      if (filters?.search) params.append('search', filters.search)
      if (filters?.bezirk?.length) {
        filters.bezirk.forEach(bezirk => params.append('bezirk', bezirk))
      }
      if (filters?.bewerbungsdatumVon) params.append('bewerbungsdatumVon', filters.bewerbungsdatumVon)
      if (filters?.bewerbungsdatumBis) params.append('bewerbungsdatumBis', filters.bewerbungsdatumBis)
      if (filters?.ort?.length) {
        filters.ort.forEach(ort => params.append('ort', ort))
      }

      const response = await apiClient.get<PaginatedResponse<AntragListDto>>(
        `/antraege?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch pending applications')
      }

      return response.data
    },
    staleTime: STALE_TIMES.REALTIME, // Very frequent updates for pending items
    gcTime: CACHE_TIMES.REALTIME,
    refetchInterval: 30000, // Refetch every 30 seconds
    ...options
  })
}

/**
 * Hook to fetch a single application by ID
 * @param id - Application ID
 * @param options - React Query options
 */
export function useAntrag(
  id: string | null,
  options?: Omit<UseQueryOptions<AntragDto>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.antraege.detail(id!),
    queryFn: async () => {
      const response = await apiClient.get<AntragDto>(`/antraege/${id}`)
      
      if (!response.success || !response.data) {
        throw new Error('Application not found')
      }
      
      return response.data
    },
    staleTime: STALE_TIMES.DETAILS,
    gcTime: CACHE_TIMES.DETAILS,
    enabled: !!id,
    ...options
  })
}

/**
 * Hook to fetch application history
 * @param antragId - Application ID
 * @param options - React Query options
 */
export function useAntragHistory(
  antragId: string | null,
  options?: Omit<UseQueryOptions<VerlaufDto[]>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.antraege.history(antragId!),
    queryFn: async () => {
      const response = await apiClient.get<VerlaufDto[]>(`/antraege/${antragId}/history`)
      
      if (!response.success) {
        throw new Error('Failed to fetch application history')
      }
      
      return response.data || []
    },
    staleTime: STALE_TIMES.DETAILS,
    gcTime: CACHE_TIMES.DETAILS,
    enabled: !!antragId,
    ...options
  })
}

/**
 * Hook to fetch dashboard statistics
 * @param options - React Query options
 */
export function useDashboardStats(
  options?: Omit<UseQueryOptions<DashboardStats>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.dashboard.stats(),
    queryFn: async () => {
      const response = await apiClient.get<DashboardStats>('/dashboard/statistics')
      
      if (!response.success || !response.data) {
        throw new Error('Failed to fetch dashboard statistics')
      }
      
      return response.data
    },
    staleTime: STALE_TIMES.DYNAMIC,
    gcTime: CACHE_TIMES.DYNAMIC,
    refetchInterval: 60000, // Refetch every minute
    ...options
  })
}

/**
 * Hook to search applications
 * @param query - Search query
 * @param options - React Query options
 */
export function useAntraegeSearch(
  query: string,
  options?: Omit<UseQueryOptions<AntragListDto[]>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.antraege.search(query),
    queryFn: async () => {
      if (!query.trim()) return []
      
      const params = new URLSearchParams({
        search: query.trim(),
        pageSize: '50' // Limit search results
      })

      const response = await apiClient.get<PaginatedResponse<AntragListDto>>(
        `/antraege/search?${params.toString()}`
      )
      
      if (!response.success) {
        throw new Error('Search failed')
      }
      
      return response.data?.data || []
    },
    staleTime: STALE_TIMES.DYNAMIC,
    gcTime: CACHE_TIMES.DYNAMIC,
    enabled: query.length >= 2, // Only search with 2+ characters
    ...options
  })
}

// ============================
// MUTATION HOOKS
// ============================

/**
 * Hook to create a new application
 * @param options - React Query mutation options
 */
export function useCreateAntrag(
  options?: UseMutationOptions<AntragDto, Error, CreateAntragRequest>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateAntragRequest) => {
      const response = await apiClient.post<AntragDto>('/antraege', data)
      
      if (!response.success || !response.data) {
        throw new Error('Failed to create application')
      }
      
      return response.data
    },
    onSuccess: (newAntrag, variables) => {
      // Invalidate and refetch applications lists
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.lists() })
      // Invalidate pending applications (new application is likely pending)
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.pending() })
      // Invalidate dashboard stats
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
      
      // Add to cache
      queryClient.setQueryData(queryKeys.antraege.detail(newAntrag.id), newAntrag)
      
      // Success toast
      toast.success(`Antrag für "${newAntrag.vollName}" wurde erfolgreich erstellt.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to create application:', error)
      toast.error('Fehler beim Erstellen des Antrags. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to update an existing application
 * @param options - React Query mutation options
 */
export function useUpdateAntrag(
  options?: UseMutationOptions<AntragDto, Error, UpdateAntragRequest>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: UpdateAntragRequest) => {
      const response = await apiClient.put<AntragDto>(`/antraege/${data.id}`, data)
      
      if (!response.success || !response.data) {
        throw new Error('Failed to update application')
      }
      
      return response.data
    },
    onSuccess: (updatedAntrag, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.antraege.detail(updatedAntrag.id), updatedAntrag)
      
      // Invalidate lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.lists() })
      // Invalidate pending applications if status might have changed
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.pending() })
      // Invalidate dashboard stats
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
      
      // Success toast
      toast.success(`Antrag für "${updatedAntrag.vollName}" wurde erfolgreich aktualisiert.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to update application:', error)
      toast.error('Fehler beim Aktualisieren des Antrags. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to update application status
 * @param options - React Query mutation options
 */
export function useUpdateAntragStatus(
  options?: UseMutationOptions<AntragDto, Error, { id: string; status: AntragStatus; reason?: string }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, status, reason }) => {
      const response = await apiClient.patch<AntragDto>(`/antraege/${id}/status`, {
        status,
        reason
      })
      
      if (!response.success || !response.data) {
        throw new Error('Failed to update application status')
      }
      
      return response.data
    },
    onSuccess: (updatedAntrag, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.antraege.detail(updatedAntrag.id), updatedAntrag)
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.lists() })
      // Invalidate status-specific lists
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.pending() })
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.approved() })
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.rejected() })
      // Invalidate dashboard stats
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
      
      // Get German status name for toast
      const statusNames = {
        [AntragStatus.Neu]: 'Neu',
        [AntragStatus.InBearbeitung]: 'In Bearbeitung',
        [AntragStatus.Wartend]: 'Wartend',
        [AntragStatus.Genehmigt]: 'Genehmigt',
        [AntragStatus.Abgelehnt]: 'Abgelehnt',
        [AntragStatus.Archiviert]: 'Archiviert'
      }
      
      // Success toast
      toast.success(
        `Status des Antrags wurde auf "${statusNames[variables.status]}" geändert.`, 
        { duration: 4000 }
      )
    },
    onError: (error) => {
      console.error('Failed to update application status:', error)
      toast.error('Fehler beim Ändern des Antragsstatus. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to delete an application
 * @param options - React Query mutation options
 */
export function useDeleteAntrag(
  options?: UseMutationOptions<void, Error, { id: string; reason?: string }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, reason }) => {
      const response = await apiClient.delete<void>(`/antraege/${id}`, {
        data: reason ? { reason } : undefined
      })
      
      if (!response.success) {
        throw new Error('Failed to delete application')
      }
    },
    onSuccess: (_, variables) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: queryKeys.antraege.detail(variables.id) })
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.lists() })
      // Invalidate status-specific lists
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.pending() })
      // Invalidate dashboard stats
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
      
      // Success toast
      toast.success('Antrag wurde erfolgreich gelöscht.', {
        duration: 4000
      })
    },
    onError: (error: any) => {
      console.error('Failed to delete application:', error)
      
      // Handle specific error cases
      if (error?.status === 409) {
        toast.error('Antrag kann nicht gelöscht werden, da er bereits bearbeitet wird.')
      } else {
        toast.error('Fehler beim Löschen des Antrags. Bitte versuchen Sie es erneut.')
      }
    },
    ...options
  })
}

/**
 * Hook to bulk update application statuses
 * @param options - React Query mutation options
 */
export function useBulkUpdateAntragStatus(
  options?: UseMutationOptions<void, Error, { ids: string[]; status: AntragStatus; reason?: string }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ ids, status, reason }) => {
      const response = await apiClient.patch<void>('/antraege/bulk-status', {
        ids,
        status,
        reason
      })
      
      if (!response.success) {
        throw new Error('Failed to bulk update application statuses')
      }
    },
    onSuccess: (_, variables) => {
      // Invalidate all application-related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.all() })
      // Invalidate dashboard stats
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
      
      const statusNames = {
        [AntragStatus.Neu]: 'Neu',
        [AntragStatus.InBearbeitung]: 'In Bearbeitung',
        [AntragStatus.Wartend]: 'Wartend',
        [AntragStatus.Genehmigt]: 'Genehmigt',
        [AntragStatus.Abgelehnt]: 'Abgelehnt',
        [AntragStatus.Archiviert]: 'Archiviert'
      }
      
      // Success toast
      toast.success(
        `${variables.ids.length} Anträge wurden auf "${statusNames[variables.status]}" gesetzt.`,
        { duration: 4000 }
      )
    },
    onError: (error) => {
      console.error('Failed to bulk update application statuses:', error)
      toast.error('Fehler beim Massenupdate der Anträge. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

// ============================
// UTILITY HOOKS
// ============================

/**
 * Hook to prefetch an application
 * @param id - Application ID to prefetch
 */
export function usePrefetchAntrag() {
  const queryClient = useQueryClient()

  return React.useCallback(
    (id: string) => {
      queryClient.prefetchQuery({
        queryKey: queryKeys.antraege.detail(id),
        queryFn: async () => {
          const response = await apiClient.get<AntragDto>(`/antraege/${id}`)
          if (!response.success || !response.data) {
            throw new Error('Application not found')
          }
          return response.data
        },
        staleTime: STALE_TIMES.DETAILS,
      })
    },
    [queryClient]
  )
}

/**
 * Hook to get cached application data without triggering a fetch
 * @param id - Application ID
 */
export function useCachedAntrag(id: string | null) {
  const queryClient = useQueryClient()
  
  return React.useMemo(() => {
    if (!id) return null
    return queryClient.getQueryData<AntragDto>(queryKeys.antraege.detail(id))
  }, [queryClient, id])
}

/**
 * Custom hook for optimistic updates
 */
export function useOptimisticAntragUpdate() {
  const queryClient = useQueryClient()

  const updateOptimistically = React.useCallback(
    (id: string, updater: (old: AntragDto) => AntragDto) => {
      queryClient.setQueryData<AntragDto>(
        queryKeys.antraege.detail(id),
        (old) => old ? updater(old) : old
      )
    },
    [queryClient]
  )

  const revertOptimisticUpdate = React.useCallback(
    (id: string) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.detail(id) })
    },
    [queryClient]
  )

  return { updateOptimistically, revertOptimisticUpdate }
}

/**
 * Hook to track application changes in real-time
 * @param enabled - Whether to enable real-time tracking
 */
export function useRealtimeAntraege(enabled: boolean = false) {
  const queryClient = useQueryClient()

  React.useEffect(() => {
    if (!enabled) return

    // Refetch pending applications more frequently when enabled
    const interval = setInterval(() => {
      queryClient.invalidateQueries({ queryKey: queryKeys.antraege.pending() })
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.stats() })
    }, 15000) // Every 15 seconds

    return () => clearInterval(interval)
  }, [enabled, queryClient])
}