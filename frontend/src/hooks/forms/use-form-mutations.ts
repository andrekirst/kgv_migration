/**
 * Custom Form Hooks mit React Query Integration
 * 
 * Spezielle Hooks für Formulare mit optimierter Performance,
 * Fehlerbehandlung und deutscher Lokalisierung
 */

'use client'

import React from 'react'
import { 
  useMutation, 
  useQueryClient, 
  UseMutationOptions,
  useQuery,
  UseQueryOptions
} from '@tanstack/react-query'
import { useForm, UseFormProps, UseFormReturn } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import toast from 'react-hot-toast'
import { apiClient } from '@/lib/api-client'
import { queryKeys } from '@/lib/query-keys'
import { 
  useCreateBezirk, 
  useUpdateBezirk, 
  useDeleteBezirk 
} from '@/hooks/api/use-bezirke'
// import { useCreateParzelle, useUpdateParzelle } from '@/hooks/api/use-parzellen'
// import { useCreateAntrag, useUpdateAntrag } from '@/hooks/api/use-antraege'
import { 
  bezirkCreateSchema, 
  bezirkUpdateSchema,
  parzelleCreateSchema,
  parzelleUpdateSchema,
  antragCreateSchema,
  antragUpdateSchema,
  type BezirkCreateFormData,
  type BezirkUpdateFormData,
  type ParzelleCreateFormData,
  type ParzelleUpdateFormData,
  type AntragCreateFormData,
  type AntragUpdateFormData
} from '@/lib/validation/schemas'
import type { Bezirk, Parzelle } from '@/types/bezirke'
import type { AntragDto } from '@/types/api'
import { 
  formatValidationErrorsForToast,
  cleanEmptyStrings,
  cleanNullValues
} from '@/lib/validation/form-utils'

// =============================================================================
// BASE FORM HOOK
// =============================================================================

interface UseKGVFormMutationOptions<TFormData, TResult> {
  mode: 'create' | 'edit'
  schema: z.ZodType<TFormData>
  defaultValues?: Partial<TFormData>
  onSuccess?: (result: TResult, formData: TFormData) => void
  onError?: (error: any, formData: TFormData) => void
  transformData?: (data: TFormData) => any
  optimisticUpdate?: boolean
  invalidateQueries?: string[][]
  showSuccessToast?: boolean
  showErrorToast?: boolean
}

