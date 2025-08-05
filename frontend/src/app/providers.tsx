'use client'

// Providers component for global app state and configuration
import * as React from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { ThemeProvider } from 'next-themes'
import { Toaster } from 'react-hot-toast'

interface ProvidersProps {
  children: React.ReactNode
}

export function Providers({ children }: ProvidersProps) {
  // Create QueryClient with German error messages and optimized defaults
  const [queryClient] = React.useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            // 5 minutes stale time for better UX
            staleTime: 5 * 60 * 1000,
            // 10 minutes cache time
            gcTime: 10 * 60 * 1000,
            // Retry failed requests 3 times
            retry: 3,
            // Retry delay with exponential backoff
            retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
            // Refetch on window focus for data freshness
            refetchOnWindowFocus: true,
            // Don't refetch on reconnect to avoid spam
            refetchOnReconnect: false,
          },
          mutations: {
            // Retry mutations once
            retry: 1,
            // Retry delay
            retryDelay: 1000,
          },
        },
      })
  )

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider
        attribute="class"
        defaultTheme="system"
        enableSystem
        storageKey="kgv-theme"
        disableTransitionOnChange
      >
        {children}
        
        {/* Toast notifications with German messages */}
        <Toaster
          position="top-right"
          reverseOrder={false}
          gutter={8}
          containerClassName=""
          containerStyle={{}}
          toastOptions={{
            // Styling
            className: '',
            duration: 5000,
            style: {
              background: '#ffffff',
              color: '#1f2937',
              border: '1px solid #e5e7eb',
              borderRadius: '0.5rem',
              fontSize: '14px',
              maxWidth: '400px',
            },
            // Success toast styling
            success: {
              duration: 4000,
              style: {
                background: '#f0fdf4',
                border: '1px solid #bbf7d0',
                color: '#166534',
              },
              iconTheme: {
                primary: '#22c55e',
                secondary: '#f0fdf4',
              },
            },
            // Error toast styling
            error: {
              duration: 6000,
              style: {
                background: '#fef2f2',
                border: '1px solid #fecaca',
                color: '#dc2626',
              },
              iconTheme: {
                primary: '#ef4444',
                secondary: '#fef2f2',
              },
            },
            // Loading toast styling
            loading: {
              duration: Infinity,
              style: {
                background: '#fffbeb',
                border: '1px solid #fde68a',
                color: '#d97706',
              },
            },
          }}
        />
        
        {/* React Query Devtools - only in development */}
        {process.env.NODE_ENV === 'development' && (
          <ReactQueryDevtools
            initialIsOpen={false}
            position="bottom-right"
            buttonPosition="bottom-right"
          />
        )}
      </ThemeProvider>
    </QueryClientProvider>
  )
}