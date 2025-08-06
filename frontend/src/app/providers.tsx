'use client'

// Providers component for global app state and configuration
import * as React from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { ThemeProvider } from 'next-themes'
import { Toaster } from 'react-hot-toast'
import { createAppQueryClient } from '@/lib/react-query-config'

interface ProvidersProps {
  children: React.ReactNode
}

export function Providers({ children }: ProvidersProps) {
  // Create QueryClient with comprehensive error handling and German localization
  const [queryClient] = React.useState(() => createAppQueryClient())

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
            toggleButtonProps={{
              style: {
                marginLeft: 'auto',
                transform: 'scale(0.8)',
                transformOrigin: 'bottom right',
                zIndex: 99999
              },
            }}
            panelProps={{
              style: {
                zIndex: 99998
              }
            }}
          />
        )}
      </ThemeProvider>
    </QueryClientProvider>
  )
}