function useKGVFormMutation<TFormData, TResult>({
  mode,
  schema,
  defaultValues,
  onSuccess,
  onError,
  transformData,
  optimisticUpdate = true,
  invalidateQueries = [],
  showSuccessToast = true,
  showErrorToast = true
}: UseKGVFormMutationOptions<TFormData, TResult>) {
  const queryClient = useQueryClient()

  // Form Setup
  const form = useForm<TFormData>({
    resolver: zodResolver(schema),
    defaultValues: defaultValues as any,
    mode: 'onBlur',
    reValidateMode: 'onChange',
    criteriaMode: 'all'
  })

  // Mutation Setup
  const mutation = useMutation({
    mutationFn: async (data: TFormData) => {
      // Daten bereinigen
      let cleanedData = cleanEmptyStrings(data as any)
      cleanedData = cleanNullValues(cleanedData)
      
      // Custom Transformation
      if (transformData) {
        cleanedData = transformData(cleanedData)
      }

      // Hier würde die eigentliche API-Integration stehen
      // Für Demo-Zwecke simulieren wir die API-Calls
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      return cleanedData as TResult
    },
    onSuccess: (result, formData) => {
      // Cache invalidieren
      invalidateQueries.forEach(queryKey => {
        queryClient.invalidateQueries({ queryKey })
      })

      // Success Toast
      if (showSuccessToast) {
        const actionText = mode === 'create' ? 'erstellt' : 'aktualisiert'
        toast.success(`Daten erfolgreich ${actionText}`, {
          duration: 3000,
          position: 'top-right'
        })
      }

      // Form zurücksetzen bei Erstellung
      if (mode === 'create') {
        form.reset()
      }

      // Custom Success Handler
      if (onSuccess) {
        onSuccess(result, formData)
      }
    },
    onError: (error: any, formData) => {
      console.error('Form mutation error:', error)

      // Server-Validierungsfehler behandeln
      if (error?.response?.data?.errors) {
        const serverErrors = error.response.data.errors
        Object.entries(serverErrors).forEach(([field, message]) => {
          form.setError(field as any, {
            type: 'server',
            message: message as string
          })
        })
      }

      // Error Toast
      if (showErrorToast) {
        const errorMessage = error?.message || 'Ein Fehler ist aufgetreten'
        toast.error(`Fehler beim Speichern: ${errorMessage}`, {
          duration: 5000,
          position: 'top-right'
        })
      }

      // Custom Error Handler
      if (onError) {
        onError(error, formData)
      }
    }
  })

  // Submit Handler
  const handleSubmit = React.useCallback(async (data: TFormData) => {
    try {
      await mutation.mutateAsync(data)
    } catch (error) {
      // Fehler wird bereits in onError behandelt
      throw error
    }
  }, [mutation])

  // Submit Error Handler (für Validierungsfehler)
  const handleSubmitError = React.useCallback((errors: any) => {
    console.error('Form validation errors:', errors)
    
    if (showErrorToast) {
      const message = formatValidationErrorsForToast(errors)
      toast.error(`Validierungsfehler: ${message}`, {
        duration: 5000,
        position: 'top-right'
      })
    }
  }, [showErrorToast])

  return {
    form,
    mutation,
    handleSubmit,
    handleSubmitError,
    isSubmitting: mutation.isPending,
    isSuccess: mutation.isSuccess,
    isError: mutation.isError,
    error: mutation.error,
    reset: () => {
      form.reset()
      mutation.reset()
    }
  }
}

// =============================================================================
// BEZIRK FORM HOOKS
// =============================================================================

export interface UseBezirkCreateFormOptions {
  onSuccess?: (bezirk: Bezirk) => void
  onError?: (error: any) => void
}

export function useBezirkCreateForm({
  onSuccess,
  onError
}: UseBezirkCreateFormOptions = {}) {
  const createBezirk = useCreateBezirk()

  return useKGVFormMutation<BezirkCreateFormData, Bezirk>({
    mode: 'create',
    schema: bezirkCreateSchema,
    defaultValues: {
      name: '',
      beschreibung: '',
      bezirksleiter: '',
      telefon: '',
      email: '',
      adresse: {
        strasse: '',
        hausnummer: '',
        plz: '',
        ort: ''
      }
    },
    onSuccess: (result, formData) => {
      // React Query Mutation verwenden
      createBezirk.mutate(formData, {
        onSuccess: (createdBezirk) => {
          if (onSuccess) {
            onSuccess(createdBezirk)
          }
        }
      })
    },
    onError,
    invalidateQueries: [
      queryKeys.bezirke.lists(),
      queryKeys.bezirke.dropdown(),
      queryKeys.bezirke.statistics()
    ]
  })
}

export interface UseBezirkEditFormOptions {
  bezirkId: number
  initialData: Partial<Bezirk>
  onSuccess?: (bezirk: Bezirk) => void
  onError?: (error: any) => void
}

