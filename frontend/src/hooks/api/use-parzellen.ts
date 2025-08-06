// React Query hooks for Parzellen (Plots) API operations
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
  Parzelle,
  ParzelleCreateRequest,
  ParzelleUpdateRequest,
  ParzellenListResponse,
  ParzellenFilter,
  ParzellenStatus,
  ParzellenAssignment,
  ParzellenHistory
} from '@/types/bezirke'
import type { ApiResponse, PaginatedResponse } from '@/types/api'

// ============================
// QUERY HOOKS
// ============================

/**
 * Hook to fetch paginated list of plots with filtering and sorting
 * @param filters - Filter and pagination parameters
 * @param options - React Query options
 */
export function useParzellen(
  filters?: ParzellenFilter,
  options?: Omit<UseQueryOptions<ParzellenListResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.parzellen.list(filters),
    queryFn: async () => {
      const params = new URLSearchParams()
      
      if (filters?.search) params.append('searchTerm', filters.search)
      if (filters?.bezirkId) params.append('bezirkId', filters.bezirkId.toString())
      if (filters?.status?.length) {
        filters.status.forEach(status => params.append('status', status))
      }
      if (filters?.aktiv !== undefined) params.append('isActive', filters.aktiv.toString())
      if (filters?.groesseMin) params.append('groesseMin', filters.groesseMin.toString())
      if (filters?.groesseMax) params.append('groesseMax', filters.groesseMax.toString())
      if (filters?.pachtMin) params.append('pachtMin', filters.pachtMin.toString())
      if (filters?.pachtMax) params.append('pachtMax', filters.pachtMax.toString())
      if (filters?.page) params.append('pageNumber', filters.page.toString())
      if (filters?.limit) params.append('pageSize', filters.limit.toString())
      if (filters?.sortBy) params.append('sortBy', filters.sortBy)
      if (filters?.sortOrder) params.append('sortDirection', filters.sortOrder)

      const response = await apiClient.get<PaginatedResponse<Parzelle>>(
        `/parzellen?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch plots')
      }

      // Transform to expected format
      return {
        parzellen: response.data.data,
        pagination: {
          page: response.data.pageNumber,
          limit: response.data.pageSize,
          total: response.data.totalCount,
          totalPages: response.data.totalPages
        },
        filters
      } as ParzellenListResponse
    },
    staleTime: STALE_TIMES.LISTS,
    gcTime: CACHE_TIMES.LISTS,
    enabled: true,
    ...options
  })
}

/**
 * Hook to fetch plots by district ID
 * @param bezirkId - District ID
 * @param filters - Additional filter parameters
 * @param options - React Query options
 */
export function useParzellenByBezirk(
  bezirkId: number | null,
  filters?: Omit<ParzellenFilter, 'bezirkId'>,
  options?: Omit<UseQueryOptions<ParzellenListResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.parzellen.byBezirk(bezirkId!, { ...filters, bezirkId: bezirkId! }),
    queryFn: async () => {
      const params = new URLSearchParams({
        bezirkId: bezirkId!.toString()
      })
      
      if (filters?.search) params.append('searchTerm', filters.search)
      if (filters?.status?.length) {
        filters.status.forEach(status => params.append('status', status))
      }
      if (filters?.aktiv !== undefined) params.append('isActive', filters.aktiv.toString())
      if (filters?.groesseMin) params.append('groesseMin', filters.groesseMin.toString())
      if (filters?.groesseMax) params.append('groesseMax', filters.groesseMax.toString())
      if (filters?.pachtMin) params.append('pachtMin', filters.pachtMin.toString())
      if (filters?.pachtMax) params.append('pachtMax', filters.pachtMax.toString())
      if (filters?.page) params.append('pageNumber', filters.page.toString())
      if (filters?.limit) params.append('pageSize', filters.limit.toString())
      if (filters?.sortBy) params.append('sortBy', filters.sortBy)
      if (filters?.sortOrder) params.append('sortDirection', filters.sortOrder)

      const response = await apiClient.get<PaginatedResponse<Parzelle>>(
        `/parzellen?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch plots by district')
      }

      return {
        parzellen: response.data.data,
        pagination: {
          page: response.data.pageNumber,
          limit: response.data.pageSize,
          total: response.data.totalCount,
          totalPages: response.data.totalPages
        },
        filters: { ...filters, bezirkId: bezirkId! }
      } as ParzellenListResponse
    },
    staleTime: STALE_TIMES.LISTS,
    gcTime: CACHE_TIMES.LISTS,
    enabled: !!bezirkId,
    ...options
  })
}

