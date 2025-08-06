/**
 * Bezirk Form Component für KGV Management System
 * 
 * Vollständiges Formular für Bezirk-Erstellung und -Bearbeitung
 * mit Zod Validation, deutscher Lokalisierung und React Query Integration
 */

'use client'

import React from 'react'
import { Controller } from 'react-hook-form'
import { Loader2, Save, X, MapPin, Phone, Mail, User } from 'lucide-react'
import { 
  useKGVForm, 
  KGVFormProvider, 
  FormField,
  useFormStatus,
  useFormActions 
} from './form-provider'
import { 
  bezirkCreateSchema, 
  bezirkUpdateSchema,
  type BezirkCreateFormData,
  type BezirkUpdateFormData 
} from '@/lib/validation/schemas'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { useCreateBezirk, useUpdateBezirk } from '@/hooks/api/use-bezirke'
import type { Bezirk } from '@/types/bezirke'

// =============================================================================
// TYPES
// =============================================================================

interface BezirkFormProps {
  initialData?: Partial<Bezirk>
  mode: 'create' | 'edit'
  onSuccess?: (bezirk: Bezirk) => void
  onCancel?: () => void
  className?: string
}

interface BezirkFormFieldsProps {
  mode: 'create' | 'edit'
}

// =============================================================================
// FORM FIELDS COMPONENT
// =============================================================================

function BezirkFormFields({ mode }: BezirkFormFieldsProps) {
  const { isSubmitting } = useFormStatus()

  return (
    <div className="space-y-6">
      {/* Grundinformationen */}
      <Card className="p-6">
        <div className="flex items-center gap-2 mb-4">
          <MapPin className="h-5 w-5 text-primary-600" />
          <h3 className="text-lg font-semibold text-gray-900">
            Bezirk Information
          </h3>
        </div>
        
        <div className="grid grid-cols-1 gap-4">
          <FormField
            name="name"
            label="Bezirksname"
            required
            description="Maximal 10 Zeichen"
          >
            <Controller
              name="name"
              render={({ field, fieldState }) => (
                <Input
                  {...field}
                  id="name"
                  placeholder="z.B. Nord, Süd, A1"
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message}
                  disabled={isSubmitting}
                  autoComplete="off"
                  maxLength={10}
                />
              )}
            />
          </FormField>

          <FormField
            name="beschreibung"
            label="Beschreibung"
            description="Kurze Beschreibung des Bezirks (optional)"
          >
            <Controller
              name="beschreibung"
              render={({ field, fieldState }) => (
                <div>
                  <textarea
                    {...field}
                    id="beschreibung"
                    rows={3}
                    placeholder="Beschreibung des Bezirks, besondere Merkmale, etc."
                    className={`flex w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:cursor-not-allowed disabled:opacity-50 transition-colors duration-200 ${
                      fieldState.error ? 'border-red-500 focus:ring-red-500' : ''
                    }`}
                    disabled={isSubmitting}
                    value={field.value || ''}
                  />
                  {fieldState.error && (
                    <p className="mt-1 text-sm text-red-600">
                      {fieldState.error.message}
                    </p>
                  )}
                </div>
              )}
            />
          </FormField>
        </div>
      </Card>
    </div>
  )
}

// =============================================================================
// FORM ACTIONS COMPONENT
// =============================================================================

interface BezirkFormActionsProps {
  mode: 'create' | 'edit'
  onCancel?: () => void
}

