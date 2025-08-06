# KGV API Integration with React Query

This directory contains comprehensive React Query hooks for integrating with the KGV (Kleingartenverein) .NET 9 Web API backend. The implementation follows React Query best practices with German localization, robust error handling, and TypeScript safety.

## 🚀 Quick Start

```typescript
import { useBezirke, useCreateBezirk, useParzellen } from '@/hooks/api'

function MyComponent() {
  // Fetch districts with filtering
  const { data: bezirke, isLoading } = useBezirke({
    aktiv: true,
    page: 1,
    limit: 10
  })
  
  // Create new district
  const createBezirk = useCreateBezirk({
    onSuccess: (newBezirk) => {
      console.log('Created:', newBezirk)
    }
  })
  
  // Fetch plots by district
  const { data: parzellen } = useParzellenByBezirk(selectedBezirkId)
  
  return (
    <div>
      {/* Your component JSX */}
    </div>
  )
}
```

## 📚 API Hooks Overview

### Bezirke (Districts) Hooks

| Hook | Purpose | Parameters |
|------|---------|------------|
| `useBezirke` | Fetch paginated districts list | `BezirkeFilter?`, `options?` |
| `useBezirk` | Fetch single district by ID | `id`, `options?` |
| `useBezirkeSearch` | Search districts | `query`, `options?` |
| `useBezirkeStatistics` | Fetch district statistics | `options?` |
| `useBezirkeDropdown` | Fetch dropdown data | `options?` |
| `useCreateBezirk` | Create new district | `options?` |
| `useUpdateBezirk` | Update existing district | `options?` |
| `useDeleteBezirk` | Delete district | `options?` |

### Parzellen (Plots) Hooks

| Hook | Purpose | Parameters |
|------|---------|------------|
| `useParzellen` | Fetch paginated plots list | `ParzellenFilter?`, `options?` |
| `useParzelle` | Fetch single plot by ID | `id`, `options?` |
| `useParzellenByBezirk` | Fetch plots by district | `bezirkId`, `filters?`, `options?` |
| `useAvailableParzellen` | Fetch available plots | `filters?`, `options?` |
| `useParzelleHistory` | Fetch plot history | `parzelleId`, `options?` |
| `useCreateParzelle` | Create new plot | `options?` |
| `useUpdateParzelle` | Update existing plot | `options?` |
| `useDeleteParzelle` | Delete plot | `options?` |
| `useAssignParzelle` | Assign plot to tenant | `options?` |
| `useUnassignParzelle` | Unassign plot | `options?` |

### Anträge (Applications) Hooks

| Hook | Purpose | Parameters |
|------|---------|------------|
| `useAntraege` | Fetch applications list | `filters?`, `pagination?`, `options?` |
| `useAntrag` | Fetch single application | `id`, `options?` |
| `usePendingAntraege` | Fetch pending applications | `filters?`, `options?` |
| `useAntragHistory` | Fetch application history | `antragId`, `options?` |
| `useAntraegeSearch` | Search applications | `query`, `options?` |
| `useCreateAntrag` | Create new application | `options?` |
| `useUpdateAntrag` | Update application | `options?` |
| `useUpdateAntragStatus` | Update application status | `options?` |
| `useDeleteAntrag` | Delete application | `options?` |
| `useBulkUpdateAntragStatus` | Bulk update statuses | `options?` |

### Dashboard & Utility Hooks

| Hook | Purpose | Parameters |
|------|---------|------------|
| `useDashboardStats` | Fetch dashboard statistics | `options?` |
| `useRealtimeAntraege` | Enable real-time updates | `enabled?` |

## 🎯 Key Features

### 1. **German Localization**
All error messages and user notifications are in German for better UX:

```typescript
// Automatic German error messages
const { error } = useBezirke()
// Error will show: "Netzwerkfehler. Bitte überprüfen Sie Ihre Internetverbindung."
```

### 2. **Comprehensive Error Handling**
- Automatic toast notifications for errors
- Different error severities (low, medium, high, critical)
- Network error detection and retry logic
- Form validation error mapping

### 3. **Optimistic Updates**
Mutations include optimistic updates for better UX:

```typescript
const updateBezirk = useUpdateBezirk({
  onMutate: async (variables) => {
    // Optimistically update UI immediately
    queryClient.setQueryData(queryKey, updatedData)
  },
  onError: (error, variables, context) => {
    // Revert on error
    queryClient.setQueryData(queryKey, context.previousData)
  }
})
```

### 4. **Intelligent Caching**
- Different cache times for different data types
- Automatic cache invalidation on mutations
- Background refetching for real-time data

### 5. **TypeScript Safety**
Full TypeScript support with proper type inference:

