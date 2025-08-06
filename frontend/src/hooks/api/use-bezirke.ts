// React Query hooks for Bezirke (Districts) API operations
'use client'

import { 
  useQuery, 
  useMutation, 
  useQueryClient, 
  UseQueryOptions,
  UseMutationOptions,
  QueryFunctionContext
} from '@tanstack/react-query'
import { apiClient } from '@/lib/api-client'
import { queryKeys, STALE_TIMES, CACHE_TIMES } from '@/lib/query-keys'
import { toast } from 'react-hot-toast'
import type {
  Bezirk,
  BezirkCreateRequest,
  BezirkUpdateRequest,
  BezirkeListResponse,
  BezirkeFilter,
  BezirkStatistiken,
  GesamtStatistiken
} from '@/types/bezirke'
import type { ApiResponse, PaginatedResponse } from '@/types/api'

// ============================
// QUERY HOOKS
// ============================

/**
 * Hook to fetch paginated list of districts with filtering and sorting
 * @param filters - Filter and pagination parameters
 * @param options - React Query options
 */
export function useBezirke(
  filters?: BezirkeFilter,
  options?: Omit<UseQueryOptions<BezirkeListResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.bezirke.list(filters),
    queryFn: async () => {
      const params = new URLSearchParams()
      
      if (filters?.search) params.append('search', filters.search)
      if (filters?.aktiv !== undefined) params.append('aktiv', filters.aktiv.toString())
      if (filters?.page) params.append('page', filters.page.toString())
      if (filters?.limit) params.append('limit', filters.limit.toString())
      if (filters?.sortBy) params.append('sortBy', filters.sortBy)
      if (filters?.sortOrder) params.append('sortOrder', filters.sortOrder)

      const response = await apiClient.get<Bezirk[]>(
        `/bezirke?${params.toString()}`
      )

      if (!response.success || !response.data) {
        throw new Error('Failed to fetch districts')
      }

      // Backend returns simple array, create mock pagination
      const bezirke = response.data
      return {
        bezirke,
        pagination: {
          page: filters?.page || 1,
          limit: filters?.limit || 20,
          total: bezirke.length,
          totalPages: 1
        },
        filters
      } as BezirkeListResponse
    },
    staleTime: STALE_TIMES.LISTS,
    gcTime: CACHE_TIMES.LISTS,
    enabled: true,
    ...options
  })
}

/**
 * Hook to fetch a single district by ID
 * @param id - District ID
 * @param options - React Query options
 */
export function useBezirk(
  id: string | number | null,
  options?: Omit<UseQueryOptions<Bezirk>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.bezirke.detail(id!),
    queryFn: async () => {
      const response = await apiClient.get<Bezirk>(`/bezirke/${id}`)
      
      if (!response.success || !response.data) {
        throw new Error('District not found')
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
 * Hook to search districts by name or description
 * @param query - Search query
 * @param options - React Query options
 */
export function useBezirkeSearch(
  query: string,
  options?: Omit<UseQueryOptions<Bezirk[]>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.bezirke.search(query),
    queryFn: async () => {
      if (!query.trim()) return []
      
      const params = new URLSearchParams({
        query: query.trim(),
        limit: '20',
        activeOnly: 'true'
      })

      const response = await apiClient.get<Bezirk[]>(
        `/bezirke/search?${params.toString()}`
      )
      
      if (!response.success) {
        throw new Error('Search failed')
      }
      
      return response.data || []
    },
    staleTime: STALE_TIMES.DYNAMIC,
    gcTime: CACHE_TIMES.DYNAMIC,
    enabled: query.length >= 2, // Only search with 2+ characters
    ...options
  })
}

/**
 * Hook to fetch district statistics
 * @param options - React Query options
 */
export function useBezirkeStatistics(
  options?: Omit<UseQueryOptions<BezirkStatistiken[]>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.bezirke.statistics(),
    queryFn: async () => {
      const response = await apiClient.get<BezirkStatistiken[]>('/bezirke/statistics')
      
      if (!response.success || !response.data) {
        throw new Error('Failed to fetch statistics')
      }
      
      return response.data
    },
    staleTime: STALE_TIMES.STATIC,
    gcTime: CACHE_TIMES.STATIC,
    ...options
  })
}

/**
 * Hook to fetch districts for dropdown/selector (cached, active only)
 * @param options - React Query options
 */
export function useBezirkeDropdown(
  options?: Omit<UseQueryOptions<Array<Pick<Bezirk, 'id' | 'name'>>>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.bezirke.dropdown(),
    queryFn: async () => {
      const response = await apiClient.get<Bezirk[]>(
        `/bezirke?aktiv=true&limit=1000&sortBy=name&sortOrder=asc`
      )
      
      if (!response.success || !response.data) {
        throw new Error('Failed to fetch districts for dropdown')
      }
      
      // Return only id and name for dropdown
      return response.data.map(bezirk => ({
        id: bezirk.id,
        name: bezirk.name
      }))
    },
    staleTime: STALE_TIMES.STATIC,
    gcTime: CACHE_TIMES.STATIC,
    ...options
  })
}

// ============================
// MUTATION HOOKS
// ============================

/**
 * Hook to create a new district
 * @param options - React Query mutation options
 */