export function useBezirkEditForm({
  bezirkId,
  initialData,
  onSuccess,
  onError
}: UseBezirkEditFormOptions) {
  const updateBezirk = useUpdateBezirk()

  return useKGVFormMutation<BezirkUpdateFormData, Bezirk>({
    mode: 'edit',
    schema: bezirkUpdateSchema,
    defaultValues: {
      name: initialData.name || '',
      beschreibung: initialData.beschreibung || '',
      bezirksleiter: initialData.bezirksleiter || '',
      telefon: initialData.telefon || '',
      email: initialData.email || '',
      adresse: {
        strasse: initialData.adresse?.strasse || '',
        hausnummer: initialData.adresse?.hausnummer || '',
        plz: initialData.adresse?.plz || '',
        ort: initialData.adresse?.ort || ''
      },
      aktiv: initialData.aktiv ?? true
    },
    onSuccess: (result, formData) => {
      // React Query Mutation verwenden
      updateBezirk.mutate({ id: bezirkId, ...formData }, {
        onSuccess: (updatedBezirk) => {
          if (onSuccess) {
            onSuccess(updatedBezirk)
          }
        }
      })
    },
    onError,
    invalidateQueries: [
      queryKeys.bezirke.lists(),
      queryKeys.bezirke.detail(bezirkId),
      queryKeys.bezirke.dropdown(),
      queryKeys.bezirke.statistics()
    ]
  })
}

// =============================================================================
// PARZELLE FORM HOOKS
// =============================================================================

export interface UseParzelleCreateFormOptions {
  preselectedBezirkId?: number
  onSuccess?: (parzelle: Parzelle) => void
  onError?: (error: any) => void
}

export function useParzelleCreateForm({
  preselectedBezirkId,
  onSuccess,
  onError
}: UseParzelleCreateFormOptions = {}) {
  return useKGVFormMutation<ParzelleCreateFormData, Parzelle>({
    mode: 'create',
    schema: parzelleCreateSchema,
    defaultValues: {
      nummer: '',
      bezirkId: preselectedBezirkId || 0,
      groesse: 0,
      beschreibung: '',
      ausstattung: [],
      monatlichePacht: 0,
      kaution: undefined,
      kuendigungsfrist: 3,
      adresse: {
        strasse: '',
        hausnummer: '',
        plz: '',
        ort: ''
      },
      bemerkungen: ''
    },
    onSuccess: (result, formData) => {
      // Hier würde die echte API-Integration stehen
      if (onSuccess) {
        onSuccess(result)
      }
    },
    onError,
    invalidateQueries: [
      ['parzellen', 'lists'],
      ['bezirke', 'lists'], // Bezirks-Statistiken könnten sich ändern
      ['bezirke', 'statistics']
    ]
  })
}

export interface UseParzelleEditFormOptions {
  parzelleId: number
  initialData: Partial<Parzelle>
  onSuccess?: (parzelle: Parzelle) => void
  onError?: (error: any) => void
}

export function useParzelleEditForm({
  parzelleId,
  initialData,
  onSuccess,
  onError
}: UseParzelleEditFormOptions) {
  return useKGVFormMutation<ParzelleUpdateFormData, Parzelle>({
    mode: 'edit',
    schema: parzelleUpdateSchema,
    defaultValues: {
      nummer: initialData.nummer || '',
      bezirkId: initialData.bezirkId || 0,
      groesse: initialData.groesse || 0,
      beschreibung: initialData.beschreibung || '',
      ausstattung: initialData.ausstattung || [],
      monatlichePacht: initialData.monatlichePacht || 0,
      kaution: initialData.kaution,
      kuendigungsfrist: initialData.kuendigungsfrist || 3,
      adresse: {
        strasse: initialData.adresse?.strasse || '',
        hausnummer: initialData.adresse?.hausnummer || '',
        plz: initialData.adresse?.plz || '',
        ort: initialData.adresse?.ort || ''
      },
      bemerkungen: initialData.bemerkungen || '',
      status: initialData.status,
      aktiv: initialData.aktiv ?? true
    },
    onSuccess: (result, formData) => {
      // Hier würde die echte API-Integration stehen
      if (onSuccess) {
        onSuccess(result)
      }
    },
    onError,
    invalidateQueries: [
      ['parzellen', 'lists'],
      ['parzellen', 'detail', parzelleId],
      ['bezirke', 'lists'],
      ['bezirke', 'statistics']
    ]
  })
}

