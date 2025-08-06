# KGV Frontend API Integration Implementation Guide

This guide provides a comprehensive overview of the React Query API integration implementation for the KGV (Kleingartenverein) frontend application.

## 🚀 What's Been Implemented

### 1. **Enhanced API Client with Axios**
- **Location**: `/src/lib/api-client.ts`
- **Features**:
  - Axios-based HTTP client with interceptors
  - Automatic authentication token handling
  - German error messages
  - Timeout and retry logic
  - Request/response logging
  - File upload/download support

### 2. **Comprehensive React Query Configuration**
- **Location**: `/src/lib/react-query-config.ts`
- **Features**:
  - Environment-specific configurations
  - Intelligent retry logic
  - Automatic error handling
  - Performance monitoring
  - Cache optimization

### 3. **Complete API Hooks Suite**
- **Bezirke Hooks**: `/src/hooks/api/use-bezirke.ts`
- **Parzellen Hooks**: `/src/hooks/api/use-parzellen.ts`  
- **Anträge Hooks**: `/src/hooks/api/use-antraege.ts`
- **Index**: `/src/hooks/api/index.ts`

### 4. **Query Keys Factory**
- **Location**: `/src/lib/query-keys.ts`
- **Features**:
  - Centralized query key management
  - Type-safe query keys
  - Cache invalidation helpers
  - Performance constants

### 5. **Advanced Error Handling**
- **Location**: `/src/lib/error-handling.ts`
- **Features**:
  - German error messages
  - Error severity levels
  - Automatic toast notifications
  - Error logging and statistics
  - Form error mapping

### 6. **Updated Provider Configuration**
- **Location**: `/src/app/providers.tsx`
- **Features**:
  - Integrated with new React Query config
  - Enhanced devtools setup
  - Optimized cache settings

### 7. **Example Component**
- **Location**: `/src/components/examples/api-integration-example.tsx`
- **Features**:
  - Comprehensive usage examples
  - Best practices demonstration
  - Real-world scenarios

## 📋 Integration Checklist

### ✅ Completed
- [x] Enhanced API client with axios integration
- [x] React Query configuration with error handling
- [x] Complete API hooks for all entities
- [x] Query keys factory and cache management
- [x] Global error handling with German messages
- [x] Provider integration and setup
- [x] Example component and documentation

### 🔄 Next Steps for Full Integration

#### 1. **Update Existing Components**
Replace existing data fetching in these components:

**Bezirke Components**:
```typescript
// In /src/components/bezirke/bezirke-list.tsx
// Replace useState + useEffect with:
import { useBezirke } from '@/hooks/api'

const { data, isLoading, error } = useBezirke({
  aktiv: true,
  page: currentPage,
  limit: 20
})
```

**Parzellen Components**:
```typescript
// In /src/components/parzellen/parzellen-list.tsx
// Replace manual API calls with:
import { useParzellen, useParzellenByBezirk } from '@/hooks/api'

const { data: parzellen } = useParzellenByBezirk(selectedBezirkId)
```

**Dashboard Components**:
```typescript
// In /src/components/dashboard/dashboard-stats.tsx
// Replace data fetching with:
import { useDashboardStats } from '@/hooks/api'

const { data: stats, isLoading } = useDashboardStats()
```

#### 2. **Form Component Updates**
Update form components to use mutation hooks:

```typescript
// Example: Bezirk creation form
import { useCreateBezirk } from '@/hooks/api'

const createBezirk = useCreateBezirk({
  onSuccess: () => {
    // Form reset and navigation
    reset()
    router.push('/bezirke')
  }
})

const onSubmit = (data) => {
  createBezirk.mutate(data)
}
```

#### 3. **Page Component Updates**
Update page components to use the new hooks:

```typescript
// In app/(dashboard)/bezirke/page.tsx
'use client'

import { useBezirke, useBezirkeStatistics } from '@/hooks/api'

export default function BezirkePage() {
  const { data: bezirke, isLoading } = useBezirke()
  const { data: stats } = useBezirkeStatistics()
  
  // Component logic
}
```

## 🛠️ Migration Examples

### Before: Manual API Calls
```typescript
// Old approach - manual state management
function BezirkeList() {
  const [bezirke, setBezirke] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    const fetchBezirke = async () => {
      setLoading(true)
      try {
        const response = await fetch('/api/bezirke')
        const data = await response.json()
        setBezirke(data)
      } catch (err) {
        setError(err)
        toast.error('Fehler beim Laden der Bezirke')
      } finally {
        setLoading(false)
      }
    }
    
    fetchBezirke()
  }, [])

  if (loading) return <div>Loading...</div>
  if (error) return <div>Error: {error.message}</div>
  
  return (
    <div>
      {bezirke.map(bezirk => (
        <div key={bezirk.id}>{bezirk.name}</div>
      ))}
    </div>
  )
}
```