export function useCreateBezirk(
  options?: UseMutationOptions<Bezirk, Error, BezirkCreateRequest>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: BezirkCreateRequest) => {
      console.log('useCreateBezirk mutationFn called with data:', data)
      
      // Transform frontend data to backend format
      const backendData = {
        name: data.name,
        description: data.beschreibung || null,
        displayName: data.displayName || null,
        sortOrder: data.sortOrder || 0,
        flaeche: data.flaeche || null
      }
      
      console.log('Transformed backend data:', backendData)
      
      const response = await apiClient.post<Bezirk>('/bezirke', backendData)
      console.log('API response:', response)
      
      if (!response.success || !response.data) {
        console.error('API response failed:', response)
        throw new Error('Failed to create district')
      }
      
      console.log('Successfully created bezirk:', response.data)
      return response.data
    },
    onSuccess: (newBezirk, variables) => {
      console.log('useCreateBezirk onSuccess called with:', { newBezirk, variables })
      
      // Invalidate and refetch districts list
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.lists() })
      // Invalidate statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.statistics() })
      // Invalidate dropdown
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.dropdown() })
      
      // Add to cache
      queryClient.setQueryData(queryKeys.bezirke.detail(newBezirk.id), newBezirk)
      
      // Success toast
      toast.success(`Bezirk "${newBezirk.name}" wurde erfolgreich erstellt.`, {
        duration: 4000
      })
      
      console.log('useCreateBezirk onSuccess completed successfully')
    },
    onError: (error) => {
      console.error('useCreateBezirk onError called with:', error)
      toast.error('Fehler beim Erstellen des Bezirks. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to update an existing district
 * @param options - React Query mutation options
 */
export function useUpdateBezirk(
  options?: UseMutationOptions<Bezirk, Error, BezirkUpdateRequest & { id: string }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, ...data }: BezirkUpdateRequest & { id: string }) => {
      const response = await apiClient.put<Bezirk>(`/bezirke/${id}`, data)
      
      if (!response.success || !response.data) {
        throw new Error('Failed to update district')
      }
      
      return response.data
    },
    onSuccess: (updatedBezirk, variables) => {
      // Update cache
      queryClient.setQueryData(queryKeys.bezirke.detail(updatedBezirk.id), updatedBezirk)
      
      // Invalidate lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.lists() })
      // Invalidate statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.statistics() })
      // Invalidate dropdown
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.dropdown() })
      
      // Success toast
      toast.success(`Bezirk "${updatedBezirk.name}" wurde erfolgreich aktualisiert.`, {
        duration: 4000
      })
    },
    onError: (error) => {
      console.error('Failed to update district:', error)
      toast.error('Fehler beim Aktualisieren des Bezirks. Bitte versuchen Sie es erneut.')
    },
    ...options
  })
}

/**
 * Hook to delete a district
 * @param options - React Query mutation options
 */
export function useDeleteBezirk(
  options?: UseMutationOptions<void, Error, { id: string; force?: boolean }>
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, force = false }: { id: string; force?: boolean }) => {
      const response = await apiClient.delete<void>(`/bezirke/${id}?force=${force}`)
      
      if (!response.success) {
        throw new Error('Failed to delete district')
      }
    },
    onSuccess: (_, variables) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: queryKeys.bezirke.detail(variables.id) })
      
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.lists() })
      // Invalidate statistics
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.statistics() })
      // Invalidate dropdown
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.dropdown() })
      
      // Success toast
      toast.success('Bezirk wurde erfolgreich gelöscht.', {
        duration: 4000
      })
    },
    onError: (error: any) => {
      console.error('Failed to delete district:', error)
      
      // Handle specific error cases
      if (error?.status === 409) {
        toast.error('Bezirk kann nicht gelöscht werden, da er noch Parzellen oder Anträge enthält.')
      } else {
        toast.error('Fehler beim Löschen des Bezirks. Bitte versuchen Sie es erneut.')
      }
    },
    ...options
  })
}

// ============================
// UTILITY HOOKS
// ============================

/**
 * Hook to prefetch a district
 * @param id - District ID to prefetch
 */
export function usePrefetchBezirk() {
  const queryClient = useQueryClient()

  return React.useCallback(
    (id: string | number) => {
      queryClient.prefetchQuery({
        queryKey: queryKeys.bezirke.detail(id),
        queryFn: async () => {
          const response = await apiClient.get<Bezirk>(`/bezirke/${id}`)
          if (!response.success || !response.data) {
            throw new Error('District not found')
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
 * Hook to get cached district data without triggering a fetch
 * @param id - District ID
 */
export function useCachedBezirk(id: string | number | null) {
  const queryClient = useQueryClient()
  
  return React.useMemo(() => {
    if (!id) return null
    return queryClient.getQueryData<Bezirk>(queryKeys.bezirke.detail(id))
  }, [queryClient, id])
}

/**
 * Custom hook to handle optimistic updates for district operations
 */
export function useOptimisticBezirkUpdate() {
  const queryClient = useQueryClient()

  const updateOptimistically = React.useCallback(
    (id: string, updater: (old: Bezirk) => Bezirk) => {
      queryClient.setQueryData<Bezirk>(
        queryKeys.bezirke.detail(id),
        (old) => old ? updater(old) : old
      )
    },
    [queryClient]
  )

  const revertOptimisticUpdate = React.useCallback(
    (id: string) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.detail(id) })
    },
    [queryClient]
  )

  return { updateOptimistically, revertOptimisticUpdate }
}

import React from 'react'