// =============================================================================
// ANTRAG FORM HOOKS
// =============================================================================

export interface UseAntragCreateFormOptions {
  onSuccess?: (antrag: AntragDto) => void
  onError?: (error: any) => void
}

export function useAntragCreateForm({
  onSuccess,
  onError
}: UseAntragCreateFormOptions = {}) {
  return useKGVFormMutation<AntragCreateFormData, AntragDto>({
    mode: 'create',
    schema: antragCreateSchema,
    defaultValues: {
      anrede: undefined,
      titel: '',
      vorname: '',
      nachname: '',
      anrede2: undefined,
      titel2: '',
      vorname2: '',
      nachname2: '',
      briefanrede: '',
      strasse: '',
      plz: '',
      ort: '',
      telefon: '',
      mobilTelefon: '',
      geschTelefon: '',
      mobilTelefon2: '',
      eMail: '',
      bewerbungsdatum: '',
      wunsch: '',
      vermerk: '',
      geburtstag: '',
      geburtstag2: ''
    },
    onSuccess: (result, formData) => {
      // Hier würde die echte API-Integration stehen
      if (onSuccess) {
        onSuccess(result)
      }
    },
    onError,
    invalidateQueries: [
      ['antraege', 'lists'],
      ['dashboard', 'stats']
    ]
  })
}

export interface UseAntragEditFormOptions {
  antragId: string
  initialData: Partial<AntragDto>
  onSuccess?: (antrag: AntragDto) => void
  onError?: (error: any) => void
}

export function useAntragEditForm({
  antragId,
  initialData,
  onSuccess,
  onError
}: UseAntragEditFormOptions) {
  return useKGVFormMutation<AntragUpdateFormData, AntragDto>({
    mode: 'edit',
    schema: antragUpdateSchema,
    defaultValues: {
      id: antragId,
      anrede: initialData.anrede,
      titel: initialData.titel || '',
      vorname: initialData.vorname || '',
      nachname: initialData.nachname || '',
      anrede2: initialData.anrede2,
      titel2: initialData.titel2 || '',
      vorname2: initialData.vorname2 || '',
      nachname2: initialData.nachname2 || '',
      briefanrede: initialData.briefanrede || '',
      strasse: initialData.strasse || '',
      plz: initialData.plz || '',
      ort: initialData.ort || '',
      telefon: initialData.telefon || '',
      mobilTelefon: initialData.mobilTelefon || '',
      geschTelefon: initialData.geschTelefon || '',
      mobilTelefon2: initialData.mobilTelefon2 || '',
      eMail: initialData.eMail || '',
      bewerbungsdatum: initialData.bewerbungsdatum || '',
      wunsch: initialData.wunsch || '',
      vermerk: initialData.vermerk || '',
      geburtstag: initialData.geburtstag || '',
      geburtstag2: initialData.geburtstag2 || '',
      status: initialData.status,
      aktuellesAngebot: initialData.aktuellesAngebot || '',
      loeschdatum: initialData.loeschdatum || '',
      bestaetigungsdatum: initialData.bestaetigungsdatum || ''
    },
    onSuccess: (result, formData) => {
      // Hier würde die echte API-Integration stehen
      if (onSuccess) {
        onSuccess(result)
      }
    },
    onError,
    invalidateQueries: [
      ['antraege', 'lists'],
      ['antraege', 'detail', antragId],
      ['dashboard', 'stats']
    ]
  })
}

// =============================================================================
// ASYNC VALIDATION HOOKS
// =============================================================================

export interface UseAsyncValidationOptions<T> {
  queryKey: string[]
  validationFn: (value: T) => Promise<boolean>
  enabled?: boolean
  debounceMs?: number
}