```typescript
const { data } = useBezirke() // data is properly typed as BezirkeListResponse
const { mutate } = useCreateBezirk() // mutate expects BezirkCreateRequest
```

## 🛠️ Advanced Usage

### Custom Query Options
All hooks accept React Query options for customization:

```typescript
const { data } = useBezirke(filters, {
  staleTime: 10 * 60 * 1000, // 10 minutes
  refetchInterval: 30000,     // Refetch every 30 seconds
  enabled: shouldFetch,       // Conditional fetching
  onSuccess: (data) => {      // Success callback
    console.log('Data loaded:', data)
  }
})
```

### Dependent Queries
Use the `enabled` option for dependent queries:

```typescript
const { data: bezirk } = useBezirk(selectedId, {
  enabled: !!selectedId // Only fetch when ID is available
})

const { data: parzellen } = useParzellenByBezirk(selectedId, undefined, {
  enabled: !!selectedId && !!bezirk // Wait for both ID and bezirk
})
```

### Real-time Updates
Enable real-time updates for dynamic data:

```typescript
// Automatic refetching every 30 seconds
const { data: pendingAntraege } = usePendingAntraege(undefined, {
  refetchInterval: 30000,
  refetchIntervalInBackground: true
})

// Use the real-time hook for automatic updates
useRealtimeAntraege(true) // Enables real-time updates
```

### Pagination
Handle pagination with proper state management:

```typescript
const [filters, setFilters] = useState<BezirkeFilter>({
  page: 1,
  limit: 20
})

const { data, isLoading } = useBezirke(filters)

const handlePageChange = (newPage: number) => {
  setFilters(prev => ({ ...prev, page: newPage }))
}
```

### Search and Filtering
Implement search with debouncing:

```typescript
const [searchQuery, setSearchQuery] = useState('')
const [debouncedQuery, setDebouncedQuery] = useState('')

// Debounce search query
useEffect(() => {
  const timer = setTimeout(() => {
    setDebouncedQuery(searchQuery)
  }, 300)
  
  return () => clearTimeout(timer)
}, [searchQuery])

// Use debounced query for search
const { data: searchResults } = useBezirkeSearch(debouncedQuery, {
  enabled: debouncedQuery.length >= 2
})
```

## 🔧 Configuration

### Environment Variables
Configure the API client in your environment:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

### Query Client Setup
The query client is automatically configured in `providers.tsx`:

```typescript
// providers.tsx
import { createAppQueryClient } from '@/lib/react-query-config'

const queryClient = createAppQueryClient() // Automatically configured
```

### Custom Configuration
Override default settings if needed:

```typescript
import { configureQueryClient } from '@/lib/react-query-config'

const customClient = configureQueryClient('development')
```

## 🐛 Error Handling

### Automatic Error Display
Errors are automatically displayed as toast notifications with German messages:

```typescript
// No manual error handling needed - automatic toast notifications
const { mutate } = useCreateBezirk()

mutate(newBezirkData) // Errors shown automatically
```

### Custom Error Handling
Override default error handling when needed:

```typescript
const createBezirk = useCreateBezirk({
  onError: (error) => {
    // Custom error handling
    console.error('Custom error handler:', error)
    // Note: Default toast is still shown unless explicitly disabled
  }
})
```

### Form Error Handling
Get form-specific error messages:

```typescript
import { createFormErrorMessage } from '@/lib/error-handling'

const handleSubmit = async (data) => {
  try {
    await createBezirk.mutateAsync(data)
  } catch (error) {
    const formErrors = createFormErrorMessage(error)
    setFieldErrors(formErrors.fields)
    setGeneralError(formErrors.general)
  }
}
```

## 📊 Performance

### Cache Management
The system automatically manages cache with intelligent invalidation:

```typescript
// Creating a bezirk automatically invalidates:
// - Bezirke lists
// - Statistics
// - Dropdown data
const createBezirk = useCreateBezirk() // Automatic cache management
```

### Prefetching
Prefetch data for better UX:

```typescript
const prefetchBezirk = usePrefetchBezirk()

const handleMouseEnter = (id: number) => {
  prefetchBezirk(id) // Prefetch on hover for instant loading
}
```

### Optimistic Updates
Built-in optimistic updates for mutations:

```typescript
// Updates UI immediately, reverts on error
const updateBezirk = useUpdateBezirk() // Automatic optimistic updates
```

## 🧪 Testing

### Mock API Responses
For testing, mock the API client:

```typescript
import { apiClient } from '@/lib/api-client'

// Mock in tests
jest.mock('@/lib/api-client', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn()
  }
}))
```

### Test Utilities
Use React Query testing utilities:

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook } from '@testing-library/react'

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  })
  
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}

