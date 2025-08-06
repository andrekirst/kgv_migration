// Query Keys Factory for React Query Cache Management
// Provides consistent and type-safe query keys across the application

import type { BezirkeFilter, ParzellenFilter } from '@/types/bezirke'
import type { AntragFilter, PaginationParams } from '@/types/api'

/**
 * Centralized query keys factory following React Query best practices
 * Each entity has its own namespace with specific query patterns
 */
export const queryKeys = {
  // Application-wide keys
  all: ['kgv'] as const,
  
  // Authentication & User
  auth: {
    all: () => [...queryKeys.all, 'auth'] as const,
    user: () => [...queryKeys.auth.all(), 'user'] as const,
    profile: () => [...queryKeys.auth.all(), 'profile'] as const,
    permissions: () => [...queryKeys.auth.all(), 'permissions'] as const,
  },

  // Bezirke (Districts)
  bezirke: {
    all: () => [...queryKeys.all, 'bezirke'] as const,
    lists: () => [...queryKeys.bezirke.all(), 'list'] as const,
    list: (filters?: BezirkeFilter) => [...queryKeys.bezirke.lists(), filters] as const,
    details: () => [...queryKeys.bezirke.all(), 'detail'] as const,
    detail: (id: string | number) => [...queryKeys.bezirke.details(), id] as const,
    statistics: () => [...queryKeys.bezirke.all(), 'statistics'] as const,
    search: (query: string) => [...queryKeys.bezirke.all(), 'search', query] as const,
    // Cached lists for dropdowns/selectors
    dropdown: () => [...queryKeys.bezirke.all(), 'dropdown'] as const,
    // Relations
    parzellen: (bezirkId: string | number) => [...queryKeys.bezirke.detail(bezirkId), 'parzellen'] as const,
    antraege: (bezirkId: string | number) => [...queryKeys.bezirke.detail(bezirkId), 'antraege'] as const,
  },

  // Parzellen (Plots)
  parzellen: {
    all: () => [...queryKeys.all, 'parzellen'] as const,
    lists: () => [...queryKeys.parzellen.all(), 'list'] as const,
    list: (filters?: ParzellenFilter) => [...queryKeys.parzellen.lists(), filters] as const,
    details: () => [...queryKeys.parzellen.all(), 'detail'] as const,
    detail: (id: string | number) => [...queryKeys.parzellen.details(), id] as const,
    statistics: () => [...queryKeys.parzellen.all(), 'statistics'] as const,
    search: (query: string) => [...queryKeys.parzellen.all(), 'search', query] as const,
    // Filtered lists
    byBezirk: (bezirkId: string | number, filters?: ParzellenFilter) => 
      [...queryKeys.parzellen.lists(), 'bezirk', bezirkId, filters] as const,
    available: (filters?: ParzellenFilter) => 
      [...queryKeys.parzellen.lists(), 'available', filters] as const,
    assigned: (filters?: ParzellenFilter) => 
      [...queryKeys.parzellen.lists(), 'assigned', filters] as const,
    // Relations
    history: (parzelleId: string | number) => [...queryKeys.parzellen.detail(parzelleId), 'history'] as const,
    assignments: (parzelleId: string | number) => [...queryKeys.parzellen.detail(parzelleId), 'assignments'] as const,
  },

  // Anträge (Applications)
  antraege: {
    all: () => [...queryKeys.all, 'antraege'] as const,
    lists: () => [...queryKeys.antraege.all(), 'list'] as const,
    list: (filters?: AntragFilter, pagination?: PaginationParams) => 
      [...queryKeys.antraege.lists(), filters, pagination] as const,
    details: () => [...queryKeys.antraege.all(), 'detail'] as const,
    detail: (id: string) => [...queryKeys.antraege.details(), id] as const,
    statistics: () => [...queryKeys.antraege.all(), 'statistics'] as const,
    search: (query: string) => [...queryKeys.antraege.all(), 'search', query] as const,
    // Status-based lists
    pending: (filters?: AntragFilter) => [...queryKeys.antraege.lists(), 'pending', filters] as const,
    approved: (filters?: AntragFilter) => [...queryKeys.antraege.lists(), 'approved', filters] as const,
    rejected: (filters?: AntragFilter) => [...queryKeys.antraege.lists(), 'rejected', filters] as const,
    // Relations
    history: (antragId: string) => [...queryKeys.antraege.detail(antragId), 'history'] as const,
    documents: (antragId: string) => [...queryKeys.antraege.detail(antragId), 'documents'] as const,
  },

  // Dashboard & Analytics
  dashboard: {
    all: () => [...queryKeys.all, 'dashboard'] as const,
    stats: () => [...queryKeys.dashboard.all(), 'stats'] as const,
    charts: () => [...queryKeys.dashboard.all(), 'charts'] as const,
    activity: () => [...queryKeys.dashboard.all(), 'activity'] as const,
    trends: (period?: string) => [...queryKeys.dashboard.all(), 'trends', period] as const,
  },

  // System & Configuration
  system: {
    all: () => [...queryKeys.all, 'system'] as const,
    health: () => [...queryKeys.system.all(), 'health'] as const,
    config: () => [...queryKeys.system.all(), 'config'] as const,
    notifications: () => [...queryKeys.system.all(), 'notifications'] as const,
  },

  // Reports & Exports
  reports: {
    all: () => [...queryKeys.all, 'reports'] as const,
    export: (type: string, filters?: any) => [...queryKeys.reports.all(), 'export', type, filters] as const,
    templates: () => [...queryKeys.reports.all(), 'templates'] as const,
  },
} as const