export function useAsyncValidation<T>({
  queryKey,
  validationFn,
  enabled = true,
  debounceMs = 500
}: UseAsyncValidationOptions<T>) {
  const [value, setValue] = React.useState<T | undefined>()
  const [debouncedValue, setDebouncedValue] = React.useState<T | undefined>()

  // Debounce der Eingabe
  React.useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value)
    }, debounceMs)

    return () => clearTimeout(timer)
  }, [value, debounceMs])

  // Async Validation Query
  const validationQuery = useQuery({
    queryKey: [...queryKey, debouncedValue],
    queryFn: () => validationFn(debouncedValue!),
    enabled: enabled && debouncedValue !== undefined,
    staleTime: 30000, // 30 Sekunden
    retry: false
  })

  return {
    setValue,
    isValidating: validationQuery.isFetching,
    isValid: validationQuery.data ?? true,
    error: validationQuery.error,
    validate: setValue
  }
}

/**
 * Hook für Eindeutigkeitsprüfung von Bezirksnamen
 */
export function useBezirkNameValidation(currentId?: number) {
  return useAsyncValidation({
    queryKey: ['bezirke', 'validate', 'name'],
    validationFn: async (name: string) => {
      if (!name) return true
      
      try {
        const response = await apiClient.get(
          `/bezirke/check-name?name=${encodeURIComponent(name)}&excludeId=${currentId || ''}`
        )
        return response.data?.isUnique !== false
      } catch (error) {
        console.warn('Name validation failed:', error)
        return true // Bei Fehlern erlauben wir die Eingabe
      }
    }
  })
}

/**
 * Hook für Eindeutigkeitsprüfung von Parzellennummern
 */
export function useParzellenNummerValidation(bezirkId?: number, currentId?: number) {
  return useAsyncValidation({
    queryKey: ['parzellen', 'validate', 'nummer', bezirkId],
    validationFn: async (nummer: string) => {
      if (!nummer || !bezirkId) return true
      
      try {
        const response = await apiClient.get(
          `/parzellen/check-nummer?nummer=${encodeURIComponent(nummer)}&bezirkId=${bezirkId}&excludeId=${currentId || ''}`
        )
        return response.data?.isUnique !== false
      } catch (error) {
        console.warn('Nummer validation failed:', error)
        return true
      }
    },
    enabled: !!bezirkId
  })
}

/**
 * Hook für E-Mail-Eindeutigkeitsprüfung
 */
export function useEmailValidation(currentId?: string) {
  return useAsyncValidation({
    queryKey: ['antraege', 'validate', 'email'],
    validationFn: async (email: string) => {
      if (!email) return true
      
      try {
        const response = await apiClient.get(
          `/antraege/check-email?email=${encodeURIComponent(email)}&excludeId=${currentId || ''}`
        )
        return response.data?.isUnique !== false
      } catch (error) {
        console.warn('Email validation failed:', error)
        return true
      }
    }
  })
}

// =============================================================================
// FORM UTILITIES
// =============================================================================

/**
 * Hook für optimistische Updates
 */
export function useOptimisticFormUpdate<T>(queryKey: string[]) {
  const queryClient = useQueryClient()

  const updateOptimistically = React.useCallback(
    (updater: (old: T | undefined) => T) => {
      queryClient.setQueryData<T>(queryKey, updater)
    },
    [queryClient, queryKey]
  )

  const revertOptimisticUpdate = React.useCallback(() => {
    queryClient.invalidateQueries({ queryKey })
  }, [queryClient, queryKey])

  return { updateOptimistically, revertOptimisticUpdate }
}

/**
 * Hook für Form Reset mit Confirmation
 */
export function useFormResetConfirmation() {
  const showResetConfirmation = React.useCallback(
    (isDirty: boolean, onConfirm: () => void) => {
      if (!isDirty) {
        onConfirm()
        return
      }

      if (window.confirm(
        'Alle ungespeicherten Änderungen gehen verloren. Möchten Sie das Formular wirklich zurücksetzen?'
      )) {
        onConfirm()
      }
    },
    []
  )

  return { showResetConfirmation }
}