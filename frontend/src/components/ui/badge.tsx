// Badge component for status indicators
import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const badgeVariants = cva(
  'inline-flex items-center rounded-md border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
  {
    variants: {
      variant: {
        default: 'border-transparent bg-primary-100 text-primary-800 hover:bg-primary-200',
        secondary: 'border-transparent bg-secondary-100 text-secondary-800 hover:bg-secondary-200',
        destructive: 'border-transparent bg-error-100 text-error-800 hover:bg-error-200',
        success: 'border-transparent bg-success-100 text-success-800 hover:bg-success-200',
        warning: 'border-transparent bg-warning-100 text-warning-800 hover:bg-warning-200',
        outline: 'border-secondary-300 text-secondary-700 hover:bg-secondary-50',
        // German KGV status colors
        neu: 'border-transparent bg-blue-100 text-blue-800',
        bearbeitung: 'border-transparent bg-yellow-100 text-yellow-800',
        wartend: 'border-transparent bg-orange-100 text-orange-800',
        genehmigt: 'border-transparent bg-green-100 text-green-800',
        abgelehnt: 'border-transparent bg-red-100 text-red-800',
        archiviert: 'border-transparent bg-gray-100 text-gray-800',
      },
      size: {
        default: 'text-xs px-2.5 py-0.5',
        sm: 'text-xs px-2 py-0.5',
        lg: 'text-sm px-3 py-1',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, size, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant, size }), className)} {...props} />
  )
}

export { Badge, badgeVariants }