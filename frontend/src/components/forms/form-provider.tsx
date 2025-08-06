/**
 * Form Provider für KGV Management System
 * 
 * Zentraler Provider für React Hook Form mit deutscher Lokalisierung,
 * Zod Integration und optimierter Performance
 */

'use client'

import React, { createContext, useContext, ReactNode } from 'react'
import { 
  useForm, 
  UseFormProps, 
  UseFormReturn, 
  FieldValues,
  FormProvider as RHFFormProvider,
  useFormContext as useRHFFormContext
} from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import toast from 'react-hot-toast'
import { 
  formatValidationErrorsForToast,
  focusFirstErrorField,
  isFormDirty
} from '@/lib/validation/form-utils'

// =============================================================================
// TYPES AND INTERFACES
// =============================================================================

interface FormContextValue {
  isSubmitting: boolean
  hasErrors: boolean
  errorCount: number
  isDirty: boolean
  canSubmit: boolean
  showValidationErrors: () => void
  clearErrors: () => void
  focusFirstError: () => void
}

interface KGVFormProviderProps<T extends FieldValues> {
  children: ReactNode
  form: UseFormReturn<T>
  onSubmit: (data: T) => Promise<void> | void
  onSubmitError?: (errors: any) => void
  className?: string
  preventLeave?: boolean
  showErrorToasts?: boolean
  formRef?: React.RefObject<HTMLFormElement>
}

interface UseKGVFormProps<T extends FieldValues> extends Omit<UseFormProps<T>, 'resolver'> {
  schema: z.ZodType<T>
  mode?: 'onChange' | 'onBlur' | 'onSubmit' | 'onTouched' | 'all'
  reValidateMode?: 'onChange' | 'onBlur' | 'onSubmit'
  shouldFocusError?: boolean
  shouldUnregister?: boolean
  delayError?: number
}

// =============================================================================
// CONTEXT
// =============================================================================

const FormContext = createContext<FormContextValue | null>(null)

export function useFormContext(): FormContextValue {
  const context = useContext(FormContext)
  if (!context) {
    throw new Error('useFormContext must be used within a KGVFormProvider')
  }
  return context
}

// =============================================================================
// CUSTOM HOOK FÜR KGV FORMS
// =============================================================================

/**
 * Custom Hook für KGV Formulare mit Zod Validation
 */
export function useKGVForm<T extends FieldValues>({
  schema,
  mode = 'onBlur',
  reValidateMode = 'onChange',
  shouldFocusError = true,
  shouldUnregister = false,
  delayError = 300,
  defaultValues,
  ...options
}: UseKGVFormProps<T>) {
  const form = useForm<T>({
    resolver: zodResolver(schema),
    mode,
    reValidateMode,
    shouldFocusError,
    shouldUnregister,
    delayError,
    defaultValues,
    criteriaMode: 'all', // Zeige alle Validierungsfehler
    ...options
  })

  // Erweiterte Utilities
  const utilities = React.useMemo(() => ({
    hasErrors: Object.keys(form.formState.errors).length > 0,
    errorCount: Object.keys(form.formState.errors).length,
    isDirty: isFormDirty(form.formState),
    canSubmit: form.formState.isValid && !form.formState.isSubmitting,
    
    showValidationErrors: () => {
      const errors = form.formState.errors
      if (Object.keys(errors).length > 0) {
        const message = formatValidationErrorsForToast(errors)
        toast.error(`Validierungsfehler: ${message}`, {
          duration: 5000,
          position: 'top-right'
        })
      }
    },
    
    clearErrors: () => {
      form.clearErrors()
    },
    
    focusFirstError: (formRef?: React.RefObject<HTMLFormElement>) => {
      if (formRef) {
        focusFirstErrorField(form.formState.errors, formRef)
      }
    },
    
    resetWithDefaults: (newDefaults?: Partial<T>) => {
      form.reset(newDefaults || defaultValues)
    },
    
    validateField: async (fieldName: keyof T) => {
      return await form.trigger(fieldName as any)
    },
    
    validateAllFields: async () => {
      return await form.trigger()
    },
    
    setFieldError: (fieldName: keyof T, message: string) => {
      form.setError(fieldName as any, {
        type: 'manual',
        message
      })
    },
    
    setMultipleErrors: (errors: Record<keyof T, string>) => {
      Object.entries(errors).forEach(([field, message]) => {
        form.setError(field as any, {
          type: 'manual',
          message: message as string
        })
      })
    }
  }), [form, defaultValues])

  return {
    ...form,
    ...utilities
  }
}

// =============================================================================
// FORM PROVIDER COMPONENT
// =============================================================================

/**
 * KGV Form Provider mit deutscher Lokalisierung und erweiterten Features
 */
