// React Query configuration with enhanced error handling and German localization
import { QueryClient, MutationCache, QueryCache } from '@tanstack/react-query'
import { errorHandler, shouldRetryError, isApiError } from './error-handling'
import { STALE_TIMES, CACHE_TIMES } from './query-keys'
import type { ApiError } from '@/types/api'

/**
 * Create a properly configured QueryClient for the KGV application
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Default stale time for lists
        staleTime: STALE_TIMES.LISTS,
        // Default cache time for lists
        gcTime: CACHE_TIMES.LISTS,
        
        // Retry configuration with intelligent error handling
        retry: (failureCount, error: any) => {
          // Don't retry on client errors except rate limiting
          if (isApiError(error) && !shouldRetryError(error)) {
            return false
          }
          
          // Max 3 retries for network/server errors
          return failureCount < 3
        },
        
        // Retry delay with exponential backoff (max 30 seconds)
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
        
        // Refetch configuration
        refetchOnWindowFocus: true,
        refetchOnReconnect: 'always',
        refetchInterval: false, // Disabled by default, enabled per query as needed
        
        // Network mode - online first, then cache
        networkMode: 'online',
        
        // Error handling
        throwOnError: false,
        
        // Use error boundary for critical server errors
        useErrorBoundary: (error: any) => {
          return isApiError(error) && error.status >= 500
        },
        
        // Meta information for error context
        meta: {
          errorContext: 'query'
        }
      },
      
      mutations: {
        // Retry configuration for mutations
        retry: (failureCount, error: any) => {
          // Don't retry client errors (4xx)
          if (isApiError(error) && error.status >= 400 && error.status < 500) {
            return false
          }
          
          // Retry once for server errors or network issues
          return failureCount < 1
        },
        
        // Retry delay for mutations (longer than queries)
        retryDelay: 2000,
        
        // Use error boundary for critical mutation errors
        useErrorBoundary: (error: any) => {
          return isApiError(error) && error.status >= 500
        },
        
        // Network mode for mutations
        networkMode: 'online',
        
        // Meta information
        meta: {
          errorContext: 'mutation'
        }
      }
    },
    
    // Global query cache with error handling
    queryCache: new QueryCache({
      onError: (error: any, query) => {
        const context = `Query: ${JSON.stringify(query.queryKey)}`
        errorHandler.handleQueryError(error, context)
        
        // Log query details in development
        if (process.env.NODE_ENV === 'development') {
          console.group(`❌ Query Error`)
          console.error('Error:', error)
          console.log('Query Key:', query.queryKey)
          console.log('Query State:', query.state)
          console.groupEnd()
        }
      },
      
      onSuccess: (data, query) => {
        // Log successful queries in development
        if (process.env.NODE_ENV === 'development' && query.queryKey[0] !== 'kgv') {
          console.log(`✅ Query Success:`, query.queryKey)
        }
      },
      
      onSettled: (data, error, query) => {
        // Query performance monitoring in development
        if (process.env.NODE_ENV === 'development') {
          const duration = Date.now() - (query.state.dataUpdatedAt || 0)
          if (duration > 1000) { // Log slow queries (>1s)
            console.warn(`🐌 Slow Query (${duration}ms):`, query.queryKey)
          }
        }
      }
    }),
    
    // Global mutation cache with error handling
    mutationCache: new MutationCache({
      onError: (error: any, variables, context, mutation) => {
        const operation = mutation.options.mutationKey || 'unknown'
        errorHandler.handleMutationError(error, operation as string, variables)
        
        // Log mutation details in development
        if (process.env.NODE_ENV === 'development') {
          console.group(`❌ Mutation Error`)
          console.error('Error:', error)
          console.log('Operation:', operation)
          console.log('Variables:', variables)
          console.log('Context:', context)
          console.groupEnd()
        }
      },
      
      onSuccess: (data, variables, context, mutation) => {
        // Log successful mutations in development
        if (process.env.NODE_ENV === 'development') {
          const operation = mutation.options.mutationKey || 'unknown'
          console.log(`✅ Mutation Success:`, operation)
        }
      },
      
      onSettled: (data, error, variables, context, mutation) => {
        // Mutation performance monitoring in development
        if (process.env.NODE_ENV === 'development') {
          const duration = Date.now() - (mutation.state.submittedAt || 0)
          if (duration > 3000) { // Log slow mutations (>3s)
            const operation = mutation.options.mutationKey || 'unknown'
            console.warn(`🐌 Slow Mutation (${duration}ms):`, operation)
          }
        }
      }
    })
  })
}

/**
 * Configure query client for different environments
 */
