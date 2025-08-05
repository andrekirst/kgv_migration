'use client'

// User menu component with authentication
import * as React from 'react'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { 
  UserIcon, 
  SettingsIcon, 
  LogOutIcon, 
  UserCircleIcon,
  ShieldIcon,
  HelpCircleIcon
} from 'lucide-react'

// Mock user data - replace with actual auth context
const mockUser = {
  name: 'Max Mustermann',
  email: 'max.mustermann@kgv.de',
  role: 'Administrator',
  initials: 'MM'
}

export function UserMenu() {
  const [isOpen, setIsOpen] = React.useState(false)

  const handleLogout = () => {
    // Implement logout logic
    console.log('Logout clicked')
  }

  return (
    <DropdownMenu.Root open={isOpen} onOpenChange={setIsOpen}>
      <DropdownMenu.Trigger asChild>
        <Button
          variant="ghost"
          className="relative h-8 w-8 rounded-full focus-ring"
          aria-label="Benutzerkonto"
        >
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-600 text-sm font-medium text-white">
            {mockUser.initials}
          </div>
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          className={cn(
            'z-50 min-w-[240px] overflow-hidden rounded-md border border-secondary-200 bg-white p-1 shadow-lg',
            'data-[state=open]:animate-in data-[state=closed]:animate-out',
            'data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            'data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
            'data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2',
            'data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2'
          )}
          align="end"
          sideOffset={4}
        >
          {/* User info */}
          <div className="px-3 py-2 border-b border-secondary-200">
            <div className="flex items-center space-x-3">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-600 text-sm font-medium text-white">
                {mockUser.initials}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-secondary-900 truncate">
                  {mockUser.name}
                </p>
                <p className="text-xs text-secondary-500 truncate">
                  {mockUser.email}
                </p>
                <p className="text-xs text-primary-600 font-medium">
                  {mockUser.role}
                </p>
              </div>
            </div>
          </div>

          {/* Menu items */}
          <div className="py-1">
            <DropdownMenu.Item className="flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer hover:bg-secondary-50 focus:bg-secondary-50 focus:outline-none">
              <UserCircleIcon className="h-4 w-4 text-secondary-500" />
              <span>Profil anzeigen</span>
            </DropdownMenu.Item>

            <DropdownMenu.Item className="flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer hover:bg-secondary-50 focus:bg-secondary-50 focus:outline-none">
              <SettingsIcon className="h-4 w-4 text-secondary-500" />
              <span>Einstellungen</span>
            </DropdownMenu.Item>

            <DropdownMenu.Item className="flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer hover:bg-secondary-50 focus:bg-secondary-50 focus:outline-none">
              <ShieldIcon className="h-4 w-4 text-secondary-500" />
              <span>Sicherheit</span>
            </DropdownMenu.Item>

            <DropdownMenu.Separator className="h-px bg-secondary-200 mx-1 my-1" />

            <DropdownMenu.Item className="flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer hover:bg-secondary-50 focus:bg-secondary-50 focus:outline-none">
              <HelpCircleIcon className="h-4 w-4 text-secondary-500" />
              <span>Hilfe & Support</span>
            </DropdownMenu.Item>

            <DropdownMenu.Separator className="h-px bg-secondary-200 mx-1 my-1" />

            <DropdownMenu.Item 
              className="flex items-center space-x-2 rounded-sm px-3 py-2 text-sm cursor-pointer hover:bg-error-50 focus:bg-error-50 focus:outline-none text-error-700"
              onSelect={handleLogout}
            >
              <LogOutIcon className="h-4 w-4" />
              <span>Abmelden</span>
            </DropdownMenu.Item>
          </div>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  )
}