/**
 * Hook to fetch available (free) plots
 * @param filters - Filter parameters
 * @param options - React Query options
 */
export function useAvailableParzellen(
  filters?: Omit<ParzellenFilter, 'status'>,
  options?: Omit<UseQueryOptions<ParzellenListResponse>, 'queryKey' | 'queryFn'>
) {
  const availableFilters: ParzellenFilter = {
    ...filters,
    status: [ParzellenStatus.FREI],
    aktiv: true
  }

  return useQuery({
    queryKey: queryKeys.parzellen.available(availableFilters),
    queryFn: async () => {
      const params = new URLSearchParams({
        status: ParzellenStatus.FREI,
        isActive: 'true'
      })
      
      if (filters?.search) params.append('searchTerm', filters.search)
      if (filters?.bezirkId) params.append('bezirkId', filters.bezirkId.toString())
      if (filters?.groesseMin) params.append('groesseMin', filters.groesseMin.toString())
      if (filters?.groesseMax) params.append('groesseMax', filters.groesseMax.toString())
      if (filters?.pachtMin) params.append('pachtMin', filters.pachtMin.toString())
      if (filters?.pachtMax) params.append('pachtMax', filters.pachtMax.toString())
      if (filters?.page) params.append('pageNumber', filters.page.toString())
      if (filters?.limit) params.append('pageSize', filters.limit.toString())
      if (filters?.sortBy) params.append('sortBy', filters.sortBy)
      if (filters?.sortOrder) params.append('sortDirection', filters.sortOrder)

      const response = await apiClient.get<PaginatedResponse<Parzelle>>(
        `/parzellen?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch available plots')
      }

      return {
        parzellen: response.data.data,
        pagination: {
          page: response.data.pageNumber,
          limit: response.data.pageSize,
          total: response.data.totalCount,
          totalPages: response.data.totalPages
        },
        filters: availableFilters
      } as ParzellenListResponse
    },
    staleTime: STALE_TIMES.DYNAMIC, // More frequent updates for availability
    gcTime: CACHE_TIMES.DYNAMIC,
    ...options
  })
}

/**
 * Hook to fetch a single plot by ID
 * @param id - Plot ID
 * @param options - React Query options
 */
export function useParzelle(
  id: string | number | null,
  options?: Omit<UseQueryOptions<Parzelle>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.parzellen.detail(id!),
    queryFn: async () => {
      const response = await apiClient.get<Parzelle>(`/parzellen/${id}`)
      
      if (!response.success || !response.data) {
        throw new Error('Plot not found')
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
 * Hook to fetch plot history
 * @param parzelleId - Plot ID
 * @param options - React Query options
 */
export function useParzelleHistory(
  parzelleId: string | number | null,
  options?: Omit<UseQueryOptions<ParzellenHistory[]>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.parzellen.history(parzelleId!),
    queryFn: async () => {
      const response = await apiClient.get<ParzellenHistory[]>(`/parzellen/${parzelleId}/history`)
      
      if (!response.success) {
        throw new Error('Failed to fetch plot history')
      }
      
      return response.data || []
    },
    staleTime: STALE_TIMES.DETAILS,
    gcTime: CACHE_TIMES.DETAILS,
    enabled: !!parzelleId,
    ...options
  })
}

// ============================
// MUTATION HOOKS
// ============================

/**
 * Hook to create a new plot
 * @param options - React Query mutation options
 */
export function useCreateParzelle(
  options?: UseMutationOptions<Parzelle, Error, ParzelleCreateRequest>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: ParzelleCreateRequest) => {
      const response = await apiClient.post<Parzelle>('/parzellen', data)
      
      if (!response.success || !response.data) {
        throw new Error('Failed to create plot')
      }
      
      return response.data
    },
    onSuccess: (newParzelle, variables) => {
      // Invalidate and refetch plots lists
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.lists() })
      // Invalidate district-specific lists
      queryClient.invalidateQueries({ 
        queryKey: ['kgv', 'parzellen', 'list', 'bezirk', newParzelle.bezirkId] 
      })
      // Invalidate available plots
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.available() })
      // Invalidate bezirk details to update statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(newParzelle.bezirkId) })
      
      // Add to cache
      queryClient.setQueryData(queryKeys.parzellen.detail(newParzelle.id), newParzelle)
      
      // Success toast
      toast.success(`Parzelle "${newParzelle.nummer}" wurde erfolgreich erstellt.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to create plot:', error)
      toast.error('Fehler beim Erstellen der Parzelle. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to update an existing plot
 * @param options - React Query mutation options
 */
export function useUpdateParzelle(
  options?: UseMutationOptions<Parzelle, Error, ParzelleUpdateRequest & { id: number }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, ...data }: ParzelleUpdateRequest & { id: number }) => {
      const response = await apiClient.put<Parzelle>(`/parzellen/${id}`, data)
      
      if (!response.success || !response.data) {
        throw new Error('Failed to update plot')
      }
      
      return response.data
    },
    onSuccess: (updatedParzelle, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.parzellen.detail(updatedParzelle.id), updatedParzelle)
      
      // Invalidate lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.lists() })
      // Invalidate district-specific lists
      queryClient.invalidateQueries({ 
        queryKey: ['kgv', 'parzellen', 'list', 'bezirk', updatedParzelle.bezirkId] 
      })
      // Invalidate available plots if status might have changed
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.available() })
      // Invalidate bezirk details to update statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(updatedParzelle.bezirkId) })
      
      // Success toast
      toast.success(`Parzelle "${updatedParzelle.nummer}" wurde erfolgreich aktualisiert.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to update plot:', error)
      toast.error('Fehler beim Aktualisieren der Parzelle. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to delete a plot
 * @param options - React Query mutation options
 */
export function useDeleteParzelle(
  options?: UseMutationOptions<void, Error, { id: number; force?: boolean }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, force = false }: { id: number; force?: boolean }) => {
      const response = await apiClient.delete<void>(`/parzellen/${id}?force=${force}`)
      
      if (!response.success) {
        throw new Error('Failed to delete plot')
      }
    },
    onSuccess: (_, variables) => {
      // Get the plot data before removal to invalidate related caches
      const plotData = queryClient.getQueryData<Parzelle>(queryKeys.parzellen.detail(variables.id))
      
      // Remove from cache
      queryClient.removeQueries({ queryKey: queryKeys.parzellen.detail(variables.id) })
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.lists() })
      
      // Invalidate district-specific lists if we know the bezirkId
      if (plotData?.bezirkId) {
        queryClient.invalidateQueries({ 
          queryKey: ['kgv', 'parzellen', 'list', 'bezirk', plotData.bezirkId] 
        })
        // Invalidate bezirk details to update statistics
        queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(plotData.bezirkId) })
      }
      
      // Invalidate available plots
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.available() })
      
      // Success toast
      toast.success('Parzelle wurde erfolgreich gelöscht.', {
        duration: 4000
      })
    },
    onError: (error: any) => {
      console.error('Failed to delete plot:', error)
      
      // Handle specific error cases
      if (error?.status === 409) {
        toast.error('Parzelle kann nicht gelöscht werden, da sie aktuell vermietet ist.')
      } else {
        toast.error('Fehler beim Löschen der Parzelle. Bitte versuchen Sie es erneut.')
      }
    },
    ...options
  })
}

/**
 * Hook to assign a plot to a tenant
 * @param options - React Query mutation options
 */
export function useAssignParzelle(
  options?: UseMutationOptions<Parzelle, Error, ParzellenAssignment>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (assignment: ParzellenAssignment) => {
      const response = await apiClient.post<Parzelle>(
        `/parzellen/${assignment.parzelleId}/assign`, 
        assignment
      )
      
      if (!response.success || !response.data) {
        throw new Error('Failed to assign plot')
      }
      
      return response.data
    },
    onSuccess: (updatedParzelle, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.parzellen.detail(updatedParzelle.id), updatedParzelle)
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.lists() })
      // Invalidate available plots (this plot is no longer available)
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.available() })
      // Invalidate district lists
      queryClient.invalidateQueries({ 
        queryKey: ['kgv', 'parzellen', 'list', 'bezirk', updatedParzelle.bezirkId] 
      })
      // Invalidate bezirk details to update statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(updatedParzelle.bezirkId) })
      
      // Success toast
      toast.success(`Parzelle "${updatedParzelle.nummer}" wurde erfolgreich zugewiesen.`, {
        duration: 4000
      })
    },
    onError: (error: any) => {
      console.error('Failed to assign plot:', error)
      
      if (error?.status === 409) {
        toast.error('Parzelle ist bereits vergeben oder nicht verfügbar.')
      } else {
        toast.error('Fehler beim Zuweisen der Parzelle. Bitte versuchen Sie es erneut.')
      }
    },
    ...options
  })
}

/**
 * Hook to unassign a plot (make it available)
 * @param options - React Query mutation options
 */
export function useUnassignParzelle(
  options?: UseMutationOptions<Parzelle, Error, { parzelleId: number; endDate?: string; reason?: string }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ parzelleId, endDate, reason }) => {
      const response = await apiClient.post<Parzelle>(
        `/parzellen/${parzelleId}/unassign`,
        { endDate, reason }
      )
      
      if (!response.success || !response.data) {
        throw new Error('Failed to unassign plot')
      }
      
      return response.data
    },
    onSuccess: (updatedParzelle, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.parzellen.detail(updatedParzelle.id), updatedParzelle)
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.lists() })
      // Invalidate available plots (this plot is now available)
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.available() })
      // Invalidate district lists
      queryClient.invalidateQueries({ 
        queryKey: ['kgv', 'parzellen', 'list', 'bezirk', updatedParzelle.bezirkId] 
      })
      // Invalidate bezirk details to update statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(updatedParzelle.bezirkId) })
      
      // Success toast
      toast.success(`Parzelle "${updatedParzelle.nummer}" ist jetzt wieder verfügbar.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to unassign plot:', error)
      toast.error('Fehler beim Freigeben der Parzelle. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

// ============================
// UTILITY HOOKS
// ============================

/**
 * Hook to prefetch a plot
 * @param id - Plot ID to prefetch
 */
export function usePrefetchParzelle() {
  const queryClient = useQueryClient()

  return React.useCallback(
    (id: string | number) => {
      queryClient.prefetchQuery({
        queryKey: queryKeys.parzellen.detail(id),
        queryFn: async () => {
          const response = await apiClient.get<Parzelle>(`/parzellen/${id}`)
          if (!response.success || !response.data) {
            throw new Error('Plot not found')
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
 * Hook to get cached plot data without triggering a fetch
 * @param id - Plot ID
 */
export function useCachedParzelle(id: string | number | null) {
  const queryClient = useQueryClient()
  
  return React.useMemo(() => {
    if (!id) return null
    return queryClient.getQueryData<Parzelle>(queryKeys.parzellen.detail(id))
  }, [queryClient, id])
}

/**
 * Custom hook for optimistic updates
 */
export function useOptimisticParzelleUpdate() {
  const queryClient = useQueryClient()

  const updateOptimistically = React.useCallback(
    (id: number, updater: (old: Parzelle) => Parzelle) => {
      queryClient.setQueryData<Parzelle>(
        queryKeys.parzellen.detail(id),
        (old) => old ? updater(old) : old
      )
    },
    [queryClient]
  )

  const revertOptimisticUpdate = React.useCallback(
    (id: number) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.parzellen.detail(id) })
    },
    [queryClient]
  )

  return { updateOptimistically, revertOptimisticUpdate }
}