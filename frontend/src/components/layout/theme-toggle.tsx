'use client'

// Theme toggle component for dark mode support
import * as React from 'react'
import { useTheme } from 'next-themes'
import { Button } from '@/components/ui/button'
import { Sun, Moon, Monitor } from 'lucide-react'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { cn } from '@/lib/utils'

export function ThemeToggle() {
  const { theme, setTheme } = useTheme()
  const [mounted, setMounted] = React.useState(false)

  // Avoid hydration mismatch
  React.useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return (
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8"
        aria-label="Theme laden..."
      >
        <div className="h-4 w-4 animate-pulse rounded bg-secondary-300" />
      </Button>
    )
  }

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          aria-label="Theme wechseln"
        >
          {theme === 'light' && <Sun className="h-4 w-4" />}
          {theme === 'dark' && <Moon className="h-4 w-4" />}
          {theme === 'system' && <Monitor className="h-4 w-4" />}
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          className={cn(
            'z-50 min-w-[160px] overflow-hidden rounded-md border border-secondary-200 bg-white p-1 shadow-lg',
            'data-[state=open]:animate-in data-[state=closed]:animate-out',
            'data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            'data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
            'data-[side=bottom]:slide-in-from-top-2'
          )}
          align="end"
          sideOffset={4}
        >
          <DropdownMenu.Item
            className={cn(
              'flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer focus:outline-none',
              theme === 'light' 
                ? 'bg-primary-50 text-primary-700' 
                : 'hover:bg-secondary-50 focus:bg-secondary-50'
            )}
            onSelect={() => setTheme('light')}
          >
            <Sun className="h-4 w-4" />
            <span>Hell</span>
            {theme === 'light' && (
              <div className="ml-auto h-2 w-2 rounded-full bg-primary-600" />
            )}
          </DropdownMenu.Item>

          <DropdownMenu.Item
            className={cn(
              'flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer focus:outline-none',
              theme === 'dark' 
                ? 'bg-primary-50 text-primary-700' 
                : 'hover:bg-secondary-50 focus:bg-secondary-50'
            )}
            onSelect={() => setTheme('dark')}
          >
            <Moon className="h-4 w-4" />
            <span>Dunkel</span>
            {theme === 'dark' && (
              <div className="ml-auto h-2 w-2 rounded-full bg-primary-600" />
            )}
          </DropdownMenu.Item>

          <DropdownMenu.Item
            className={cn(
              'flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer focus:outline-none',
              theme === 'system' 
                ? 'bg-primary-50 text-primary-700' 
                : 'hover:bg-secondary-50 focus:bg-secondary-50'
            )}
            onSelect={() => setTheme('system')}
          >
            <Monitor className="h-4 w-4" />
            <span>System</span>
            {theme === 'system' && (
              <div className="ml-auto h-2 w-2 rounded-full bg-primary-600" />
            )}
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  )
}