function BezirkFormActions({ mode, onCancel }: BezirkFormActionsProps) {
  const { isSubmitting, canSubmit, hasErrors } = useFormStatus()
  const { showValidationErrors } = useFormActions()

  const handleShowErrors = () => {
    if (hasErrors) {
      showValidationErrors()
    }
  }

  return (
    <div className="flex items-center justify-between pt-6 border-t border-gray-200">
      <div className="flex items-center space-x-2">
        {hasErrors && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleShowErrors}
            className="text-red-600 border-red-300 hover:bg-red-50"
          >
            Fehler anzeigen
          </Button>
        )}
      </div>

      <div className="flex items-center space-x-3">
        {onCancel && (
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            <X className="h-4 w-4 mr-2" />
            Abbrechen
          </Button>
        )}
        
        <Button
          type="submit"
          disabled={!canSubmit || isSubmitting}
          className="min-w-[120px]"
        >
          {isSubmitting ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Speichert...
            </>
          ) : (
            <>
              <Save className="h-4 w-4 mr-2" />
              {mode === 'create' ? 'Erstellen' : 'Speichern'}
            </>
          )}
        </Button>
      </div>
    </div>
  )
}

// =============================================================================
// MAIN FORM COMPONENT
// =============================================================================

/**
 * Bezirk Form Hauptkomponente
 */
export function BezirkForm({
  initialData,
  mode,
  onSuccess,
  onCancel,
  className = ''
}: BezirkFormProps) {
  const formRef = React.useRef<HTMLFormElement>(null)
  
  // Mutations
  const createBezirk = useCreateBezirk()
  const updateBezirk = useUpdateBezirk()

  // Schema und Defaultwerte basierend auf Modus
  const schema = mode === 'create' ? bezirkCreateSchema : bezirkUpdateSchema
  const defaultValues = React.useMemo(() => {
    if (mode === 'create') {
      return {
        name: '',
        beschreibung: ''
      } as BezirkCreateFormData
    } else {
      return {
        name: initialData?.name || '',
        beschreibung: initialData?.beschreibung || '',
        bezirksleiter: initialData?.bezirksleiter || '',
        telefon: initialData?.telefon || '',
        email: initialData?.email || '',
        adresse: {
          strasse: initialData?.adresse?.strasse || '',
          hausnummer: initialData?.adresse?.hausnummer || '',
          plz: initialData?.adresse?.plz || '',
          ort: initialData?.adresse?.ort || ''
        },
        aktiv: initialData?.aktiv ?? true
      } as BezirkUpdateFormData
    }
  }, [mode, initialData])

  // Form Hook
  const form = useKGVForm({
    schema,
    defaultValues,
    mode: 'onBlur',
    reValidateMode: 'onChange'
  })

  // Submit Handler
  const handleSubmit = React.useCallback(async (data: BezirkCreateFormData | BezirkUpdateFormData) => {
    try {
      let result: Bezirk

      if (mode === 'create') {
        result = await createBezirk.mutateAsync(data as BezirkCreateFormData)
      } else {
        const updateData = {
          id: initialData?.id!,
          ...data
        }
        result = await updateBezirk.mutateAsync(updateData)
      }

      if (onSuccess) {
        onSuccess(result)
      }

      // Form zurücksetzen bei Erstellung
      if (mode === 'create') {
        form.reset()
      }

    } catch (error: any) {
      console.error('Form submission error:', error)
      throw error // Wird vom FormProvider behandelt
    }
  }, [mode, createBezirk, updateBezirk, initialData?.id, onSuccess, form])

  return (
    <div className={`bezirk-form ${className}`}>
      <KGVFormProvider
        form={form}
        onSubmit={handleSubmit}
        formRef={formRef}
        preventLeave={true}
        showErrorToasts={true}
      >
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold text-gray-900">
                {mode === 'create' ? 'Neuen Bezirk erstellen' : 'Bezirk bearbeiten'}
              </h2>
              <p className="mt-1 text-sm text-gray-600">
                {mode === 'create' 
                  ? 'Erfassen Sie die Grunddaten für einen neuen Bezirk'
                  : 'Bearbeiten Sie die Bezirksdaten'
                }
              </p>
            </div>
          </div>

          <BezirkFormFields mode={mode} />
          <BezirkFormActions mode={mode} onCancel={onCancel} />
        </div>
      </KGVFormProvider>
    </div>
  )
}

// =============================================================================
// EXPORT
// =============================================================================

export type { BezirkFormProps }
export default BezirkForm