/**
 * Helper functions for common query key operations
 */
export const queryKeyHelpers = {
  /**
   * Invalidate all queries for a specific entity
   */
  invalidateEntity: (queryClient: any, entity: keyof typeof queryKeys) => {
    return queryClient.invalidateQueries({ queryKey: queryKeys[entity].all() })
  },

  /**
   * Remove all queries for a specific entity
   */
  removeEntity: (queryClient: any, entity: keyof typeof queryKeys) => {
    return queryClient.removeQueries({ queryKey: queryKeys[entity].all() })
  },

  /**
   * Prefetch entity list with filters
   */
  prefetchList: async (queryClient: any, entity: string, filters?: any) => {
    switch (entity) {
      case 'bezirke':
        return queryClient.prefetchQuery({
          queryKey: queryKeys.bezirke.list(filters),
          staleTime: 5 * 60 * 1000, // 5 minutes
        })
      case 'parzellen':
        return queryClient.prefetchQuery({
          queryKey: queryKeys.parzellen.list(filters),
          staleTime: 5 * 60 * 1000,
        })
      case 'antraege':
        return queryClient.prefetchQuery({
          queryKey: queryKeys.antraege.list(filters),
          staleTime: 2 * 60 * 1000, // 2 minutes for more dynamic data
        })
    }
  },

  /**
   * Get cached data without triggering a fetch
   */
  getCachedData: <T>(queryClient: any, queryKey: readonly unknown[]): T | undefined => {
    return queryClient.getQueryData<T>(queryKey)
  },

  /**
   * Set cached data
   */
  setCachedData: <T>(queryClient: any, queryKey: readonly unknown[], data: T) => {
    return queryClient.setQueryData<T>(queryKey, data)
  },

  /**
   * Check if query is currently fetching
   */
  isFetching: (queryClient: any, queryKey: readonly unknown[]): boolean => {
    return queryClient.isFetching({ queryKey }) > 0
  },

  /**
   * Reset query error state
   */
  resetQueryError: (queryClient: any, queryKey: readonly unknown[]) => {
    return queryClient.resetQueries({ queryKey })
  },
}

/**
 * Mutation keys for consistent cache invalidation
 */
export const mutationKeys = {
  // Bezirke mutations
  bezirke: {
    create: 'create-bezirk',
    update: 'update-bezirk',
    delete: 'delete-bezirk',
  },
  
  // Parzellen mutations
  parzellen: {
    create: 'create-parzelle',
    update: 'update-parzelle',
    delete: 'delete-parzelle',
    assign: 'assign-parzelle',
    unassign: 'unassign-parzelle',
  },

  // Anträge mutations
  antraege: {
    create: 'create-antrag',
    update: 'update-antrag',
    updateStatus: 'update-antrag-status',
    delete: 'delete-antrag',
  },

  // Authentication mutations
  auth: {
    login: 'login',
    logout: 'logout',
    refresh: 'refresh-token',
    register: 'register',
  },
} as const

/**
 * Query key type helpers for better TypeScript support
 */
export type QueryKey = ReturnType<typeof queryKeys[keyof typeof queryKeys]['all']>
export type BezirkeQueryKey = ReturnType<typeof queryKeys.bezirke[keyof typeof queryKeys.bezirke]>
export type ParzellenQueryKey = ReturnType<typeof queryKeys.parzellen[keyof typeof queryKeys.parzellen]>
export type AntraegeQueryKey = ReturnType<typeof queryKeys.antraege[keyof typeof queryKeys.antraege]>

/**
 * Default stale times for different types of data
 */
export const STALE_TIMES = {
  // Static/configuration data - 30 minutes
  STATIC: 30 * 60 * 1000,
  
  // Entity lists - 5 minutes
  LISTS: 5 * 60 * 1000,
  
  // Entity details - 10 minutes
  DETAILS: 10 * 60 * 1000,
  
  // Dynamic data (applications, activity) - 2 minutes
  DYNAMIC: 2 * 60 * 1000,
  
  // Real-time data (notifications, live stats) - 30 seconds
  REALTIME: 30 * 1000,
} as const

/**
 * Default cache times for different types of data
 */
export const CACHE_TIMES = {
  // Keep static data for 1 hour
  STATIC: 60 * 60 * 1000,
  
  // Keep lists for 15 minutes
  LISTS: 15 * 60 * 1000,
  
  // Keep details for 30 minutes
  DETAILS: 30 * 60 * 1000,
  
  // Keep dynamic data for 5 minutes
  DYNAMIC: 5 * 60 * 1000,
  
  // Keep real-time data for 1 minute
  REALTIME: 60 * 1000,
} as const