export function configureQueryClient(environment: 'development' | 'production' | 'test' = 'production'): QueryClient {
  const baseClient = createQueryClient()
  
  switch (environment) {
    case 'development':
      // More aggressive refetching and shorter cache times for development
      baseClient.setDefaultOptions({
        queries: {
          staleTime: STALE_TIMES.DYNAMIC, // Shorter stale time
          gcTime: CACHE_TIMES.DYNAMIC,    // Shorter cache time
          refetchOnWindowFocus: true,
          refetchInterval: false, // Can be enabled per query for testing
        }
      })
      break
      
    case 'test':
      // Disable network requests and retries for testing
      baseClient.setDefaultOptions({
        queries: {
          retry: false,
          staleTime: Infinity,
          gcTime: Infinity,
          refetchOnWindowFocus: false,
          refetchOnReconnect: false,
          networkMode: 'offlineFirst'
        },
        mutations: {
          retry: false,
          networkMode: 'offlineFirst'
        }
      })
      break
      
    case 'production':
      // Optimized settings for production
      baseClient.setDefaultOptions({
        queries: {
          staleTime: STALE_TIMES.LISTS,
          gcTime: CACHE_TIMES.LISTS,
          refetchOnWindowFocus: true,
          refetchOnReconnect: 'always'
        }
      })
      break
  }
  
  return baseClient
}

/**
 * Query client factory with environment detection
 */
export function createAppQueryClient(): QueryClient {
  const environment = process.env.NODE_ENV as 'development' | 'production' | 'test'
  return configureQueryClient(environment)
}

/**
 * Utility to clear all cache data (useful for logout)
 */
export function clearAllCache(queryClient: QueryClient): void {
  queryClient.clear()
  
  // Also clear error log
  errorHandler.clearErrorLog()
}

/**
 * Utility to invalidate authentication-related queries
 */
export function invalidateAuthQueries(queryClient: QueryClient): void {
  queryClient.invalidateQueries({ queryKey: ['kgv', 'auth'] })
  queryClient.invalidateQueries({ queryKey: ['kgv', 'dashboard'] })
}

/**
 * Utility to prefetch critical data on app startup
 */
export async function prefetchCriticalData(queryClient: QueryClient): Promise<void> {
  const prefetchPromises = [
    // Prefetch user data if authenticated
    queryClient.prefetchQuery({
      queryKey: ['kgv', 'auth', 'user'],
      staleTime: STALE_TIMES.STATIC
    }),
    
    // Prefetch dashboard stats
    queryClient.prefetchQuery({
      queryKey: ['kgv', 'dashboard', 'stats'],
      staleTime: STALE_TIMES.DYNAMIC
    }),
    
    // Prefetch dropdown data for forms
    queryClient.prefetchQuery({
      queryKey: ['kgv', 'bezirke', 'dropdown'],
      staleTime: STALE_TIMES.STATIC
    })
  ]
  
  try {
    await Promise.allSettled(prefetchPromises)
  } catch (error) {
    console.warn('Some prefetch operations failed:', error)
  }
}

/**
 * Performance monitoring utilities
 */
export const queryPerformance = {
  /**
   * Log query performance metrics
   */
  logMetrics: (queryClient: QueryClient) => {
    if (process.env.NODE_ENV !== 'development') return
    
    const cache = queryClient.getQueryCache()
    const queries = cache.getAll()
    
    console.group('📊 Query Performance Metrics')
    console.log(`Total Queries: ${queries.length}`)
    console.log(`Active Queries: ${queries.filter(q => q.getObserversCount() > 0).length}`)
    console.log(`Stale Queries: ${queries.filter(q => q.isStale()).length}`)
    console.log(`Error Queries: ${queries.filter(q => q.state.status === 'error').length}`)
    
    // Memory usage estimate
    const memoryUsage = JSON.stringify(cache).length
    console.log(`Estimated Cache Size: ${(memoryUsage / 1024).toFixed(2)} KB`)
    
    console.groupEnd()
  },
  
  /**
   * Get slow queries (queries that took longer than threshold)
   */
  getSlowQueries: (queryClient: QueryClient, thresholdMs: number = 1000) => {
    const cache = queryClient.getQueryCache()
    const queries = cache.getAll()
    
    return queries.filter(query => {
      const duration = (query.state.dataUpdatedAt || 0) - (query.state.fetchFailureTime || 0)
      return duration > thresholdMs
    }).map(query => ({
      queryKey: query.queryKey,
      duration: (query.state.dataUpdatedAt || 0) - (query.state.fetchFailureTime || 0),
      status: query.state.status
    }))
  }
}

/**
 * Dev tools integration
 */
export const devTools = {
  /**
   * Export cache data for debugging
   */
  exportCache: (queryClient: QueryClient) => {
    if (process.env.NODE_ENV !== 'development') return
    
    const cache = queryClient.getQueryCache()
    const data = cache.getAll().reduce((acc, query) => {
      acc[JSON.stringify(query.queryKey)] = {
        data: query.state.data,
        status: query.state.status,
        dataUpdatedAt: query.state.dataUpdatedAt,
        error: query.state.error
      }
      return acc
    }, {} as Record<string, any>)
    
    console.log('Cache Export:', data)
    return data
  },
  
  /**
   * Clear specific query pattern
   */
  clearPattern: (queryClient: QueryClient, pattern: string[]) => {
    if (process.env.NODE_ENV !== 'development') return
    
    queryClient.removeQueries({ queryKey: pattern })
    console.log(`Cleared queries matching:`, pattern)
  }
}

// Export default configured client
export default createAppQueryClient