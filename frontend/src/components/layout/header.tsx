'use client'

// Header component with German KGV branding
import * as React from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { UserMenu } from './user-menu'
import { ThemeToggle } from './theme-toggle'
import { 
  HomeIcon, 
  FileTextIcon, 
  UsersIcon, 
  MapIcon, 
  BarChart3Icon,
  MenuIcon,
  BellIcon
} from 'lucide-react'

interface HeaderProps {
  onMenuToggle?: () => void
  isMobileMenuOpen?: boolean
}

const navigation = [
  { name: 'Dashboard', href: '/', icon: HomeIcon, description: 'Übersicht und Statistiken' },
  { name: 'Anträge', href: '/antraege', icon: FileTextIcon, description: 'Anträge verwalten' },
  { name: 'Personen', href: '/personen', icon: UsersIcon, description: 'Personenverwaltung' },
  { name: 'Bezirke', href: '/bezirke', icon: MapIcon, description: 'Bezirksverwaltung' },
  { name: 'Berichte', href: '/berichte', icon: BarChart3Icon, description: 'Berichte und Analysen' },
]

export function Header({ onMenuToggle, isMobileMenuOpen }: HeaderProps) {
  const pathname = usePathname()

  return (
    <header className="sticky top-0 z-50 w-full border-b border-secondary-200 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/60">
      <div className="container flex h-16 max-w-screen-2xl items-center">
        {/* Mobile menu button */}
        <Button
          variant="ghost"
          size="icon"
          className="mr-2 md:hidden"
          onClick={onMenuToggle}
          aria-label={isMobileMenuOpen ? 'Menü schließen' : 'Menü öffnen'}
        >
          <MenuIcon className="h-5 w-5" />
        </Button>

        {/* Logo and title */}
        <div className="mr-8 flex items-center space-x-2">
          <Link href="/" className="flex items-center space-x-2 focus-ring rounded-md p-1">
            <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary-600">
              <span className="text-sm font-bold text-white">KGV</span>
            </div>
            <div className="hidden font-bold sm:inline-block">
              <span className="text-primary-900">Kleingartenverein</span>
              <span className="ml-2 text-sm text-secondary-600">Verwaltung</span>
            </div>
          </Link>
        </div>

        {/* Navigation - Desktop */}
        <nav className="hidden md:flex items-center space-x-6 text-sm font-medium">
          {navigation.map((item) => {
            const isActive = pathname === item.href || (item.href !== '/' && pathname.startsWith(item.href))
            const Icon = item.icon
            
            return (
              <Link
                key={item.name}
                href={item.href}
                className={cn(
                  'flex items-center space-x-2 transition-colors hover:text-primary-600 focus-ring rounded-md px-3 py-2',
                  isActive 
                    ? 'text-primary-600 font-semibold' 
                    : 'text-secondary-700'
                )}
                title={item.description}
              >
                <Icon className="h-4 w-4" />
                <span>{item.name}</span>
              </Link>
            )
          })}
        </nav>

        {/* Right side actions */}
        <div className="ml-auto flex items-center space-x-2">
          {/* Notifications */}
          <Button
            variant="ghost"
            size="icon"
            className="relative"
            aria-label="Benachrichtigungen"
          >
            <BellIcon className="h-4 w-4" />
            <span className="absolute -top-1 -right-1 h-2 w-2 rounded-full bg-error-500" />
          </Button>

          {/* Theme toggle */}
          <ThemeToggle />

          {/* User menu */}
          <UserMenu />
        </div>
      </div>

      {/* Mobile Navigation */}
      {isMobileMenuOpen && (
        <div className="border-t border-secondary-200 bg-white md:hidden">
          <nav className="flex flex-col space-y-1 p-4">
            {navigation.map((item) => {
              const isActive = pathname === item.href || (item.href !== '/' && pathname.startsWith(item.href))
              const Icon = item.icon
              
              return (
                <Link
                  key={item.name}
                  href={item.href}
                  className={cn(
                    'flex items-center space-x-3 rounded-md px-3 py-2 text-sm font-medium transition-colors focus-ring',
                    isActive 
                      ? 'bg-primary-50 text-primary-700 font-semibold' 
                      : 'text-secondary-700 hover:bg-secondary-50'
                  )}
                >
                  <Icon className="h-4 w-4 flex-shrink-0" />
                  <div>
                    <div>{item.name}</div>
                    <div className="text-xs text-secondary-500">{item.description}</div>
                  </div>
                </Link>
              )
            })}
          </nav>
        </div>
      )}
    </header>
  )
}