test('useBezirke hook', async () => {
  const { result } = renderHook(() => useBezirke(), {
    wrapper: createWrapper()
  })
  
  // Test hook behavior
})
```

## 🔍 Debugging

### React Query Devtools
The devtools are automatically enabled in development:

- Open your app in development mode
- Look for the React Query devtools icon in the bottom-right corner
- Inspect queries, mutations, and cache state

### Error Logging
All errors are logged with context:

```typescript
import { errorHandler } from '@/lib/error-handling'

// Get recent errors for debugging
const recentErrors = errorHandler.getRecentErrors(10)

// Get error statistics
const stats = errorHandler.getErrorStats()
```

### Performance Monitoring
Monitor query performance in development:

```typescript
import { queryPerformance } from '@/lib/react-query-config'

// Log performance metrics
queryPerformance.logMetrics(queryClient)

// Get slow queries
const slowQueries = queryPerformance.getSlowQueries(queryClient, 1000)
```

## 📝 Best Practices

### 1. **Use Proper Keys**
Always use the provided query keys:

```typescript
import { queryKeys } from '@/lib/query-keys'

// Good
queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.lists() })

// Avoid hardcoded keys
queryClient.invalidateQueries({ queryKey: ['bezirke'] })
```

### 2. **Handle Loading States**
Always handle loading and error states:

```typescript
const { data, isLoading, error } = useBezirke()

if (isLoading) return <LoadingSpinner />
if (error) return <ErrorMessage error={error} />
if (!data) return <EmptyState />

return <DataDisplay data={data} />
```

### 3. **Use Filters Properly**
Use filters for better caching and performance:

```typescript
// Good - specific filters for better caching
const { data } = useBezirke({ aktiv: true, sortBy: 'name' })

// Less optimal - generic queries without filters
const { data } = useBezirke()
```

### 4. **Implement Optimistic Updates**
Use optimistic updates for better UX:

```typescript
const updateBezirk = useUpdateBezirk({
  onMutate: async (variables) => {
    // Cancel outgoing refetches
    await queryClient.cancelQueries({ queryKey: queryKeys.bezirke.detail(variables.id) })
    
    // Snapshot previous value
    const previousBezirk = queryClient.getQueryData(queryKeys.bezirke.detail(variables.id))
    
    // Optimistically update
    queryClient.setQueryData(queryKeys.bezirke.detail(variables.id), variables)
    
    return { previousBezirk }
  },
  onError: (err, variables, context) => {
    // Rollback on error
    if (context?.previousBezirk) {
      queryClient.setQueryData(queryKeys.bezirke.detail(variables.id), context.previousBezirk)
    }
  }
})
```

### 5. **Manage Memory**
Clean up when components unmount:

```typescript
useEffect(() => {
  return () => {
    // Cleanup if needed (usually automatic)
    queryClient.removeQueries({ queryKey: queryKeys.bezirke.detail(id) })
  }
}, [])
```

## 🚦 Status Codes

The API integration handles all standard HTTP status codes with German messages:

- **200-299**: Success (automatic handling)
- **400**: Bad Request - "Ungültige Anfrage"
- **401**: Unauthorized - "Sie sind nicht angemeldet"
- **403**: Forbidden - "Unzureichende Berechtigung"
- **404**: Not Found - "Ressource nicht gefunden"
- **409**: Conflict - "Konflikt bei der Verarbeitung"
- **422**: Validation Error - "Validierungsfehler"
- **429**: Rate Limited - "Zu viele Anfragen"
- **500+**: Server Errors - "Serverfehler"

## 📋 Migration Guide

### From Direct fetch() calls:

```typescript
// Before
const [data, setData] = useState(null)
const [loading, setLoading] = useState(false)

useEffect(() => {
  setLoading(true)
  fetch('/api/bezirke')
    .then(res => res.json())
    .then(setData)
    .catch(console.error)
    .finally(() => setLoading(false))
}, [])

// After
const { data, isLoading } = useBezirke()
```

### From axios directly:

```typescript
// Before
const [bezirke, setBezirke] = useState([])

const fetchBezirke = async () => {
  try {
    const response = await axios.get('/api/bezirke')
    setBezirke(response.data)
  } catch (error) {
    toast.error('Error loading bezirke')
  }
}

// After
const { data: bezirke } = useBezirke() // Error handling automatic
```

## 🤝 Contributing

When adding new API hooks:

1. Follow the existing patterns
2. Add proper TypeScript types
3. Include error handling
4. Add to the main index file
5. Update this documentation
6. Add tests if possible

## 📖 Additional Resources

- [React Query Documentation](https://tanstack.com/query/latest)
- [KGV API Documentation](../../../src/KGV.API/README.md)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Next.js Documentation](https://nextjs.org/docs)