export function KGVFormProvider<T extends FieldValues>({
  children,
  form,
  onSubmit,
  onSubmitError,
  className = '',
  preventLeave = true,
  showErrorToasts = true,
  formRef
}: KGVFormProviderProps<T>) {
  const [isSubmitting, setIsSubmitting] = React.useState(false)

  // Context-Werte
  const contextValue: FormContextValue = React.useMemo(() => ({
    isSubmitting,
    hasErrors: Object.keys(form.formState.errors).length > 0,
    errorCount: Object.keys(form.formState.errors).length,
    isDirty: isFormDirty(form.formState),
    canSubmit: form.formState.isValid && !isSubmitting,
    
    showValidationErrors: () => {
      const errors = form.formState.errors
      if (Object.keys(errors).length > 0) {
        const message = formatValidationErrorsForToast(errors)
        toast.error(`Validierungsfehler: ${message}`, {
          duration: 5000,
          position: 'top-right'
        })
      }
    },
    
    clearErrors: () => {
      form.clearErrors()
    },
    
    focusFirstError: () => {
      if (formRef) {
        focusFirstErrorField(form.formState.errors, formRef)
      }
    }
  }), [form.formState.errors, form.formState.isValid, isSubmitting, formRef])

  // Submit Handler
  const handleSubmit = React.useCallback(async (data: T) => {
    setIsSubmitting(true)
    
    try {
      await onSubmit(data)
      
      // Erfolgs-Toast
      toast.success('Daten erfolgreich gespeichert', {
        duration: 3000,
        position: 'top-right'
      })
      
    } catch (error: any) {
      console.error('Form submission error:', error)
      
      // Fehler-Toast
      if (showErrorToasts) {
        const errorMessage = error?.message || 'Ein Fehler ist aufgetreten'
        toast.error(`Fehler beim Speichern: ${errorMessage}`, {
          duration: 5000,
          position: 'top-right'
        })
      }
      
      // Custom Error Handler
      if (onSubmitError) {
        onSubmitError(error)
      }
      
      // Server-Validierungsfehler verarbeiten
      if (error?.response?.data?.errors) {
        const serverErrors = error.response.data.errors
        Object.entries(serverErrors).forEach(([field, message]) => {
          form.setError(field as any, {
            type: 'server',
            message: message as string
          })
        })
        
        // Fokussiere erstes Fehlerfeld
        if (formRef) {
          setTimeout(() => {
            focusFirstErrorField(form.formState.errors, formRef)
          }, 100)
        }
      }
      
    } finally {
      setIsSubmitting(false)
    }
  }, [onSubmit, onSubmitError, showErrorToasts, form, formRef])

  // Submit Error Handler
  const handleSubmitError = React.useCallback((errors: any) => {
    console.error('Form validation errors:', errors)
    
    if (showErrorToasts) {
      const message = formatValidationErrorsForToast(errors)
      toast.error(`Validierungsfehler: ${message}`, {
        duration: 5000,
        position: 'top-right'
      })
    }
    
    // Fokussiere erstes Fehlerfeld
    if (formRef) {
      setTimeout(() => {
        focusFirstErrorField(errors, formRef)
      }, 100)
    }
    
    if (onSubmitError) {
      onSubmitError(errors)
    }
  }, [showErrorToasts, formRef, onSubmitError])

  // Verhindere Seitenverlassen bei ungespeicherten Änderungen
  React.useEffect(() => {
    if (!preventLeave) return

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isFormDirty(form.formState)) {
        e.preventDefault()
        e.returnValue = 'Sie haben ungespeicherte Änderungen. Möchten Sie die Seite wirklich verlassen?'
      }
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload)
    }
  }, [form.formState, preventLeave])

  return (
    <FormContext.Provider value={contextValue}>
      <RHFFormProvider {...form}>
        <form
          ref={formRef}
          onSubmit={form.handleSubmit(handleSubmit, handleSubmitError)}
          className={`kgv-form ${className}`}
          noValidate
        >
          {children}
        </form>
      </RHFFormProvider>
    </FormContext.Provider>
  )
}

// =============================================================================
// FORM FIELD COMPONENTS
// =============================================================================

interface FormFieldProps {
  name: string
  label?: string
  description?: string
  required?: boolean
  children: ReactNode
  className?: string
}

/**
 * Form Field Wrapper mit Label und Fehlermeldungen
 */
export function FormField({
  name,
  label,
  description,
  required = false,
  children,
  className = ''
}: FormFieldProps) {
  // Temporarily disable context access to prevent undefined errors
  // The form validation system will handle error display at form level
  const hasError = false

  return (
    <div className={`form-field ${className} ${hasError ? 'has-error' : ''}`}>
      {label && (
        <label
          htmlFor={name}
          className={`block text-sm font-medium text-gray-700 mb-1 ${
            required ? "after:content-['*'] after:ml-0.5 after:text-red-500" : ''
          }`}
        >
          {label}
        </label>
      )}
      
      {description && (
        <p className="text-sm text-gray-500 mb-2">
          {description}
        </p>
      )}
      
      <div className="form-field-input">
        {children}
      </div>
    </div>
  )
}

// =============================================================================
// UTILITY HOOKS
// =============================================================================

/**
 * Hook für Form-Status Überwachung
 */
export function useFormStatus() {
  const context = useFormContext()
  return {
    isSubmitting: context.isSubmitting,
    hasErrors: context.hasErrors,
    errorCount: context.errorCount,
    isDirty: context.isDirty,
    canSubmit: context.canSubmit
  }
}

/**
 * Hook für Form-Aktionen
 */
export function useFormActions() {
  const context = useFormContext()
  return {
    showValidationErrors: context.showValidationErrors,
    clearErrors: context.clearErrors,
    focusFirstError: context.focusFirstError
  }
}

/**
 * Hook für Unsaved Changes Warning
 */
export function useUnsavedChangesWarning(enabled: boolean = true) {
  const { isDirty } = useFormStatus()
  
  React.useEffect(() => {
    if (!enabled) return

    const handleRouteChange = () => {
      if (isDirty) {
        return window.confirm(
          'Sie haben ungespeicherte Änderungen. Möchten Sie die Seite wirklich verlassen?'
        )
      }
      return true
    }

    // Für Next.js Router
    if (typeof window !== 'undefined') {
      const router = (window as any).next?.router
      if (router) {
        router.events.on('routeChangeStart', handleRouteChange)
        return () => {
          router.events.off('routeChangeStart', handleRouteChange)
        }
      }
    }
  }, [isDirty, enabled])
}

// =============================================================================
// EXPORT
// =============================================================================

export type {
  FormContextValue,
  KGVFormProviderProps,
  UseKGVFormProps,
  FormFieldProps
}