### After: React Query Hooks
```typescript
// New approach - React Query hooks
import { useBezirke } from '@/hooks/api'

function BezirkeList() {
  const { data: bezirkeData, isLoading, error } = useBezirke({
    aktiv: true,
    sortBy: 'name'
  })

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorMessage error={error} />
  if (!bezirkeData?.bezirke) return <EmptyState />
  
  return (
    <div>
      {bezirkeData.bezirke.map(bezirk => (
        <BezirkCard key={bezirk.id} bezirk={bezirk} />
      ))}
      <Pagination 
        current={bezirkeData.pagination.page}
        total={bezirkeData.pagination.totalPages}
      />
    </div>
  )
}
```

## 🎯 Key Benefits Achieved

### 1. **Automatic Error Handling**
- German error messages throughout
- Automatic toast notifications
- Different error severities
- Form-specific error handling

### 2. **Intelligent Caching**
- Automatic cache invalidation
- Background refetching
- Optimistic updates
- Memory optimization

### 3. **Better Developer Experience**
- Full TypeScript support
- React Query devtools
- Performance monitoring
- Comprehensive documentation

### 4. **Production Ready**
- Error boundaries integration
- Retry logic with exponential backoff
- Network error handling
- Loading and error states

### 5. **Real-time Capabilities**
- Automatic refetching
- Background updates
- Real-time data hooks
- WebSocket ready architecture

## 🔧 Configuration Options

### Environment Configuration
```env
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

### Development vs Production
The system automatically adapts based on `NODE_ENV`:

- **Development**: More logging, shorter cache times, devtools enabled
- **Production**: Optimized caching, error reporting, performance monitoring
- **Test**: Disabled network requests, infinite cache

### Custom Configuration
Override defaults when needed:

```typescript
// Custom hook options
const { data } = useBezirke(filters, {
  staleTime: 10 * 60 * 1000,  // 10 minutes
  refetchInterval: 30000,      // 30 seconds
  enabled: shouldFetch,        // Conditional
})
```

## 📊 Performance Improvements

### Cache Efficiency
- **Lists**: 5-minute stale time, 15-minute cache
- **Details**: 10-minute stale time, 30-minute cache
- **Static Data**: 30-minute stale time, 1-hour cache
- **Real-time**: 30-second stale time, 1-minute cache

### Network Optimizations
- Request deduplication
- Background refetching
- Intelligent retry logic
- Automatic error recovery

### Memory Management
- Automatic garbage collection
- Query result sharing
- Optimized re-renders
- Background cleanup

## 🧪 Testing Strategy

### Unit Tests
Test individual hooks with mocked API responses:

```typescript
import { renderHook } from '@testing-library/react'
import { useBezirke } from '@/hooks/api'

test('useBezirke loads data correctly', async () => {
  // Mock API response
  const mockData = { bezirke: [], pagination: {...} }
  
  const { result } = renderHook(() => useBezirke(), {
    wrapper: QueryClientWrapper
  })
  
  await waitFor(() => {
    expect(result.current.data).toEqual(mockData)
  })
})
```

### Integration Tests
Test complete user flows with real API interactions.

### E2E Tests
Test the full application with Playwright or Cypress.

## 🚨 Important Notes

### Breaking Changes
- Replace all manual `fetch()` calls with hooks
- Update error handling to use new patterns
- Remove manual loading state management
- Update type imports

### Migration Priority
1. **High Priority**: Dashboard, main list components
2. **Medium Priority**: Detail pages, forms
3. **Low Priority**: Utility components, edge cases

### Rollback Plan
The old API client remains available if needed:
- Keep existing components working during migration
- Gradual component-by-component migration
- Easy rollback if issues arise

## 📞 Support and Troubleshooting

### Common Issues

**1. Network Errors**
```typescript
// Check API URL configuration
console.log(process.env.NEXT_PUBLIC_API_URL)

// Verify backend is running
curl http://localhost:5000/api/health
```

**2. Type Errors**
```typescript
// Ensure types are imported correctly
import type { BezirkeFilter } from '@/types/bezirke'

// Check hook usage matches type definitions
const { data } = useBezirke(filters) // filters must match BezirkeFilter
```

**3. Cache Issues**
```typescript
// Clear cache manually if needed
queryClient.clear()

// Or invalidate specific queries
queryClient.invalidateQueries({ queryKey: queryKeys.bezirke.all() })
```

### Debug Tools
- React Query Devtools (development)
- Error handler statistics
- Performance monitoring
- Cache inspection utilities

## 🎉 Conclusion

The KGV frontend now has a production-ready, comprehensive API integration layer that provides:

- **Type Safety**: Full TypeScript support
- **German UX**: Localized error messages
- **Performance**: Intelligent caching and optimization
- **Reliability**: Robust error handling and retry logic
- **Developer Experience**: Great debugging tools and documentation

The implementation follows React Query best practices and is ready for production use. The next step is to gradually migrate existing components to use the new hooks, starting with the most critical user-facing components.

For detailed usage examples, see the [API Hooks Documentation](./src/hooks/api/README.md) and the [Example Component](./src/components/examples/api-integration-example.tsx).