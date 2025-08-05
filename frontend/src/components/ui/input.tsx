// Input component with German form standards
import * as React from 'react'
import { cn } from '@/lib/utils'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean
  helperText?: string
  label?: string
  required?: boolean
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, error, helperText, label, required, id, ...props }, ref) => {
    const inputId = id || React.useId()
    const helperTextId = helperText ? `${inputId}-helper` : undefined
    
    return (
      <div className="space-y-2">
        {label && (
          <label 
            htmlFor={inputId}
            className={cn(
              'block text-sm font-medium text-secondary-700',
              required && "after:content-['*'] after:ml-0.5 after:text-error-500"
            )}
          >
            {label}
          </label>
        )}
        <input
          type={type}
          id={inputId}
          className={cn(
            'flex h-10 w-full rounded-md border border-secondary-300 bg-white px-3 py-2 text-sm',
            'placeholder:text-secondary-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
            'disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-secondary-50',
            'transition-colors duration-200',
            error && 'border-error-500 focus:ring-error-500',
            className
          )}
          ref={ref}
          aria-invalid={error}
          aria-describedby={helperTextId}
          aria-required={required}
          {...props}
        />
        {helperText && (
          <p 
            id={helperTextId}
            className={cn(
              'text-xs',
              error ? 'text-error-600' : 'text-secondary-500'
            )}
          >
            {helperText}
          </p>
        )}
      </div>
    )
  }
)
Input.displayName = 'Input'

export { Input }